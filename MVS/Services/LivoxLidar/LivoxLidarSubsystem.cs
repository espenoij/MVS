using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace MVS
{
    /// <summary>
    /// Manages the Livox SDK2 lifecycle and point-cloud accumulation for helideck calibration.
    /// Call Connect() to start the SDK, StartScan()/StopScan() to accumulate a point cloud,
    /// then FitPlane() to run the Jacobi plane fit and return a LivoxLidarPlaneFitResult.
    /// </summary>
    public class LivoxLidarSubsystem
    {
        // ── State ────────────────────────────────────────────────────────────

        public enum Status { Disconnected, Connecting, Connected, Scanning, Error }

        private Status _status = Status.Disconnected;
        public Status CurrentStatus
        {
            get { return _status; }
            private set { _status = value; StatusChanged?.Invoke(value); }
        }

        private int _accumulatedPoints;
        public int AccumulatedPoints
        {
            get { return _accumulatedPoints; }
            private set { _accumulatedPoints = value; PointCountUpdated?.Invoke(value); }
        }

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<Status>   StatusChanged;
        public event Action<int>      PointCountUpdated;
        public event Action<string>   ErrorOccurred;

        // ── Point cloud buffer ───────────────────────────────────────────────

        private readonly List<(float x, float y, float z)> _buffer
            = new List<(float, float, float)>(500_000);
        private readonly object _bufferLock = new object();
        private bool _scanning = false;

        // Handle → IP mapping (populated by info-change callback)
        private readonly Dictionary<uint, string> _handleToIp = new Dictionary<uint, string>();
        private readonly object _handleLock = new object();

        // ── SDK state ────────────────────────────────────────────────────────

        private bool _sdkInitialised = false;
        private string _configFilePath;

        // Delegate references kept alive to prevent GC collection
        private LivoxLidarApi.LivoxLidarPointCloudCallback _pointCloudDelegate;
        private LivoxLidarApi.LivoxLidarInfoChangeCallback _infoChangeDelegate;
        private GCHandle _pointCloudHandle;
        private GCHandle _infoChangeHandle;

        // ── Filter settings ──────────────────────────────────────────────────

        public float RangeMinMm  { get; set; } = 500f;
        public float RangeMaxMm  { get; set; } = 30_000f;
        public float AzimuthMinDeg { get; set; } = -180f;
        public float AzimuthMaxDeg { get; set; } =  180f;
        public float ElevationMinDeg { get; set; } = -90f;
        public float ElevationMaxDeg { get; set; } =  90f;

        // ── Simulation settings ──────────────────────────────────────────────

        public double SimPitchDeg    { get; set; } = 2.0;
        public double SimRollDeg     { get; set; } = 1.5;
        public double SimNoiseMm     { get; set; } = 10.0;
        public double SimLidarYawDeg { get; set; } = 0.0;
        public int    SimPointCount  { get; set; } = 50_000;

        private Thread        _simThread;
        private volatile bool _simRunning;

        // ── Public operations ────────────────────────────────────────────────

        public bool Connect(string configFilePath, ErrorHandler errorHandler)
        {
            if (_sdkInitialised)
                return true;

            _configFilePath = configFilePath;

            try
            {
                _pointCloudDelegate = OnPointCloud;
                _infoChangeDelegate = OnInfoChange;
                _pointCloudHandle   = GCHandle.Alloc(_pointCloudDelegate);
                _infoChangeHandle   = GCHandle.Alloc(_infoChangeDelegate);

                CurrentStatus = Status.Connecting;

                bool ok = LivoxLidarApi.LivoxLidarSdkInit(configFilePath, IntPtr.Zero, IntPtr.Zero);
                if (!ok)
                {
                    CurrentStatus = Status.Error;
                    errorHandler?.Insert(new ErrorMessage(
                        DateTime.UtcNow, ErrorMessageType.LivoxLidar,
                        ErrorMessageCategory.AdminUser,
                        $"Livox SDK init failed. Check config file: {configFilePath}"));
                    return false;
                }

                _sdkInitialised = true;
                LivoxLidarApi.SetLivoxLidarInfoChangeCallback(_infoChangeDelegate, IntPtr.Zero);
                LivoxLidarApi.SetLivoxLidarPointCloudCallBack(_pointCloudDelegate, IntPtr.Zero);

                CurrentStatus = Status.Connected;
                return true;
            }
            catch (DllNotFoundException ex)
            {
                CurrentStatus = Status.Error;
                errorHandler?.Insert(new ErrorMessage(
                    DateTime.UtcNow, ErrorMessageType.LivoxLidar,
                    ErrorMessageCategory.AdminUser,
                    $"livox_lidar_sdk_shared.dll not found — copy the Livox SDK2 DLL to the application folder.\n\n{ex.Message}"));
                return false;
            }
            catch (Exception ex)
            {
                CurrentStatus = Status.Error;
                ErrorOccurred?.Invoke(ex.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            _scanning = false;

            if (_sdkInitialised)
            {
                try { LivoxLidarApi.LivoxLidarSdkUninit(); }
                catch { }
                _sdkInitialised = false;
            }

            if (_pointCloudHandle.IsAllocated) _pointCloudHandle.Free();
            if (_infoChangeHandle.IsAllocated)  _infoChangeHandle.Free();

            lock (_handleLock)  _handleToIp.Clear();
            lock (_bufferLock)  _buffer.Clear();

            _accumulatedPoints = 0;
            CurrentStatus = Status.Disconnected;
        }

        public void StartScan()
        {
            if (!_sdkInitialised) return;

            lock (_bufferLock)
            {
                _buffer.Clear();
                _accumulatedPoints = 0;
            }

            _scanning = true;
            CurrentStatus = Status.Scanning;
        }

        public void StopScan()
        {
            _simRunning = false;
            _scanning   = false;
            CurrentStatus = _sdkInitialised ? Status.Connected : Status.Disconnected;
        }

        /// <summary>
        /// Runs the plane fit on the accumulated point cloud.
        /// Returns null if fewer than 20 points are available.
        /// </summary>
        public LivoxLidarPlaneFitResult FitPlane()
        {
            List<(float, float, float)> snapshot;
            lock (_bufferLock)
                snapshot = new List<(float, float, float)>(_buffer);

            return LivoxLidarPlaneFitter.Fit(snapshot);
        }

        public void ClearBuffer()
        {
            lock (_bufferLock)
            {
                _buffer.Clear();
                _accumulatedPoints = 0;
            }
            PointCountUpdated?.Invoke(0);
        }

        public List<(float x, float y, float z)> GetPointCloudSnapshot()
        {
            lock (_bufferLock)
                return new List<(float, float, float)>(_buffer);
        }

        // ── Simulation ───────────────────────────────────────────────────────

        /// <summary>
        /// Generates a synthetic helideck point cloud without real hardware.
        /// Runs the same range/azimuth/elevation filter pipeline as live data.
        /// Fires PointCountUpdated as points accumulate; call StopScan() to abort early.
        /// When complete, status returns to Disconnected (no real SDK session).
        /// </summary>
        public void StartSimulation()
        {
            if (_scanning) return;

            lock (_bufferLock)
            {
                _buffer.Clear();
                _accumulatedPoints = 0;
            }

            _simRunning = true;
            _scanning   = true;
            CurrentStatus = Status.Scanning;

            _simThread = new Thread(SimulationWorker) { IsBackground = true, Name = "LivoxSimulator" };
            _simThread.Start();
        }

        private void SimulationWorker()
        {
            var    rng      = new Random();
            double pitchRad = SimPitchDeg    * Math.PI / 180.0;
            double rollRad  = SimRollDeg     * Math.PI / 180.0;
            double yawRad   = SimLidarYawDeg * Math.PI / 180.0;
            double cosYaw   = Math.Cos(yawRad);
            double sinYaw   = Math.Sin(yawRad);

            const int BatchSize = 500;
            int batches = Math.Max(1, SimPointCount / BatchSize);

            for (int b = 0; b < batches && _simRunning; b++)
            {
                var batch = new List<(float, float, float)>(BatchSize);
                for (int i = 0; i < BatchSize; i++)
                {
                    // Rejection-sample within a regular hexagonal helideck boundary.
                    // Circumradius 12 000 mm → flat-side hexagon ~20 785 mm wide, 24 000 mm tall.
                    // The bow edge (flat side facing sensor +X) sits at x = ±apothem ≈ ±10 392 mm,
                    // runs in Y from −6 000 mm to +6 000 mm, and has length = 12 000 mm.
                    const float HexRadius = 12_000f;
                    float x, y;
                    do
                    {
                        x = (float)((rng.NextDouble() * 2.0 - 1.0) * HexRadius);
                        y = (float)((rng.NextDouble() * 2.0 - 1.0) * HexRadius);
                    }
                    while (!IsInsideHexagon(x, y, HexRadius));

                    float z = (float)(800.0
                                     + x * Math.Tan(pitchRad)
                                     + y * Math.Tan(rollRad)
                                     + NextGaussian(rng, SimNoiseMm));

                    // Rotate the point into the LiDAR frame (yaw of sensor relative to deck)
                    float xr = (float)(x * cosYaw - y * sinYaw);
                    float yr = (float)(x * sinYaw + y * cosYaw);
                    batch.Add((xr, yr, z));
                }

                var filtered = new List<(float, float, float)>(batch.Count);
                foreach (var (x, y, z) in batch)
                {
                    float range = (float)Math.Sqrt(x * x + y * y + z * z);
                    if (range < RangeMinMm || range > RangeMaxMm) continue;

                    float azDeg = (float)(Math.Atan2(y, x) * 180.0 / Math.PI);
                    float elDeg = (float)(Math.Atan2(z, Math.Sqrt(x * x + y * y)) * 180.0 / Math.PI);

                    if (azDeg < AzimuthMinDeg || azDeg > AzimuthMaxDeg)    continue;
                    if (elDeg < ElevationMinDeg || elDeg > ElevationMaxDeg) continue;

                    filtered.Add((x, y, -z));
                }

                if (filtered.Count > 0)
                {
                    lock (_bufferLock)
                    {
                        _buffer.AddRange(filtered);
                        Interlocked.Exchange(ref _accumulatedPoints, _buffer.Count);
                    }
                    PointCountUpdated?.Invoke(_accumulatedPoints);
                }

                Thread.Sleep(50);
            }

            _scanning   = false;
            _simRunning = false;
            CurrentStatus = _sdkInitialised ? Status.Connected : Status.Disconnected;
        }

        private static double NextGaussian(Random rng, double sigma)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return sigma * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        /// <summary>
        /// Returns true when (x, y) lies inside a flat-side regular hexagon
        /// centred at the origin with circumradius <paramref name="r"/>.
        /// The flat sides face the sensor ±X direction (the bow/stern edges),
        /// running in Y at x = ±r·√3/2 (the apothem ≈ 10 392 mm for r = 12 000 mm).
        /// Conditions: |x| ≤ r·√3/2  AND  |y| + |x|/√3 ≤ r
        /// </summary>
        private static bool IsInsideHexagon(float x, float y, float r)
        {
            float ax = Math.Abs(x);
            float ay = Math.Abs(y);
            return ax <= r * 0.866025f          // |x| ≤ r·√3/2  (apothem = flat-side extent)
                && ay + ax * 0.577350f <= r;    // |y| + |x|/√3 ≤ r
        }

        // ── SDK callbacks ────────────────────────────────────────────────────

        private void OnInfoChange(uint handle, IntPtr info, IntPtr clientData)
        {
            if (info == IntPtr.Zero) return;
            try
            {
                string ip = LivoxLidarApi.ReadAsciiString(info, LivoxLidarApi.StateInfoOffsetIp, 16);
                lock (_handleLock)
                    _handleToIp[handle] = ip;
            }
            catch { }
        }

        private void OnPointCloud(uint handle, byte devType, IntPtr data, IntPtr clientData)
        {
            if (!_scanning || data == IntPtr.Zero) return;

            try
            {
                ushort dotNum   = LivoxLidarApi.ReadUInt16(data, LivoxLidarApi.PacketOffsetDotNum);
                byte   dataType = Marshal.ReadByte(data, LivoxLidarApi.PacketOffsetDataType);
                IntPtr dataPtr  = data + LivoxLidarApi.PacketHeaderSize;

                int pointSize;
                Func<IntPtr, int, (float x, float y, float z)> readPoint;

                switch (dataType)
                {
                    case LivoxLidarApi.CartesianCoordinate32:
                        pointSize  = LivoxLidarApi.SizeCartesianHighPoint;
                        readPoint  = ReadCartesianHigh;
                        break;
                    case LivoxLidarApi.CartesianCoordinate16:
                        pointSize  = LivoxLidarApi.SizeCartesianLowPoint;
                        readPoint  = ReadCartesianLow;
                        break;
                    case LivoxLidarApi.SphericalCoordinate:
                        pointSize  = LivoxLidarApi.SizeSphericalPoint;
                        readPoint  = ReadSpherical;
                        break;
                    default:
                        return;
                }

                var batch = new List<(float, float, float)>(dotNum);

                for (int i = 0; i < dotNum; i++)
                {
                    var (x, y, z) = readPoint(dataPtr, i * pointSize);

                    float range = (float)Math.Sqrt(x*x + y*y + z*z);
                    if (range < RangeMinMm || range > RangeMaxMm) continue;

                    float azDeg  = (float)(Math.Atan2(y, x) * 180.0 / Math.PI);
                    float elDeg  = (float)(Math.Atan2(z, Math.Sqrt(x*x + y*y)) * 180.0 / Math.PI);

                    if (azDeg < AzimuthMinDeg || azDeg > AzimuthMaxDeg)     continue;
                    if (elDeg < ElevationMinDeg || elDeg > ElevationMaxDeg)  continue;

                    // Negate Z: LiDAR is mounted upside-down, so raw +Z points toward the
                    // helideck. Flipping Z puts the helideck at negative Z and the sensor
                    // origin above it, which is the physically correct orientation.
                    batch.Add((x, y, -z));
                }

                if (batch.Count > 0)
                {
                    lock (_bufferLock)
                    {
                        _buffer.AddRange(batch);
                        Interlocked.Exchange(ref _accumulatedPoints, _buffer.Count);
                    }
                    PointCountUpdated?.Invoke(_accumulatedPoints);
                }
            }
            catch { }
        }

        // ── Point readers ────────────────────────────────────────────────────

        private static (float x, float y, float z) ReadCartesianHigh(IntPtr ptr, int offset)
        {
            float x = LivoxLidarApi.ReadInt32(ptr, offset)     * 1f;
            float y = LivoxLidarApi.ReadInt32(ptr, offset + 4) * 1f;
            float z = LivoxLidarApi.ReadInt32(ptr, offset + 8) * 1f;
            return (x, y, z);
        }

        private static (float x, float y, float z) ReadCartesianLow(IntPtr ptr, int offset)
        {
            float x = LivoxLidarApi.ReadInt16(ptr, offset)     * 10f;  // cm → mm
            float y = LivoxLidarApi.ReadInt16(ptr, offset + 2) * 10f;
            float z = LivoxLidarApi.ReadInt16(ptr, offset + 4) * 10f;
            return (x, y, z);
        }

        private static (float x, float y, float z) ReadSpherical(IntPtr ptr, int offset)
        {
            float depth = LivoxLidarApi.ReadUInt32(ptr, offset);
            float theta = LivoxLidarApi.ReadUInt16(ptr, offset + 4) * 0.01f * (float)(Math.PI / 180.0);
            float phi   = LivoxLidarApi.ReadUInt16(ptr, offset + 6) * 0.01f * (float)(Math.PI / 180.0);
            float x = depth * (float)(Math.Sin(theta) * Math.Cos(phi));
            float y = depth * (float)(Math.Sin(theta) * Math.Sin(phi));
            float z = depth * (float)Math.Cos(theta);
            return (x, y, z);
        }
    }
}

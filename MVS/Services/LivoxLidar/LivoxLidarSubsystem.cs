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
            _scanning = false;
            if (_sdkInitialised)
                CurrentStatus = Status.Connected;
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

                    batch.Add((x, y, z));
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

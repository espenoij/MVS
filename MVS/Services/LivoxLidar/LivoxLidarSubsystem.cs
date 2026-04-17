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
        public int   MaxBufferPoints  { get; set; } = 500_000;

        // ── Simulation settings ──────────────────────────────────────────────

        public double SimPitchDeg    { get; set; } = 2.0;
        public double SimRollDeg     { get; set; } = 1.5;
        public double SimNoiseMm     { get; set; } = 10.0;
        public double SimLidarYawDeg { get; set; } = 0.0;
        public int    SimPointCount  { get; set; } = 50_000;

        private Thread        _simThread;
        private volatile bool _simRunning;

        /// <summary>
        /// Diagnostic summary of the last simulation run (face distribution per cube).
        /// </summary>
        public string LastSimulationDiagnostics { get; private set; }

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

            var filtered = LivoxLidarPlaneFitter.FilterPlaneInliers(snapshot);
            return LivoxLidarPlaneFitter.Fit(filtered);
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

            // Cube sitting on the helideck: 2 m side, positioned near the bow edge.
            // Rotated 30° so two side faces are visible from the sensor.
            const double CubeHalf    = 1000.0; // 2 m side → 1000 mm half
            const double CubeCentreX = 7800.0;
            const double CubeCentreY = 0.0;
            const double CubeYawDeg  = 30.0;
            double       cubeYawRad  = CubeYawDeg * Math.PI / 180.0;

            // Second smaller cube: 0.5 m side, placed on the stern-side of the helideck.
            // Kept closer to the centre than cube 1 so its top face remains visible
            // for pitch values up to ~8° (at x = −7800 even 3° hid the top).
            const double Cube2Half    = 250.0;  // 0.5 m side → 250 mm half
            const double Cube2CentreX = -3500.0;
            const double Cube2CentreY = 2000.0;

            // Pre-compute cos/sin for cube 1 yaw (used for rotated footprint & occlusion checks).
            double cosC1 = Math.Cos(cubeYawRad);
            double sinC1 = Math.Sin(cubeYawRad);

            // Fraction of points allocated to cube surfaces
            const double CubeFraction  = 0.08;
            const double Cube2Fraction = 0.10;

            int dbgCube1 = 0, dbgCube2F0 = 0, dbgCube2F3 = 0, dbgCube2F5 = 0, dbgDeck = 0;

            // Diagnostic: compute cube2 visibility exactly as GenerateCubePoint does
            double dbgDeckZ2 = 800.0 + Cube2CentreX * Math.Tan(pitchRad) + Cube2CentreY * Math.Tan(rollRad);
            bool   dbgVis5   = (dbgDeckZ2 - 2.0 * Cube2Half) > 0;
            string dbgVis    = $"Cube2 deckZ={dbgDeckZ2:F2}, top={dbgDeckZ2 - 2.0 * Cube2Half:F2}, vis5={dbgVis5}, " +
                               $"pitch={SimPitchDeg:F3}°({pitchRad:F6}), roll={SimRollDeg:F3}°({rollRad:F6})";

            for (int b = 0; b < batches && _simRunning; b++)
            {
                var batch = new List<(float, float, float)>(BatchSize);
                for (int i = 0; i < BatchSize; i++)
                {
                    double px, py, pz;

                    double roll = rng.NextDouble();
                    if (roll < CubeFraction)
                    {
                        // Generate a point on cube 1 (rotated)
                        if (!GenerateCubePoint(rng, CubeCentreX, CubeCentreY, CubeHalf,
                                pitchRad, rollRad, SimNoiseMm, cubeYawRad, out px, out py, out pz))
                        { i--; continue; }
                        dbgCube1++;
                    }
                    else if (roll < CubeFraction + Cube2Fraction)
                    {
                        // Generate a point on cube 2 (axis-aligned)
                        if (!GenerateCubePoint(rng, Cube2CentreX, Cube2CentreY, Cube2Half,
                                pitchRad, rollRad, SimNoiseMm, 0.0, out px, out py, out pz))
                        { i--; continue; }
                        // Classify face by coordinate proximity
                        if (Math.Abs(px - (Cube2CentreX + Cube2Half)) < 1.0) dbgCube2F0++;
                        else if (Math.Abs(py - (Cube2CentreY - Cube2Half)) < 1.0) dbgCube2F3++;
                        else dbgCube2F5++;
                    }
                    else
                    {
                    // Rejection-sample within a regular hexagonal helideck boundary.
                    // Circumradius 12 000 mm → flat-side hexagon ~20 785 mm wide, 24 000 mm tall.
                    // The bow edge (flat side facing sensor +X) sits at x = ±apothem ≈ ±10 392 mm,
                    // runs in Y from −6 000 mm to +6 000 mm, and has length = 12 000 mm.
                    const float HexRadius = 12_000f;
                    float hx, hy;
                    do
                    {
                        hx = (float)((rng.NextDouble() * 2.0 - 1.0) * HexRadius);
                        hy = (float)((rng.NextDouble() * 2.0 - 1.0) * HexRadius);
                    }
                    while (!IsInsideHexagon(hx, hy, HexRadius));

                    // Skip deck points that fall inside either cube footprint.
                    // Cube 1: rotate point into cube-local frame and check ±half.
                    double dx1 = hx - CubeCentreX;
                    double dy1 = hy - CubeCentreY;
                    double lx1 =  dx1 * cosC1 + dy1 * sinC1;
                    double ly1 = -dx1 * sinC1 + dy1 * cosC1;
                    if ((Math.Abs(lx1) <= CubeHalf && Math.Abs(ly1) <= CubeHalf) ||
                        (hx >= Cube2CentreX - Cube2Half && hx <= Cube2CentreX + Cube2Half &&
                         hy >= Cube2CentreY - Cube2Half && hy <= Cube2CentreY + Cube2Half))
                    {
                        i--;
                        continue;
                    }

                     dbgDeck++;
                    px = hx;
                    py = hy;
                    pz = 800.0
                       + px * Math.Tan(pitchRad)
                       + py * Math.Tan(rollRad)
                       + NextGaussian(rng, SimNoiseMm);

                    // Occlusion: discard deck points whose line-of-sight is blocked by either cube.
                    double deckZ1 = 800.0
                                  + CubeCentreX * Math.Tan(pitchRad)
                                  + CubeCentreY * Math.Tan(rollRad);
                    double deckZ2 = 800.0
                                  + Cube2CentreX * Math.Tan(pitchRad)
                                  + Cube2CentreY * Math.Tan(rollRad);
                    // Occlusion: transform ray (origin→point) into cube 1 local frame.
                    // Sensor origin in cube-local XY: rotate(-yaw) * (0 - cubeCentre)
                    double sox1 =  -CubeCentreX * cosC1 - CubeCentreY * sinC1;
                    double soy1 =   CubeCentreX * sinC1 - CubeCentreY * cosC1;
                    // Deck point in cube-local XY: rotate(-yaw) * (P - cubeCentre)
                    double spx1 =  (px - CubeCentreX) * cosC1 + (py - CubeCentreY) * sinC1;
                    double spy1 = -(px - CubeCentreX) * sinC1 + (py - CubeCentreY) * cosC1;
                    // Ray direction in local frame
                    double rdx1 = spx1 - sox1, rdy1 = spy1 - soy1, rdz1 = pz; // pz - 0
                    if (RayHitsOBB(sox1, soy1, 0, rdx1, rdy1, rdz1,
                            -CubeHalf, -CubeHalf, deckZ1 - 2.0 * CubeHalf,
                             CubeHalf,  CubeHalf, deckZ1) ||
                        RayHitsAABB(
                            px, py, pz,
                            Cube2CentreX - Cube2Half, Cube2CentreY - Cube2Half, deckZ2 - 2.0 * Cube2Half,
                            Cube2CentreX + Cube2Half, Cube2CentreY + Cube2Half, deckZ2))
                    {
                        i--;
                        continue;
                    }
                    }

                    // Rotate the point into the LiDAR frame (yaw of sensor relative to deck)
                    float xr = (float)(px * cosYaw - py * sinYaw);
                    float yr = (float)(px * sinYaw + py * cosYaw);
                    batch.Add((xr, yr, (float)pz));
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
                        int max = MaxBufferPoints;
                        if (max > 0 && _buffer.Count > max)
                            _buffer.RemoveRange(0, _buffer.Count - max);
                        Interlocked.Exchange(ref _accumulatedPoints, _buffer.Count);
                    }
                    PointCountUpdated?.Invoke(_accumulatedPoints);
                }

                Thread.Sleep(50);
            }

            LastSimulationDiagnostics =
                $"Cube1: {dbgCube1}  |  Cube2 +X: {dbgCube2F0}, -Y: {dbgCube2F3}, Top: {dbgCube2F5}  |  Deck: {dbgDeck}\n{dbgVis}";

            _scanning   = false;
            _simRunning = false;
            CurrentStatus = _sdkInitialised ? Status.Connected : Status.Disconnected;
        }

        /// <summary>
        /// Generates a random point on a visible face of a cube sitting on the tilted deck.
        /// The cube may be yaw-rotated around its vertical axis by <paramref name="cubeYawRad"/>.
        /// Returns false if no face is visible (caller should retry).
        /// </summary>
        private static bool GenerateCubePoint(
            Random rng, double cx, double cy, double half,
            double pitchRad, double rollRad, double noiseSigma,
            double cubeYawRad,
            out double px, out double py, out double pz)
        {
            px = py = pz = 0;

            double cosR = Math.Cos(cubeYawRad);
            double sinR = Math.Sin(cubeYawRad);

            double deckZ = 800.0 + cx * Math.Tan(pitchRad) + cy * Math.Tan(rollRad);
            double zCentre = deckZ - half;

            // Back-face culling with rotated face normals.
            // Each horizontal face normal is rotated by cubeYawRad; the dot product
            // of the rotated normal with (sensor − face_centre) determines visibility.
            double cxr = cx * cosR + cy * sinR;   // cx projected onto rotated +X axis
            double cyr = -cx * sinR + cy * cosR;   // cx projected onto rotated +Y axis
            bool[] vis = new bool[6];
            vis[0] = -(cxr + half) > 0;             // rotated +X face
            vis[1] =  (cxr - half) > 0;             // rotated −X face
            vis[2] = -(cyr + half) > 0;             // rotated +Y face
            vis[3] =  (cyr - half) > 0;             // rotated −Y face
            vis[4] = -deckZ > 0;                    // bottom (on deck)
            vis[5] =  (deckZ - 2.0 * half) > 0;    // top (toward sensor)

            int visCount = 0;
            for (int f = 0; f < 6; f++) if (vis[f]) visCount++;
            if (visCount == 0) return false;

            int pick = rng.Next(visCount);
            int face = -1;
            for (int f = 0; f < 6; f++)
            {
                if (!vis[f]) continue;
                if (pick == 0) { face = f; break; }
                pick--;
            }

            // Generate point in local (unrotated) cube coords, then rotate into world.
            double u  = (rng.NextDouble() * 2.0 - 1.0) * half;
            double v  = (rng.NextDouble() * 2.0 - 1.0) * half;
            double lx = 0, ly = 0;
            switch (face)
            {
                case 0: lx =  half; ly = u;  pz = zCentre + v;          break;
                case 1: lx = -half; ly = u;  pz = zCentre + v;          break;
                case 2: lx = u;     ly =  half; pz = zCentre + v;       break;
                case 3: lx = u;     ly = -half; pz = zCentre + v;       break;
                case 4: lx = u;     ly = v;  pz = deckZ;                break;
                default:lx = u;     ly = v;  pz = deckZ - 2.0 * half;   break;
            }

            // Rotate local XY offset into world frame
            px = cx + lx * cosR - ly * sinR;
            py = cy + lx * sinR + ly * cosR;
            pz += NextGaussian(rng, noiseSigma);
            return true;
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
        /// <summary>
        /// Slab-method ray-AABB intersection test.  Ray goes from the origin (0,0,0)
        /// to point (px,py,pz).  Returns true when the ray hits the box at t &lt; 1
        /// (i.e. the box occludes the point from the sensor's perspective).
        /// </summary>
        private static bool RayHitsAABB(
            double px, double py, double pz,
            double bMinX, double bMinY, double bMinZ,
            double bMaxX, double bMaxY, double bMaxZ)
        {
            double tMin = double.MinValue;
            double tMax = double.MaxValue;

            // X slab
            if (Math.Abs(px) > 1e-12)
            {
                double t1 = bMinX / px;
                double t2 = bMaxX / px;
                if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }
            else if (0 < bMinX || 0 > bMaxX) return false;

            // Y slab
            if (Math.Abs(py) > 1e-12)
            {
                double t1 = bMinY / py;
                double t2 = bMaxY / py;
                if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }
            else if (0 < bMinY || 0 > bMaxY) return false;

            // Z slab
            if (Math.Abs(pz) > 1e-12)
            {
                double t1 = bMinZ / pz;
                double t2 = bMaxZ / pz;
                if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }
            else if (0 < bMinZ || 0 > bMaxZ) return false;

            // Hit counts only if the entry point is before the target (t < 1)
            // and the exit point is in front of the origin (tMax > 0).
            return tMin < 1.0 && tMax > 0.0;
        }

        /// <summary>
        /// Slab-method ray-AABB intersection with an arbitrary ray origin.
        /// Ray: O + t*D for t in [0,1].  Returns true when the ray hits the box at t &lt; 1.
        /// </summary>
        private static bool RayHitsOBB(
            double ox, double oy, double oz,
            double dx, double dy, double dz,
            double bMinX, double bMinY, double bMinZ,
            double bMaxX, double bMaxY, double bMaxZ)
        {
            double tMin = double.MinValue;
            double tMax = double.MaxValue;

            // X slab
            if (Math.Abs(dx) > 1e-12)
            {
                double t1 = (bMinX - ox) / dx;
                double t2 = (bMaxX - ox) / dx;
                if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }
            else if (ox < bMinX || ox > bMaxX) return false;

            // Y slab
            if (Math.Abs(dy) > 1e-12)
            {
                double t1 = (bMinY - oy) / dy;
                double t2 = (bMaxY - oy) / dy;
                if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }
            else if (oy < bMinY || oy > bMaxY) return false;

            // Z slab
            if (Math.Abs(dz) > 1e-12)
            {
                double t1 = (bMinZ - oz) / dz;
                double t2 = (bMaxZ - oz) / dz;
                if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
                if (t1 > tMin) tMin = t1;
                if (t2 < tMax) tMax = t2;
                if (tMin > tMax) return false;
            }
            else if (oz < bMinZ || oz > bMaxZ) return false;

            return tMin < 1.0 && tMax > 0.0;
        }

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
                        int max = MaxBufferPoints;
                        if (max > 0 && _buffer.Count > max)
                            _buffer.RemoveRange(0, _buffer.Count - max);
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

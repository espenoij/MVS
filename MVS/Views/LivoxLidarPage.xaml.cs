using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    public partial class LivoxLidarPage : System.Windows.Controls.UserControl
    {
        private LivoxLidarVM _vm;

        // ── Trackball state ───────────────────────────────────────────────────
        private bool   _isDragging;
        private Point  _lastMousePos;
        private double _rotX = 20.0;   // degrees around X-axis
        private double _rotY = 0.0;    // degrees around Y-axis
        private double _zoom = 15000.0; // camera distance

        // Last fit centroid, normal and zoom - used by Reset View / Top Down
        private double _centroidX;
        private double _centroidY;
        private double _centroidZ;
        private double _defaultZoom = 15000.0;
        private double _fitNormalX = 0.0;
        private double _fitNormalY = 0.0;
        private double _fitNormalZ = 1.0;
        private double _vesselFwdX = 1.0;
        private double _vesselFwdY = 0.0;
        private double _vesselFwdZ = 0.0;

        // Arrow visuals — added programmatically to avoid XAML codegen dependency
        private readonly ModelVisual3D _forwardArrowVisual      = new ModelVisual3D();
        private readonly ModelVisual3D _normalArrowVisual       = new ModelVisual3D();
        private readonly ModelVisual3D _lidarForwardArrowVisual = new ModelVisual3D();
        private readonly ModelVisual3D _marBoxVisual            = new ModelVisual3D();
        private readonly ModelVisual3D _helideckOutlineVisual   = new ModelVisual3D();

        public LivoxLidarPage()
        {
            InitializeComponent();
        }

        public void Init(LivoxLidarVM vm)
        {
            _vm = vm;
            DataContext = vm;
            vm.FitResultReady += OnFitResultReady;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LivoxLidarVM.StatusMessage))
                    tbStatus.ScrollToEnd();
            };
            viewport3D.Children.Add(_forwardArrowVisual);
            viewport3D.Children.Add(_normalArrowVisual);
            viewport3D.Children.Add(_lidarForwardArrowVisual);
            viewport3D.Children.Add(_marBoxVisual);
            viewport3D.Children.Add(_helideckOutlineVisual);
        }

        // ── Fit result → 3D scene update ─────────────────────────────────────

        private void OnFitResultReady(LivoxLidarPlaneFitResult fit)
        {
            if (!fit.IsValid) return;

            var snapshot = _vm.GetPointCloudSnapshot();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    UpdatePointCloud(snapshot);
                    UpdateScene(fit, snapshot);
                }));
        }

        private void UpdateScene(LivoxLidarPlaneFitResult fit, List<(float x, float y, float z)> points)
        {
            UpdatePlaneMesh(fit);
            UpdateMarBox(fit);
            UpdateHelideckOutline(fit, points);
            UpdateArrows(fit);
            UpdateCamera(fit);
        }

        /// <summary>
        /// Rebuild the point cloud mesh from a list of points.
        /// Called by MainWindow after a scan is stopped so the view reflects the full buffer.
        /// </summary>
        public void UpdatePointCloud(List<(float x, float y, float z)> points)
        {
            if (points == null || points.Count == 0)
            {
                pointCloudVisual.Content = null;
                return;
            }

            // Downsample to at most 20 000 points for display performance
            const int MaxDisplay = 20_000;
            int step = Math.Max(1, points.Count / MaxDisplay);

            // Find Z range across displayed points for heat-map normalisation
            float zMin = float.MaxValue, zMax = float.MinValue;
            for (int i = 0; i < points.Count; i += step)
            {
                float z = points[i].z;
                if (z < zMin) zMin = z;
                if (z > zMax) zMax = z;
            }
            float zRange = zMax - zMin;
            if (zRange < 1f) zRange = 1f; // guard: prevent divide-by-zero on flat scan

            var positions     = new Point3DCollection();
            var indices       = new Int32Collection();
            var texCoords     = new PointCollection();

            float quadHalfSize = 30f; // mm — display size of each point marker

            int idx = 0;
            for (int i = 0; i < points.Count; i += step)
            {
                var (x, y, z) = points[i];

                // U = normalised height: 0 = lowest (blue), 1 = highest (red)
                double u = (z - zMin) / zRange;
                var uv = new Point(u, 0.5);

                // Each point = two triangles forming a small square in the XY plane
                positions.Add(new Point3D(x - quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y + quadHalfSize, z));
                positions.Add(new Point3D(x - quadHalfSize, y + quadHalfSize, z));

                texCoords.Add(uv); texCoords.Add(uv);
                texCoords.Add(uv); texCoords.Add(uv);

                indices.Add(idx);     indices.Add(idx + 1); indices.Add(idx + 2);
                indices.Add(idx);     indices.Add(idx + 2); indices.Add(idx + 3);
                idx += 4;
            }

            var mesh = new MeshGeometry3D
            {
                Positions          = positions,
                TextureCoordinates = texCoords,
                TriangleIndices    = indices
            };

            // Heat-map gradient: blue (low Z) → cyan → green → yellow → red (high Z)
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint   = new Point(1, 0.5);
            brush.GradientStops.Add(new GradientStop(Colors.Blue,    0.00));
            brush.GradientStops.Add(new GradientStop(Colors.Cyan,   0.25));
            brush.GradientStops.Add(new GradientStop(Colors.Lime,   0.50));
            brush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.75));
            brush.GradientStops.Add(new GradientStop(Colors.Red,    1.00));
            brush.Freeze();

            // EmissiveMaterial renders at full texture brightness regardless of scene lighting
            var material = new EmissiveMaterial(brush);
            pointCloudVisual.Content = new GeometryModel3D(mesh, material);
        }

        private void UpdatePlaneMesh(LivoxLidarPlaneFitResult fit)
        {
            // Build a rectangle in the fitted plane, centred on the centroid
            double cx = fit.CentroidX, cy = fit.CentroidY, cz = fit.CentroidZ;

            // Primary and secondary axes of the plane
            double ax = fit.VesselForwardX, ay = fit.VesselForwardY, az = fit.VesselForwardZ;

            // Secondary axis = cross product of normal × primary
            double nx = fit.NormalX, ny = fit.NormalY, nz = fit.NormalZ;
            double sx = ny*az - nz*ay, sy = nz*ax - nx*az, sz = nx*ay - ny*ax;
            double slen = Math.Sqrt(sx*sx + sy*sy + sz*sz);
            if (slen > 0) { sx /= slen; sy /= slen; sz /= slen; }

            double ep = fit.ExtentPrimary   + 200;  // add 200mm margin
            double es = fit.ExtentSecondary + 200;

            Point3D p0 = P(cx - ax*ep - sx*es, cy - ay*ep - sy*es, cz - az*ep - sz*es);
            Point3D p1 = P(cx + ax*ep - sx*es, cy + ay*ep - sy*es, cz + az*ep - sz*es);
            Point3D p2 = P(cx + ax*ep + sx*es, cy + ay*ep + sy*es, cz + az*ep + sz*es);
            Point3D p3 = P(cx - ax*ep + sx*es, cy - ay*ep + sy*es, cz - az*ep + sz*es);

            var mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection { p0, p1, p2, p3 },
                TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }
            };

            var brush   = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)); // semi-transparent black
            var material = new DiffuseMaterial(brush);
            planeVisual.Content = new GeometryModel3D(mesh, material);
        }

        private void UpdateArrows(LivoxLidarPlaneFitResult fit)
        {
            var origin = new Point3D(0, 0, 0);

            double maxExtent   = Math.Max(fit.ExtentPrimary, fit.ExtentSecondary);
            double arrowLength = Math.Max(maxExtent * 0.6, 4000.0);
            double shaftRadius = arrowLength * 0.02;

            // Vessel forward: +X in sensor coordinate system (Mid-360: +X = forward)
            var forwardMesh = BuildArrowMesh(origin, new Vector3D(1, 0, 0), arrowLength, shaftRadius);
            var forwardMat  = MakeArrowMaterial(Colors.Red);
            _forwardArrowVisual.Content = new GeometryModel3D { Geometry = forwardMesh, Material = forwardMat, BackMaterial = forwardMat };

            // Plane normal
            var normalMesh = BuildArrowMesh(origin,
                new Vector3D(fit.NormalX, fit.NormalY, fit.NormalZ), arrowLength, shaftRadius);
            var normalMat  = MakeArrowMaterial(Color.FromRgb(0, 120, 255));
            _normalArrowVisual.Content = new GeometryModel3D { Geometry = normalMesh, Material = normalMat, BackMaterial = normalMat };

            // LiDAR forward: bow–stern heading axis as computed by the plane fit
            var lidarFwdMesh = BuildArrowMesh(origin,
                new Vector3D(fit.VesselForwardX, fit.VesselForwardY, fit.VesselForwardZ), arrowLength, shaftRadius);
            var lidarFwdMat  = MakeArrowMaterial(Color.FromRgb(160, 32, 240));
            _lidarForwardArrowVisual.Content = new GeometryModel3D { Geometry = lidarFwdMesh, Material = lidarFwdMat, BackMaterial = lidarFwdMat };
        }

        private static Material MakeArrowMaterial(Color color)
        {
            var brush = new SolidColorBrush(color);
            var group = new MaterialGroup();
            group.Children.Add(new DiffuseMaterial(brush));
            group.Children.Add(new EmissiveMaterial(brush));
            return group;
        }

        private static MeshGeometry3D BuildArrowMesh(Point3D origin, Vector3D dir, double length, double shaftRadius)
        {
            dir.Normalize();

            // Perpendicular basis around the arrow axis
            Vector3D refVec = Math.Abs(dir.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            Vector3D perp1  = Vector3D.CrossProduct(dir, refVec);
            perp1.Normalize();
            Vector3D perp2 = Vector3D.CrossProduct(perp1, dir); // already unit-length

            double shaftLen   = length * 0.75;
            double headRadius = shaftRadius * 2.5;

            const int N         = 10;
            double    angleStep = 2.0 * Math.PI / N;

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();

            Point3D shaftBase = origin;
            Point3D shaftTop  = origin + dir * shaftLen;
            Point3D headBase  = shaftTop;
            Point3D tip       = origin + dir * length;

            // Shaft cylinder — bottom ring (0..N-1), top ring (N..2N-1)
            for (int pass = 0; pass < 2; pass++)
            {
                Point3D centre = pass == 0 ? shaftBase : shaftTop;
                for (int i = 0; i < N; i++)
                {
                    double a = i * angleStep;
                    positions.Add(centre + shaftRadius * (Math.Cos(a) * perp1 + Math.Sin(a) * perp2));
                }
            }

            // Shaft side faces
            for (int i = 0; i < N; i++)
            {
                int next = (i + 1) % N;
                indices.Add(i);      indices.Add(i + N);    indices.Add(next + N);
                indices.Add(i);      indices.Add(next + N); indices.Add(next);
            }

            // Shaft bottom cap (fan from vertex 0)
            for (int i = 1; i < N - 1; i++)
            {
                indices.Add(0); indices.Add(i + 1); indices.Add(i);
            }

            // Cone head — base ring (2N..3N-1), tip (3N)
            int headStart = positions.Count;
            for (int i = 0; i < N; i++)
            {
                double a = i * angleStep;
                positions.Add(headBase + headRadius * (Math.Cos(a) * perp1 + Math.Sin(a) * perp2));
            }
            int tipIdx = positions.Count;
            positions.Add(tip);

            // Cone side faces
            for (int i = 0; i < N; i++)
            {
                int next = (i + 1) % N;
                indices.Add(headStart + i); indices.Add(tipIdx); indices.Add(headStart + next);
            }

            // Cone base cap (fan from headStart)
            for (int i = 1; i < N - 1; i++)
            {
                indices.Add(headStart); indices.Add(headStart + i); indices.Add(headStart + i + 1);
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }

        private static Point3D P(double x, double y, double z) => new Point3D(x, y, z);

        /// <summary>
        /// Draws thin black lines tracing the convex-hull outline of the point cloud projected
        /// onto the fitted plane.  The hull is lifted 150 mm along the plane normal so the tubes
        /// sit above the coplanar plane mesh and point-cloud quads (prevents z-fighting).
        /// </summary>
        private void UpdateHelideckOutline(LivoxLidarPlaneFitResult fit, List<(float x, float y, float z)> points)
        {
            if (points == null || points.Count == 0)
            {
                _helideckOutlineVisual.Content = null;
                return;
            }

            double cx = fit.CentroidX, cy = fit.CentroidY, cz = fit.CentroidZ;
            double ax = fit.VesselForwardX, ay = fit.VesselForwardY, az = fit.VesselForwardZ;
            double nx = fit.NormalX, ny = fit.NormalY, nz = fit.NormalZ;
            double lx = ny*az - nz*ay, ly = nz*ax - nx*az, lz = nx*ay - ny*ax;
            double llen = Math.Sqrt(lx*lx + ly*ly + lz*lz);
            if (llen > 1e-9) { lx /= llen; ly /= llen; lz /= llen; }

            // Project every point onto the plane's 2-D (forward u, lateral v) coordinates.
            // Downsample to at most 5 000 points — the hull only needs boundary samples.
            int step = Math.Max(1, points.Count / 5000);
            var pts2d = new List<(double u, double v)>(points.Count / step + 1);
            for (int i = 0; i < points.Count; i += step)
            {
                var (px, py, pz) = points[i];
                double dx = px - cx, dy = py - cy, dz = pz - cz;
                pts2d.Add((dx*ax + dy*ay + dz*az, dx*lx + dy*ly + dz*lz));
            }

            var hull = ConvexHull2D(pts2d);
            if (hull.Count < 3) { _helideckOutlineVisual.Content = null; return; }

            // Lift along the normal to clear z-fighting with the plane and point-cloud surfaces.
            const double NormalOffset = 150.0;
            double ox = cx + nx * NormalOffset;
            double oy = cy + ny * NormalOffset;
            double oz = cz + nz * NormalOffset;

            double maxExtent  = Math.Max(fit.ExtentPrimary, fit.ExtentSecondary);
            double tubeRadius = Math.Max(maxExtent * 0.008, 50.0);

            var brush = new SolidColorBrush(Colors.Black);
            brush.Freeze();
            var mat = new MaterialGroup();
            mat.Children.Add(new DiffuseMaterial(brush));
            mat.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(20, 20, 20))));

            var group = new Model3DGroup();
            for (int i = 0; i < hull.Count; i++)
            {
                var (u0, v0) = hull[i];
                var (u1, v1) = hull[(i + 1) % hull.Count];
                var p0 = P(ox + ax*u0 + lx*v0, oy + ay*u0 + ly*v0, oz + az*u0 + lz*v0);
                var p1 = P(ox + ax*u1 + lx*v1, oy + ay*u1 + ly*v1, oz + az*u1 + lz*v1);
                group.Children.Add(new GeometryModel3D(BuildTubeMesh(p0, p1, tubeRadius), mat) { BackMaterial = mat });
            }

            _helideckOutlineVisual.Content = group;
        }

        /// <summary>
        /// Computes the 2-D convex hull of <paramref name="pts"/> using the Graham scan.
        /// Returns vertices in counter-clockwise order.
        /// </summary>
        private static List<(double u, double v)> ConvexHull2D(List<(double u, double v)> pts)
        {
            if (pts.Count < 3) return new List<(double, double)>(pts);

            // Pivot: lowest v, leftmost u on tie
            int pivotIdx = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                if (pts[i].v < pts[pivotIdx].v ||
                   (pts[i].v == pts[pivotIdx].v && pts[i].u < pts[pivotIdx].u))
                    pivotIdx = i;
            }
            var pivot = pts[pivotIdx];

            // Sort remaining points by polar angle from pivot; break ties by keeping farthest
            var others = new List<(double u, double v)>(pts.Count - 1);
            for (int i = 0; i < pts.Count; i++)
                if (i != pivotIdx) others.Add(pts[i]);

            others.Sort((a, b) =>
            {
                double cross = (a.u - pivot.u) * (b.v - pivot.v) - (a.v - pivot.v) * (b.u - pivot.u);
                if (Math.Abs(cross) > 1e-9) return cross > 0 ? -1 : 1;
                double da = (a.u - pivot.u)*(a.u - pivot.u) + (a.v - pivot.v)*(a.v - pivot.v);
                double db = (b.u - pivot.u)*(b.u - pivot.u) + (b.v - pivot.v)*(b.v - pivot.v);
                return db.CompareTo(da); // farthest first — prunes collinear near-points
            });

            var stack = new List<(double u, double v)> { pivot };
            foreach (var pt in others)
            {
                while (stack.Count > 1 &&
                       Hull2DCross(stack[stack.Count - 2], stack[stack.Count - 1], pt) <= 0)
                    stack.RemoveAt(stack.Count - 1);
                stack.Add(pt);
            }
            return stack;
        }

        private static double Hull2DCross((double u, double v) o, (double u, double v) a, (double u, double v) b)
            => (a.u - o.u) * (b.v - o.v) - (a.v - o.v) * (b.u - o.u);

        /// <summary>
        /// Draws the four edges of the minimum-area bounding rectangle (MAR) that was used to
        /// determine the vessel forward direction.
        /// rectangle is clearly visible; the purple VesselForward arrow lies parallel to the two
        /// longer (port / starboard) sides.
        /// </summary>
        private void UpdateMarBox(LivoxLidarPlaneFitResult fit)
        {
            double cx = fit.CentroidX, cy = fit.CentroidY, cz = fit.CentroidZ;

            // Vessel forward axis (unit vector in the fitted plane)
            double ax = fit.VesselForwardX, ay = fit.VesselForwardY, az = fit.VesselForwardZ;

            // Lateral axis = N × forward  (same convention as UpdatePlaneMesh)
            double nx = fit.NormalX, ny = fit.NormalY, nz = fit.NormalZ;
            double lx = ny*az - nz*ay, ly = nz*ax - nx*az, lz = nx*ay - ny*ax;
            double llen = Math.Sqrt(lx*lx + ly*ly + lz*lz);
            if (llen > 1e-9) { lx /= llen; ly /= llen; lz /= llen; }

            // Signed MAR extents along the post-flip forward / lateral axes
            double fmin = fit.FwdExtentMin, fmax = fit.FwdExtentMax;
            double smin = fit.LatExtentMin, smax = fit.LatExtentMax;

            // Four corners of the MAR bounding rectangle (stern-port → bow-port → bow-stbd → stern-stbd)
            Point3D c0 = P(cx + ax*fmin + lx*smin, cy + ay*fmin + ly*smin, cz + az*fmin + lz*smin);
            Point3D c1 = P(cx + ax*fmax + lx*smin, cy + ay*fmax + ly*smin, cz + az*fmax + lz*smin);
            Point3D c2 = P(cx + ax*fmax + lx*smax, cy + ay*fmax + ly*smax, cz + az*fmax + lz*smax);
            Point3D c3 = P(cx + ax*fmin + lx*smax, cy + ay*fmin + ly*smax, cz + az*fmin + lz*smax);

            double maxExtent  = Math.Max(fit.ExtentPrimary, fit.ExtentSecondary);
            double tubeRadius = Math.Max(maxExtent * 0.008, 60.0);

            // Port / starboard edges (parallel to VesselForward) — slightly thicker to emphasise alignment
            var matFwd  = MakeArrowMaterial(Color.FromRgb(255, 195,   0));  // amber
            var matStern = MakeArrowMaterial(Color.FromRgb(200, 150,  0));  // darker amber
            var matBow  = MakeArrowMaterial(Color.FromRgb(160,  32, 240));  // purple — matches VesselForward arrow

            var group = new Model3DGroup();
            // Port side (c0 → c1) and starboard side (c3 → c2): parallel to forward
            group.Children.Add(new GeometryModel3D(BuildTubeMesh(c0, c1, tubeRadius * 1.3), matFwd)   { BackMaterial = matFwd });
            group.Children.Add(new GeometryModel3D(BuildTubeMesh(c3, c2, tubeRadius * 1.3), matFwd)   { BackMaterial = matFwd });
            // Stern edge (c0 → c3): darker amber
            group.Children.Add(new GeometryModel3D(BuildTubeMesh(c0, c3, tubeRadius), matStern) { BackMaterial = matStern });
            // Bow edge (c1 → c2): purple — the transverse edge that determines the forward direction
            group.Children.Add(new GeometryModel3D(BuildTubeMesh(c1, c2, tubeRadius * 1.3), matBow)   { BackMaterial = matBow });

            _marBoxVisual.Content = group;
        }

        /// <summary>
        /// Builds a plain cylinder mesh from <paramref name="from"/> to <paramref name="to"/>
        /// with the given cross-section <paramref name="radius"/>.
        /// </summary>
        private static MeshGeometry3D BuildTubeMesh(Point3D from, Point3D to, double radius)
        {
            var dir = to - from;
            double length = dir.Length;
            if (length < 1e-6) return new MeshGeometry3D();
            dir.Normalize();

            Vector3D refVec = Math.Abs(dir.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            Vector3D perp1  = Vector3D.CrossProduct(dir, refVec);
            perp1.Normalize();
            Vector3D perp2 = Vector3D.CrossProduct(perp1, dir); // already unit-length

            const int N         = 8;
            double    angleStep = 2.0 * Math.PI / N;

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();

            for (int pass = 0; pass < 2; pass++)
            {
                Point3D centre = pass == 0 ? from : to;
                for (int i = 0; i < N; i++)
                {
                    double a = i * angleStep;
                    positions.Add(centre + radius * (Math.Cos(a) * perp1 + Math.Sin(a) * perp2));
                }
            }

            for (int i = 0; i < N; i++)
            {
                int next = (i + 1) % N;
                indices.Add(i);        indices.Add(i + N);    indices.Add(next + N);
                indices.Add(i);        indices.Add(next + N); indices.Add(next);
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }

        private void UpdateCamera(LivoxLidarPlaneFitResult fit)
        {
            // Derive zoom from the plane's extents so the whole surface fills the view.
            // PerspectiveCamera FOV=45° (horizontal) → half-FOV=22.5°.
            // zoom = maxHalfExtent / tan(22.5°) × 1.2 (20% margin)
            double ep = fit.ExtentPrimary   + 200.0;
            double es = fit.ExtentSecondary + 200.0;
            _defaultZoom = Math.Max(ep, es) / Math.Tan(22.5 * Math.PI / 180.0) * 1.2;
            if (_defaultZoom < 1000) _defaultZoom = 1000;
            _centroidX = fit.CentroidX;
            _centroidY = fit.CentroidY;
            _centroidZ = fit.CentroidZ;
            _fitNormalX = fit.NormalX;
            _fitNormalY = fit.NormalY;
            _fitNormalZ = fit.NormalZ;
            _vesselFwdX = fit.VesselForwardX;
            _vesselFwdY = fit.VesselForwardY;
            _vesselFwdZ = fit.VesselForwardZ;
            _zoom = _defaultZoom;
            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);
        }

        // ── Trackball mouse handlers ──────────────────────────────────────────

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _rotX = 20.0;
            _rotY = 0.0;
            _zoom = _defaultZoom;
            camera.UpDirection = new Vector3D(0, 1, 0);
            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);
        }

        private void TopDownCamera_Click(object sender, RoutedEventArgs e)
        {
            // Place the camera along the fitted plane normal looking at the centroid.
            // Derive equivalent trackball angles so mouse-drag continues to work.
            double nx = _fitNormalX, ny = _fitNormalY, nz = _fitNormalZ;
            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-6) { nx /= len; ny /= len; nz /= len; }
            else             { nx = 0; ny = 0; nz = 1; }

            // ApplyCameraTransform positions the camera at:
            //   camX = cx + zoom * sin(radY) * cos(radX)
            //   camY = cy - zoom * sin(radX)
            //   camZ = cz + zoom * cos(radY) * cos(radX)
            // Solving for rotX/rotY that place the camera along (nx,ny,nz):
            _rotX = -Math.Asin(Math.Max(-1.0, Math.Min(1.0, ny))) * 180.0 / Math.PI;
            double cosX = Math.Cos(_rotX * Math.PI / 180.0);
            if (cosX < 1e-6) cosX = 1e-6;
            _rotY = Math.Atan2(nx / cosX, nz / cosX) * 180.0 / Math.PI;
            _zoom = _defaultZoom;

            // Orient "up" on screen to match the vessel forward direction
            camera.UpDirection = new Vector3D(_vesselFwdX, _vesselFwdY, _vesselFwdZ);

            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);
        }

        // -- Trackball mouse handlers ------------------------------------------

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging  = true;
            _lastMousePos = e.GetPosition(viewport3D);
            viewport3D.CaptureMouse();
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            viewport3D.ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var pos   = e.GetPosition(viewport3D);
            double dx = pos.X - _lastMousePos.X;
            double dy = pos.Y - _lastMousePos.Y;
            _lastMousePos = pos;

            _rotY -= dx * 0.5;
            _rotX -= dy * 0.5;
            _rotX  = Math.Max(-89, Math.Min(89, _rotX));

            ApplyCameraTransform(0, 0, 0);
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _zoom *= e.Delta > 0 ? 1.1 : 0.9;
            _zoom  = Math.Max(100, _zoom);
            ApplyCameraTransform(0, 0, 0);
        }

        private void ApplyCameraTransform(double cx, double cy, double cz)
        {
            double radX = _rotX * Math.PI / 180.0;
            double radY = _rotY * Math.PI / 180.0;

            double camX = cx + _zoom * Math.Sin(radY) * Math.Cos(radX);
            double camY = cy - _zoom * Math.Sin(radX);
            double camZ = cz + _zoom * Math.Cos(radY) * Math.Cos(radX);

            camera.Position      = new Point3D(camX, camY, camZ);
            camera.LookDirection = new Vector3D(cx - camX, cy - camY, cz - camZ);
        }
    }
}

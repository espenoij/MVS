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
        private readonly ModelVisual3D _forwardArrowVisual = new ModelVisual3D();
        private readonly ModelVisual3D _normalArrowVisual  = new ModelVisual3D();
        private readonly ModelVisual3D _bowArrowVisual     = new ModelVisual3D();

        // Deck edge visuals
        private readonly ModelVisual3D _deckEdgeLineVisual   = new ModelVisual3D();
        private readonly ModelVisual3D _deckEdgePointsVisual = new ModelVisual3D();
        private readonly ModelVisual3D _hexVerticesVisual    = new ModelVisual3D();

        public LivoxLidarPage()
        {
            InitializeComponent();
        }

        public void Init(LivoxLidarVM vm)
        {
            _vm = vm;
            DataContext = vm;
            vm.FitResultReady += OnFitResultReady;
            vm.DeckEdgeResultReady += OnDeckEdgeResultReady;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LivoxLidarVM.StatusMessage))
                    tbStatus.ScrollToEnd();
            };
            viewport3D.Children.Add(_forwardArrowVisual);
            viewport3D.Children.Add(_normalArrowVisual);
            viewport3D.Children.Add(_bowArrowVisual);
            viewport3D.Children.Add(_deckEdgeLineVisual);
            viewport3D.Children.Add(_deckEdgePointsVisual);
            viewport3D.Children.Add(_hexVerticesVisual);
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
            UpdateArrows(fit);
            UpdateCamera(fit);

            // Clear previous deck edge visuals (will be re-drawn on next FindDeckEdge)
            _bowArrowVisual.Content       = null;
            _deckEdgeLineVisual.Content   = null;
            _deckEdgePointsVisual.Content = null;
            _hexVerticesVisual.Content    = null;
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

        // ── Deck edge result → 3D scene update ───────────────────────────────

        private void OnDeckEdgeResultReady(LivoxLidarDeckEdgeResult edge)
        {
            if (!edge.IsValid) return;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() => UpdateDeckEdge(edge)));
        }

        private void UpdateDeckEdge(LivoxLidarDeckEdgeResult edge)
        {
            _hexVerticesVisual.Content = null;

            // Edge line: plain tube (no arrowhead) along the bow edge
            double shaftRadius = Math.Max(edge.HalfLength * 2.0, 4000.0) * 0.015;
            var edgeStart = new Point3D(
                edge.MidpointX - edge.HalfLength * edge.DirectionX,
                edge.MidpointY - edge.HalfLength * edge.DirectionY,
                edge.MidpointZ - edge.HalfLength * edge.DirectionZ);
            var edgeEnd = new Point3D(
                edge.MidpointX + edge.HalfLength * edge.DirectionX,
                edge.MidpointY + edge.HalfLength * edge.DirectionY,
                edge.MidpointZ + edge.HalfLength * edge.DirectionZ);
            var edgeMesh = BuildTubeMesh(edgeStart, edgeEnd, shaftRadius);
            var edgeMat  = MakeArrowMaterial(Color.FromRgb(0, 200, 80));
            _deckEdgeLineVisual.Content = new GeometryModel3D { Geometry = edgeMesh, Material = edgeMat, BackMaterial = edgeMat };

            // Hex vertex markers — bright yellow cubes at each of the 6 estimated vertices
            if (edge.HexVertices3D != null && edge.HexVertices3D.Count == 6)
            {
                double h         = Math.Max(edge.HalfLength * 0.04, 150.0);
                var    positions = new Point3DCollection();
                var    indices   = new Int32Collection();

                foreach (var v in edge.HexVertices3D)
                {
                    int    b  = positions.Count;
                    double vx = v.X, vy = v.Y, vz = v.Z;
                    positions.Add(new Point3D(vx - h, vy - h, vz - h)); // b+0
                    positions.Add(new Point3D(vx + h, vy - h, vz - h)); // b+1
                    positions.Add(new Point3D(vx + h, vy + h, vz - h)); // b+2
                    positions.Add(new Point3D(vx - h, vy + h, vz - h)); // b+3
                    positions.Add(new Point3D(vx - h, vy - h, vz + h)); // b+4
                    positions.Add(new Point3D(vx + h, vy - h, vz + h)); // b+5
                    positions.Add(new Point3D(vx + h, vy + h, vz + h)); // b+6
                    positions.Add(new Point3D(vx - h, vy + h, vz + h)); // b+7
                    // bottom (z-)
                    indices.Add(b);   indices.Add(b+1); indices.Add(b+2);
                    indices.Add(b);   indices.Add(b+2); indices.Add(b+3);
                    // top (z+)
                    indices.Add(b+4); indices.Add(b+6); indices.Add(b+5);
                    indices.Add(b+4); indices.Add(b+7); indices.Add(b+6);
                    // front (y-)
                    indices.Add(b);   indices.Add(b+5); indices.Add(b+1);
                    indices.Add(b);   indices.Add(b+4); indices.Add(b+5);
                    // back (y+)
                    indices.Add(b+2); indices.Add(b+7); indices.Add(b+3);
                    indices.Add(b+2); indices.Add(b+6); indices.Add(b+7);
                    // left (x-)
                    indices.Add(b);   indices.Add(b+3); indices.Add(b+7);
                    indices.Add(b);   indices.Add(b+7); indices.Add(b+4);
                    // right (x+)
                    indices.Add(b+1); indices.Add(b+6); indices.Add(b+2);
                    indices.Add(b+1); indices.Add(b+5); indices.Add(b+6);
                }

                var markerMesh  = new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
                var markerBrush = new SolidColorBrush(Color.FromRgb(0, 110, 40));
                markerBrush.Freeze();
                var markerMat = new EmissiveMaterial(markerBrush);
                _hexVerticesVisual.Content = new GeometryModel3D { Geometry = markerMesh, Material = markerMat, BackMaterial = markerMat };
            }

            // Bow direction arrow: from lidar origin toward the bow
            double bowArrowLen    = Math.Max(edge.HalfLength * 0.8, 4000.0);
            double bowShaftRadius = bowArrowLen * 0.02;
            var bowMesh = BuildArrowMesh(new Point3D(0, 0, 0),
                new Vector3D(edge.VesselForwardX, edge.VesselForwardY, edge.VesselForwardZ),
                bowArrowLen, bowShaftRadius);
            var bowMat = MakeArrowMaterial(Color.FromRgb(0, 200, 80));
            _bowArrowVisual.Content = new GeometryModel3D { Geometry = bowMesh, Material = bowMat, BackMaterial = bowMat };

            // Highlight edge points
            UpdateEdgePointsMesh(edge.EdgePoints);
        }

        private void UpdateEdgePointsMesh(List<(float x, float y, float z)> points)
        {
            if (points == null || points.Count == 0)
            {
                _deckEdgePointsVisual.Content = null;
                return;
            }

            const int MaxDisplay = 5000;
            int step = Math.Max(1, points.Count / MaxDisplay);

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();
            float quadHalfSize = 50f;

            int idx = 0;
            for (int i = 0; i < points.Count; i += step)
            {
                var (x, y, z) = points[i];
                positions.Add(new Point3D(x - quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y + quadHalfSize, z));
                positions.Add(new Point3D(x - quadHalfSize, y + quadHalfSize, z));

                indices.Add(idx);     indices.Add(idx + 1); indices.Add(idx + 2);
                indices.Add(idx);     indices.Add(idx + 2); indices.Add(idx + 3);
                idx += 4;
            }

            var mesh = new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
            var brush = new SolidColorBrush(Color.FromRgb(0, 200, 80));
            brush.Freeze();
            _deckEdgePointsVisual.Content = new GeometryModel3D(mesh, new EmissiveMaterial(brush));
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

        private static MeshGeometry3D BuildTubeMesh(Point3D from, Point3D to, double radius)
        {
            var dir = to - from;
            if (dir.Length < 1e-10) return new MeshGeometry3D();
            dir.Normalize();

            Vector3D refVec = Math.Abs(dir.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            Vector3D perp1  = Vector3D.CrossProduct(dir, refVec); perp1.Normalize();
            Vector3D perp2  = Vector3D.CrossProduct(perp1, dir);

            const int N         = 10;
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
                indices.Add(i);       indices.Add(i + N);    indices.Add(next + N);
                indices.Add(i);       indices.Add(next + N); indices.Add(next);
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }

        private static Point3D P(double x, double y, double z) => new Point3D(x, y, z);

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

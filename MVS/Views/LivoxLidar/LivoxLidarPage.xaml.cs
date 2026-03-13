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

        public LivoxLidarPage()
        {
            InitializeComponent();
        }

        public void Init(LivoxLidarVM vm)
        {
            _vm = vm;
            DataContext = vm;
            vm.FitResultReady += OnFitResultReady;
        }

        // ── Fit result → 3D scene update ─────────────────────────────────────

        private void OnFitResultReady(LivoxLidarPlaneFitResult fit)
        {
            if (!fit.IsValid) return;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() => UpdateScene(fit)));
        }

        private void UpdateScene(LivoxLidarPlaneFitResult fit)
        {
            // For the point cloud we build a mesh from a snapshot
            // Ask the subsystem for a downsampled snapshot via the VM
            // (the VM holds the subsystem reference; we request directly through fit metadata)
            // For simplicity, we draw the fitted plane only here and let the
            // code-behind rebuild the point mesh when explicitly requested.

            UpdatePlaneMesh(fit);
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

            var positions = new Point3DCollection();
            var indices   = new Int32Collection();
            var normals   = new Vector3DCollection();

            float quadHalfSize = 30f; // mm — display size of each point marker

            int idx = 0;
            for (int i = 0; i < points.Count; i += step)
            {
                var (x, y, z) = points[i];

                // Each point = two triangles forming a small square in the XY plane
                positions.Add(new Point3D(x - quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y - quadHalfSize, z));
                positions.Add(new Point3D(x + quadHalfSize, y + quadHalfSize, z));
                positions.Add(new Point3D(x - quadHalfSize, y + quadHalfSize, z));

                normals.Add(new Vector3D(0, 0, 1));
                normals.Add(new Vector3D(0, 0, 1));
                normals.Add(new Vector3D(0, 0, 1));
                normals.Add(new Vector3D(0, 0, 1));

                indices.Add(idx);     indices.Add(idx + 1); indices.Add(idx + 2);
                indices.Add(idx);     indices.Add(idx + 2); indices.Add(idx + 3);
                idx += 4;
            }

            var mesh = new MeshGeometry3D
            {
                Positions  = positions,
                Normals    = normals,
                TriangleIndices = indices
            };

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 200, 255)));
            pointCloudVisual.Content = new GeometryModel3D(mesh, material);
        }

        private void UpdatePlaneMesh(LivoxLidarPlaneFitResult fit)
        {
            // Build a rectangle in the fitted plane, centred on the centroid
            double cx = fit.CentroidX, cy = fit.CentroidY, cz = fit.CentroidZ;

            // Primary and secondary axes of the plane
            double ax = fit.AxisX, ay = fit.AxisY, az = fit.AxisZ;

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

            var brush   = new SolidColorBrush(Color.FromArgb(80, 255, 165, 0)); // semi-transparent orange
            var material = new DiffuseMaterial(brush);
            planeVisual.Content = new GeometryModel3D(mesh, material);
        }

        private static Point3D P(double x, double y, double z) => new Point3D(x, y, z);

        private void UpdateCamera(LivoxLidarPlaneFitResult fit)
        {
            _zoom = fit.ClearanceMm * 2.5;
            if (_zoom < 1000) _zoom = 1000;
            ApplyCameraTransform(fit.CentroidX, fit.CentroidY, fit.CentroidZ);
        }

        // ── Trackball mouse handlers ──────────────────────────────────────────

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

            _rotY += dx * 0.5;
            _rotX += dy * 0.5;
            _rotX  = Math.Max(-89, Math.Min(89, _rotX));

            ApplyCameraTransform(0, 0, 0);
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _zoom *= e.Delta > 0 ? 0.9 : 1.1;
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

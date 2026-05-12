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
        private double _rotX = 0.0;   // degrees around X-axis
        private double _rotY = 0.0;  // degrees around Y-axis
        private double _zoom = 15000.0; // camera distance

        // Last fit centroid, normal and zoom - used by Reset View / Top Down
        private double _centroidX;
        private double _centroidY;
        private double _centroidZ;
        private double _lookAtX;
        private double _lookAtY;
        private double _lookAtZ;
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

        private bool _cameraFitted;

        // Store the last point cloud for re-rendering when settings change
        private List<(float x, float y, float z)> _lastPointCloud;

        // Store last fit and edge results for re-rendering when settings change
        private LivoxLidarPlaneFitResult _lastFitResult;
        private LivoxLidarDeckEdgeResult _lastEdgeResult;

        // Cached brushes/materials. Rebuilt when EnableEmissiveColors changes.
        private MaterialFactory _materials = new MaterialFactory(emissiveEnabled: false);

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
            vm.ScanCleared += OnScanCleared;
            vm.SimulationStarted += () => Dispatcher.BeginInvoke(new Action(() =>
            {
                var tc = FindName("tabControl") as Telerik.Windows.Controls.RadTabControl;
                if (tc != null) tc.SelectedIndex = 1;
            }));
            vm.PointCloudUpdated += points => UpdatePointCloud(points);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.EnableEmissiveColors))
                {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            // Rebuild the material cache so subsequent renders pick up the new flag.
                            _materials = new MaterialFactory(vm.EnableEmissiveColors);
                            // Re-render point cloud
                            if (_lastPointCloud != null)
                            {
                                UpdatePointCloud(_lastPointCloud);
                            }

                            // Re-render fit visuals (plane and arrows)
                            if (_lastFitResult != null && _lastFitResult.IsValid)
                            {
                                UpdatePlaneMesh(_lastFitResult);
                                UpdateArrows(_lastFitResult);
                            }

                            // Re-render edge visuals
                            if (_lastEdgeResult != null && _lastEdgeResult.IsValid)
                            {
                                UpdateDeckEdge(_lastEdgeResult);
                            }

                            // Re-render axis indicators
                            InitializeAxisIndicators();
                        }));
                }
            };
            viewport3D.Children.Add(_forwardArrowVisual);
            viewport3D.Children.Add(_normalArrowVisual);
            viewport3D.Children.Add(_bowArrowVisual);
            viewport3D.Children.Add(_deckEdgeLineVisual);
            viewport3D.Children.Add(_deckEdgePointsVisual);
            viewport3D.Children.Add(_hexVerticesVisual);

            // Initialise the cached materials from the current VM setting.
            _materials = new MaterialFactory(vm.EnableEmissiveColors);

            // Initialize the 3D axis indicators in their separate viewport
            InitializeAxisIndicators();

            // Explicitly set the camera
            // is fully initialised before the first scan renders any points.
            ApplyCameraTransform(0, 0, 0);
        }

        // ── Fit result → 3D scene update ─────────────────────────────────────

        private void OnScanCleared()
        {
            if (Dispatcher.CheckAccess())
            {
                ClearScene();
            }
            else
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(ClearScene));
            }
        }

        private void ClearScene()
        {
            _cameraFitted = false;
            _lastPointCloud = null;
            _lastFitResult = null;
            _lastEdgeResult = null;
            pointCloudVisual.Content      = null;
            planeVisual.Content           = null;
            _forwardArrowVisual.Content   = null;
            _normalArrowVisual.Content    = null;
            _bowArrowVisual.Content       = null;
            _deckEdgeLineVisual.Content   = null;
            _deckEdgePointsVisual.Content = null;
            _hexVerticesVisual.Content    = null;
            // Axis indicators remain visible in their separate viewport
        }

        private void OnFitResultReady(LivoxLidarPlaneFitResult fit)
        {
            if (!fit.IsValid) return;

            var snapshot = _vm.GetPointCloudSnapshot();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    UpdatePointCloud(snapshot);
                    UpdateScene(fit, snapshot);
                    _vm.AppendStatus($"Plane fit — Pitch: {fit.PitchDeg:+0.00;-0.00;0.00}°  Roll: {fit.RollDeg:+0.00;-0.00;0.00}°  RMSE: {fit.FitRmse:0.0} mm  Points: {fit.PointCount:N0}");
                }));
        }

        private void UpdateScene(LivoxLidarPlaneFitResult fit, List<(float x, float y, float z)> points)
        {
            _lastFitResult = fit;
            UpdatePlaneMesh(fit);
            UpdateArrows(fit);
            UpdateCamera(fit);

            // Clear previous deck edge visuals
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
                _lastPointCloud = null;
                return;
            }

            // Store the points for re-rendering when settings change
            _lastPointCloud = points;

            // Build a downsampled quad-per-point mesh with height-based UVs.
            int maxDisplay = _vm != null ? Math.Max(_vm.MaxDisplayPoints, 100) : 20_000;
            var meshResult = PointCloudMeshBuilder.Build(points, maxDisplay);
            if (meshResult == null)
            {
                pointCloudVisual.Content = null;
                return;
            }
            var mesh = meshResult.Mesh;
            int displayCount = meshResult.DisplayedPointCount;

            // Heat-map material (cached, frozen) shaded by the per-vertex U coordinate.
            var heatmapMat = _materials.HeatmapMaterial;
            pointCloudVisual.Content = new GeometryModel3D(mesh, heatmapMat) { BackMaterial = heatmapMat };

            // Auto-fit the camera the first time points are shown so the cloud is visible
            // before the user runs Analyse (which calls UpdateCamera).
            if (!_cameraFitted &&
                PointCloudMeshBuilder.TryComputeFitBounds(points, maxDisplay,
                    out float cx, out float cy, out float cz,
                    out float maxHalfX, out float maxHalfY))
            {
                _centroidX = cx;
                _centroidY = cy;
                _centroidZ = cz;
                _defaultZoom = ComputeFitZoom(Math.Max(maxHalfX + 200, 1000), Math.Max(maxHalfY + 200, 1000));
                ApplyTopDownView();
                _cameraFitted = true;
            }
        }

        // ── Shared arrow sizing ──────────────────────────────────────────────
        // All arrows in the main viewport derive their length and shaft radius
        // from a single source so they look visually consistent. The edge
        // indicator tube keeps its real physical length (2 × HalfLength) but
        // adopts the shared shaft radius.
        private const double ArrowShaftRadiusFraction = 0.02; // shaft radius / length

        private void GetArrowDimensions(out double length, out double shaftRadius)
        {
            // Prefer the plane fit extent (stable scene size). Fall back to the
            // edge half-length if no fit is available yet, then to a constant.
            double baseSize;
            if (_lastFitResult != null && _lastFitResult.IsValid)
            {
                double maxExtent = Math.Max(_lastFitResult.ExtentPrimary, _lastFitResult.ExtentSecondary);
                baseSize = maxExtent * 0.3;
            }
            else if (_lastEdgeResult != null && _lastEdgeResult.IsValid)
            {
                baseSize = _lastEdgeResult.HalfLength * 1.2;
            }
            else
            {
                baseSize = 6000.0;
            }

            length      = Math.Max(baseSize, 6000.0);
            shaftRadius = length * ArrowShaftRadiusFraction;
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
            _lastEdgeResult = edge;
            _hexVerticesVisual.Content = null;

            // Shared arrow dimensions — same length & thickness as the other arrows
            GetArrowDimensions(out double arrowLength, out double shaftRadius);

            // Edge line: plain tube (no arrowhead) along the bow edge.
            // Length is the real physical edge segment; thickness is shared.
            var edgeStart = new Point3D(
                edge.MidpointX - edge.HalfLength * edge.DirectionX,
                edge.MidpointY - edge.HalfLength * edge.DirectionY,
                edge.MidpointZ - edge.HalfLength * edge.DirectionZ);
            var edgeEnd = new Point3D(
                edge.MidpointX + edge.HalfLength * edge.DirectionX,
                edge.MidpointY + edge.HalfLength * edge.DirectionY,
                edge.MidpointZ + edge.HalfLength * edge.DirectionZ);
            var edgeMesh = TubeMeshBuilder.Build(edgeStart, edgeEnd, shaftRadius);
            var edgeMat  = _materials.SolidMaterial(Color.FromRgb(255, 140, 0));
            _deckEdgeLineVisual.Content = new GeometryModel3D { Geometry = edgeMesh, Material = edgeMat, BackMaterial = edgeMat };

            // Hull vertex markers — bright magenta cubes at each estimated boundary vertex
            double markerHalf = Math.Max(edge.HalfLength * 0.02, 75.0);
            var markerMesh = HullMarkerMeshBuilder.Build(edge.HullVertices3D, markerHalf);
            if (markerMesh != null)
            {
                var markerMat = _materials.SolidMaterial(Color.FromRgb(255, 0, 255));
                _hexVerticesVisual.Content = new GeometryModel3D { Geometry = markerMesh, Material = markerMat, BackMaterial = markerMat };
            }

            // Bow direction arrow: same length & thickness as the other arrows
            var bowMesh = ArrowMeshBuilder.Build(new Point3D(0, 0, 0),
                new Vector3D(edge.VesselForwardX, edge.VesselForwardY, edge.VesselForwardZ),
                arrowLength, shaftRadius);
            var bowMat = _materials.SolidMaterial(Color.FromRgb(255, 140, 0));
            _bowArrowVisual.Content = new GeometryModel3D { Geometry = bowMesh, Material = bowMat, BackMaterial = bowMat };

            // Highlight edge points
            UpdateEdgePointsMesh(edge.EdgePoints);
        }

        private void UpdateEdgePointsMesh(List<(float x, float y, float z)> points)
        {
            var mesh = EdgePointsMeshBuilder.Build(points);
            if (mesh == null)
            {
                _deckEdgePointsVisual.Content = null;
                return;
            }

            var mat = _materials.SolidMaterial(Color.FromRgb(0, 200, 80));
            _deckEdgePointsVisual.Content = new GeometryModel3D(mesh, mat);
        }

        private void UpdatePlaneMesh(LivoxLidarPlaneFitResult fit)
        {
            var mesh = PlaneMeshBuilder.Build(fit);
            // Semi-transparent black is not cached because the alpha channel is intentional;
            // it is built once per fit and there is only ever one plane in the scene.
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)));
            planeVisual.Content = new GeometryModel3D(mesh, material);
        }

        private void UpdateArrows(LivoxLidarPlaneFitResult fit)
        {
            _lastFitResult = fit;
            var origin = new Point3D(0, 0, 0);

            // Shared arrow dimensions — used by every arrow in the main viewport
            GetArrowDimensions(out double arrowLength, out double shaftRadius);

            // Vessel forward: +X in sensor coordinate system (Mid-360: +X = forward)
            var forwardMesh = ArrowMeshBuilder.Build(origin, new Vector3D(1, 0, 0), arrowLength, shaftRadius);
            var forwardMat  = _materials.SolidMaterial(Colors.Red);
            _forwardArrowVisual.Content = new GeometryModel3D { Geometry = forwardMesh, Material = forwardMat, BackMaterial = forwardMat };

            // Plane normal
            var normalMesh = ArrowMeshBuilder.Build(origin,
                new Vector3D(fit.NormalX, fit.NormalY, fit.NormalZ), arrowLength, shaftRadius);
            var normalMat  = _materials.SolidMaterial(Color.FromRgb(0, 120, 255));
            _normalArrowVisual.Content = new GeometryModel3D { Geometry = normalMesh, Material = normalMat, BackMaterial = normalMat };
        }

        private void InitializeAxisIndicators()
        {
            // Create small 3D axis arrows at the origin for the separate axis viewport
            // These will rotate with the main camera but stay in a fixed screen position
            const double axisLength = 900.0;  // Arrow length for the indicator viewport
            const double axisRadius = 25.0;   // Shaft radius
            var origin = new Point3D(0, 0, 0);

            // Get the axis visuals from XAML
            var xVisual = FindName("axisXVisual") as ModelVisual3D;
            var yVisual = FindName("axisYVisual") as ModelVisual3D;
            var zVisual = FindName("axisZVisual") as ModelVisual3D;

            if (xVisual == null || yVisual == null || zVisual == null) return;

            // X-axis: Red, pointing right (forward/bow direction)
            var xMesh = ArrowMeshBuilder.Build(origin, new Vector3D(1, 0, 0), axisLength, axisRadius);
            var xMat = _materials.SolidMaterial(Colors.Red);
            xVisual.Content = new GeometryModel3D { Geometry = xMesh, Material = xMat, BackMaterial = xMat };

            // Y-axis: Green, pointing away (starboard direction)
            var yMesh = ArrowMeshBuilder.Build(origin, new Vector3D(0, 1, 0), axisLength, axisRadius);
            var yMat = _materials.SolidMaterial(Colors.Green);
            yVisual.Content = new GeometryModel3D { Geometry = yMesh, Material = yMat, BackMaterial = yMat };

            // Z-axis: Dark blue, pointing up
            var zMesh = ArrowMeshBuilder.Build(origin, new Vector3D(0, 0, 1), axisLength, axisRadius);
            var zMat = _materials.SolidMaterial(Color.FromRgb(0, 60, 180));
            zVisual.Content = new GeometryModel3D { Geometry = zMesh, Material = zMat, BackMaterial = zMat };
        }


        private void UpdateCamera(LivoxLidarPlaneFitResult fit)
        {
            // Derive zoom from the plane's extents so the whole surface fills the view,
            // accounting for both horizontal and vertical FOV based on viewport aspect ratio.
            double ep = fit.ExtentPrimary   + 200.0;
            double es = fit.ExtentSecondary + 200.0;
            _defaultZoom = ComputeFitZoom(ep, es);
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
            ApplyPerspectiveView();
            _cameraFitted = true;
        }

        private double ComputeFitZoom(double halfExtentH, double halfExtentV)
        {
            // FieldOfView is the horizontal FOV in WPF's PerspectiveCamera.
            double hFovRad = camera.FieldOfView * Math.PI / 360.0; // half horizontal FOV

            // Derive vertical half-FOV from viewport aspect ratio
            double aspect = viewport3D.ActualWidth / Math.Max(viewport3D.ActualHeight, 1.0);
            if (aspect < 0.01) aspect = 1.0; // guard before layout
            double vFovRad = Math.Atan(Math.Tan(hFovRad) / aspect);

            // Zoom needed so each extent fits within the corresponding FOV half-angle
            double zoomH = halfExtentH / Math.Tan(hFovRad);
            double zoomV = halfExtentV / Math.Tan(vFovRad);

            return Math.Max(zoomH, zoomV) * 1.2; // 20% margin
        }

        private void ApplyTopDownView()
        {
            double nx = _fitNormalX, ny = _fitNormalY, nz = _fitNormalZ;
            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-6) { nx /= len; ny /= len; nz /= len; }
            else { nx = 0; ny = 0; nz = 1; }

            _rotX = -Math.Asin(Math.Max(-1.0, Math.Min(1.0, ny))) * 180.0 / Math.PI;
            double cosX = Math.Cos(_rotX * Math.PI / 180.0);
            if (cosX < 1e-6) cosX = 1e-6;
            _rotY = Math.Atan2(nx / cosX, nz / cosX) * 180.0 / Math.PI;
            _zoom = _defaultZoom;

            camera.UpDirection = new Vector3D(_vesselFwdX, _vesselFwdY, _vesselFwdZ);
            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);
        }

        private void ApplyPerspectiveView()
        {
            // Start from the top-down orientation (aligned to fit normal),
            // then apply configurable H and V rotations
            double nx = _fitNormalX, ny = _fitNormalY, nz = _fitNormalZ;
            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-6) { nx /= len; ny /= len; nz /= len; }
            else { nx = 0; ny = 0; nz = 1; }

            _rotX = -Math.Asin(Math.Max(-1.0, Math.Min(1.0, ny))) * 180.0 / Math.PI;
            _rotX += _vm.PerspectiveRotX;   // V: configurable vertical tilt
            double cosX = Math.Cos(_rotX * Math.PI / 180.0);
            if (cosX < 1e-6) cosX = 1e-6;
            _rotY = Math.Atan2(nx / cosX, nz / cosX) * 180.0 / Math.PI;
            _rotY += _vm.PerspectiveRotY;  // H: configurable horizontal rotation
            _zoom = _defaultZoom;

            camera.UpDirection = new Vector3D(_vesselFwdX, _vesselFwdY, _vesselFwdZ);
            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);

            // Z roll = 0: set UpDirection to world Z projected perp to look direction (the natural zero).
            Vector3D ld = camera.LookDirection;
            ld.Normalize();
            Vector3D worldZ = new Vector3D(0, 0, 1);
            Vector3D refUp = worldZ - Vector3D.DotProduct(worldZ, ld) * ld;
            double refLen = refUp.Length;
            if (refLen > 1e-6)
            {
                camera.UpDirection = new Vector3D(refUp.X / refLen, refUp.Y / refLen, refUp.Z / refLen);
            }
            UpdateAxisCamera();
        }

        // ── Trackball mouse handlers

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            ApplyTopDownView();
        }

        private void PerspectiveView_Click(object sender, RoutedEventArgs e)
        {
            ApplyPerspectiveView();
        }

        // -- Trackball mouse handlers ------------------------------------------

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging  = true;
            _lastMousePos = e.GetPosition((IInputElement)sender);
            ((IInputElement)sender).CaptureMouse();
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((IInputElement)sender).ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var pos   = e.GetPosition((IInputElement)sender);
            double dx = pos.X - _lastMousePos.X;
            double dy = pos.Y - _lastMousePos.Y;
            _lastMousePos = pos;

            // Rotate the camera around the look-at point using screen-space axes
            var lookAt = new Point3D(_lookAtX, _lookAtY, _lookAtZ);
            Vector3D offset = camera.Position - lookAt;

            // Screen-right = cross(LookDirection, UpDirection)
            Vector3D lookDir = camera.LookDirection;
            Vector3D up = camera.UpDirection;
            Vector3D right = Vector3D.CrossProduct(lookDir, up);
            if (right.Length > 1e-10) right.Normalize(); else return;

            // Clean up perpendicular to both look and right
            up = Vector3D.CrossProduct(right, lookDir);
            if (up.Length > 1e-10) up.Normalize(); else return;

            double angleH = -dx * 0.5;
            double angleV = -dy * 0.5;

            offset = RotateVector(offset, up, angleH);
            offset = RotateVector(offset, right, angleV);

            camera.Position = lookAt + offset;
            camera.LookDirection = new Vector3D(-offset.X, -offset.Y, -offset.Z);

            // Rotate the up direction to stay consistent
            var newUp = RotateVector(camera.UpDirection, up, angleH);
            newUp = RotateVector(newUp, right, angleV);
            camera.UpDirection = newUp;

            // Back-compute _rotX/_rotY/_zoom so zoom and presets still work
            SyncTrackballAnglesFromCamera();

            // Update the axis indicator to match the new rotation
            UpdateAxisCamera();
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _zoom *= e.Delta > 0 ? 1.1 : 0.9;
            _zoom  = Math.Max(100, _zoom);
            ApplyCameraTransform(_lookAtX, _lookAtY, _lookAtZ);
        }

        private static Vector3D RotateVector(Vector3D v, Vector3D axis, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double dot = Vector3D.DotProduct(v, axis);
            Vector3D cross = Vector3D.CrossProduct(axis, v);
            return v * cos + cross * sin + axis * dot * (1 - cos);
        }

        private void SyncTrackballAnglesFromCamera()
        {
            Vector3D offset = camera.Position - new Point3D(_lookAtX, _lookAtY, _lookAtZ);
            _zoom = offset.Length;
            if (_zoom < 1e-10) return;
            double nx = offset.X / _zoom;
            double ny = offset.Y / _zoom;
            double nz = offset.Z / _zoom;
            _rotX = -Math.Asin(Math.Max(-1.0, Math.Min(1.0, -ny))) * 180.0 / Math.PI;
            double cosX = Math.Cos(_rotX * Math.PI / 180.0);
            if (Math.Abs(cosX) > 1e-6)
                _rotY = Math.Atan2(nx / cosX, nz / cosX) * 180.0 / Math.PI;
        }

        private void ApplyCameraTransform(double cx, double cy, double cz)
        {
            double radX = _rotX * Math.PI / 180.0;
            double radY = _rotY * Math.PI / 180.0;

            double camX = cx + _zoom * Math.Sin(radY) * Math.Cos(radX);
            double camY = cy - _zoom * Math.Sin(radX);
            double camZ = cz + _zoom * Math.Cos(radY) * Math.Cos(radX);

            _lookAtX = cx;
            _lookAtY = cy;
            _lookAtZ = cz;

            camera.Position      = new Point3D(camX, camY, camZ);
            camera.LookDirection = new Vector3D(cx - camX, cy - camY, cz - camZ);

            // Update the axis indicator camera to match the main camera rotation
            UpdateAxisCamera();
        }

        private void UpdateAxisCamera()
        {
            // Synchronize the axis viewport camera with the main camera's exact orientation
            // The axis camera looks at the origin from the same direction as the main camera
            var axisCamera = FindName("axisCamera") as PerspectiveCamera;
            if (axisCamera == null) return;

            // Get the main camera's look direction (normalized)
            Vector3D lookDir = camera.LookDirection;
            lookDir.Normalize();

            // Position the axis camera at a fixed distance from the origin,
            // in the opposite direction of where the main camera is looking
            const double axisCamDist = 3000.0;
            Point3D axisCamPos = new Point3D(-lookDir.X * axisCamDist, 
                                             -lookDir.Y * axisCamDist, 
                                             -lookDir.Z * axisCamDist);

            // The axis camera looks back toward the origin with the same orientation
            axisCamera.Position = axisCamPos;
            axisCamera.LookDirection = new Vector3D(lookDir.X, lookDir.Y, lookDir.Z);
            axisCamera.UpDirection = camera.UpDirection;

            // Update rotation readout
            var tbRotX = FindName("tbRotX") as System.Windows.Controls.TextBlock;
            var tbRotY = FindName("tbRotY") as System.Windows.Controls.TextBlock;
            var tbRotZ = FindName("tbRotZ") as System.Windows.Controls.TextBlock;
            if (tbRotX != null) tbRotX.Text = $"{_rotX:+0.0;-0.0;0.0}°";
            if (tbRotY != null) tbRotY.Text = $"{_rotY:+0.0;-0.0;0.0}°";
            if (tbRotZ != null)
            {
                Vector3D ld = camera.LookDirection;
                ld.Normalize();
                Vector3D worldZ = new Vector3D(0, 0, 1);
                Vector3D refUp = worldZ - Vector3D.DotProduct(worldZ, ld) * ld;
                double refLen = refUp.Length;
                double rotZ = 0.0;
                if (refLen > 1e-6)
                {
                    refUp /= refLen;
                    Vector3D actualUp = camera.UpDirection;
                    actualUp.Normalize();
                    double dot = Math.Max(-1.0, Math.Min(1.0, Vector3D.DotProduct(refUp, actualUp)));
                    double angle = Math.Acos(dot) * 180.0 / Math.PI;
                    Vector3D cross = Vector3D.CrossProduct(refUp, actualUp);
                    if (Vector3D.DotProduct(cross, ld) < 0) angle = -angle;
                    rotZ = angle;
                }
                tbRotZ.Text = $"{rotZ:+0.0;-0.0;0.0}°";
            }
        }
    }
}

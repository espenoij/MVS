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

        // Camera + trackball: all state lives inside the controller.
        private TrackballCameraController _trackball;

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

            // Trackball controller drives the main camera and (optionally) the axis camera.
            _trackball = new TrackballCameraController(camera,
                () => viewport3D.ActualWidth / Math.Max(viewport3D.ActualHeight, 1.0));
            _trackball.AttachAxisCamera(FindName("axisCamera") as PerspectiveCamera);
            _trackball.SetRotationChangedCallback(UpdateRotationReadout);

            // Initialize the 3D axis indicators in their separate viewport
            InitializeAxisIndicators();

            // Explicitly set the camera
            // is fully initialised before the first scan renders any points.
            _trackball.ApplyCameraTransform(0, 0, 0);
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
                _trackball.SetCentroid(cx, cy, cz);
                _trackball.SetDefaultZoom(
                    _trackball.ComputeFitZoom(Math.Max(maxHalfX + 200, 1000), Math.Max(maxHalfY + 200, 1000)));
                _trackball.ApplyTopDownView();
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
            double defaultZoom = _trackball.ComputeFitZoom(ep, es);
            if (defaultZoom < 1000) defaultZoom = 1000;

            _trackball.SetFitContext(
                fit.CentroidX, fit.CentroidY, fit.CentroidZ,
                fit.NormalX, fit.NormalY, fit.NormalZ,
                fit.VesselForwardX, fit.VesselForwardY, fit.VesselForwardZ,
                defaultZoom);

            _trackball.ApplyPerspectiveView(_vm.PerspectiveRotX, _vm.PerspectiveRotY);
            _cameraFitted = true;
        }

        // ── Trackball mouse handlers

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _trackball.ApplyTopDownView();
        }

        private void PerspectiveView_Click(object sender, RoutedEventArgs e)
        {
            _trackball.ApplyPerspectiveView(_vm.PerspectiveRotX, _vm.PerspectiveRotY);
        }

        // -- Trackball mouse handlers ------------------------------------------

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _trackball.OnMouseLeftButtonDown((IInputElement)sender, e);
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _trackball.OnMouseLeftButtonUp((IInputElement)sender, e);
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            _trackball.OnMouseMove((IInputElement)sender, e);
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _trackball.OnMouseWheel(e);
        }

        private void UpdateRotationReadout(double rotX, double rotY, double rotZ)
        {
            var tbRotX = FindName("tbRotX") as System.Windows.Controls.TextBlock;
            var tbRotY = FindName("tbRotY") as System.Windows.Controls.TextBlock;
            var tbRotZ = FindName("tbRotZ") as System.Windows.Controls.TextBlock;
            if (tbRotX != null) tbRotX.Text = $"{rotX:+0.0;-0.0;0.0}°";
            if (tbRotY != null) tbRotY.Text = $"{rotY:+0.0;-0.0;0.0}°";
            if (tbRotZ != null) tbRotZ.Text = $"{rotZ:+0.0;-0.0;0.0}°";
        }
    }
}

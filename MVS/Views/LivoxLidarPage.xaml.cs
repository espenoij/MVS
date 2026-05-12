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

        // 3D scene visuals are owned by the renderer.
        private LidarSceneRenderer _scene;

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
                            _scene.SetMaterials(_materials);

                            // Re-render point cloud
                            if (_lastPointCloud != null)
                            {
                                UpdatePointCloud(_lastPointCloud);
                            }

                            // Re-render fit visuals (plane and arrows)
                            if (_lastFitResult != null && _lastFitResult.IsValid)
                            {
                                _scene.RenderPlane(_lastFitResult);
                                LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                                    out double arrowLen, out double shaftR);
                                _scene.RenderFitArrows(_lastFitResult, arrowLen, shaftR);
                            }

                            // Re-render edge visuals
                            if (_lastEdgeResult != null && _lastEdgeResult.IsValid)
                            {
                                LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                                    out double arrowLen2, out double shaftR2);
                                _scene.RenderDeckEdge(_lastEdgeResult, arrowLen2, shaftR2);
                            }

                            // Re-render axis indicators
                            _scene.RenderAxisIndicators();
                        }));
                }
            };
            // Initialise the cached materials from the current VM setting.
            _materials = new MaterialFactory(vm.EnableEmissiveColors);

            // Renderer owns the 3D visuals and exposes high-level Render* methods.
            _scene = new LidarSceneRenderer(pointCloudVisual, planeVisual, _materials);
            _scene.AttachToViewport(viewport3D);
            _scene.SetAxisIndicatorVisuals(
                FindName("axisXVisual") as ModelVisual3D,
                FindName("axisYVisual") as ModelVisual3D,
                FindName("axisZVisual") as ModelVisual3D);

            // Trackball controller drives the main camera and (optionally) the axis camera.
            _trackball = new TrackballCameraController(camera,
                () => viewport3D.ActualWidth / Math.Max(viewport3D.ActualHeight, 1.0));
            _trackball.AttachAxisCamera(FindName("axisCamera") as PerspectiveCamera);
            _trackball.SetRotationChangedCallback(UpdateRotationReadout);

            // Initialize the 3D axis indicators in their separate viewport
            _scene.RenderAxisIndicators();

            // Explicitly set the camera so it is fully initialised before the first scan renders any points.
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
            _scene.ClearAll();
        }

        private void OnFitResultReady(LivoxLidarPlaneFitResult fit)
        {
            if (!fit.IsValid) return;

            var snapshot = _vm.GetPointCloudSnapshot();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    UpdatePointCloud(snapshot);
                    UpdateScene(fit);
                    _vm.AppendStatus($"Plane fit — Pitch: {fit.PitchDeg:+0.00;-0.00;0.00}°  Roll: {fit.RollDeg:+0.00;-0.00;0.00}°  RMSE: {fit.FitRmse:0.0} mm  Points: {fit.PointCount:N0}");
                }));
        }

        private void UpdateScene(LivoxLidarPlaneFitResult fit)
        {
            _lastFitResult = fit;
            _scene.RenderPlane(fit);
            LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                out double arrowLen, out double shaftR);
            _scene.RenderFitArrows(fit, arrowLen, shaftR);
            UpdateCamera(fit);

            // Clear previous deck edge visuals
            _scene.ClearDeckEdge();
        }

        /// <summary>
        /// Rebuild the point cloud mesh from a list of points.
        /// Called by MainWindow after a scan is stopped so the view reflects the full buffer.
        /// </summary>
        public void UpdatePointCloud(List<(float x, float y, float z)> points)
        {
            if (points == null || points.Count == 0)
            {
                _scene.RenderPointCloud(null, 0);
                _lastPointCloud = null;
                return;
            }

            _lastPointCloud = points;

            int maxDisplay = _vm != null ? Math.Max(_vm.MaxDisplayPoints, 100) : 20_000;
            var meshResult = _scene.RenderPointCloud(points, maxDisplay);
            if (meshResult == null) return;

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

        // ── Deck edge result → 3D scene update ───────────────────────────────

        private void OnDeckEdgeResultReady(LivoxLidarDeckEdgeResult edge)
        {
            if (!edge.IsValid) return;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    _lastEdgeResult = edge;
                    LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                        out double arrowLen, out double shaftR);
                    _scene.RenderDeckEdge(edge, arrowLen, shaftR);
                }));
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

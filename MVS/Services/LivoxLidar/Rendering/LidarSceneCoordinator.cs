using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MVS
{
    /// <summary>
    /// Coordinates the LiDAR ViewModel events with the 3D scene renderer and
    /// the trackball camera controller. Owns the per-render state
    /// (last point cloud / fit / edge results, camera-fitted flag, material
    /// cache) so that the page code-behind can be reduced to pure WPF glue.
    /// </summary>
    internal class LidarSceneCoordinator
    {
        private readonly LivoxLidarVM _vm;
        private readonly LidarSceneRenderer _scene;
        private readonly TrackballCameraController _trackball;
        private readonly Dispatcher _dispatcher;
        private readonly Action _onSimulationStarted;

        private MaterialFactory _materials;

        private bool _cameraFitted;
        private List<(float x, float y, float z)> _lastPointCloud;
        private LivoxLidarPlaneFitResult _lastFitResult;
        private LivoxLidarDeckEdgeResult _lastEdgeResult;

        internal LidarSceneCoordinator(
            LivoxLidarVM vm,
            LidarSceneRenderer scene,
            TrackballCameraController trackball,
            MaterialFactory materials,
            Dispatcher dispatcher,
            Action onSimulationStarted)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _trackball = trackball ?? throw new ArgumentNullException(nameof(trackball));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _onSimulationStarted = onSimulationStarted;
        }

        /// <summary>
        /// Subscribes to all VM events that drive the 3D scene. Call once
        /// during page initialization.
        /// </summary>
        internal void Attach()
        {
            _vm.FitResultReady += OnFitResultReady;
            _vm.DeckEdgeResultReady += OnDeckEdgeResultReady;
            _vm.ScanCleared += OnScanCleared;
            _vm.PointCloudUpdated += UpdatePointCloud;
            _vm.PropertyChanged += OnVmPropertyChanged;

            if (_onSimulationStarted != null)
                _vm.SimulationStarted += () => _dispatcher.BeginInvoke(_onSimulationStarted);
        }

        // ── Material cache rebuild on emissive toggle ────────────────────────

        private void OnVmPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LivoxLidarVM.EnableEmissiveColors)) return;

            _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                _materials = new MaterialFactory(_vm.EnableEmissiveColors);
                _scene.SetMaterials(_materials);

                if (_lastPointCloud != null)
                    UpdatePointCloud(_lastPointCloud);

                if (_lastFitResult != null && _lastFitResult.IsValid)
                {
                    _scene.RenderPlane(_lastFitResult);
                    LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                        out double arrowLen, out double shaftR);
                    _scene.RenderFitArrows(_lastFitResult, arrowLen, shaftR);
                }

                if (_lastEdgeResult != null && _lastEdgeResult.IsValid)
                {
                    LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                        out double arrowLen2, out double shaftR2);
                    _scene.RenderDeckEdge(_lastEdgeResult, arrowLen2, shaftR2);
                }

                _scene.RenderAxisIndicators();
            }));
        }

        // ── Scan cleared ─────────────────────────────────────────────────────

        private void OnScanCleared()
        {
            if (_dispatcher.CheckAccess())
                ClearScene();
            else
                _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ClearScene));
        }

        private void ClearScene()
        {
            _cameraFitted = false;
            _lastPointCloud = null;
            _lastFitResult = null;
            _lastEdgeResult = null;
            _scene.ClearAll();
        }

        // ── Fit result → 3D scene update ─────────────────────────────────────

        private void OnFitResultReady(LivoxLidarPlaneFitResult fit)
        {
            if (!fit.IsValid) return;

            var snapshot = _vm.GetPointCloudSnapshot();
            _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
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
            _scene.ClearDeckEdge();
        }

        // ── Deck edge result → 3D scene update ───────────────────────────────

        private void OnDeckEdgeResultReady(LivoxLidarDeckEdgeResult edge)
        {
            if (!edge.IsValid) return;
            _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                _lastEdgeResult = edge;
                LidarSceneRenderer.GetArrowDimensions(_lastFitResult, _lastEdgeResult,
                    out double arrowLen, out double shaftR);
                _scene.RenderDeckEdge(edge, arrowLen, shaftR);
            }));
        }

        // ── Point cloud rebuild (also called externally from MainWindow) ─────

        internal void UpdatePointCloud(List<(float x, float y, float z)> points)
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

            // Auto-fit the camera the first time points are shown so the cloud
            // is visible before the user runs Analyse.
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

        // ── Camera fit ───────────────────────────────────────────────────────

        private void UpdateCamera(LivoxLidarPlaneFitResult fit)
        {
            // Derive zoom from the plane's extents so the whole surface fills
            // the view, accounting for both horizontal and vertical FOV based
            // on viewport aspect ratio.
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
    }
}

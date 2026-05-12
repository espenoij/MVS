using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace MVS
{
    public partial class LivoxLidarPage : System.Windows.Controls.UserControl
    {
        // Camera + trackball: all state lives inside the controller.
        private TrackballCameraController _trackball;

        // 3D scene visuals are owned by the renderer.
        private LidarSceneRenderer _scene;

        // VM ↔ scene/camera glue lives in the coordinator.
        private LidarSceneCoordinator _coordinator;

        private LivoxLidarVM _vm;

        public LivoxLidarPage()
        {
            InitializeComponent();
        }

        public void Init(LivoxLidarVM vm)
        {
            _vm = vm;
            DataContext = vm;

            // Cached brushes/materials. Rebuilt by the coordinator when EnableEmissiveColors changes.
            var materials = new MaterialFactory(vm.EnableEmissiveColors);

            // Renderer owns the 3D visuals and exposes high-level Render* methods.
            _scene = new LidarSceneRenderer(pointCloudVisual, planeVisual, materials);
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

            // Coordinator wires VM events to the renderer / trackball.
            _coordinator = new LidarSceneCoordinator(vm, _scene, _trackball, materials, Dispatcher,
                onSimulationStarted: SwitchToSimulationTab);
            _coordinator.Attach();

            // Initialize the 3D axis indicators in their separate viewport
            _scene.RenderAxisIndicators();

            // Explicitly set the camera so it is fully initialised before the first scan renders any points.
            _trackball.ApplyCameraTransform(0, 0, 0);
        }

        /// <summary>
        /// Rebuild the point cloud mesh from a list of points.
        /// Called by MainWindow after a scan is stopped so the view reflects the full buffer.
        /// </summary>
        public void UpdatePointCloud(List<(float x, float y, float z)> points)
        {
            _coordinator?.UpdatePointCloud(points);
        }

        private void SwitchToSimulationTab()
        {
            var tc = FindName("tabControl") as Telerik.Windows.Controls.RadTabControl;
            if (tc != null) tc.SelectedIndex = 1;
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

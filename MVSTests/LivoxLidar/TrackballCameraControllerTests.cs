using System;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.LivoxLidar
{
    /// <summary>
    /// Focused integration tests for <see cref="TrackballCameraController"/>.
    /// WPF camera objects require an STA thread.
    /// </summary>
    [TestClass]
    public class TrackballCameraControllerTests
    {
        [TestMethod]
        public void DefaultZoom_BeforeAnyView_IsInitial()
        {
            StaTestHelper.Run(() =>
            {
                var cam = new PerspectiveCamera { FieldOfView = 60 };
                var controller = new TrackballCameraController(cam, () => 1.0);
                Assert.AreEqual(15000.0, controller.Zoom, 1e-9);
                Assert.AreEqual(0.0, controller.RotX, 1e-9);
                Assert.AreEqual(0.0, controller.RotY, 1e-9);
            });
        }

        [TestMethod]
        public void ApplyTopDownView_PositionsCameraAlongFitNormal()
        {
            StaTestHelper.Run(() =>
            {
                var cam = new PerspectiveCamera { FieldOfView = 60 };
                var controller = new TrackballCameraController(cam, () => 1.0);

                controller.SetFitContext(
                    centroidX: 0, centroidY: 0, centroidZ: 0,
                    normalX: 0, normalY: 0, normalZ: 1,
                    vesselFwdX: 1, vesselFwdY: 0, vesselFwdZ: 0,
                    defaultZoom: 5000);

                controller.ApplyTopDownView();

                Assert.IsTrue(cam.Position.Z > 0,
                    $"Top-down camera Z should be positive (was {cam.Position.Z}).");
                Assert.IsTrue(cam.LookDirection.Z < 0,
                    "Top-down camera should look in −Z.");
                Assert.AreEqual(1.0, cam.UpDirection.X, 1e-6);
                Assert.AreEqual(0.0, cam.UpDirection.Y, 1e-6);
                Assert.AreEqual(0.0, cam.UpDirection.Z, 1e-6);
                Assert.AreEqual(5000.0, controller.Zoom, 1e-6);
            });
        }

        [TestMethod]
        public void ApplyPerspectiveView_ProducesNonTopDownPosition()
        {
            StaTestHelper.Run(() =>
            {
                var cam = new PerspectiveCamera { FieldOfView = 60 };
                var controller = new TrackballCameraController(cam, () => 1.0);
                controller.SetFitContext(0, 0, 0, 0, 0, 1, 1, 0, 0, defaultZoom: 5000);

                controller.ApplyPerspectiveView(extraRotXDeg: -45, extraRotYDeg: 0);

                double horizontalDist = Math.Sqrt(cam.Position.X * cam.Position.X +
                                                  cam.Position.Y * cam.Position.Y);
                Assert.IsTrue(horizontalDist > 1.0,
                    $"Perspective view should offset the camera horizontally (was {horizontalDist}).");
            });
        }

        [TestMethod]
        public void ComputeFitZoom_DelegatesToCameraMath()
        {
            StaTestHelper.Run(() =>
            {
                var cam = new PerspectiveCamera { FieldOfView = 60 };
                var controller = new TrackballCameraController(cam, () => 1.0);

                double zoom = controller.ComputeFitZoom(1000, 1000);
                double expected = CameraMath.ComputeFitZoom(1000, 1000, 60, 1.0);
                Assert.AreEqual(expected, zoom, 1e-9);
            });
        }
    }
}

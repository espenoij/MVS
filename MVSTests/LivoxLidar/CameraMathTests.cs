using System;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class CameraMathTests
    {
        private const double Tol = 1e-6;

        // ── ComputeFitZoom ───────────────────────────────────────────────────

        [TestMethod]
        public void ComputeFitZoom_SquareAspectAndEqualExtents_HorizontalAndVerticalAgree()
        {
            // For aspect = 1, vFov == hFov, so zoomH == zoomV.
            double zoom = CameraMath.ComputeFitZoom(
                halfExtentH: 1000, halfExtentV: 1000,
                horizontalFovDeg: 60, aspectRatio: 1.0, margin: 1.0);

            // tan(30°) ≈ 0.5773502691896
            double expected = 1000.0 / Math.Tan(30.0 * Math.PI / 180.0);
            Assert.AreEqual(expected, zoom, 1e-6);
        }

        [TestMethod]
        public void ComputeFitZoom_LargerHorizontalExtent_ReturnsHorizontalLimit()
        {
            double zoom = CameraMath.ComputeFitZoom(
                halfExtentH: 5000, halfExtentV: 100,
                horizontalFovDeg: 60, aspectRatio: 1.0, margin: 1.0);

            double expected = 5000.0 / Math.Tan(30.0 * Math.PI / 180.0);
            Assert.AreEqual(expected, zoom, 1e-6);
        }

        [TestMethod]
        public void ComputeFitZoom_TallViewport_VerticalFovDominates()
        {
            // aspect < 1 (tall viewport) → vertical FOV is LARGER than horizontal FOV
            // (vFov = atan(tan(hFov)/aspect)), so the same vertical extent fits at a
            // SMALLER camera distance than with a square viewport.
            double zoomTall = CameraMath.ComputeFitZoom(
                halfExtentH: 100, halfExtentV: 1000,
                horizontalFovDeg: 60, aspectRatio: 0.5, margin: 1.0);
            double zoomSquare = CameraMath.ComputeFitZoom(
                halfExtentH: 100, halfExtentV: 1000,
                horizontalFovDeg: 60, aspectRatio: 1.0, margin: 1.0);

            Assert.IsTrue(zoomTall < zoomSquare,
                $"Tall viewport zoom should be less than square viewport zoom (tall={zoomTall}, square={zoomSquare}).");
        }

        [TestMethod]
        public void ComputeFitZoom_AppliesMargin()
        {
            double noMargin = CameraMath.ComputeFitZoom(1000, 1000, 60, 1.0, 1.0);
            double withMargin = CameraMath.ComputeFitZoom(1000, 1000, 60, 1.0, 1.5);
            Assert.AreEqual(noMargin * 1.5, withMargin, 1e-6);
        }

        [TestMethod]
        public void ComputeFitZoom_ZeroAspect_TreatedAsOne()
        {
            double zoomZero = CameraMath.ComputeFitZoom(1000, 1000, 60, 0.0, 1.0);
            double zoomOne  = CameraMath.ComputeFitZoom(1000, 1000, 60, 1.0, 1.0);
            Assert.AreEqual(zoomOne, zoomZero, 1e-6);
        }

        // ── RotateVector ─────────────────────────────────────────────────────

        [TestMethod]
        public void RotateVector_ZeroAngle_ReturnsInput()
        {
            var v = new Vector3D(1, 2, 3);
            var rotated = CameraMath.RotateVector(v, new Vector3D(0, 0, 1), 0);
            AssertVectorsEqual(v, rotated, Tol);
        }

        [TestMethod]
        public void RotateVector_90DegAroundZ_MapsXToY()
        {
            var rotated = CameraMath.RotateVector(new Vector3D(1, 0, 0), new Vector3D(0, 0, 1), 90);
            AssertVectorsEqual(new Vector3D(0, 1, 0), rotated, Tol);
        }

        [TestMethod]
        public void RotateVector_RoundTripRecoversInput()
        {
            var v = new Vector3D(0.3, -0.7, 1.1);
            var axis = new Vector3D(0, 1, 0);
            var rotated = CameraMath.RotateVector(v, axis, 37.5);
            var back    = CameraMath.RotateVector(rotated, axis, -37.5);
            AssertVectorsEqual(v, back, 1e-9);
        }

        // ── RotationFromNormal ───────────────────────────────────────────────

        [TestMethod]
        public void RotationFromNormal_VerticalNormal_ZeroAngles()
        {
            CameraMath.RotationFromNormal(0, 0, 1, out double rx, out double ry);
            Assert.AreEqual(0, rx, 1e-9);
            Assert.AreEqual(0, ry, 1e-9);
        }

        [TestMethod]
        public void RotationFromNormal_NormalizesInput()
        {
            CameraMath.RotationFromNormal(0, 0, 5, out double rxScaled, out double ryScaled);
            CameraMath.RotationFromNormal(0, 0, 1, out double rxUnit, out double ryUnit);
            Assert.AreEqual(rxUnit, rxScaled, 1e-9);
            Assert.AreEqual(ryUnit, ryScaled, 1e-9);
        }

        [TestMethod]
        public void RotationFromNormal_ZeroVector_FallsBackToVertical()
        {
            CameraMath.RotationFromNormal(0, 0, 0, out double rx, out double ry);
            Assert.AreEqual(0, rx, 1e-9);
            Assert.AreEqual(0, ry, 1e-9);
        }

        // ── CameraPositionFromAngles ─────────────────────────────────────────

        [TestMethod]
        public void CameraPositionFromAngles_ZeroAngles_OnPositiveZ()
        {
            var pos = CameraMath.CameraPositionFromAngles(10, 20, 30, 0, 0, 100);
            AssertPointEqual(new Point3D(10, 20, 30 + 100), pos, Tol);
        }

        [TestMethod]
        public void CameraPositionFromAngles_LiesOnSphereOfRadiusZoom()
        {
            var centre = new Point3D(123, -45, 67);
            var pos = CameraMath.CameraPositionFromAngles(centre.X, centre.Y, centre.Z, 32, 47, 250);
            double dist = (pos - centre).Length;
            Assert.AreEqual(250.0, dist, 1e-6);
        }

        // ── SyncAnglesFromOffset ─────────────────────────────────────────────

        [TestMethod]
        public void SyncAnglesFromOffset_RoundTripRecoversAngles()
        {
            // Pick angles in the principal range where the inverse is well defined.
            const double cx = 0, cy = 0, cz = 0;
            const double rotX = 25, rotY = -40, zoom = 500;

            var pos = CameraMath.CameraPositionFromAngles(cx, cy, cz, rotX, rotY, zoom);
            var offset = pos - new Point3D(cx, cy, cz);
            CameraMath.SyncAnglesFromOffset(offset, out double rxBack, out double ryBack, out double zoomBack);

            Assert.AreEqual(zoom, zoomBack, 1e-6);
            Assert.AreEqual(rotX, rxBack, 1e-3);
            Assert.AreEqual(rotY, ryBack, 1e-3);
        }

        [TestMethod]
        public void SyncAnglesFromOffset_ZeroVector_AllZeros()
        {
            CameraMath.SyncAnglesFromOffset(new Vector3D(0, 0, 0),
                out double rx, out double ry, out double zoom);
            Assert.AreEqual(0, zoom, 1e-12);
            Assert.AreEqual(0, rx, 1e-12);
            Assert.AreEqual(0, ry, 1e-12);
        }

        // ── RollFreeUp / ComputeRollAngleDeg ─────────────────────────────────

        [TestMethod]
        public void RollFreeUp_NonVerticalLook_OrthogonalToLook()
        {
            var look = new Vector3D(1, 0, -0.3);
            var up = CameraMath.RollFreeUp(look);
            Assert.IsTrue(up.HasValue);
            double dot = Vector3D.DotProduct(up.Value, look);
            Assert.AreEqual(0, dot, 1e-9);
            Assert.AreEqual(1.0, up.Value.Length, 1e-9);
        }

        [TestMethod]
        public void RollFreeUp_LookAlongZ_ReturnsNull()
        {
            Assert.IsNull(CameraMath.RollFreeUp(new Vector3D(0, 0, 1)));
            Assert.IsNull(CameraMath.RollFreeUp(new Vector3D(0, 0, -1)));
        }

        [TestMethod]
        public void ComputeRollAngleDeg_RollFreeUp_ReturnsZero()
        {
            var look = new Vector3D(1, 0.2, -0.3);
            var up = CameraMath.RollFreeUp(look).Value;
            double roll = CameraMath.ComputeRollAngleDeg(look, up);
            Assert.AreEqual(0, roll, 1e-9);
        }

        [TestMethod]
        public void ComputeRollAngleDeg_RotatedUp_MatchesAppliedAngle()
        {
            var look = new Vector3D(1, 0, 0);
            var up   = CameraMath.RollFreeUp(look).Value;     // = (0, 0, 1)
            var rotated = CameraMath.RotateVector(up, look, 30);
            double roll = CameraMath.ComputeRollAngleDeg(look, rotated);
            Assert.AreEqual(30, Math.Abs(roll), 1e-6);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void AssertVectorsEqual(Vector3D expected, Vector3D actual, double tol)
        {
            Assert.AreEqual(expected.X, actual.X, tol, "X mismatch");
            Assert.AreEqual(expected.Y, actual.Y, tol, "Y mismatch");
            Assert.AreEqual(expected.Z, actual.Z, tol, "Z mismatch");
        }

        private static void AssertPointEqual(Point3D expected, Point3D actual, double tol)
        {
            Assert.AreEqual(expected.X, actual.X, tol, "X mismatch");
            Assert.AreEqual(expected.Y, actual.Y, tol, "Y mismatch");
            Assert.AreEqual(expected.Z, actual.Z, tol, "Z mismatch");
        }
    }
}

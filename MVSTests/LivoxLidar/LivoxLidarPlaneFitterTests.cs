using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class LivoxLidarPlaneFitterTests
    {
        // Build a roughly square deck of points lying in z = 0, optionally tilted
        // about the X axis (pitch) or Y axis (roll). Sensor sits at the origin
        // looking down (deck below the sensor in production), but the algorithm
        // is geometric so the orientation is what matters here.
        private static List<(float x, float y, float z)> BuildDeck(
            int gridSize = 20,
            double extentMm = 5000,
            double pitchDeg = 0,
            double rollDeg = 0,
            double offsetZ = -1000)
        {
            var pts = new List<(float, float, float)>(gridSize * gridSize);
            double pitchRad = pitchDeg * Math.PI / 180.0;
            double rollRad  = rollDeg  * Math.PI / 180.0;

            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    double x = -extentMm + 2 * extentMm * i / (gridSize - 1);
                    double y = -extentMm + 2 * extentMm * j / (gridSize - 1);
                    // Plane tilt: z = x*tan(pitch) + y*tan(roll)
                    double z = offsetZ + x * Math.Tan(pitchRad) + y * Math.Tan(rollRad);
                    pts.Add(((float)x, (float)y, (float)z));
                }
            }
            return pts;
        }

        [TestMethod]
        public void Fit_TooFewPoints_ReturnsInvalid()
        {
            var pts = new List<(float, float, float)> { (0, 0, 0), (1, 0, 0) };
            var r = LivoxLidarPlaneFitter.Fit(pts);
            Assert.IsFalse(r.IsValid);
        }

        [TestMethod]
        public void Fit_FlatDeck_ReportsZeroPitchAndRoll()
        {
            var r = LivoxLidarPlaneFitter.Fit(BuildDeck());
            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(0.0, r.PitchDeg, 0.05);
            Assert.AreEqual(0.0, r.RollDeg,  0.05);
            Assert.IsTrue(r.FitRmse < 1.0, "Flat deck should fit with near-zero RMSE.");
        }

        [TestMethod]
        public void Fit_FlatDeck_NormalPointsTowardSensor()
        {
            var r = LivoxLidarPlaneFitter.Fit(BuildDeck(offsetZ: -1000));
            Assert.IsTrue(r.IsValid);
            // Normal must be unit length and oriented +Z (toward sensor at origin).
            double len = Math.Sqrt(r.NormalX * r.NormalX + r.NormalY * r.NormalY + r.NormalZ * r.NormalZ);
            Assert.AreEqual(1.0, len, 1e-6);
            Assert.IsTrue(r.NormalZ > 0);
        }

        [TestMethod]
        public void Fit_ClearanceMatchesPlaneOffset()
        {
            var r = LivoxLidarPlaneFitter.Fit(BuildDeck(offsetZ: -1500));
            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(1500.0, r.ClearanceMm, 1.0);
        }

        [TestMethod]
        public void Fit_PitchOnlyDeck_RecoversInclination()
        {
            // The split between pitch and roll depends on the estimated vessel-forward
            // direction (bow-edge PCA), which is ambiguous on a perfectly square deck.
            // The total inclination magnitude, however, is a stable invariant.
            var r = LivoxLidarPlaneFitter.Fit(BuildDeck(pitchDeg: 5));
            Assert.IsTrue(r.IsValid);
            double inc = Math.Sqrt(r.PitchDeg * r.PitchDeg + r.RollDeg * r.RollDeg);
            Assert.AreEqual(5.0, inc, 0.5);
        }

        [TestMethod]
        public void Fit_RollOnlyDeck_RecoversInclination()
        {
            var r = LivoxLidarPlaneFitter.Fit(BuildDeck(rollDeg: -3));
            Assert.IsTrue(r.IsValid);
            double inc = Math.Sqrt(r.PitchDeg * r.PitchDeg + r.RollDeg * r.RollDeg);
            Assert.AreEqual(3.0, inc, 0.5);
        }

        [TestMethod]
        public void Fit_RecordsCentroidAndPointCount()
        {
            var pts = BuildDeck(gridSize: 10, offsetZ: -2000);
            var r = LivoxLidarPlaneFitter.Fit(pts);
            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(pts.Count, r.PointCount);
            Assert.AreEqual(0.0,    r.CentroidX, 1.0);
            Assert.AreEqual(0.0,    r.CentroidY, 1.0);
            Assert.AreEqual(-2000.0, r.CentroidZ, 1.0);
        }

        [TestMethod]
        public void Fit_VesselForwardIsUnitLengthAndForwardish()
        {
            var r = LivoxLidarPlaneFitter.Fit(BuildDeck());
            Assert.IsTrue(r.IsValid);
            double len = Math.Sqrt(r.VesselForwardX * r.VesselForwardX +
                                   r.VesselForwardY * r.VesselForwardY +
                                   r.VesselForwardZ * r.VesselForwardZ);
            Assert.AreEqual(1.0, len, 1e-6);
            Assert.IsTrue(r.VesselForwardX >= 0,
                "Vessel forward should always be in the +X half-space by convention.");
        }

        [TestMethod]
        public void FilterPlaneInliers_EmptyOrTinyInput_ReturnsInputUnchanged()
        {
            var tiny = new List<(float, float, float)> { (0, 0, 0), (1, 1, 1) };
            var result = LivoxLidarPlaneFitter.FilterPlaneInliers(tiny);
            Assert.AreSame(tiny, result);
        }

        [TestMethod]
        public void FilterPlaneInliers_RemovesGrossOutliersAboveDeck()
        {
            var pts = BuildDeck(gridSize: 25);  // 625 inliers
            // Add 30 obvious outliers ~3 m above the deck
            var rng = new Random(1);
            for (int i = 0; i < 30; i++)
                pts.Add(((float)(rng.NextDouble() * 1000),
                         (float)(rng.NextDouble() * 1000),
                         3000));

            int before = pts.Count;
            var filtered = LivoxLidarPlaneFitter.FilterPlaneInliers(pts);
            Assert.IsTrue(filtered.Count < before,
                $"Filter should drop outliers (before={before}, after={filtered.Count}).");
            Assert.IsTrue(filtered.Count >= 600,
                "Filter should retain (most of) the 625 deck inliers.");
        }
    }
}

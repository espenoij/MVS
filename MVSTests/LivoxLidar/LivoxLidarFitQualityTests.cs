using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.LivoxLidar
{
    [TestClass]
    public class LivoxLidarFitQualityTests
    {
        [DataTestMethod]
        [DataRow(-1.0,  FitRmseQuality.Unknown)]
        [DataRow(double.NaN, FitRmseQuality.Unknown)]
        [DataRow(0.0,   FitRmseQuality.Good)]
        [DataRow(14.999, FitRmseQuality.Good)]
        [DataRow(15.0,  FitRmseQuality.Acceptable)]
        [DataRow(29.999, FitRmseQuality.Acceptable)]
        [DataRow(30.0,  FitRmseQuality.Marginal)]
        [DataRow(59.999, FitRmseQuality.Marginal)]
        [DataRow(60.0,  FitRmseQuality.Bad)]
        [DataRow(500.0, FitRmseQuality.Bad)]
        public void Classify_BandsAreContiguousAndInclusiveAtLowerEdge(double rmseMm, FitRmseQuality expected)
        {
            Assert.AreEqual(expected, LivoxLidarFitQuality.Classify(rmseMm));
        }

        [TestMethod]
        public void Label_ReturnsDashForUnknown()
        {
            Assert.AreEqual("—", LivoxLidarFitQuality.Label(FitRmseQuality.Unknown));
        }

        [TestMethod]
        public void Label_ReturnsHumanReadableForKnownBands()
        {
            Assert.AreEqual("Good",       LivoxLidarFitQuality.Label(FitRmseQuality.Good));
            Assert.AreEqual("Acceptable", LivoxLidarFitQuality.Label(FitRmseQuality.Acceptable));
            Assert.AreEqual("Marginal",   LivoxLidarFitQuality.Label(FitRmseQuality.Marginal));
            Assert.AreEqual("Bad",        LivoxLidarFitQuality.Label(FitRmseQuality.Bad));
        }

        [TestMethod]
        public void Correction_ExposesQualityWhenActive()
        {
            var c = new LivoxLidarCorrection();
            c.Apply(0, 0, 0, fitRmse: 5.0, pointCount: 100);
            Assert.AreEqual(FitRmseQuality.Good, c.FitRmseQuality);
            Assert.AreEqual("Good", c.FitRmseQualityString);

            c.Apply(0, 0, 0, fitRmse: 45.0, pointCount: 100);
            Assert.AreEqual(FitRmseQuality.Marginal, c.FitRmseQuality);
            Assert.AreEqual("Marginal", c.FitRmseQualityString);
        }

        [TestMethod]
        public void Correction_ReportsUnknownWhenInactive()
        {
            var c = new LivoxLidarCorrection();
            Assert.AreEqual(FitRmseQuality.Unknown, c.FitRmseQuality);
            Assert.AreEqual("—", c.FitRmseQualityString);
        }
    }
}

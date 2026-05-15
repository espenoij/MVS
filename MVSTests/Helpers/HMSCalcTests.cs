using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MVSTests.Helpers
{
    [TestClass]
    public class HMSCalcTests
    {
        private const double Tol = 1e-9;

        [TestMethod]
        public void ToRadians_180_EqualsPi()
        {
            Assert.AreEqual(Math.PI, 180.0.ToRadians(), Tol);
        }

        [TestMethod]
        public void ToDegrees_Pi_Equals180()
        {
            Assert.AreEqual(180.0, Math.PI.ToDegrees(), Tol);
        }

        [TestMethod]
        public void ToDegreesAndToRadians_RoundTrip()
        {
            double original = 42.5;
            double back = original.ToRadians().ToDegrees();
            Assert.AreEqual(original, back, Tol);
        }

        [TestMethod]
        public void Inclination_ZeroPitchAndRoll_IsZero()
        {
            Assert.AreEqual(0.0, HMSCalc.Inclination(0, 0), Tol);
        }

        [TestMethod]
        public void Inclination_PitchOnly_EqualsAbsPitch()
        {
            Assert.AreEqual(15.0, HMSCalc.Inclination(15, 0), 1e-9);
            Assert.AreEqual(15.0, HMSCalc.Inclination(-15, 0), 1e-9);
        }

        [TestMethod]
        public void Inclination_RollOnly_EqualsAbsRoll()
        {
            Assert.AreEqual(20.0, HMSCalc.Inclination(0, 20), 1e-9);
            Assert.AreEqual(20.0, HMSCalc.Inclination(0, -20), 1e-9);
        }

        [TestMethod]
        public void Inclination_EqualPitchAndRoll_Symmetric()
        {
            Assert.AreEqual(HMSCalc.Inclination(10, 10), HMSCalc.Inclination(-10, -10), 1e-12);
        }

        [TestMethod]
        public void KnotsToMS_RoundTripsToMStoKnots()
        {
            double original = 12.5;
            Assert.AreEqual(original, HMSCalc.MStoKnots(HMSCalc.KnotsToMS(original)), 1e-9);
        }

        [TestMethod]
        public void KnotsToMS_OneKnot_EqualsFactor()
        {
            Assert.AreEqual(HMSCalc.KnotsToMSFactor, HMSCalc.KnotsToMS(1.0), 1e-12);
        }
    }
}

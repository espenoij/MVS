using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Services
{
    [TestClass]
    public class LivoxLidarCorrectionTests
    {
        [TestMethod]
        public void NewInstance_IsInactive_AndStringsShowDash()
        {
            var c = new LivoxLidarCorrection();
            Assert.IsFalse(c.IsActive);
            Assert.AreEqual("Not active", c.StatusString);
            Assert.AreEqual("—", c.PitchOffsetString);
            Assert.AreEqual("—", c.RollOffsetString);
            Assert.AreEqual("—", c.HeadingOffsetString);
            Assert.AreEqual("—", c.FitRmseString);
            Assert.AreEqual("—", c.PointCountString);
            Assert.AreEqual("—", c.TimestampString);
        }

        [TestMethod]
        public void Apply_PopulatesAllValuesAndActivates()
        {
            var c = new LivoxLidarCorrection();
            c.Apply(pitchOffset: 1.234, rollOffset: -2.345, headingOffset: 12.0,
                    fitRmse: 5.5, pointCount: 1234);

            Assert.IsTrue(c.IsActive);
            Assert.AreEqual("Active", c.StatusString);
            Assert.AreEqual(1.234, c.PitchOffset, 1e-12);
            Assert.AreEqual(-2.345, c.RollOffset, 1e-12);
            Assert.AreEqual(12.0, c.HeadingOffset, 1e-12);
            Assert.AreEqual(5.5, c.FitRmse, 1e-12);
            Assert.AreEqual(1234, c.PointCount);
            Assert.AreNotEqual(DateTime.MinValue, c.Timestamp);

            // String formats are invariant-culture in production code.
            Assert.AreEqual("1.234°", c.PitchOffsetString);
            Assert.AreEqual("-2.345°", c.RollOffsetString);
            Assert.AreEqual("12.000°", c.HeadingOffsetString);
            Assert.AreEqual("5.5 mm", c.FitRmseString);
        }

        [TestMethod]
        public void Clear_ResetsAllValuesAndDeactivates()
        {
            var c = new LivoxLidarCorrection();
            c.Apply(1, 2, 3, 4, 5);
            c.Clear();

            Assert.IsFalse(c.IsActive);
            Assert.AreEqual(0, c.PitchOffset, 1e-12);
            Assert.AreEqual(0, c.RollOffset, 1e-12);
            Assert.AreEqual(0, c.HeadingOffset, 1e-12);
            Assert.AreEqual(0, c.FitRmse, 1e-12);
            Assert.AreEqual(0, c.PointCount);
            Assert.AreEqual(DateTime.MinValue, c.Timestamp);
            Assert.AreEqual("—", c.PitchOffsetString);
        }

        [TestMethod]
        public void IsActiveSet_RaisesNotificationsForAllDependentStrings()
        {
            var c = new LivoxLidarCorrection();
            var changed = new List<string>();
            c.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            c.IsActive = true;

            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.IsActive));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.StatusString));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.PitchOffsetString));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.RollOffsetString));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.HeadingOffsetString));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.FitRmseString));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.PointCountString));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.TimestampString));
        }

        [TestMethod]
        public void PitchOffsetSet_RaisesPitchOffsetAndPitchOffsetString()
        {
            var c = new LivoxLidarCorrection();
            var changed = new List<string>();
            c.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            c.PitchOffset = 0.5;

            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.PitchOffset));
            CollectionAssert.Contains(changed, nameof(LivoxLidarCorrection.PitchOffsetString));
        }
    }
}

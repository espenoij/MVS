using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Services
{
    /// <summary>
    /// Tests the deterministic, side-effect-free calculation modes of <see cref="DataCalculations"/>.
    /// The error handler and admin VM parameters are not exercised by the success paths
    /// of these modes, so null is passed.
    /// </summary>
    [TestClass]
    public class DataCalculationsTests
    {
        // Use the same culture the production code parses with to keep tests
        // robust on developer machines with non-invariant locales.
        private static string Fmt(double v) => v.ToString(Constants.cultureInfo);

        private static double Run(CalculationType type, double parameter, double input,
            DateTime? timestamp = null)
        {
            var calc = new DataCalculations(type, parameter);
            return calc.DoCalculations(Fmt(input), timestamp ?? DateTime.UtcNow,
                null, ErrorMessageCategory.None, null);
        }

        [TestMethod]
        public void Addition_AddsParameterToInput()
        {
            Assert.AreEqual(15.0, Run(CalculationType.Addition, 5, 10), 1e-9);
        }

        [TestMethod]
        public void Subtraction_SubtractsParameterFromInput()
        {
            Assert.AreEqual(7.0, Run(CalculationType.Subtraction, 3, 10), 1e-9);
        }

        [TestMethod]
        public void Multiplication_MultipliesInputByParameter()
        {
            Assert.AreEqual(20.0, Run(CalculationType.Multiplication, 4, 5), 1e-9);
        }

        [TestMethod]
        public void Division_DividesInputByParameter()
        {
            Assert.AreEqual(2.5, Run(CalculationType.Division, 4, 10), 1e-9);
        }

        [TestMethod]
        public void Division_ByZero_ReturnsZero()
        {
            Assert.AreEqual(0.0, Run(CalculationType.Division, 0, 10), 1e-9);
        }

        [TestMethod]
        public void Arithmetic_NonNumericInput_ReturnsNaN()
        {
            var calc = new DataCalculations(CalculationType.Addition, 1);
            double r = calc.DoCalculations("abc", DateTime.UtcNow, null, ErrorMessageCategory.None, null);
            Assert.IsTrue(double.IsNaN(r));
        }

        [TestMethod]
        public void TimeAverage_AveragesValuesWithinWindow()
        {
            var calc = new DataCalculations(CalculationType.TimeAverage, 60);
            var t0 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            calc.DoCalculations(Fmt(10), t0,                    null, ErrorMessageCategory.None, null);
            calc.DoCalculations(Fmt(20), t0.AddSeconds(1),      null, ErrorMessageCategory.None, null);
            double avg = calc.DoCalculations(Fmt(30), t0.AddSeconds(2),
                                              null, ErrorMessageCategory.None, null);

            Assert.AreEqual(20.0, avg, 1e-9);
        }

        [TestMethod]
        public void TimeAverage_ExpiresOldValuesPastWindow()
        {
            // Window = 10 s. The first sample expires before the third arrives.
            var calc = new DataCalculations(CalculationType.TimeAverage, 10);
            var t0 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            calc.DoCalculations(Fmt(100), t0,                    null, ErrorMessageCategory.None, null);
            calc.DoCalculations(Fmt(200), t0.AddSeconds(5),      null, ErrorMessageCategory.None, null);
            double avg = calc.DoCalculations(Fmt(300), t0.AddSeconds(20),
                                              null, ErrorMessageCategory.None, null);

            // Only the 300 sample remains within the 10 s window of the latest timestamp.
            Assert.AreEqual(300.0, avg, 1e-9);
        }

        [TestMethod]
        public void TimeMaxAbsolute_ReturnsLargestAbsoluteValue()
        {
            var calc = new DataCalculations(CalculationType.TimeMaxAbsolute, 60);
            var t0 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            calc.DoCalculations(Fmt(3),  t0,               null, ErrorMessageCategory.None, null);
            calc.DoCalculations(Fmt(-7), t0.AddSeconds(1), null, ErrorMessageCategory.None, null);
            double max = calc.DoCalculations(Fmt(5), t0.AddSeconds(2),
                                              null, ErrorMessageCategory.None, null);

            Assert.AreEqual(7.0, max, 1e-9);
        }

        [TestMethod]
        public void TimeMaxAbsolute_ExpiresMaxOnceOutOfWindow()
        {
            var calc = new DataCalculations(CalculationType.TimeMaxAbsolute, 5);
            var t0 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            calc.DoCalculations(Fmt(100), t0,                    null, ErrorMessageCategory.None, null);
            double max = calc.DoCalculations(Fmt(20), t0.AddSeconds(60),
                                              null, ErrorMessageCategory.None, null);

            // 100 has expired; new max is 20.
            Assert.AreEqual(20.0, max, 1e-9);
        }

        [TestMethod]
        public void DefaultConstructor_UsesCalculationTypeNone()
        {
            var calc = new DataCalculations();
            Assert.AreEqual(CalculationType.None, calc.type);
            Assert.AreEqual(0.0, calc.parameter, 1e-12);
            // The switch's default branch parses the numeric input and returns it as-is.
            double r = calc.DoCalculations(Fmt(123), DateTime.UtcNow, null, ErrorMessageCategory.None, null);
            Assert.AreEqual(123.0, r, 1e-9);
        }
    }
}

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Helpers
{
    [TestClass]
    public class LabelAbsConverterTests
    {
        [TestMethod]
        public void Convert_NegativeNumber_StripsMinusSign()
        {
            var c = new LabelAbsConverter();
            Assert.AreEqual("12.5", c.Convert("-12.5", typeof(string), null, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void Convert_PositiveNumber_LeavesUnchanged()
        {
            var c = new LabelAbsConverter();
            Assert.AreEqual("42", c.Convert(42, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void Convert_StringWithMultipleMinuses_StripsAll()
        {
            var c = new LabelAbsConverter();
            Assert.AreEqual("12", c.Convert("--12", typeof(string), null, CultureInfo.InvariantCulture));
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void ConvertBack_Throws()
        {
            var c = new LabelAbsConverter();
            c.ConvertBack("any", typeof(string), null, CultureInfo.InvariantCulture);
        }
    }
}

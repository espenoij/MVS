using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MVSTests
{
    [TestClass]
    public class DataVerificationTests
    {
        [TestMethod]
        public void DataValidationTests()
        {
            bool result;

            result = DataValidation.IsValidEmailAddress("espen@swirees.com");
            Assert.IsTrue(result);

            result = DataValidation.IsValidEmailAddress("swirees.com");
            Assert.IsFalse(result);

            result = DataValidation.IsValidEmailAddress("espen@swirees");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DoubleTests()
        {
            double result;

            DataValidation.Double("5", 0, 10, 0, out result);
            Assert.IsTrue(result == 5);

            DataValidation.Double("15", 0, 10, 4, out result);
            Assert.IsTrue(result == 4);
        }
    }
}

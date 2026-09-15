using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

namespace MVSTests.Models
{
    [TestClass]
    public class MruReportMetadataTests
    {
        [TestMethod]
        public void CreateDefault_SetsSampleRateToTwoHz()
        {
            MruReportMetadata metadata = MruReportMetadata.CreateDefault();

            Assert.AreEqual(2d, metadata.SampleRateHz);
        }

        [TestMethod]
        public void ApplyDefaultsToEmptyFields_WhenSampleRateMissing_SetsSampleRateToTwoHz()
        {
            MruReportMetadata metadata = new();

            metadata.ApplyDefaultsToEmptyFields();

            Assert.AreEqual(2d, metadata.SampleRateHz);
        }
    }
}

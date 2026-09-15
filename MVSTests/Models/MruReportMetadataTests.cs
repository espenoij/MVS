using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using System.Collections.Generic;

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
        public void CreateDefault_SetsLidarFields()
        {
            MruReportMetadata metadata = MruReportMetadata.CreateDefault();

            Assert.AreEqual("Enter the name or role of the LiDAR user.", metadata.LidarUser);
            Assert.AreEqual("Livox Tech", metadata.LidarManufacturer);
            Assert.AreEqual("Livox Mid-360 S", metadata.LidarModel);
            Assert.AreEqual("Enter the installed LiDAR serial number.", metadata.LidarSerialNumber);
            Assert.AreEqual("Enter the LiDAR firmware version used during the scan.", metadata.LidarFirmwareVersion);
        }

        [TestMethod]
        public void CreateDefault_SetsReferenceMruFields()
        {
            MruReportMetadata metadata = MruReportMetadata.CreateDefault();

            Assert.AreEqual("NORSUB", metadata.ReferenceManufacturer);
            Assert.AreEqual("MRU Marine 9000 H", metadata.ReferenceModel);
            Assert.AreEqual("The reference MRU is located on the helideck deck, center.", metadata.ReferenceInstallationLocation);
        }

        [TestMethod]
        public void ApplyDefaultsToEmptyFields_WhenSampleRateMissing_SetsSampleRateToTwoHz()
        {
            MruReportMetadata metadata = new();

            metadata.ApplyDefaultsToEmptyFields();

            Assert.AreEqual(2d, metadata.SampleRateHz);
        }

        [TestMethod]
        public void ApplyDefaultsToEmptyFields_WhenLidarFieldsMissing_SetsLidarDefaults()
        {
            MruReportMetadata metadata = new();

            metadata.ApplyDefaultsToEmptyFields();

            Assert.AreEqual("Enter the name or role of the LiDAR user.", metadata.LidarUser);
            Assert.AreEqual("Livox Tech", metadata.LidarManufacturer);
            Assert.AreEqual("Livox Mid-360 S", metadata.LidarModel);
            Assert.AreEqual("Enter the installed LiDAR serial number.", metadata.LidarSerialNumber);
            Assert.AreEqual("Enter the LiDAR firmware version used during the scan.", metadata.LidarFirmwareVersion);
        }

        [TestMethod]
        public void ApplyDefaultsToEmptyFields_WhenReferenceMruFieldsMissing_SetsReferenceMruDefaults()
        {
            MruReportMetadata metadata = new();

            metadata.ApplyDefaultsToEmptyFields();

            Assert.AreEqual("NORSUB", metadata.ReferenceManufacturer);
            Assert.AreEqual("MRU Marine 9000 H", metadata.ReferenceModel);
            Assert.AreEqual("The reference MRU is located on the helideck deck, center.", metadata.ReferenceInstallationLocation);
        }

        [TestMethod]
        public void GetMissingRequiredFields_WhenMetadataEmpty_ReturnsAllRequiredFieldNames()
        {
            MruReportMetadata metadata = new();

            List<string> expected =
            [
                "Test objective",
                "Applicable standards",
                "Vessel MRU manufacturer",
                "Vessel MRU model",
                "Vessel MRU serial number",
                "Reference MRU manufacturer",
                "Reference MRU model",
                "LiDAR manufacturer",
                "LiDAR model",
                "Vessel MRU installation location",
                "Reference MRU installation location",
                "Mounting arrangement",
                "Coordinate system",
                "Sensor separation",
                "Data acquisition method",
                "Synchronization method",
                "Sample rate (Hz)",
                "Logging configuration",
                "Time synchronization notes",
                "Filtering notes",
                "Data processing notes",
                "Assessment",
            ];

            CollectionAssert.AreEqual(expected, new List<string>(metadata.GetMissingRequiredFields()));
            Assert.IsFalse(metadata.HasAllRequiredFields());
        }

        [TestMethod]
        public void HasAllRequiredFields_WhenMandatoryFieldsFilled_ReturnsTrue()
        {
            MruReportMetadata metadata = new()
            {
                TestObjective = "Objective",
                ApplicableStandards = "Standards",
                DutManufacturer = "Maker",
                DutModel = "Model",
                DutSerialNumber = "123",
                ReferenceManufacturer = "Ref maker",
                ReferenceModel = "Ref model",
                LidarManufacturer = "LiDAR maker",
                LidarModel = "LiDAR model",
                DutInstallationLocation = "Vessel bridge deck",
                ReferenceInstallationLocation = "Reference rack",
                MountingArrangement = "Rigid mount",
                CoordinateSystem = "Vessel axes",
                SensorSeparation = "0.5 m",
                DataAcquisitionMethod = "Logged simultaneously",
                SynchronizationMethod = "Shared time base",
                SampleRateHz = 2,
                LoggingConfiguration = "Continuous",
                TimeSynchronizationNotes = "Aligned before analysis",
                FilteringNotes = "No extra filtering",
                DataProcessingNotes = "Deviation statistics computed",
                AcceptanceCriteriaDiscussion = "Assessment text",
            };

            Assert.AreEqual(0, metadata.GetMissingRequiredFields().Count);
            Assert.IsTrue(metadata.HasAllRequiredFields());
        }
    }
}

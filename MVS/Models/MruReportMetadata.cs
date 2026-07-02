using System;

namespace MVS
{
    /// <summary>
    /// Operator-supplied descriptive metadata that backs the detailed MRU
    /// verification report. None of these values are measured by the
    /// application; they are captured on the Projects page and persisted with
    /// the project so the generated report can present a complete, professional
    /// engineering document (equipment identification, test setup, environmental
    /// conditions, acceptance criteria, observations and recommendations).
    ///
    /// The whole object is serialised to a single JSON column on the project
    /// table, so adding a new field here requires no database migration.
    /// All string members default to <see cref="string.Empty"/> so the report
    /// builder never has to guard against nulls.
    /// </summary>
    public class MruReportMetadata
    {
        // ---- Section 2: Scope and objective ----
        public string TestObjective { get; set; } = string.Empty;
        public string ApplicableStandards { get; set; } = string.Empty;

        // ---- Section 3: Equipment - MRU under test (DUT, vessel-installed) ----
        public string DutManufacturer { get; set; } = string.Empty;
        public string DutModel { get; set; } = string.Empty;
        public string DutSerialNumber { get; set; } = string.Empty;
        public string DutFirmwareVersion { get; set; } = string.Empty;

        // ---- Section 3: Equipment - Reference MRU ----
        public string ReferenceManufacturer { get; set; } = string.Empty;
        public string ReferenceModel { get; set; } = string.Empty;
        public string ReferenceSerialNumber { get; set; } = string.Empty;
        public string ReferenceFirmwareVersion { get; set; } = string.Empty;
        public DateTime? ReferenceCalibrationDate { get; set; }
        public string ReferenceCalibrationCertificateNumber { get; set; } = string.Empty;

        public string AdditionalEquipment { get; set; } = string.Empty;

        // ---- Section 4: Test setup ----
        public string DutInstallationLocation { get; set; } = string.Empty;
        public string ReferenceInstallationLocation { get; set; } = string.Empty;
        public string MountingArrangement { get; set; } = string.Empty;
        public string CoordinateSystem { get; set; } = string.Empty;
        public string SensorSeparation { get; set; } = string.Empty;
        public string DataAcquisitionMethod { get; set; } = string.Empty;
        public string SynchronizationMethod { get; set; } = string.Empty;
        public double? SampleRateHz { get; set; }
        public string LoggingConfiguration { get; set; } = string.Empty;

        // ---- Section 5: Test conditions ----
        public string LoadingCondition { get; set; } = string.Empty;
        public string VesselSpeed { get; set; } = string.Empty;
        public string OperationalMode { get; set; } = string.Empty;
        public string SeaState { get; set; } = string.Empty;
        public string WindConditions { get; set; } = string.Empty;
        public string WaveConditions { get; set; } = string.Empty;
        public string CurrentConditions { get; set; } = string.Empty;
        public string EnvironmentalNotes { get; set; } = string.Empty;

        // ---- Section 6: Data processing methodology (narrative additions) ----
        public string TimeSynchronizationNotes { get; set; } = string.Empty;
        public string FilteringNotes { get; set; } = string.Empty;
        public string DataProcessingNotes { get; set; } = string.Empty;

        // ---- Section 11: Compliance assessment / acceptance criteria ----
        // Maximum allowable mean deviation for a "Pass" verdict. Angles are in
        // degrees, heave in metres. Null means "no criterion entered" and the
        // axis is reported as "Not assessed".
        public double? AcceptanceCriteriaPitch { get; set; }
        public double? AcceptanceCriteriaRoll { get; set; }
        public double? AcceptanceCriteriaHeave { get; set; }
        public string ManufacturerSpecifications { get; set; } = string.Empty;

        // ---- Sections 9 & 13: Observations and recommendations ----
        public string Observations { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;

        // ---- Section 14: Appendices ----
        public string AppendixNotes { get; set; } = string.Empty;

        /// <summary>
        /// Returns a deep copy so that copying a <see cref="Project"/> does not
        /// share the same metadata instance between the two projects.
        /// </summary>
        public MruReportMetadata Clone()
        {
            return new MruReportMetadata
            {
                TestObjective = TestObjective,
                ApplicableStandards = ApplicableStandards,

                DutManufacturer = DutManufacturer,
                DutModel = DutModel,
                DutSerialNumber = DutSerialNumber,
                DutFirmwareVersion = DutFirmwareVersion,

                ReferenceManufacturer = ReferenceManufacturer,
                ReferenceModel = ReferenceModel,
                ReferenceSerialNumber = ReferenceSerialNumber,
                ReferenceFirmwareVersion = ReferenceFirmwareVersion,
                ReferenceCalibrationDate = ReferenceCalibrationDate,
                ReferenceCalibrationCertificateNumber = ReferenceCalibrationCertificateNumber,

                AdditionalEquipment = AdditionalEquipment,

                DutInstallationLocation = DutInstallationLocation,
                ReferenceInstallationLocation = ReferenceInstallationLocation,
                MountingArrangement = MountingArrangement,
                CoordinateSystem = CoordinateSystem,
                SensorSeparation = SensorSeparation,
                DataAcquisitionMethod = DataAcquisitionMethod,
                SynchronizationMethod = SynchronizationMethod,
                SampleRateHz = SampleRateHz,
                LoggingConfiguration = LoggingConfiguration,

                LoadingCondition = LoadingCondition,
                VesselSpeed = VesselSpeed,
                OperationalMode = OperationalMode,
                SeaState = SeaState,
                WindConditions = WindConditions,
                WaveConditions = WaveConditions,
                CurrentConditions = CurrentConditions,
                EnvironmentalNotes = EnvironmentalNotes,

                TimeSynchronizationNotes = TimeSynchronizationNotes,
                FilteringNotes = FilteringNotes,
                DataProcessingNotes = DataProcessingNotes,

                AcceptanceCriteriaPitch = AcceptanceCriteriaPitch,
                AcceptanceCriteriaRoll = AcceptanceCriteriaRoll,
                AcceptanceCriteriaHeave = AcceptanceCriteriaHeave,
                ManufacturerSpecifications = ManufacturerSpecifications,

                Observations = Observations,
                Recommendations = Recommendations,

                AppendixNotes = AppendixNotes,
            };
        }
    }
}

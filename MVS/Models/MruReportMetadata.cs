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
        /// Creates a metadata instance pre-filled with editable default
        /// boilerplate for the descriptive and methodology fields of the report.
        /// Only fields that carry safe, reusable standard text are populated;
        /// equipment-identity, situational/measured, acceptance-criteria and
        /// result fields are intentionally left empty so the report never states
        /// unverified specifics. Used when a project has no saved report metadata
        /// yet; the parameterless constructor still yields an all-empty object for
        /// null-safety fallbacks.
        /// </summary>
        public static MruReportMetadata CreateDefault()
        {
            return new MruReportMetadata
            {
                // ---- Section 2: Scope and objective ----
                TestObjective =
                    "Verify that the vessel-installed Motion Reference Unit (MRU) under test meets the " +
                    "required measurement accuracy for pitch, roll and heave by comparison against a " +
                    "calibrated reference MRU under representative operating conditions, and determine any " +
                    "orientation corrections required for the vessel unit.",
                ApplicableStandards =
                    "Verification performed in accordance with the equipment manufacturer's specifications " +
                    "and the applicable vessel/class requirements.",

                // ---- Section 3: Equipment ----
                AdditionalEquipment = "None.",

                // ---- Section 4: Test setup ----
                MountingArrangement =
                    "Both units rigidly mounted to the vessel structure with their measurement axes aligned " +
                    "to the vessel reference frame.",
                CoordinateSystem =
                    "Vessel-fixed right-handed coordinate system: X positive forward, Y positive to " +
                    "starboard, Z positive downward. Rotations follow the right-hand rule.",
                DataAcquisitionMethod =
                    "Outputs from the unit under test and the reference MRU logged simultaneously to a " +
                    "common time base for the full duration of the capture.",
                SynchronizationMethod =
                    "Both units synchronised to a common time reference prior to logging.",
                LoggingConfiguration =
                    "Continuous logging of all motion channels at the configured sample rate.",

                // ---- Section 6: Data processing methodology ----
                TimeSynchronizationNotes =
                    "Reference and vessel channels time-aligned sample-by-sample to a common reference clock " +
                    "prior to statistical analysis.",
                FilteringNotes =
                    "No additional filtering applied beyond the sensors' native output; raw logged samples " +
                    "used for the comparison.",
                DataProcessingNotes =
                    "Per-sample deviations (vessel minus reference) computed for each axis; descriptive " +
                    "statistics and correlation calculated over the full capture.",
            };
        }

        /// <summary>
        /// Fills any blank descriptive/methodology fields with the standard
        /// default boilerplate (see <see cref="CreateDefault"/>) without
        /// overwriting values the operator has already entered. Returns true if
        /// at least one field was populated.
        /// </summary>
        public bool ApplyDefaultsToEmptyFields()
        {
            MruReportMetadata defaults = CreateDefault();
            bool changed = false;

            if (string.IsNullOrWhiteSpace(TestObjective)) { TestObjective = defaults.TestObjective; changed = true; }
            if (string.IsNullOrWhiteSpace(ApplicableStandards)) { ApplicableStandards = defaults.ApplicableStandards; changed = true; }
            if (string.IsNullOrWhiteSpace(AdditionalEquipment)) { AdditionalEquipment = defaults.AdditionalEquipment; changed = true; }
            if (string.IsNullOrWhiteSpace(MountingArrangement)) { MountingArrangement = defaults.MountingArrangement; changed = true; }
            if (string.IsNullOrWhiteSpace(CoordinateSystem)) { CoordinateSystem = defaults.CoordinateSystem; changed = true; }
            if (string.IsNullOrWhiteSpace(DataAcquisitionMethod)) { DataAcquisitionMethod = defaults.DataAcquisitionMethod; changed = true; }
            if (string.IsNullOrWhiteSpace(SynchronizationMethod)) { SynchronizationMethod = defaults.SynchronizationMethod; changed = true; }
            if (string.IsNullOrWhiteSpace(LoggingConfiguration)) { LoggingConfiguration = defaults.LoggingConfiguration; changed = true; }
            if (string.IsNullOrWhiteSpace(TimeSynchronizationNotes)) { TimeSynchronizationNotes = defaults.TimeSynchronizationNotes; changed = true; }
            if (string.IsNullOrWhiteSpace(FilteringNotes)) { FilteringNotes = defaults.FilteringNotes; changed = true; }
            if (string.IsNullOrWhiteSpace(DataProcessingNotes)) { DataProcessingNotes = defaults.DataProcessingNotes; changed = true; }

            return changed;
        }

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

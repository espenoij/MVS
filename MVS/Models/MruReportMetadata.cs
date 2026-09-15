using System;
using System.Collections.Generic;

namespace MVS
{
    /// <summary>
    /// Operator-supplied descriptive metadata that backs the detailed MRU
    /// verification report. None of these values are measured by the
    /// application; they are captured on the Projects page and persisted with
    /// the project so the generated report can present a complete, professional
    /// engineering document (equipment identification, test setup, environmental
    /// conditions, acceptance criteria, observations and appendix notes).
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

        // ---- Section 3: Equipment - MRU under test (vessel-installed) ----
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

        // ---- Section 3: Equipment - LiDAR ----
        public string LidarUser { get; set; } = string.Empty;
        public string LidarManufacturer { get; set; } = string.Empty;
        public string LidarModel { get; set; } = string.Empty;
        public string LidarSerialNumber { get; set; } = string.Empty;
        public string LidarFirmwareVersion { get; set; } = string.Empty;

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
        // Operator narrative discussing the verification data quality and results
        // in business language. The criteria thresholds and measured values are
        // derived automatically from the verification process.
        public string AcceptanceCriteriaDiscussion { get; set; } = string.Empty;

        public string ManufacturerSpecifications { get; set; } = string.Empty;

        // ---- Section 5: Observations ----
        public string Observations { get; set; } = string.Empty;

        // ---- Section 7: Appendices ----
        public string AppendixNotes { get; set; } = string.Empty;

        /// <summary>
        /// Returns the user-visible names of required Report Details fields that are
        /// still blank and therefore block report generation.
        /// </summary>
        public IReadOnlyList<string> GetMissingRequiredFields()
        {
            List<string> missing = new();

            AddIfMissing(missing, TestObjective, "Test objective");
            AddIfMissing(missing, ApplicableStandards, "Applicable standards");
            AddIfMissing(missing, DutManufacturer, "Vessel MRU manufacturer");
            AddIfMissing(missing, DutModel, "Vessel MRU model");
            AddIfMissing(missing, DutSerialNumber, "Vessel MRU serial number");
            AddIfMissing(missing, ReferenceManufacturer, "Reference MRU manufacturer");
            AddIfMissing(missing, ReferenceModel, "Reference MRU model");
            AddIfMissing(missing, LidarManufacturer, "LiDAR manufacturer");
            AddIfMissing(missing, LidarModel, "LiDAR model");
            AddIfMissing(missing, DutInstallationLocation, "Vessel MRU installation location");
            AddIfMissing(missing, ReferenceInstallationLocation, "Reference MRU installation location");
            AddIfMissing(missing, MountingArrangement, "Mounting arrangement");
            AddIfMissing(missing, CoordinateSystem, "Coordinate system");
            AddIfMissing(missing, SensorSeparation, "Sensor separation");
            AddIfMissing(missing, DataAcquisitionMethod, "Data acquisition method");
            AddIfMissing(missing, SynchronizationMethod, "Synchronization method");

            if (!SampleRateHz.HasValue)
                missing.Add("Sample rate (Hz)");

            AddIfMissing(missing, LoggingConfiguration, "Logging configuration");
            AddIfMissing(missing, TimeSynchronizationNotes, "Time synchronization notes");
            AddIfMissing(missing, FilteringNotes, "Filtering notes");
            AddIfMissing(missing, DataProcessingNotes, "Data processing notes");
            AddIfMissing(missing, AcceptanceCriteriaDiscussion, "Assessment");

            return missing;
        }

        /// <summary>
        /// Returns true when all required Report Details fields are populated.
        /// </summary>
        public bool HasAllRequiredFields()
        {
            return GetMissingRequiredFields().Count == 0;
        }

        private static void AddIfMissing(ICollection<string> missing, string value, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(missing);

            if (string.IsNullOrWhiteSpace(value))
                missing.Add(fieldName);
        }

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
                    "Verify agreement between the vessel-installed Motion Reference Unit (MRU) and a " +
                    "calibrated reference MRU during representative operating conditions, and determine any " +
                    "required pitch, roll and heave corrections.",
                ApplicableStandards =
                    "Verification performed in accordance with manufacturer specifications and applicable " +
                    "vessel, client and class requirements.",

                // ---- Section 3: Equipment ----
                ReferenceManufacturer = "NORSUB",
                ReferenceModel = "MRU Marine 9000 H",
                LidarUser = "Enter the name or role of the LiDAR user.",
                LidarManufacturer = "Livox Tech",
                LidarModel = "Livox Mid-360 S",
                LidarSerialNumber = "Enter the installed LiDAR serial number.",
                LidarFirmwareVersion = "Enter the LiDAR firmware version used during the scan.",
                AdditionalEquipment = "None.",

                // ---- Section 4: Test setup ----
                ReferenceInstallationLocation = "The reference MRU is located on the helideck deck, center.",
                MountingArrangement =
                    "Both units rigidly mounted to the vessel structure with their measurement axes aligned " +
                    "to the vessel reference frame.",
                CoordinateSystem =
                    "Vessel-fixed right-handed coordinate system: X positive forward, Y positive to " +
                    "starboard, Z positive downward. Rotations follow the right-hand rule.",
                DataAcquisitionMethod =
                    "Reference and vessel MRU outputs were recorded simultaneously using a common time " +
                    "base throughout the verification period.",
                SynchronizationMethod =
                    "Both units were synchronised to a common time reference prior to data acquisition.",
                SampleRateHz = 2,
                LoggingConfiguration =
                    "Continuous logging of all motion channels at the configured sample rate (2Hz).",

                // ---- Section 6: Data processing methodology ----
                TimeSynchronizationNotes =
                    "Reference and vessel data streams were time-aligned prior to statistical analysis to " +
                    "ensure sample-to-sample comparability.",
                FilteringNotes =
                    "No additional filtering applied beyond the sensors' native output; raw logged samples " +
                    "used for the comparison.",
                DataProcessingNotes =
                    "Deviations were calculated for each sample and axis using vessel minus reference " +
                    "measurements. Statistical metrics were then computed over the full capture period.",
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
            if (string.IsNullOrWhiteSpace(ReferenceManufacturer)) { ReferenceManufacturer = defaults.ReferenceManufacturer; changed = true; }
            if (string.IsNullOrWhiteSpace(ReferenceModel)) { ReferenceModel = defaults.ReferenceModel; changed = true; }
            if (string.IsNullOrWhiteSpace(LidarUser)) { LidarUser = defaults.LidarUser; changed = true; }
            if (string.IsNullOrWhiteSpace(LidarManufacturer)) { LidarManufacturer = defaults.LidarManufacturer; changed = true; }
            if (string.IsNullOrWhiteSpace(LidarModel)) { LidarModel = defaults.LidarModel; changed = true; }
            if (string.IsNullOrWhiteSpace(LidarSerialNumber)) { LidarSerialNumber = defaults.LidarSerialNumber; changed = true; }
            if (string.IsNullOrWhiteSpace(LidarFirmwareVersion)) { LidarFirmwareVersion = defaults.LidarFirmwareVersion; changed = true; }
            if (string.IsNullOrWhiteSpace(AdditionalEquipment)) { AdditionalEquipment = defaults.AdditionalEquipment; changed = true; }
            if (string.IsNullOrWhiteSpace(ReferenceInstallationLocation)) { ReferenceInstallationLocation = defaults.ReferenceInstallationLocation; changed = true; }
            if (string.IsNullOrWhiteSpace(MountingArrangement)) { MountingArrangement = defaults.MountingArrangement; changed = true; }
            if (string.IsNullOrWhiteSpace(CoordinateSystem)) { CoordinateSystem = defaults.CoordinateSystem; changed = true; }
            if (string.IsNullOrWhiteSpace(DataAcquisitionMethod)) { DataAcquisitionMethod = defaults.DataAcquisitionMethod; changed = true; }
            if (string.IsNullOrWhiteSpace(SynchronizationMethod)) { SynchronizationMethod = defaults.SynchronizationMethod; changed = true; }
            if (!SampleRateHz.HasValue) { SampleRateHz = defaults.SampleRateHz; changed = true; }
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

                LidarUser = LidarUser,
                LidarManufacturer = LidarManufacturer,
                LidarModel = LidarModel,
                LidarSerialNumber = LidarSerialNumber,
                LidarFirmwareVersion = LidarFirmwareVersion,

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

                AcceptanceCriteriaDiscussion = AcceptanceCriteriaDiscussion,
                ManufacturerSpecifications = ManufacturerSpecifications,

                Observations = Observations,

                AppendixNotes = AppendixNotes,
            };
        }
    }
}

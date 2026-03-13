using System.Configuration;

namespace MVS
{
    /// <summary>
    /// Persists LiDAR connection settings and the last applied correction to app.config.
    /// </summary>
    public class LivoxLidarSystemConfig : ConfigurationSection
    {
        private static readonly ConfigurationProperty _instance
            = new ConfigurationProperty(null, typeof(LivoxLidarSystemConfig), null,
                ConfigurationPropertyOptions.IsDefaultCollection);

        // ── Connection ────────────────────────────────────────────────────────

        [ConfigurationProperty("ipAddress", DefaultValue = "192.168.1.3")]
        public string IpAddress
        {
            get { return (string)this["ipAddress"]; }
            set { this["ipAddress"] = value; }
        }

        [ConfigurationProperty("configFilePath", DefaultValue = "Config\\LivoxLidar\\mid360_config.json")]
        public string ConfigFilePath
        {
            get { return (string)this["configFilePath"]; }
            set { this["configFilePath"] = value; }
        }

        // ── Sector filters ────────────────────────────────────────────────────

        [ConfigurationProperty("rangeMinMm", DefaultValue = "500")]
        public string RangeMinMm
        {
            get { return (string)this["rangeMinMm"]; }
            set { this["rangeMinMm"] = value; }
        }

        [ConfigurationProperty("rangeMaxMm", DefaultValue = "30000")]
        public string RangeMaxMm
        {
            get { return (string)this["rangeMaxMm"]; }
            set { this["rangeMaxMm"] = value; }
        }

        [ConfigurationProperty("azimuthMinDeg", DefaultValue = "-180")]
        public string AzimuthMinDeg
        {
            get { return (string)this["azimuthMinDeg"]; }
            set { this["azimuthMinDeg"] = value; }
        }

        [ConfigurationProperty("azimuthMaxDeg", DefaultValue = "180")]
        public string AzimuthMaxDeg
        {
            get { return (string)this["azimuthMaxDeg"]; }
            set { this["azimuthMaxDeg"] = value; }
        }

        [ConfigurationProperty("elevationMinDeg", DefaultValue = "-90")]
        public string ElevationMinDeg
        {
            get { return (string)this["elevationMinDeg"]; }
            set { this["elevationMinDeg"] = value; }
        }

        [ConfigurationProperty("elevationMaxDeg", DefaultValue = "90")]
        public string ElevationMaxDeg
        {
            get { return (string)this["elevationMaxDeg"]; }
            set { this["elevationMaxDeg"] = value; }
        }

        // ── Stored correction (last applied) ──────────────────────────────────

        [ConfigurationProperty("correctionActive", DefaultValue = "false")]
        public string CorrectionActive
        {
            get { return (string)this["correctionActive"]; }
            set { this["correctionActive"] = value; }
        }

        [ConfigurationProperty("correctionPitch", DefaultValue = "0")]
        public string CorrectionPitch
        {
            get { return (string)this["correctionPitch"]; }
            set { this["correctionPitch"] = value; }
        }

        [ConfigurationProperty("correctionRoll", DefaultValue = "0")]
        public string CorrectionRoll
        {
            get { return (string)this["correctionRoll"]; }
            set { this["correctionRoll"] = value; }
        }

        [ConfigurationProperty("correctionHeading", DefaultValue = "0")]
        public string CorrectionHeading
        {
            get { return (string)this["correctionHeading"]; }
            set { this["correctionHeading"] = value; }
        }

        [ConfigurationProperty("correctionTimestamp", DefaultValue = "")]
        public string CorrectionTimestamp
        {
            get { return (string)this["correctionTimestamp"]; }
            set { this["correctionTimestamp"] = value; }
        }
    }
}

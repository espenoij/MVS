using System;
using System.Configuration;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    // Kan ikke bruker ErrorHandler på meldingene herfra
    // Fordi ErrorHandler bruker DatabaseHandler, og DatabaseHandler bruker Config...

    public class Config
    {
        private Configuration appConfig;
        private Configuration dataConfig;

        public Config()
        {
            // App config (standard fil)
            appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Data config (egen fil)
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = @"ClientData.config";
            dataConfig = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }

        // Lese fra appSettings
        public string Read(string key, ConfigType type = ConfigType.App)
        {
            string result = string.Empty;

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settings = config.AppSettings.Settings;
                result = settings[key]?.Value;
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Config (Read 1)\n\nError reading from config file", ex.Message);
            }

            return result;
        }

        // Lese double verdi fra appSettings med default verdi input
        public double ReadWithDefault(string key, double defaultValue, ConfigType type = ConfigType.App)
        {
            string readString = string.Empty;

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settings = config.AppSettings.Settings;
                readString = settings[key]?.Value;
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Config (Read 2): Error reading from config file", ex.Message);
            }

            // Konvertere fra string til double
            if (double.TryParse(readString, Constants.numberStyle, Constants.cultureInfo, out double readValue))
                return readValue;
            else
                return defaultValue;
        }

        // Lese int verdi fra appSettings med default verdi input
        public int ReadWithDefault(string key, int defaultValue, ConfigType type = ConfigType.App)
        {
            string readString = string.Empty;

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settings = config.AppSettings.Settings;
                readString = settings[key]?.Value;
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Config (Read 3): Error reading from config file", ex.Message);
            }

            // Konvertere fra string til double
            if (int.TryParse(readString, Constants.numberStyle, Constants.cultureInfo, out int readValue))
                return readValue;
            else
                return defaultValue;
        }

        // Lese int verdi fra appSettings med default verdi input
        public string ReadWithDefault(string key, string defaultValue, ConfigType type = ConfigType.App)
        {
            string readString = string.Empty;

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settings = config.AppSettings.Settings;
                readString = settings[key]?.Value;
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Config (Read 4): Error reading from config file", ex.Message);
            }

            // Sjekke om vi skal returnere default
            if (readString == null)
                return defaultValue;
            else
                return readString;
        }

        // Lese fra en bestemt seksjon
        public string Read(string key, string section, ConfigType type = ConfigType.App)
        {
            string result = string.Empty;

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settingsSection = config.GetSection(section) as AppSettingsSection;
                if (settingsSection != null)
                {
                    if (settingsSection.Settings[key] != null)
                        result = settingsSection.Settings[key].Value;
                    else
                        throw new System.Configuration.ConfigurationErrorsException(string.Format("Configuration key: ({0}) not found.", key));
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (Read 5)\n\nError reading from config file:\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return result;
        }

        // Skrive til appSettings
        public void Write(string key, string value, ConfigType type = ConfigType.App)
        {
            // Dersom nøkkelen ikke eksisterer vil den bli opprettet.

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settings = config.AppSettings.Settings;
                if (settings[key] != null)
                {
                    settings[key].Value = value;
                }
                else
                {
                    settings.Add(key, value);
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (Write 1)\n\nError writing to config file:\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Skrive til en bestemt seksjon
        public void Write(string key, string value, string section, ConfigType type = ConfigType.App)
        {
            // NB! Seksjonen må eksistere i config file. Oppretter IKKE ny seksjon.
            // Dersom nøkkelen ikke eksisterer vil den derimot bli opprettet.

            Configuration config;
            if (type == ConfigType.App)
                config = appConfig;
            else
                config = dataConfig;

            try
            {
                var settingsSection = config.GetSection(section) as AppSettingsSection;
                if (settingsSection != null)
                {
                    if (settingsSection.Settings[key] != null)
                    {
                        settingsSection.Settings[key].Value = value;
                    }
                    else
                    {
                        settingsSection.Settings.Add(key, value);
                    }
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (Write 2)\n\nError writing to config file:\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Oppdatere eksisterende sensor data i config fil
        public void SetClientData(HMSData clientSensorData)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            HMSDataConfig sensorConfig = new HMSDataConfig(clientSensorData);

            try
            {
                // Hente seksjon
                var clientConfigSection = dataConfig.GetSection(ConfigKey.HMSData) as HMSDataConfigSection;
                if (clientConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < clientConfigSection.ClientDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (clientConfigSection.ClientDataItems[i]?.id == sensorConfig.id)
                        {
                            // Oppdatere data
                            clientConfigSection.ClientDataItems[i] = sensorConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.HMSData);
                }
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Client Config (SetClientData): Could not update sensor data in config file", ex.Message);
            }
        }

        // Hente ut liste med alle klient sensor data
        public HMSDataConfigCollection GetClientDataList()
        {
            try
            {
                // Hente seksjon
                var sensorConfigSection = dataConfig.GetSection(ConfigKey.HMSData) as HMSDataConfigSection;
                if (sensorConfigSection != null)
                {
                    return sensorConfigSection.ClientDataItems;
                }
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Client Config (GetClientDataList): Could not read all sensor data from config file", ex.Message);
            }

            return null;
        }

        // Oppdatere helicopter WSI limits
        public void SetHelicopterWSILimit(HelicopterType type, double limit)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            HelicopterWSILimitConfig HelicopterWSILimitConfig = new HelicopterWSILimitConfig(type, limit);

            try
            {
                // Hente seksjon
                var HelicopterWSILimitConfigSection = dataConfig.GetSection(ConfigKey.HelicopterWSILimit) as HelicopterWSILimitConfigSection;
                if (HelicopterWSILimitConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < HelicopterWSILimitConfigSection.HelicopterWSILimitDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (HelicopterWSILimitConfigSection.HelicopterWSILimitDataItems[i]?.id == HelicopterWSILimitConfig.id)
                        {
                            // Oppdatere data
                            HelicopterWSILimitConfigSection.HelicopterWSILimitDataItems[i] = HelicopterWSILimitConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.HelicopterWSILimit);
                }
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Helicopter WSI Limit Config (SetHelicopterWSILimit): Could not update helicopter WSI limit data in config file", ex.Message);
            }
        }

        // Hente ut liste med alle helikopter operatør data
        public HelicopterOperatorConfigCollection GetHelicopterOperatorDataList()
        {
            try
            {
                // Hente seksjon
                var helicopterOperatorConfigSection = dataConfig.GetSection(ConfigKey.HelicopterOperator) as HelicopterOperatorConfigSection;
                if (helicopterOperatorConfigSection != null)
                {
                    return helicopterOperatorConfigSection.HelicopterOperatorDataItems;
                }
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Helicopter Operator Config (GetHelicopterOperatorDataList): Could not read all data from config file", ex.Message);
            }

            return null;
        }
    }

    public enum ConfigType
    {
        App,
        Data
    }
}
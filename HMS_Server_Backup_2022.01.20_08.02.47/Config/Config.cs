﻿using System;
using System.Configuration;
using Telerik.Windows.Controls;

namespace HMS_Server
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
            configMap.ExeConfigFilename = @"SensorData.config";
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
                RadWindow.Alert(string.Format("Config (Read 1)\n\nError reading from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
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

            return readString;
        }

        // Lese double verdi fra appSettings med default verdi input
        public double Read(string key, double defaultValue, ConfigType type = ConfigType.App)
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
                RadWindow.Alert(string.Format("Config (Read 5)\n\nError reading from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            // Konvertere fra string til double
            if (double.TryParse(readString, Constants.numberStyle, Constants.cultureInfo, out double readValue))
                return readValue;
            else
                return defaultValue;
        }

        // Lese double verdi fra appSettings med default verdi input
        public int Read(string key, int defaultValue, ConfigType type = ConfigType.App)
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
                RadWindow.Alert(string.Format("Config (Read 6)\n\nError reading from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            // Konvertere fra string til double
            if (int.TryParse(readString, Constants.numberStyle, Constants.cultureInfo, out int readValue))
                return readValue;
            else
                return defaultValue;
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
                RadWindow.Alert(string.Format("Config (Read 7)\n\nError reading from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return result;
        }

        // Lese fra en bestemt seksjon, med default verdi input
        public double Read(string key, string section, int defaultValue, ConfigType type = ConfigType.App)
        {
            string readString = string.Empty;

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
                        readString = settingsSection.Settings[key].Value;
                    else
                        throw new System.Configuration.ConfigurationErrorsException(string.Format("Configuration key: ({0}) not found.", key));
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (Read 8)\n\nError reading from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            // Konvertere fra string til double
            if (int.TryParse(readString, Constants.numberStyle, Constants.cultureInfo, out int readValue))
                return readValue;
            else
                return defaultValue;
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
                RadWindow.Alert(string.Format("Config (Write 1)\n\nError writing to config file\n\n{0}", TextHelper.Wrap(ex.Message)));
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
                RadWindow.Alert(string.Format("Config (Write 2)\n\nError writing to config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Opprette nye sensor data i config fil
        public void NewData(SensorData sensorData)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            try
            {
                // Først leser vi nextID variabelen
                string result = this.Read(ConfigKey.nextID, ConfigKey.SensorSectionHeader, ConfigType.Data);

                // Dersom vi finner nextID variabelen og der er data
                if (result != string.Empty)
                {
                    // Konverter til integer
                    int nextID = Convert.ToInt32(result);

                    // Overfør til ny sensorData
                    sensorData.id = nextID;

                    // Må oppdatere next ID variabelen til neste +1
                    nextID++;

                    // og skrive den tilbake til XML fil
                    this.Write(ConfigKey.nextID, nextID.ToString(), ConfigKey.SensorSectionHeader, ConfigType.Data);
                }

                SensorConfig sensorConfig = new SensorConfig(sensorData);

                // Hente seksjon
                var sensorConfigSection = dataConfig.GetSection(ConfigKey.SensorSectionData) as SensorConfigSection;
                if (sensorConfigSection != null)
                {
                    // Legge inn sensor data
                    sensorConfigSection.SensorDataItems.Add(sensorConfig);

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.SensorSectionData);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (NewData)\n\nCould not add sensor data to config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Oppdatere eksisterende sensor data i config fil
        public void SetData(SensorData sensorData)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            SensorConfig sensorConfig = new SensorConfig(sensorData);

            try
            {
                // Hente seksjon
                var sensorConfigSection = dataConfig.GetSection(ConfigKey.SensorSectionData) as SensorConfigSection;
                if (sensorConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < sensorConfigSection.SensorDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (sensorConfigSection.SensorDataItems[i]?.id == sensorConfig.id)
                        {
                            // Oppdatere data
                            sensorConfigSection.SensorDataItems[i] = sensorConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.SensorSectionData);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (SetData)\n\nCould not add sensor data to config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Hente ut liste med alle data
        public SensorConfigCollection GetAllSensorData()
        {
            try
            {
                // Hente seksjon
                var sensorConfigSection = dataConfig.GetSection(ConfigKey.SensorSectionData) as SensorConfigSection;
                if (sensorConfigSection != null)
                {
                    return sensorConfigSection.SensorDataItems;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (GetAllData)\n\nCould not read all sensor data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return null;
        }

        // Slette sensor data fra fil
        public void DeleteData(SensorData sensorData)
        {
            SensorConfig sensorConfig = new SensorConfig(sensorData);
            try
            {
                // Hente seksjon
                var sensorConfigSection = dataConfig.GetSection(ConfigKey.SensorSectionData) as SensorConfigSection;
                if (sensorConfigSection != null)
                {
                    sensorConfigSection.SensorDataItems.Remove(sensorConfig);

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.SensorSectionData);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (DeleteData)\n\nCould not delete sensor data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
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
                RadWindow.Alert(string.Format("Client Config (SetClientData)\n\nCould not update sensor data in config file\n\n{0}", TextHelper.Wrap(ex.Message)));
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
                RadWindow.Alert(string.Format("Client Config (GetClientDataList)\n\nCould not read all sensor data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return null;
        }
        
        // Oppdatere eksisterende test data i config fil
        public void SetTestData(HMSData testData)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            TestDataConfig testDataConfig = new TestDataConfig(testData);

            try
            {
                // Hente seksjon
                var testDataConfigSection = dataConfig.GetSection(ConfigKey.TestData) as TestDataConfigSection;
                if (testDataConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < testDataConfigSection.ClientDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (testDataConfigSection.ClientDataItems[i]?.id == testDataConfig.id)
                        {
                            // Oppdatere data
                            testDataConfigSection.ClientDataItems[i] = testDataConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.TestData);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Client Config (SetTestData)\n\nCould not update test data in config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Hente ut liste med alle test data
        public TestDataConfigCollection GetTestDataList()
        {
            try
            {
                // Hente seksjon
                var testDataConfigSection = dataConfig.GetSection(ConfigKey.TestData) as TestDataConfigSection;
                if (testDataConfigSection != null)
                {
                    return testDataConfigSection.ClientDataItems;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Client Config (GetTestDataList)\n\nCould not read all test data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return null;
        }

        // Oppdatere eksisterende referanse data i config fil
        public void SetReferenceData(HMSData testData)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            ReferenceDataConfig referenceDataConfig = new ReferenceDataConfig(testData);

            try
            {
                // Hente seksjon
                var referenceDataConfigSection = dataConfig.GetSection(ConfigKey.ReferenceData) as ReferenceDataConfigSection;
                if (referenceDataConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < referenceDataConfigSection.ClientDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (referenceDataConfigSection.ClientDataItems[i]?.id == referenceDataConfig.id)
                        {
                            // Oppdatere data
                            referenceDataConfigSection.ClientDataItems[i] = referenceDataConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.ReferenceData);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Client Config (SetReferenceData)\n\nCould not update reference data in config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Hente ut liste med alle referanse data
        public ReferenceDataConfigCollection GetReferenceDataList()
        {
            try
            {
                // Hente seksjon
                var referenceDataConfigSection = dataConfig.GetSection(ConfigKey.ReferenceData) as ReferenceDataConfigSection;
                if (referenceDataConfigSection != null)
                {
                    return referenceDataConfigSection.ClientDataItems;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Client Config (GetReferenceDataList)\n\nCould not read all reference data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return null;
        }


        // Oppdatere eksisterende sensor group data i config fil
        public void SetSensorGroupIDData(SensorGroup sensor)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            SensorGroupIDConfig sensorIDConfig = new SensorGroupIDConfig(sensor);

            try
            {
                // Hente seksjon
                var sensorIDConfigSection = dataConfig.GetSection(ConfigKey.SensorGroupID) as SensorGroupIDConfigSection;
                if (sensorIDConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < sensorIDConfigSection.SensorIDDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (sensorIDConfigSection.SensorIDDataItems[i]?.id == sensorIDConfig.id)
                        {
                            // Oppdatere data
                            sensorIDConfigSection.SensorIDDataItems[i] = sensorIDConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.SensorGroupID);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Sensor Group ID Config (SetSensorIDData)\n\nCould not update sensor data in config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Hente ut liste med alle sensor group data
        public SensorGroupIDConfigCollection GetSensorGroupIDDataList()
        {
            try
            {
                // Hente seksjon
                var sensorIDConfigSection = dataConfig.GetSection(ConfigKey.SensorGroupID) as SensorGroupIDConfigSection;
                if (sensorIDConfigSection != null)
                {
                    return sensorIDConfigSection.SensorIDDataItems;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Sensor Group ID Config (GetSensorIDDataList)\n\nCould not read all sensor ID data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
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
                RadWindow.Alert(string.Format("Helicopter WSI Limit Config (SetHelicopterWSILimit)\n\nCould not update helicopter WSI limit data in config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Hente ut liste med alle data
        public HelicopterWSILimitConfigCollection GetHelicopterWSILimits()
        {
            try
            {
                // Hente seksjon
                var HelicopterWSILimitConfigSection = dataConfig.GetSection(ConfigKey.HelicopterWSILimit) as HelicopterWSILimitConfigSection;
                if (HelicopterWSILimitConfigSection != null)
                {
                    return HelicopterWSILimitConfigSection.HelicopterWSILimitDataItems;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Helicopter WSI Limit Config (GetHelicopterWSILimits)\n\nCould not read all helicopter WSI limit data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return null;
        }

        // Oppdatere eksisterende lights output data i config fil
        public void SetLightsOutputData(SensorData sensorData)
        {
            // NB! Dersom data med angitt ID allerede finnes vil dette gi feilmelding
            SensorConfig sensorConfig = new SensorConfig(sensorData);

            try
            {
                // Hente seksjon
                var sensorConfigSection = dataConfig.GetSection(ConfigKey.LightsOutput) as SensorConfigSection;
                if (sensorConfigSection != null)
                {
                    bool found = false;

                    // Søke etter sensor verdi med angitt ID
                    for (int i = 0; i < sensorConfigSection.SensorDataItems.Count && !found; i++)
                    {
                        // Sjekke ID
                        if (sensorConfigSection.SensorDataItems[i]?.id == sensorConfig.id)
                        {
                            // Oppdatere data
                            sensorConfigSection.SensorDataItems[i] = sensorConfig;
                            found = true;
                        }
                    }

                    // Oppdater XML fil
                    dataConfig.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigKey.SensorSectionData);
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (SetLightsOutputData)\n\nCould not update lights output data in config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        // Hente ut liste med alle test data
        public SensorConfig GetLightsOutputData()
        {
            try
            {
                // Hente seksjon
                var testDataConfigSection = dataConfig.GetSection(ConfigKey.LightsOutput) as SensorConfigSection;
                if (testDataConfigSection != null)
                {
                    return testDataConfigSection.SensorDataItems[0];
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Config (GetLightsOutputData)\n\nCould not read lights output data from config file\n\n{0}", TextHelper.Wrap(ex.Message)));
            }

            return null;
        }
    }

    public class ConfigSection
    {
        // App: Serial Port Configuration
        public const string SerialPortConfig = "SerialPortConfig";

        // App: MODBUS config
        public const string ModbusConfig = "ModbusConfig";

        // App: File Reader
        public const string FileReaderConfig = "FileReaderConfig";

        // Data: Sensor Data Setup
        public const string SensorData = "SensorData";

        // App: ErrorMessages
        public const string ErrorMessages = "ErrorMessages";
    }


    public enum ConfigType
    {
        App,
        Data
    }
}
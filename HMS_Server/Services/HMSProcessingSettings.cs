using System;

namespace HMS_Server
{
    class HMSProcessingSettings
    {
        private HMSData helideckWindSensorHeight = new HMSData();
        private HMSData helideckWindSensorDistance = new HMSData();
        private HMSData areaWindSensorHeight = new HMSData();
        private HMSData areaWindSensorDistance = new HMSData();
        private HMSData ndbFrequency = new HMSData();
        private HMSData ndbIdent = new HMSData();
        private HMSData vhfFrequency = new HMSData();
        private HMSData logFrequency = new HMSData();
        private HMSData marineChannel = new HMSData();
        private HMSData vesselName = new HMSData();
        private HMSData emailServer = new HMSData();
        private HMSData emailPort = new HMSData();
        private HMSData emailUsername = new HMSData();
        private HMSData emailPassword = new HMSData();
        private HMSData emailSecureConnection = new HMSData();
        private HMSData restrictedSector = new HMSData();

        private AdminSettingsVM adminSettingsVM;

        public HMSProcessingSettings(DataCollection hmsOutputData, AdminSettingsVM adminSettingsVM)
        {
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(helideckWindSensorHeight);
            hmsOutputDataList.Add(helideckWindSensorDistance);
            hmsOutputDataList.Add(areaWindSensorHeight);
            hmsOutputDataList.Add(areaWindSensorDistance);

            hmsOutputDataList.Add(ndbFrequency);
            hmsOutputDataList.Add(ndbIdent);
            hmsOutputDataList.Add(vhfFrequency);
            hmsOutputDataList.Add(logFrequency);
            hmsOutputDataList.Add(marineChannel);

            hmsOutputDataList.Add(vesselName);

            hmsOutputDataList.Add(emailServer);
            hmsOutputDataList.Add(emailPort);
            hmsOutputDataList.Add(emailUsername);
            hmsOutputDataList.Add(emailPassword);
            hmsOutputDataList.Add(emailSecureConnection);

            hmsOutputDataList.Add(restrictedSector);

            // Trenger ikke å lagre disse i databasen, derfor ingen database table navn
            helideckWindSensorHeight.id = (int)ValueType.SettingsHelideckWindSensorHeight;
            helideckWindSensorHeight.name = "Helideck Wind Sensor Height (NOROG)";
            helideckWindSensorHeight.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            helideckWindSensorDistance.id = (int)ValueType.SettingsHelideckWindSensorDistance;
            helideckWindSensorDistance.name = "Helideck Wind Sensor Distance (NOROG)";
            helideckWindSensorDistance.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            areaWindSensorHeight.id = (int)ValueType.SettingsAreaWindSensorHeight;
            areaWindSensorHeight.name = "Area Wind Sensor Height (NOROG)";
            areaWindSensorHeight.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            areaWindSensorDistance.id = (int)ValueType.SettingsAreaWindSensorDistance;
            areaWindSensorDistance.name = "Area Wind Sensor Distance (NOROG)";
            areaWindSensorDistance.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            ndbFrequency.id = (int)ValueType.SettingsNDBFrequency;
            ndbFrequency.name = "NDB Frequency";
            ndbFrequency.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            ndbIdent.id = (int)ValueType.SettingsNDBIdent;
            ndbIdent.name = "NDB Ident (CAP)";
            ndbIdent.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            vhfFrequency.id = (int)ValueType.SettingsVHFFrequency;
            vhfFrequency.name = "VHF Frequency";
            vhfFrequency.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            logFrequency.id = (int)ValueType.SettingsLogFrequency;
            logFrequency.name = "Log Frequency (CAP)";
            logFrequency.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            marineChannel.id = (int)ValueType.SettingsMarineChannel;
            marineChannel.name = "Marine Channel (CAP)";
            marineChannel.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            vesselName.id = (int)ValueType.SettingsVesselName;
            vesselName.name = "Vessel/Installation Name";
            vesselName.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            emailServer.id = (int)ValueType.SettingsEmailServer;
            emailServer.name = "Email Server";
            emailServer.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            emailPort.id = (int)ValueType.SettingsEmailPort;
            emailPort.name = "Email Port";
            emailPort.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            emailUsername.id = (int)ValueType.SettingsEmailUsername;
            emailUsername.name = "Email Username";
            emailUsername.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            emailPassword.id = (int)ValueType.SettingsEmailPassword;
            emailPassword.name = "Email Password";
            emailPassword.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            emailSecureConnection.id = (int)ValueType.SettingsEmailSecureConnection;
            emailSecureConnection.name = "Email Secure Connection";
            emailSecureConnection.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            restrictedSector.id = (int)ValueType.SettingsRestrictedSector;
            restrictedSector.name = "Restricted Sector (CAP)";
            restrictedSector.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
        }

        public void Update()
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            // Helideck vind
            helideckWindSensorHeight.data = (int)adminSettingsVM.helideckHeight;
            helideckWindSensorHeight.timestamp = DateTime.UtcNow;
            helideckWindSensorHeight.status = DataStatus.OK;

            helideckWindSensorDistance.data = adminSettingsVM.windSensorDistance;
            helideckWindSensorDistance.timestamp = DateTime.UtcNow;
            helideckWindSensorDistance.status = DataStatus.OK;

            // Area wind
            areaWindSensorHeight.data = adminSettingsVM.windSensorHeight;
            areaWindSensorHeight.timestamp = DateTime.UtcNow;
            areaWindSensorHeight.status = DataStatus.OK;

            areaWindSensorDistance.data = adminSettingsVM.windSensorDistance;
            areaWindSensorDistance.timestamp = DateTime.UtcNow;
            areaWindSensorDistance.status = DataStatus.OK;

            // NDB
            if (double.TryParse(adminSettingsVM.ndbFreq_NOROG, out double ndbFreqValue))
                ndbFrequency.data = ndbFreqValue;
            else
                ndbFrequency.data = Constants.NDBFrequencyDefault;

            ndbFrequency.timestamp = DateTime.UtcNow;
            ndbFrequency.status = DataStatus.OK;

            ndbIdent.data3 = adminSettingsVM.ndbIdent;
            ndbIdent.timestamp = DateTime.UtcNow;
            ndbIdent.status = DataStatus.OK;

            // VHF
            if (double.TryParse(adminSettingsVM.vhfFreq, out double vhfFreqValue))
                vhfFrequency.data = vhfFreqValue;
            else
                vhfFrequency.data = Constants.VHFFrequencyDefault;

            vhfFrequency.timestamp = DateTime.UtcNow;
            vhfFrequency.status = DataStatus.OK;

            // Log Frequency
            if (double.TryParse(adminSettingsVM.logFreq, out double logFreqValue))
                logFrequency.data = logFreqValue;
            else
                logFrequency.data = Constants.VHFFrequencyDefault;

            logFrequency.timestamp = DateTime.UtcNow;
            logFrequency.status = DataStatus.OK;

            // Marine Channel
            marineChannel.data = adminSettingsVM.marineChannel;
            marineChannel.timestamp = DateTime.UtcNow;
            marineChannel.status = DataStatus.OK;

            // Vessel Name
            vesselName.data3 = adminSettingsVM.vesselName;
            vesselName.timestamp = DateTime.UtcNow;
            vesselName.status = DataStatus.OK;

            // Email Server
            emailServer.data3 = adminSettingsVM.emailServer;
            emailServer.timestamp = DateTime.UtcNow;
            emailServer.status = DataStatus.OK;

            emailPort.data = adminSettingsVM.emailPort;
            emailPort.timestamp = DateTime.UtcNow;
            emailPort.status = DataStatus.OK;

            emailUsername.data3 = adminSettingsVM.emailUsername;
            emailUsername.timestamp = DateTime.UtcNow;
            emailUsername.status = DataStatus.OK;

            emailPassword.data3 = adminSettingsVM.emailPassword;
            emailPassword.timestamp = DateTime.UtcNow;
            emailPassword.status = DataStatus.OK;

            if (adminSettingsVM.emailSecureConnection)
                emailSecureConnection.data = 1;
            else
                emailSecureConnection.data = 0;
            emailSecureConnection.timestamp = DateTime.UtcNow;
            emailSecureConnection.status = DataStatus.OK;

            // Restricted Sector
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                if (double.TryParse(adminSettingsVM.restrictedSectorFrom, out double valueFrom) &&
                    double.TryParse(adminSettingsVM.restrictedSectorTo, out double valueTo))
                {
                    restrictedSector.data = valueFrom;
                    restrictedSector.data2 = valueTo;
                    restrictedSector.status = DataStatus.OK;
                }
                else
                {
                    restrictedSector.data = Constants.HeadingDefault;
                    restrictedSector.data2 = Constants.HeadingDefault;
                    restrictedSector.status = DataStatus.TIMEOUT_ERROR;
                }
            }
            else
            {
                restrictedSector.status = DataStatus.OK;
            }
            restrictedSector.timestamp = DateTime.UtcNow;
        }
    }
}
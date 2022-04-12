﻿class ConfigKey
{
    // App
    public const string ServerAddress = "ServerAddress";
    public const string ServerPort = "ServerPort";
    public const string DataTimeout = "DataTimeout";
    public const string ClientUpdateFrequencyUI = "ClientUpdateFrequencyUI";
    public const string ChartDataUpdateFrequency20m = "ChartDataUpdateFrequency20m";
    public const string ChartDataUpdateFrequency3h = "ChartDataUpdateFrequency3h";

    // App: General Settings
    public const string RegulationStandard = "RegulationStandard";
    public const string ClientIsMaster = "ClientIsMaster";
    public const string HelicopterType = "HelicopterType";
    public const string HelideckCategory = "HelideckCategory";
    public const string HelideckCategorySetting = "HelideckCategorySetting";
    public const string DayNight = "DayNight";
    public const string DisplayMode = "DisplayMode";
    public const string OnDeckHelicopterHeading = "OnDeckHelicopterHeading";
    public const string OnDeckTime = "OnDeckTime";
    public const string OnDeckVesselHeading = "OnDeckVesselHeading";
    public const string OnDeckWindDirection = "OnDeckWindDirection";
    public const string VesselImage = "VesselImage";
    public const string HelideckLocation = "HelideckLocation";
    public const string MSICorrectionR = "MSICorrectionR";
    public const string HelideckHeight = "HelideckHeight";
    public const string WindSensorHeight = "WindSensorHeight";
    public const string WindSensorDistance = "WindSensorDistance";
    public const string AirPressureSensorHeight = "AirPressureSensorHeight";
    public const string NDBFrequency = "NDBFrequency";
    public const string VHFFrequency = "VHFFrequency";
    public const string EnableReportEmail = "EnableReportEmail";
    public const string ActivateEMS = "ClientIsMaster";

    // App: Wind/Heading
    public const string WindMeasurement = "WindMeasurement";
    public const string HelideckHeadingOffset = "HelideckHeadingOffset";

    // APP: TCP/IP
    public const string DataRequestFrequency = "DataRequestFrequency";
    public const string SensorStatusRequestFrequency = "SensorStatusRequestFrequency";

    // HMS data
    public const string HMSData = "HMSData";
    public const string HMSDataItems = "HMSDataItems";

    // Client: Report data
    public const string Report = "Report";
    public const string NameOfHLO = "NameOfHLO";
    public const string Email = "Email";
    public const string Telephone = "Telephone";
    public const string DynamicPositioning = "DynamicPositioning";
    public const string AccurateMonitoringEquipment = "AccurateMonitoringEquipment";
    public const string AreaWindDirection = "AreaWindDirection";
    public const string AreaWindVelocity = "AreaWindVelocity";
    public const string AreaWindGust = "AreaWindGust";
    public const string SeaSprayObserved = "SeaSprayObserved";
    public const string OtherWeatherInfo = "OtherWeatherInfo";
    public const string FlightNumber = "FlightNumber";
    public const string ReturnLoad = "ReturnLoad";
    public const string Luggage = "Luggage";
    public const string Cargo = "Cargo";
    public const string TotalLoad = "TotalLoad";
    public const string HelifuelAvailable = "HelifuelAvailable";
    public const string FuelQuantity = "FuelQuantity";
    public const string Routing1 = "Routing1";
    public const string Routing2 = "Routing2";
    public const string Routing3 = "Routing3";
    public const string Routing4 = "Routing4";
    public const string LogInfoRemarks = "LogInfoRemarks";
    public const string EmailTo = "EmailTo";
    public const string EmailCC = "EmailCC";
    public const string EmailSubject = "EmailSubject";
    public const string EmailBody = "EmailBody";
    public const string SendHMSScreenCapture = "SendHMSScreenCapture";
    public const string EmailServer = "EmailServer";
    public const string EmailPort = "EmailPort";
    public const string EmailUsername = "EmailUsername";
    public const string EmailPassword = "EmailPassword";
    public const string EmailSecureConnection = "EmailSecureConnection";
    public const string RescueRecoveryAvailable = "RescueRecoveryAvailable";
    public const string NDBServiceable = "NDBServiceable";
    public const string ColdFlaring = "ColdFlaring";
    public const string LightningPresent = "LightningPresent";
    public const string AnyUnserviceableSensors = "AnyUnserviceableSensors";
    public const string AnyUnserviceableSensorsComments = "AnyUnserviceableSensorsComments";

    // Sensor Group ID Data
    public const string SensorGroupID = "SensorGroupID";

    // Helicopter WSI Limit Data
    public const string HelicopterWSILimit = "HelicopterWSILimit";

    // Helicopter WSI Limit Data
    public const string HelicopterOperator = "HelicopterOperator";


    /************************************************************************/

    // Server App: Database
    public const string DatabaseAddress = "DatabaseAddress";
    public const string DatabasePort = "DatabasePort";
    public const string DatabaseName = "DatabaseName";
    public const string DatabaseUserID = "DatabaseUserID";
    public const string DatabasePassword = "DatabasePassword";
    public const string DataStorageTime = "DataStorageTime";
    public const string ErrorMessageStorageTime = "ErrorMessageStorageTime";

    // Server App: Various
    public const string DatabaseSaveFrequency = "DatabaseSaveFrequency";
    //public const string DataTimeout = "DataTimeout";
    public const string GUIDataLimit = "GUIDataLimit";

    // Server App: Serial Port Configuration
    public const string ShowControlChars = "ShowControlChars";
    public const string TotalDataLines = "TotalDataLines";

    // Server App: Error Messages
    public const string ErrorMessagesView = "ErrorMessagesView";
    public const string ErrorMessagesType = "ErrorMessagesType";
    public const string ErrorMessagesSelection = "ErrorMessagesSelection";

    // Server Data: Header
    public const string SensorSectionHeader = "Header";
    public const string nextID = "nextID";

    // Server Data: Sensor Data
    public const string SensorSectionData = "SensorData";
}

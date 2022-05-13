class ConfigKey
{
    // App
    public const string ServerAddress = "ServerAddress";
    public const string ServerPort = "ServerPort";
    public const string DataTimeout = "DataTimeout";

    // App: General Settings
    public const string RegulationStandard = "RegulationStandard";
    public const string VesselName = "VesselName";
    public const string ClientIsMaster = "ClientIsMaster";
    public const string SemiSubmersible = "SemiSubmersible";
    public const string HelicopterType = "HelicopterType";
    public const string HelideckCategory = "HelideckCategory";
    public const string DayNight = "DayNight";
    public const string DisplayMode = "DisplayMode";
    public const string OnDeckHelicopterHeading = "OnDeckHelicopterHeading";
    public const string OnDeckTime = "OnDeckTime";
    public const string OnDeckVesselHeading = "OnDeckVesselHeading";
    public const string OnDeckWindDirection = "OnDeckWindDirection";
    public const string VesselImage = "VesselImage";
    public const string HelideckLocation = "HelideckLocation";
    public const string MSICorrectionR = "MSICorrectionR";
    public const string WindSamplesPerTransmission = "WindSamplesPerTransmission";
    public const string OverrideWindBuffer = "OverrideWindBuffer";
    public const string OverrideMotionBuffer = "OverrideMotionBuffer";
    public const string RestrictedSectorFrom = "RestrictedSectorFrom";
    public const string RestrictedSectorTo = "RestrictedSectorTo";
    public const string HelideckHeight = "HelideckHeight";
    public const string WindDirectionReference = "WindDirectionReference";
    public const string VesselHeadingReference = "VesselHeadingReference";
    public const string WindSensorHeight = "WindSensorHeight";
    public const string WindSensorDistance = "WindSensorDistance";
    public const string AirPressureSensorHeight = "AirPressureSensorHeight";
    public const string MagneticDeclination = "MagneticDeclination";
    public const string HelideckLightsOutput = "HelideckLightsOutput";
    public const string NDBInstalled_NOROG = "NDBInstalled_NOROG";
    public const string NDBInstalled_CAP = "NDBInstalled_CAP";
    public const string NDBFrequency_NOROG = "NDBFrequency_NOROG";
    public const string NDBFrequency_CAP = "NDBFrequency_CAP";
    public const string NDBIdent = "NDBIdent";
    public const string VHFFrequency = "VHFFrequency";
    public const string TrafficFrequency = "TrafficFrequency";
    public const string LogFrequency = "LogFrequency";
    public const string MarineChannel = "MarineChannel";
    public const string EnableEMS = "EnableEMS";
    public const string AutoStartHMS = "AutoStartHMS";

    // Lights output
    public const string LightsOutputAddress1 = "LightsOutputAddress1";
    public const string LightsOutputAddress2 = "LightsOutputAddress2";
    public const string LightsOutputAddress3 = "LightsOutputAddress3";

    // App: Wind/Heading
    public const string WindMeasurement = "WindMeasurement";
    public const string HelideckHeadingOffset = "HelideckHeadingOffset";

    // APP: TCP/IP
    public const string DataRequestFrequency = "DataRequestFrequency";
    public const string SensorStatusRequestFrequency = "SensorStatusRequestFrequency";

    // HMS data
    public const string HMSData = "HMSData";
    public const string HMSDataItems = "HMSDataItems";

    // Test/Reference Data
    public const string TestData = "TestData";
    public const string TestDataItems = "TestDataItems";
    public const string ReferenceData = "ReferenceData";
    public const string ReferenceDataItems = "ReferenceDataItems";

    // Lights Output Data
    public const string LightsOutput = "LightsOutput";

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
    public const string SendHMSScreenCapture = "SendHMSScreenCapture";
    public const string EmailServer = "EmailServer";
    public const string EmailPort = "EmailPort";
    public const string EmailUsername = "EmailUsername";
    public const string EmailPassword = "EmailPassword";
    public const string EmailSecureConnection = "EmailSecureConnection";
    public const string DataVerificationEnabled = "DataVerificationEnabled";
    public const string LicenseOwner = "LicenseOwner";
    public const string LicenseLocation = "LicenseLocation";
    public const string LicenseMaxClients = "LicenseMaxClients";
    public const string DeviceID = "DeviceID";
    public const string ActivationStatus = "ActivationStatus";

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
    public const string SetupGUIDataLimit = "SetupGUIDataLimit";
    public const string ServerUIUpdateFrequency = "ServerUIUpdateFrequency";
    public const string DatabaseSaveFrequency = "DatabaseSaveFrequency";
    public const string HMSProcessingFrequency = "HMSProcessingFrequency";
    public const string LightsOutputFrequency = "LightsOutputFrequency";

    // Server App: Serial Port Configuration
    public const string ShowControlChars = "ShowControlChars";
    public const string SensorDataType = "SensorDataType";
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

using System;
using System.Globalization;

class Constants
{
    // Software Name
    public const string appNameServer = "HMS_Server";
    public const string appNameClient = "HMS_Client";

    // Regulatory Standard
    public const string RegulatoryStandard = "HMS Rev 9b";

    // Regulatory Standard
    public const string HLLLimitsVersion = "HLL Issue 4 (Nov 2020)";

    // Software Version
    public const string SoftwareVersionPostfix = "(Dev)";

    // Admin Mode Password
    public const string AdminModePassword = "swireworld";
    public const string ActivationPassword = "swirefuture";

    // Max antall datafelt i en pakke
    public const int PacketDataFields = 60;                     // Korresponderer til felt i XAML fil (lvPacketData)

    // Timing
    public const int ClientUIUpdateFrequencyDefault = 1000;
    public const int ClientUIUpdateFrequencyMin = 100;
    public const int ClientUIUpdateFrequencyMax = 5000;

    public const int ChartUpdateFrequencyUI20mDefault = 5000;
    public const int ChartUpdateFrequencyUI3hDefault = 15000;
    public const int ChartUpdateFrequencyUIMin = 500;
    public const int ChartUpdateFrequencyUIMax = 60000;

    public const int DataRequestFrequencyDefault = 500;
    public const int DataRequestFrequencyMin = 100;
    public const int DataRequestFrequencyMax = 5000;

    public const int SensorStatusRequestFrequencyDefault = 5000;
    public const int SensorStatusRequestFrequencyMin = 100;
    public const int SensorStatusRequestFrequencyMax = 60000;

    public const int HMSProcessingFrequencyDefault = 250;
    public const int HMSProcessingFrequencyMin = 100;
    public const int HMSProcessingFrequencyMax = 5000;

    public const int LightsOutputFrequencyDefault = 1000;
    public const int LightsOutputFrequencyMin = 100;
    public const int LightsOutputFrequencyMax = 5000;

    public const int ServerUIUpdateFrequencyDefault = 1000;
    public const int ServerUIUpdateFrequencyMin = 100;
    public const int ServerUIUpdateFrequencyMax = 5000;

    //  Maintenance Frequency
    public const int DBHMSSaveFrequency = 1000;
    public const int DBMaintenanceFrequency = 43200000;         // Millisekund (12 timer)

    // User Inputs Set Check frequency
    public const int UserInputsSetCheckFrequency = 3000;

    // Nummer stil og kultur-avhengig notasjon
    public static NumberStyles numberStyle = NumberStyles.Any;
    public static CultureInfo cultureInfo = CultureInfo.CurrentCulture;
    //public static CultureInfo cultureInfo = new CultureInfo("en-US");

    // Sensor ID not set
    public const int SensorIDNotSet = -1;

    // Time Stamp
    public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    public const string TimestampNotSet = "-";

    // MODBUS
    public const int ModbusTimeout = 500;                   // Millisekund
    public const int ModbusCoilMin = 1;
    public const int ModbusCoilMax = 9999;
    public const int ModbusDiscreteInputMin = 10001;
    public const int ModbusDiscreteInputMax = 19999;
    public const int ModbusInputRegisterMin = 30001;
    public const int ModbusInputRegisterMax = 39999;
    public const int ModbusHoldingRegisterMin = 40001;
    public const int ModbusHoldingRegisterMax = 49999;
    public const int ModbusDefaultAddress = ModbusCoilMin;
    public const int ModbusRegistersMax = 125;
    public const int ModbusSlaveMin = 1;
    public const int ModbusSlaveMax = 247;

    // TCP/IP Socket
    public const string EOF = "<EOF>";
    //public const string CommandGetDataUpdate = "<get_data_update>";
    //public const string CommandGetSensorStatus = "<get_sensor_status>";
    //public const string CommandSetUserInputs = "<set_user_inputs>";
    //public const string CommandGetUserInputs = "<get_user_inputs>";

    // Socket Timeout
    public const int SocketTimeout = 1000;

    // No Data
    public const string NoData = "<No Data>";
    public const string NotAvailable = "—";
    public const string HelideckReportNoData = "";

    // Data Calculatiosn
    public const int DataCalculationSteps = 4;

    // Error Message
    public const int MaxErrorMessages = 200;

    // Server Connection
    public const string DefaultServerAddress = "127.0.0.1";
    public const int ServerPortDefault = 8108;
    public const int PortMin = 0;
    public const int PortMax = 65535;

    // MODBUS
    public const int MODBUSPortDefault = 502;

    // MODBUS Slave ID
    public const int MODBUSSlaveIDMin = 0;
    public const int MODBUSSlaveIDMax = byte.MaxValue; // 255
    public const int MODBUSSlaveIDDefault = 1;

    // Database
    public const string DefaultDatabaseName = "ses_hms";
    public const string DefaultDatabaseAddress = "127.0.0.1";
    public const int DefaultDatabasePort = 3306;
    public const int DatabaseStorageTimeMin = 0;
    public const int DatabaseStorageTimeMax = 365;
    public const int DatabaseStorageTimeDefault = 183;
    public const int DatabaseMessagesStorageTimeDefault = 90; 

    // Program Save Frequency (default save to database frequency)
    public const int DatabaseSaveFreqDefault = 1000;
    public const int DatabaseSaveFreqMin = 500;
    public const int DatabaseSaveFreqMax = 5000;

    // File Read Frequency
    public const int FileReadFreqDefault = 500;
    public const int FileReadFreqMin = 100;
    public const int FileReadFreqMax = 120000;

    // File Reader
    public const string FileReaderDelimiterDefault = ",";

    // Data Timeout
    public const double DataTimeoutDefault = 10000;
    public const double DataTimeoutMin = 500;
    public const double DataTimeoutMax = 180000; // 3 min

    // GUI Data Limit
    public const int GUIDataLimitDefault = 200;
    public const int GUIDataLimitMin = 100;
    public const int GUIDataLimitMax = 5000;

    public const int GUIDataLinesDefault = 10;
    public const int GUIDataLinesMin = 1;
    public const int GUIDataLinesMax = 100;

    // Max sensors
    public const int MaxSensors = 11;

    // Sensor Gruppe
    public const int NO_SENSOR_GROUP_ID = -1;

    // Helideck Height
    public const double HelideckHeightMin = 0;
    public const double HelideckHeightMax = 500;
    public const double HelideckHeightDefault = 0;

    // Wind Sensor Height
    public const double WindSensorHeightMin = 0;
    public const double WindSensorHeightMax = 500;
    public const double WindSensorHeightDefault = 0;

    // Wind Sensor Distance
    public const double WindSensorDistanceMin = 0;
    public const double WindSensorDistanceMax = 500;
    public const double WindSensorDistanceDefault = 0;

    // Wave Height Cutoff
    public const double WaveHeightCutoffMin = 0;
    public const double WaveHeightCutoffMax = 1;
    public const double WaveHeightCutoffDefault = 0.1;

    // Vind justert til X meter over helideck
    public const double WindAdjustmentAboveHelideck = 10;

    // NDB Frequency
    public const double NDBFrequencyMin = 190;
    public const double NDBFrequencyMax = 1750;
    public const double NDBFrequencyDefault = NDBFrequencyMin;

    // VHF Frequency
    public const double VHFFrequencyMin = 30;
    public const double VHFFrequencyMax = 299;
    public const double VHFFrequencyDefault = VHFFrequencyMin;

    // Marine Channel
    public const double MarineChannelMin = 1;
    public const double MarineChannelMax = 88;
    public const double MarineChannelDefault = 16;

    // Wind Correction R
    public const double MSICorrectionRMin = 1;
    public const double WindCorrectionRMax = 2;

    // Wind Sensor Data Rate
    public const double WindSamplesPerTransmissionMin = 1;
    public const double WindSamplesPerTransmissionMax = 60;
    public const double WindSamplesPerTransmissionDefault = 1;

    // Helicopter WSI Limits
    public const double HelicopterWSIMin = 1;
    public const double HelicopterWSIMax = 100;
    public const double HelicopterWSIDefault = 43;

    // 20 minutes in seconds
    public const int Minutes20 = 1200;
    // 30 minutes in seconds
    public const int Minutes30 = 1800;
    // 3 hours in seconds
    public const int Hours3 = 10800;

    // Data Calculations Buffer størrelse
    //public const double Minutes20Buffer = 5;
    //public const double Minutes30Buffer = 6;
    //public const double Hours3Buffer = 20;

    // MSI / WSI Max
    public const double MSIMax = 91;  // CAP
    public const double WSIMax = 100; // CAP

    // Chart Axis Margins
    public const double PitchAxisMargin = 1;
    public const double RollAxisMargin = 1;
    public const double InclinationAxisMargin = 0.5;
    public const double HeaveHeightAxisMargin = 1;
    public const double SignificantHeaveRateAxisMargin = 0.3;

    // Heading
    public const int HeadingMin = 1;
    public const int HeadingMax = 360;
    public const int HeadingDefault = HeadingMax;
    public const int MagneticDeclinationDefault = 0;

    // Motion Limit Defaults
    public const double MotionLimitDefaultPitchRoll = 4;
    public const double MotionLimitDefaultInclination = 4.5;
    public const double MotionLimitDefaultHeaveHeight = 5;
    public const double MotionLimitDefaultSignificantHeaveRate = 1.3;

    // PDF A4
    public const double A4Width = 3508;
    public const double A4Height = 2480;

    // Helideck Report: Return Load (passengers)
    public const int ReturnLoadMin = 0;
    public const int ReturnLoadMax = 50;
    public const int ReturnLoadDefault = 0;

    // Helideck Report: HelicopterLoad
    public const int HelicopterLoadMin = 0;
    public const int HelicopterLoadMax = 100000;
    public const int HelicopterLoadDefault = 0;

    // Konstanter for atmosfæriske kalkulasjoner
    public const double KelvinZero = 273.15;
    public const double GravityAcceleration = 9.80665;
    public const double SpecificGasConstantDryAir = 287.058;
    public const double StandardPressure = 1013.25;
    public const double MeanAdiabaticLapseRate = 0.0065;

    // Filer og kataloger
    public const string HelideckReportFolder = "Helideck Report";
    public const string HelideckReportFilename = "helideck_report";
    public const string HelideckReportName = "Helidekkrapport";
    public const string ScreenCaptureFilename = "hms_screencapture";
    public const string OffshoreWeatherReportFilename = "offshore_weather_report";

    // Email
    public const int DefaultSMTPPort = 25;

    // Cloud Layers
    public const int TOTAL_CLOUD_LAYERS = 4;

    // Name not set (pga admin mode ikke på i server)
    public const string NameNotSet = "No name (server admin mode is off)";

    // License / Activation
    public const int LicenseMaxClientsMin = 0;
    public const int LicenseMaxClientsMax = 100;
    public const int LicenseMaxClientsDefault = 0;

    // Landing Status Trend
    public const int landingTrendDisplayListMax = 400;          // Hvor mange segmenter trend linjen skal deles opp i / oppløsning på trend linjen
                                                                // På skjerm med 3840 pixel bredde er linjen ca 1400 pixler bred
                                                                // På 1920 skjerm vil den være ca 700 pixler bred
                                                                // Opplever "flicker" på trend linjen med for høy oppløsning, satt ned til 400
    public const int landingTrendHistoryDisplayListMax = 1000;

    // RWD Trend
    public const int rwdTrendDisplayListMax = 300;              // Hvor mange segmenter trend linjen skal deles opp i / oppløsning på trend linjen
                                                                // På skjerm med 3840 pixel bredde er linjen ca 1000 pixler bred
                                                                // På 1920 skjerm vil den være ca 500 pixler bred
                                                                // Opplever "flicker" på trend linjen med for høy oppløsning, satt ned til 300

    // Graph Accuracy
    public const double GraphCullFrequency20m = 4000;           // 4 sek
    public const double GraphCullFrequency30m = 6000;           // 6 sek
    public const double GraphCullFrequency3h = 30000;           // 30 sek

    // Korreksjoner for DateTimeContinuousAxis
    // Disse korreksjonene legges inn for å få tidspunkt-label på X aksen til å vises korrekt
    public const int ChartTimeCorrMin = 3;
    public const int ChartTimeCorrMax = -2;

    // Buffer Fill
    public const double WindBufferFill95Pct2m = 456;            // 4 hz * 2 min * 60 sek * 0.95 (95%)
    public const double WindBufferFill95Pct10m = 2280;          // 4 hz * 10 min * 60 sek * 0.95 (95%)
    public const double MotionBufferFill99Pct = 2376;           // 2 hz * 20 min * 60 sek * 0.99 (99%)

    // Binary data delimiter
    public const string BinaryDataDelimiter = "-";

    // EMS Page: Wave Chart max
    public const int WaveChartSWHInc = 5;
    public const int WaveChartPeriodInc = 10;

}
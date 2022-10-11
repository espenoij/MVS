using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

class CommonTypes
{
}

public enum HelicopterCategory
{
    Medium,
    Heavy
}

public enum HelideckCategory
{
    [Description("CAT 1")]
    Category1,
    [Description("CAT 1 (Semi-Sub)")]
    Category1_Semisub,
    [Description("CAT 2")]
    Category2,
    [Description("CAT 3")]
    Category3,
    [Description("CAT 2 or CAT 3")]
    Category2_or_3
}

public enum DayNight
{
    Day,
    Night
}

public enum HelicopterType
{
    [Description("Default - CAT A")]
    Default,    // Heavy
    [Description("AS332 - CAT A")]
    AS332,      // Heavy
    [Description("AS365 - CAT B")]
    AS365,      // Medium
    [Description("AW139 - CAT B")]
    AW139,      // Medium
    [Description("AW169 - CAT B")]
    AW169,      // Medium
    [Description("AW189 - CAT A")]
    AW189,      // Heavy
    [Description("B212 - CAT B")]
    B212,       // Medium
    [Description("B412 - CAT B")]
    B412,       // Medium
    //[Description("B525 - CAT B")]
    //B525,       // Medium
    [Description("EC135 - CAT B")]
    EC135,      // Medium
    [Description("EC155 - CAT B")]
    EC155,      // Medium
    //[Description("EC175 - CAT B")]
    //EC175,      // Medium
    [Description("EC225 - CAT A")]
    EC225,      // Heavy
    [Description("H145 - CAT B")]
    H145,       // Medium
    [Description("H175 - CAT B")]
    H175,       // Medium
    [Description("S61 - CAT A")]
    S61,        // Heavy
    [Description("S76 - CAT B")]
    S76,        // Medium
    [Description("S92 - CAT A")]
    S92         // Heavy
}

public enum DisplayMode
{
    PreLanding,
    OnDeck
}

public enum LimitType
{
    PitchRoll,
    Inclination,
    SignificantHeaveRate,
    HeaveHeight
}

public enum DataStatus
{
    TIMEOUT_ERROR, // Timeout / error
    OK,
    OK_NA,          // Brukes til wind og motion data som må buffres til en gitt prosent før de kan vises
    NONE
}

public enum LimitStatus
{
    NA,
    OVER_LIMIT,
    OK
}

public enum ValueType
{
    // NB! Matcher ID i config fil
    // Dette er variabler som blir direkte linket til sensor data.
    // Må matche med verdiene i SensorData.config.
    None = 0,
    Latitude = 1,
    Longitude = 2,
    Pitch = 3,
    Roll = 4,
    Heave = 5,
    HeaveRate = 6,
    AirTemperature = 7,
    AirHumidity = 8,
    AirPressure = 9,
    Visibility = 10,
    Weather = 11,
    CloudLayer1Base = 12,
    CloudLayer1Coverage = 13,
    CloudLayer2Base = 14,
    CloudLayer2Coverage = 15,
    CloudLayer3Base = 16,
    CloudLayer3Coverage = 17,
    CloudLayer4Base = 18,
    CloudLayer4Coverage = 19,
    SensorWindDirection = 20,
    SensorWindSpeed = 21,
    VesselHeading = 22,
    VesselCOG = 23,
    VesselSOG = 24,
    AccelerationX = 25,
    AccelerationY = 26,
    AccelerationZ = 27,
    Wave = 28,
    SeaTemperature = 29,

    // Database
    Database = 30,

    // Sensor Status
    SensorMRU = 31,
    SensorGyro = 32,
    SensorWind = 33,
    SensorSOGCOG = 34,

    // Ikke lagret i config fil
    // Dette er data som beregnes fra variablene over, dvs. fra sensor data
    PitchMax20m,
    PitchMax3h,
    PitchMaxUp20m,
    PitchMaxDown20m,
    RollMax20m,
    RollMax3h,
    RollMaxLeft20m,
    RollMaxRight20m,
    HeaveHeight,
    HeaveHeightMax20m,
    HeaveHeightMax3h,
    HeavePeriodMean,
    SignificantHeaveRate,
    SignificantHeaveRateMax20m,
    SignificantHeaveRateMax3h,
    SignificantHeaveRate95pct,
    MaxHeaveRate,
    Inclination,
    InclinationMax20m,
    InclinationMax3h,
    AirDewPoint,
    AirPressureQNH,
    AirPressureQFE,
    HelideckLight,
    LandingStatus,          // CAP
    RWDStatus,              // CAP
    MSI,                    // CAP
    WSI,                    // CAP
    RelativeWindDir,
    AreaWindDirection2m,
    AreaWindSpeed2m,
    AreaWindGust2m,
    HelideckWindDirectionRT,
    HelideckWindSpeedRT,
    HelideckWindDirection2m,
    HelideckWindDirection10m,
    HelideckWindSpeed2m,
    HelideckWindSpeed10m,
    HelideckWindGust2m,
    HelideckWindGust10m,
    EMSWindDirectionRT,
    EMSWindSpeedRT,
    EMSWindDirection2m,
    EMSWindDirection10m,
    EMSWindSpeed2m,
    EMSWindSpeed10m,
    EMSWindGust2m,
    EMSWindGust10m,
    HelideckHeading,
    WindDirectionDelta,
    VesselHeadingDelta,
    HelicopterHeading,
    WaveMax20m,
    WaveMax3h,
    WavePeriod,
    WavePeriodMax20m,
    WavePeriodMax3h,
    SignificantWaveHeight,
    SignificantWaveHeightMax20m,
    SignificantWaveHeightMax3h,

    // Motion Limits
    MotionLimitPitchRoll,
    MotionLimitInclination,
    MotionLimitHeaveHeight,
    MotionLimitSignificantHeaveRate,

    // Diverse andre data
    SettingsHelicopterType,
    SettingsHelideckCategory,
    SettingsDayNight,
    SettingsDisplayMode,
    SettingsOnDeckTime,
    SettingsOnDeckHelicopterHeading,
    SettingsOnDeckHelicopterHeadingCorrected,
    SettingsOnDeckVesselHeading,
    SettingsOnDeckWindDirection,
    SettingsHelideckWindSensorHeight,
    SettingsHelideckWindSensorDistance,
    SettingsAreaWindSensorHeight,
    SettingsAreaWindSensorDistance,
    SettingsNDBFrequency,
    SettingsNDBIdent,
    SettingsVHFFrequency,
    SettingsLogFrequency,
    SettingsMarineChannel,
    SettingsDynamicPositioning,
    SettingsAccurateMonitoringEquipment,
    SettingsVesselName,
    SettingsEmailServer,
    SettingsEmailPort,
    SettingsEmailUsername,
    SettingsEmailPassword,
    SettingsEmailSecureConnection,
    SettingsRestrictedSector,
    SettingsDataVerification,
}

public enum HelideckStatusType
{
    NO_DATA,
    OFF,
    BLUE,
    AMBER,
    RED,
    GREEN
}

public enum WindMeasurement
{
    [Description("Real Time")]
    RealTime,
    [Description("2-Minute Mean")]
    TwoMinuteMean,
    [Description("10-Minute Mean")]
    TenMinuteMean
}

public class GustData
{
    // Variabler til bruk under kalkulasjoner
    public List<Wind> gust3SecDataList = new List<Wind>();
    public List<Wind> gustDataList = new List<Wind>();
    public double windDataGustMax = 0;
    public double minutes;
    public DateTime lastTimeStamp;

    // Result
    public double windGust = 0;

    public void Reset()
    {
        gust3SecDataList.Clear();
        gustDataList.Clear();
        windDataGustMax = 0;
        lastTimeStamp = DateTime.MinValue;

        windGust = 0;
    }
}

public class Wind
{
    public double dir { get; set; }
    public double spd { get; set; }
    public DateTime timestamp { get; set; }
}

public enum DatabaseMaintenanceType
{
    SENSOR,
    HMS,
    STATUS,
    ALL
}

public enum DecimalSeparator
{
    //[Description("OS Default")]
    //OSDefault,
    [Description("Point")]
    Point,
    [Description("Comma")]
    Comma
}

public enum EmailStatus
{
    PREVIEW,
    SENDING,
    SUCCESS,
    FAILED
}

public class HelicopterOperator
{
    public int id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
}

public enum LightsOutputType
{
    Off,
    Blue,
    BlueFlash,
    Amber,
    AmberFlash,
    Red,
    RedFlash,
    Green,
    GreenFlash
}

public enum DirectionReference
{
    [Description("Vessel Heading")]
    VesselHeading,
    [Description("Magnetic North")]
    MagneticNorth,
    [Description("True North")]
    TrueNorth
}

public class HelideckStatus
{
    public HelideckStatusType status { get; set; }
    public DateTime timestamp { get; set; }

    public double wind { get; set; }
    public double rwd { get; set; }

    public HelideckStatus()
    {
    }

    public HelideckStatus(HelideckStatus helideckStatus)
    {
        status = helideckStatus.status;
        timestamp = helideckStatus.timestamp;
    }
}

public class RWDData : INotifyPropertyChanged
{
    // Change notification
    public event PropertyChangedEventHandler PropertyChanged;

    public double _rwd { get; set; }
    public double rwd 
    {
        get
        {
            return _rwd;
        }

        set
        {
            _rwd = value;
            OnPropertyChanged(nameof(graphDataY));
        }
    }

    public double _wind { get; set; }
    public double wind
    {
        get
        {
            return _wind;
        }

        set
        {
            _wind = value;
            OnPropertyChanged(nameof(graphDataX));
        }
    }

    public DataStatus status { get; set; }
    public DateTime timestamp { get; set; }

    public void Set(RWDData rwdData)
    {
        rwd = rwdData.rwd;
        wind = rwdData.wind;
        status = rwdData.status;
        timestamp = rwdData.timestamp;
    }

    public double graphDataX
    {
        get
        {
            if (status == DataStatus.OK)
            {
                double val = wind;
                if (val > 60)
                    val = 60;
                return val;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double graphDataY
    {
        get
        {
            if (status == DataStatus.OK)
            {
                double val = Math.Abs(rwd);
                if (val > 60)
                    val = 60;
                return val;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    // Variabel oppdatert
    // Dersom navn ikke er satt brukes kallende medlem sitt navn
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public enum WarningBarMessageType
{
    RestartRequired,
    DataVerification,
}

public enum InputDataType
{
    Text,
    Binary
}

public enum BinaryType
{
    [Description("Byte (1 Byte)")]
    Byte,
    [Description("Int16 (2 Bytes)")]
    Int16,
    [Description("Uint16 (2 Bytes)")]
    Uint16,
    [Description("Int32 (4 Bytes)")]
    Int32,
    [Description("Uint32 (4 Bytes)")]
    Uint32,
    [Description("Float (4 Bytes)")]
    Float,
    [Description("Long (8 Bytes)")]
    Long,
    [Description("Ulong (8 Bytes)")]
    Ulong,
    [Description("Int64 (8 Bytes)")]
    Int64,
    [Description("Uint64 (8 Bytes)")]
    Uint64,
    [Description("Double (8 Byte)")]
    Double
}
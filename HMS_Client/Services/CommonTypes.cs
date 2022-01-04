using System;
using System.Collections.Generic;
using System.ComponentModel;

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
    [Description("Category 1")]
    Category1,
    [Description("Category 2")]
    Category2,
    [Description("Category 3")]
    Category3
}

public enum DayNight
{
    Day,
    Night
}

public enum HelicopterType
{
    AS332,      // Heavy
    AS365,      // Medium
    AW139,      // Medium
    AW169,      // Medium
    AW189,      // Heavy
    B212,       // Medium
    B412,       // Medium
    B525,       // Medium
    EC135,      // Medium
    EC155,      // Medium
    EC175,      // Medium
    EC225,      // Heavy
    H145,       // Medium
    H175,       // Medium
    S61,        // Heavy
    S76,        // Medium
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
    HeaveAmplitude
}

public enum DataStatus
{
    TIMEOUT_ERROR, // Timeout / error
    OK,
    NONE
}

public enum LimitStatus
{
    OVER_LIMIT,
    OK
}

public enum ValueType
{
    // NB! Matcher ID i config fil
    // Dette er variabler som blir direkte linket til sensor data
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
    VesselSpeed = 23,
    AccelerationX = 24,
    AccelerationY = 25,
    AccelerationZ = 26,

    // Data Verification
    TimeID = 27,
    SensorMRU = 28,
    SensorGyro = 29,
    SensorWind = 30,

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
    HeaveAmplitude,
    HeaveAmplitudeMax20m,
    HeaveAmplitudeMax3h,
    HeavePeriodMean,
    SignificantHeaveRate,
    SignificantHeaveRateMax20m,
    SignificantHeaveRateMax3h,
    SignificantHeaveRate95pct,
    MaxHeaveRate,
    SignificantWaveHeight,
    Inclination,
    InclinationMax20m,
    InclinationMax3h,
    AirDewPoint,
    AirPressureQNH,
    AirPressureQFE,
    HelideckStatus,
    MSI,
    WSI,
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
    HelideckHeading,
    WindDirectionDelta,
    VesselHeadingDelta,
    HelicopterHeading,

    // Motion Limits
    MotionLimitPitchRoll,
    MotionLimitInclination,
    MotionLimitHeaveAmplitude,
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
    SettingsRestrictedSector
}

public enum HelideckStatusType
{
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
    //public List<Wind> windDataList = new List<Wind>();
    public List<Wind> gust3SecDataList = new List<Wind>();
    public List<Wind> gustDataList = new List<Wind>();
    //public double windDataDirTotal = 0;
    //public double windDataSpdTotal = 0;
    public double windDataGustMax = 0;
    public double minutes;

    // Results
    //public double windDir = 0;
    //public double windSpeed = 0;
    public double windGust = 0;

    public void Reset()
    {
        //windDataList.Clear();
        gust3SecDataList.Clear();
        gustDataList.Clear();

        //windDataDirTotal = 0;
        //windDataSpdTotal = 0;
        windDataGustMax = 0;

        //windDir = 0;
        //windSpeed = 0;
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
    Point,
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

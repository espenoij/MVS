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
    [Description("EC135 - CAT B")]
    EC135,      // Medium
    [Description("EC155 - CAT B")]
    EC145,       // Medium
    [Description("EC175 - CAT B")]
    EC155,      // Medium
    [Description("EC145 - CAT B")]
    EC175,       // Medium
    [Description("EC225 - CAT A")]
    EC225,      // Heavy
    [Description("S76 - CAT B")]
    S76,        // Medium
    [Description("S92 - CAT A")]
    S92,         // Heavy

    //[Description("S61 - CAT A")]
    //S61,        // Heavy
    //[Description("B525 - CAT B")]
    //B525,       // Medium
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
    None = 0,

    // NB! Matcher ID i config fil
    // Dette er variabler som blir direkte linket til sensor data.
    // Må matche med verdiene i SensorData.config.
    Ref_Pitch = 1,
    Ref_Roll = 2,
    Ref_Heave = 3,

    Test_Pitch = 4,
    Test_Roll = 5,
    Test_Heave = 6,

    // Ikke lagret i config fil
    // Dette er data som beregnes fra variablene over, dvs. fra sensor data
    Ref_PitchMean20m,
    Ref_PitchMax20m,
    Ref_PitchMaxUp20m,
    Ref_PitchMaxDown20m,

    Ref_RollMean20m,
    Ref_RollMax20m,
    Ref_RollMaxLeft20m,
    Ref_RollMaxRight20m,

    Ref_HeaveMax20m,
    Ref_HeaveAmplitudeMean20m,
    Ref_HeaveAmplitudeMax20m,

    Test_PitchMean20m,
    Test_PitchMax20m,
    Test_PitchMaxUp20m,
    Test_PitchMaxDown20m,

    Test_RollMean20m,
    Test_RollMax20m,
    Test_RollMaxLeft20m,
    Test_RollMaxRight20m,

    Test_HeaveMean20m,
    Test_HeaveMax20m,
    Test_HeaveAmplitudeMax20m,

    // Motion Limits
    MotionLimitPitchRoll,
    MotionLimitHeaveAmplitude,
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
                double val = _wind;
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
                double val = Math.Abs(_rwd);
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
    protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public enum WarningBarMessageType
{
    RestartRequired
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

public enum OutputConnectionType
{
    [Description("MODBUS RTU")]
    MODBUS_RTU,
    [Description("ADAM-4060")]
    ADAM_4060
}

public enum VesselImage
{
    None,
    Triangle,
    Rig,
    Ship
}

public enum RegulationStandard
{
    NOROG,
    CAP
}

public enum OperationsMode
{
    Recording,
    Test,
    Stop
}

public enum SensorType
{
    None,
    SerialPort,
    ModbusRTU,      // Remote Terminal Unit (serial port)
    ModbusASCII,
    ModbusTCP,
    FileReader,
    FixedValue
}

public enum DatabaseSaveFrequency
{
    Sensor,
    Program,
    Freq_2hz = 500,
    Freq_1hz = 1000,
    Freq_2sec = 2000,
    Freq_3sec = 3000,
    Freq_4sec = 4000,
    Freq_5sec = 5000
}

public enum ModbusObjectType
{
    None,
    Coil,
    DiscreteInput,
    InputRegister,
    HoldingRegister
}

public enum MRUType
{
    [Description("None")]
    None,
    [Description("Reference MRU")]
    ReferenceMRU,
    [Description("Test MRU")]
    TestMRU
}

public enum VerificationInputSetup
{
    [Description("No Input")]
    None,
    [Description("Reference MRU Only")]
    ReferenceMRU,
    [Description("Reference MRU + Test MRU")]
    ReferenceMRU_TestMRU,
}


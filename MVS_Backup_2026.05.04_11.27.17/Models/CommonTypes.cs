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
    Ref_PitchMax,
    Ref_PitchMaxUp,
    Ref_PitchMaxDown,
    Ref_PitchMean,
    Ref_PitchMeanMax,

    Ref_RollMax,
    Ref_RollMaxLeft,
    Ref_RollMaxRight,
    Ref_RollMean,
    Ref_RollMeanMax,

    Ref_HeaveMax,
    Ref_HeaveMaxUp,
    Ref_HeaveMaxDown,
    Ref_HeaveMean,
    Ref_HeaveMeanMax,

    Test_PitchMax,
    Test_PitchMaxUp,
    Test_PitchMaxDown,
    Test_PitchMean,
    Test_PitchMeanMax,

    Test_RollMax,
    Test_RollMaxLeft,
    Test_RollMaxRight,
    Test_RollMean,
    Test_RollMeanMax,

    Test_HeaveMax,
    Test_HeaveMaxUp,
    Test_HeaveMaxDown,
    Test_HeaveMean,
    Test_HeaveMeanMax,

    Dev_Pitch,
    Dev_PitchMean,
    Dev_PitchMax,

    Dev_Roll,
    Dev_RollMean,
    Dev_RollMax,

    Dev_Heave,
    Dev_HeaveMean,
    Dev_HeaveMax,

    // Motion Limits
    MotionLimitPitchRoll,
    MotionLimitHeaveAmplitude,
}

public enum ProjectStatusType
{
    OFF,
    RED,
    AMBER,
    GREEN
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
    Stop,
    ViewData
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

public enum InputMRUType
{
    [Description("No Input")]
    None,
    [Description("Reference MRU")]
    ReferenceMRU,
    [Description("Test MRU")]
    TestMRU,
    [Description("Reference MRU + Test MRU")]
    ReferenceMRU_TestMRU,
}

public enum ImportResultCode
{
    OK,
    DatabaseError,
    ConnectionToMVSDatabaseFailed,
    NoDataFoundForSelectedTimeframe
}
public enum ProcessingType
{
    LIVE_DATA,
    DATA_ANALYSIS
}



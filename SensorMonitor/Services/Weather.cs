using System.ComponentModel;

static class Weather
{
    public static int Encode(int severity, int phenomena1, int phenomena2)
    {
        // Slår de tre verdiene sammen til en verdi som sendes til klient. Bruker 8 bit til hver verdi.
        return (severity << 16) | (phenomena1 << 8) | (phenomena2 << 0);
    }

    public static WeatherSeverity DecodeSeverity(int encodedWeather)
    {
        // Dekoder weather severity fra encoded verdi
        return (WeatherSeverity)((encodedWeather >> 16) & 0xFF);
    }

    public static WeatherPhenomena DecodePhenomena1(int encodedWeather)
    {
        // Dekoder weather phenomena fra encoded verdi
        return (WeatherPhenomena)((encodedWeather >> 8) & 0xFF);
    }

    public static WeatherPhenomena DecodePhenomena2(int encodedWeather)
    {
        // Dekoder weather phenomena fra encoded verdi
        return (WeatherPhenomena)((encodedWeather >> 0) & 0xFF);
    }

    public static WeatherPhenomena GetPhenomena(string str)
    {
        switch (str)
        {
            case "BC":
                return WeatherPhenomena.BC;
            case "BL":
                return WeatherPhenomena.BL;
            case "BR":
                return WeatherPhenomena.BR;
            case "DR":
                return WeatherPhenomena.DR;
            case "DS":
                return WeatherPhenomena.DS;
            case "DU":
                return WeatherPhenomena.DU;
            case "DZ":
                return WeatherPhenomena.DZ;
            case "FC":
                return WeatherPhenomena.FC;
            case "FG":
                return WeatherPhenomena.FG;
            case "FU":
                return WeatherPhenomena.FU;
            case "FZ":
                return WeatherPhenomena.FZ;
            case "GR":
                return WeatherPhenomena.GR;
            case "GS":
                return WeatherPhenomena.GS;
            case "HZ":
                return WeatherPhenomena.HZ;
            case "IC":
                return WeatherPhenomena.IC;
            case "MI":
                return WeatherPhenomena.MI;
            case "PL":
                return WeatherPhenomena.PL;
            case "PO":
                return WeatherPhenomena.PO;
            case "PR":
                return WeatherPhenomena.PR;
            case "PY":
                return WeatherPhenomena.PY;
            case "RA":
                return WeatherPhenomena.RA;
            case "SA":
                return WeatherPhenomena.SA;
            case "SG":
                return WeatherPhenomena.SG;
            case "SH":
                return WeatherPhenomena.SH;
            case "SN":
                return WeatherPhenomena.SN;
            case "SQ":
                return WeatherPhenomena.SQ;
            case "SS":
                return WeatherPhenomena.SS;
            case "TS":
                return WeatherPhenomena.TS;
            case "UP":
                return WeatherPhenomena.UP;
            case "VA":
                return WeatherPhenomena.VA;
            case "VC":
                return WeatherPhenomena.VC;

            default:
                return WeatherPhenomena.None;
        }
    }
}

public enum WeatherPhenomena
{
    [Description("None")]
    None,
    [Description("Patches of")]
    BC,
    [Description("Blowing")]
    BL,
    [Description("Mist")]
    BR,
    [Description("Low Drifting")]
    DR,
    [Description("Dust Storm")]
    DS,
    [Description("Dust")]
    DU,
    [Description("Drizzle")]
    DZ,
    [Description("Funnel Cloud")]
    FC,
    [Description("Fog")]
    FG,
    [Description("Smoke")]
    FU,
    [Description("Freezing")]
    FZ,
    [Description("Hail")]
    GR,
    [Description("Small Hail")]
    GS,
    [Description("Haze")]
    HZ,
    [Description("Ice Crystals")]
    IC,
    [Description("Shallow")]
    MI,
    [Description("Ice Pellets")]
    PL,
    [Description("Well-Developed Dust")]
    PO,
    [Description("Partial")]
    PR,
    [Description("Spray")]
    PY,
    [Description("Rain")]
    RA,
    [Description("Sand")]
    SA,
    [Description("Snow Grains")]
    SG,
    [Description("Showers of")]
    SH,
    [Description("Snow")]
    SN,
    [Description("Squals Moderate")]
    SQ,
    [Description("Sandstorm")]
    SS,
    [Description("Thunderstorm")]
    TS,
    [Description("Unknown Precipitation")]
    UP,
    [Description("Volcanic Ash")]
    VA,
    [Description("In the Vicinity")]
    VC
}

public enum WeatherSeverity
{
    [Description("")]
    None,
    [Description("Light")]
    Light,
    [Description("Moderate")]
    Moderate,
    [Description("Heavy")]
    Heavy
}

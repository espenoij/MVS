using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace MVS
{
    public class DataCalculations
    {
        public CalculationType type { get; set; }
        public double parameter { get; set; }

        // Time Average
        private double timeAverageTotal = 0;
        private List<TimeData> timeAverageDataList = new List<TimeData>();

        // Time Max Absolute
        private double timeMaxAbsoluteMaxValue = 0;
        private List<TimeData> timeMaxAbsoluteDataList = new List<TimeData>();

        // Time Heighest
        private double timeHighestMaxValue = double.MinValue;
        private List<TimeData> timeHighestDataList = new List<TimeData>();

        // Time Lowest
        private double timeLowestMinValue = double.MaxValue;
        private List<TimeData> timeLowestDataList = new List<TimeData>();

        // Time Max Wave Height
        private double timeMaxWaveHeightMaxValue = 0;
        private double timeMaxWaveHeightLast = double.NaN;
        private WavePhase timeMaxWaveHeightWavePhase = WavePhase.Init;
        private double timeMaxWaveHeightWaveTop = double.NaN;
        private double timeMaxWaveHeightWaveBottom = double.NaN;
        private List<TimeData> timeMaxWaveHeightDataList = new List<TimeData>();

        // Wave Amplitude
        private double waveAmplitudeLast = double.NaN;
        private WavePhase waveAmplitudeWavePhase = WavePhase.Init;
        private double waveAmplitudeWaveTop = double.NaN;
        private double waveAmplitudeWaveBottom = double.NaN;
        private double waveAmplitudeValue = double.NaN;

        // Wave Mean Height
        private double waveMeanAmplitudeLast = double.NaN;
        private WavePhase waveMeanAmplitudeWavePhase = WavePhase.Init;
        private double waveMeanAmplitudeWaveTop = double.NaN;
        private double waveMeanAmplitudeWaveBottom = double.NaN;
        private double waveMeanAmplitudeValue = double.NaN;
        private double waveMeanAmplitudeTotal = 0;
        private List<TimeData> waveMeanAmplitudeDataList = new List<TimeData>();

        public DataCalculations()
        {
            type = CalculationType.None;
            parameter = 0.0;
        }

        public DataCalculations(CalculationType type, double parameter)
        {
            this.type = type;
            this.parameter = parameter;
        }

        public double DoCalculations(string newData, DateTime newTimeStamp, ErrorHandler errorHandler, ErrorMessageCategory errorMessageCat, AdminSettingsVM adminSettingsVM)
        {
            // Hvorfor inndata som string? Fordi vi prosesserer ikke-numerisk data også, f.eks. vær-koder.

            // Returnerer ikke numerisk verdi som default
            double result = double.NaN;

            try
            {
                if (newData != null)
                {
                    // NB! Viktig å bruke cultureInfo i konvertering til og fra string
                    double value;

                    switch (type)
                    {
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Addisjon
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        case CalculationType.Addition:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = value + parameter;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Subtraction
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        case CalculationType.Subtraction:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = value - parameter;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Multiplication
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        case CalculationType.Multiplication:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = value * parameter;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Division
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        case CalculationType.Division:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                if (parameter != 0.0)
                                    result = value / parameter;
                                else
                                    result = 0.0;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Average
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        // Beskrivelse:
                        // Returnerer snittverdien av et data sett innsamlet over en gitt tid

                        // Brukes til:
                        // - Wind speed

                        case CalculationType.TimeAverage:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                // Sjekke at ny verdi ikke er lik den forrige som ble lagt inn i datasettet -> unngå duplikater
                                if (!double.IsNaN(value) &&
                                    timeAverageDataList.LastOrDefault()?.timestamp != newTimeStamp)
                                {
                                    // Legge inn den nye verdien i data settet
                                    timeAverageDataList.Add(new TimeData() { data = value, timestamp = newTimeStamp });

                                    // Legge til i total summen
                                    timeAverageTotal += value;

                                    // Sjekke om vi skal ta ut gamle verdier
                                    if (parameter != 0)
                                    {
                                        while (timeAverageDataList.Count > 0 && timeAverageDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                        {
                                            // Trekke fra i total summen
                                            timeAverageTotal -= timeAverageDataList[0].data;

                                            // Fjerne fra verdi listen
                                            timeAverageDataList.RemoveAt(0);
                                        }
                                    }
                                }

                                // Beregne gjennomsnitt av de verdiene som ligger i datasettet
                                if (timeAverageDataList.Count > 0)
                                    result = timeAverageTotal / timeAverageDataList.Count;
                                else
                                    result = 0;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Max Absolute
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer høyeste verdi i et datasett innsamlet over en angitt tid.

                        /// Brukes til:
                        /// Max pitch
                        /// Max roll
                        /// Max Inclination beregnes av klienten ut i fra pitch og roll

                        case CalculationType.TimeMaxAbsolute:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                // Sjekke at ny verdi ikke er lik den forrige som ble lagt inn i datasettet -> unngå duplikater
                                if (!double.IsNaN(value) &&
                                    timeMaxAbsoluteDataList.LastOrDefault()?.timestamp != newTimeStamp)
                                {

                                    // Legge inn den nye absolutte verdien i data settet
                                    timeMaxAbsoluteDataList.Add(new TimeData()
                                    {
                                        data = Math.Abs(value),
                                        timestamp = newTimeStamp
                                    });

                                    // Større max verdi?
                                    if (Math.Abs(value) > timeMaxAbsoluteMaxValue)
                                    {
                                        timeMaxAbsoluteMaxValue = Math.Abs(value);
                                    }

                                    // Sjekke om vi skal ta ut gamle verdier
                                    bool findNewMaxValue = false;
                                    if (parameter != 0)
                                    {
                                        while (timeMaxAbsoluteDataList.Count > 0 && timeMaxAbsoluteDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                        {
                                            // Sjekke om dette var høyeste verdi
                                            if (timeMaxAbsoluteDataList[0].data == timeMaxAbsoluteMaxValue)
                                            {
                                                // Finne ny høyeste verdi
                                                findNewMaxValue = true;
                                            }

                                            // Fjerne gammel verdi fra verdiliste
                                            timeMaxAbsoluteDataList.RemoveAt(0);
                                        }
                                    }

                                    // Finne ny høyeste verdi
                                    if (findNewMaxValue)
                                    {
                                        double oldMaxValue = timeMaxAbsoluteMaxValue;
                                        bool foundNewMax = false;
                                        timeMaxAbsoluteMaxValue = 0;

                                        for (int i = 0; i < timeMaxAbsoluteDataList.Count && !foundNewMax; i++)
                                        {
                                            // Kan avslutte søket dersom vi finne en verdi like den gamle max verdien (ingen er høyere)
                                            if (timeMaxAbsoluteDataList[i]?.data == oldMaxValue)
                                            {
                                                timeMaxAbsoluteMaxValue = oldMaxValue;
                                                foundNewMax = true;
                                            }
                                            else
                                            {
                                                if (timeMaxAbsoluteDataList[i]?.data > timeMaxAbsoluteMaxValue)
                                                    timeMaxAbsoluteMaxValue = timeMaxAbsoluteDataList[i].data;
                                            }
                                        }
                                    }
                                }

                                result = timeMaxAbsoluteMaxValue;
                            }
                            break;


                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Max
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer høyeste verdi i et datasett innsamlet over en angitt tid.

                        /// Brukes til:
                        /// Max pitch up
                        /// Max roll left

                        case CalculationType.TimeMax:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                // Sjekke at ny verdi ikke er lik den forrige som ble lagt inn i datasettet -> unngå duplikater
                                if (!double.IsNaN(value) &&
                                    timeHighestDataList.LastOrDefault()?.timestamp != newTimeStamp)
                                {
                                    // Legge inn den nye verdien i data settet
                                    timeHighestDataList.Add(new TimeData()
                                    {
                                        data = value,
                                        timestamp = newTimeStamp
                                    });

                                    // Større max verdi?
                                    if (value > timeHighestMaxValue)
                                    {
                                        timeHighestMaxValue = value;
                                    }

                                    // Sjekke om vi skal ta ut gamle verdier
                                    bool findNewMaxValue = false;
                                    if (parameter != 0)
                                    {
                                        while (timeHighestDataList.Count > 0 && timeHighestDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                        {
                                            // Sjekke om dette var høyeste verdi
                                            if (timeHighestDataList[0].data == timeHighestMaxValue)
                                            {
                                                // Finne ny høyeste verdi
                                                findNewMaxValue = true;
                                            }

                                            // Fjerne gammel verdi fra verdiliste
                                            timeHighestDataList.RemoveAt(0);
                                        }
                                    }

                                    // Finne ny høyeste verdi
                                    if (findNewMaxValue)
                                    {
                                        double oldMaxValue = timeHighestMaxValue;
                                        timeHighestMaxValue = double.MinValue;
                                        bool foundNewMax = false;

                                        for (int i = 0; i < timeHighestDataList.Count && !foundNewMax; i++)
                                        {
                                            // Kan avslutte søket dersom vi finne en verdi like den gamle max verdien (ingen er høyere)
                                            if (timeHighestDataList[i]?.data == oldMaxValue)
                                            {
                                                timeHighestMaxValue = oldMaxValue;
                                                foundNewMax = true; ;
                                            }
                                            else
                                            {
                                                if (timeHighestDataList[i]?.data > timeHighestMaxValue)
                                                    timeHighestMaxValue = timeHighestDataList[i].data;
                                            }
                                        }
                                    }
                                }

                                result = timeHighestMaxValue;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Min
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer laveste verdi i et datasett innsamlet over en angitt tid.

                        /// Brukes til:
                        /// Max pitch up
                        /// Max roll left

                        case CalculationType.TimeMin:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                // Sjekke at ny verdi ikke er lik den forrige som ble lagt inn i datasettet -> unngå duplikater
                                if (!double.IsNaN(value) &&
                                    timeLowestDataList.LastOrDefault()?.timestamp != newTimeStamp)
                                {
                                    // Legge inn den nye verdien i data settet
                                    timeLowestDataList.Add(new TimeData()
                                    {
                                        data = value,
                                        timestamp = newTimeStamp
                                    });

                                    // Mindre enn minste verdi?
                                    if (value < timeLowestMinValue)
                                    {
                                        timeLowestMinValue = value;
                                    }

                                    // Sjekke om vi skal ta ut gamle verdier
                                    bool findNewMaxValue = false;
                                    if (parameter != 0)
                                    {
                                        while (timeLowestDataList.Count > 0 && timeLowestDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                        {
                                            // Sjekke om dette var laveste verdi
                                            if (timeLowestDataList[0].data == timeLowestMinValue)
                                            {
                                                // Finne ny laveste verdi
                                                findNewMaxValue = true;
                                            }

                                            // Fjerne gammel verdi fra verdiliste
                                            timeLowestDataList.RemoveAt(0);
                                        }
                                    }

                                    // Finne ny laveste verdi
                                    if (findNewMaxValue)
                                    {
                                        double oldMinValue = timeLowestMinValue;
                                        timeLowestMinValue = double.MaxValue;
                                        bool foundNewMin = false;

                                        for (int i = 0; i < timeLowestDataList.Count && !foundNewMin; i++)
                                        {
                                            // Kan avslutte søket dersom vi finne en verdi like den gamle max verdien (ingen er høyere)
                                            if (timeLowestDataList[i]?.data == oldMinValue)
                                            {
                                                timeLowestMinValue = oldMinValue;
                                                foundNewMin = true;
                                            }
                                            else
                                            {
                                                if (timeLowestDataList[i]?.data < timeLowestMinValue)
                                                    timeLowestMinValue = timeLowestDataList[i].data;
                                            }
                                        }
                                    }
                                }

                                result = timeLowestMinValue;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Max Height
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer høyeste height målt i et datasett innsamlet over en angitt tid.
                        /// 
                        /// Input:
                        /// Bølgehøyde data
                        /// 
                        /// Brukes til:
                        /// Heave Amplitude Max
                        /// 
                        case CalculationType.TimeMaxAmplitude:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                switch (timeMaxWaveHeightWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(timeMaxWaveHeightLast))
                                        {
                                            if (value > timeMaxWaveHeightLast)
                                                timeMaxWaveHeightWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < timeMaxWaveHeightLast)
                                                timeMaxWaveHeightWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < timeMaxWaveHeightLast && value > 0)
                                        {
                                            timeMaxWaveHeightWaveTop = timeMaxWaveHeightLast;

                                            if (!double.IsNaN(timeMaxWaveHeightWaveBottom))
                                            {
                                                double height = Math.Abs(timeMaxWaveHeightWaveTop) + Math.Abs(timeMaxWaveHeightWaveBottom);

                                                // Legge inn height i data listen
                                                timeMaxWaveHeightDataList.Add(new TimeData()
                                                {
                                                    data = height,
                                                    timestamp = newTimeStamp
                                                });

                                                // Ny max height?
                                                if (height > timeMaxWaveHeightMaxValue)
                                                    timeMaxWaveHeightMaxValue = height;
                                            }

                                            // På vei ned
                                            timeMaxWaveHeightWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > timeMaxWaveHeightLast && value < 0)
                                        {
                                            timeMaxWaveHeightWaveBottom = timeMaxWaveHeightLast;

                                            if (!double.IsNaN(timeMaxWaveHeightWaveTop))
                                            {
                                                double height = Math.Abs(timeMaxWaveHeightWaveTop) + Math.Abs(timeMaxWaveHeightWaveBottom);

                                                // Legge inn height i data listen
                                                timeMaxWaveHeightDataList.Add(new TimeData()
                                                {
                                                    data = height,
                                                    timestamp = newTimeStamp
                                                });

                                                // Ny max height?
                                                if (height > timeMaxWaveHeightMaxValue)
                                                    timeMaxWaveHeightMaxValue = height;
                                            }

                                            // På vei opp igjen
                                            timeMaxWaveHeightWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                timeMaxWaveHeightLast = value;

                                // Sjekke om vi skal ta ut gamle verdier
                                bool findNewMaxValue = false;
                                if (parameter != 0)
                                {
                                    while (timeMaxWaveHeightDataList.Count > 0 && timeMaxWaveHeightDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                    {
                                        // Var dette gammel max verdi?
                                        if (timeMaxWaveHeightDataList[0].data == timeMaxWaveHeightMaxValue)
                                        {
                                            // Finne ny høyeste verdi
                                            findNewMaxValue = true;
                                        }

                                        // Fjerne fra verdiliste
                                        timeMaxWaveHeightDataList.RemoveAt(0);
                                    }
                                }

                                // Finne ny høyeste verdi
                                if (findNewMaxValue)
                                {
                                    double oldMaxValue = timeMaxWaveHeightMaxValue;
                                    timeMaxWaveHeightMaxValue = 0;
                                    bool foundNewMax = false;

                                    for (int i = 0; i < timeMaxWaveHeightDataList.Count && !foundNewMax; i++)
                                    {
                                        // Kan avslutte søket dersom vi finner en verdi like den gamle max verdien (ingen er høyere)
                                        if (timeMaxWaveHeightDataList[i]?.data == oldMaxValue)
                                        {
                                            timeMaxWaveHeightMaxValue = oldMaxValue;
                                            foundNewMax = true;
                                        }
                                        else
                                        {
                                            if (timeMaxWaveHeightDataList[i]?.data > timeMaxWaveHeightMaxValue)
                                                timeMaxWaveHeightMaxValue = timeMaxWaveHeightDataList[i].data;
                                        }
                                    }
                                }

                                result = timeMaxWaveHeightMaxValue;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Mean Wave Height
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer snitt-høyde i oscilerende data over en gitt tid
                        /// 
                        /// Input:
                        /// Bølgehøyde data
                        /// 
                        /// Brukes til:
                        /// Mean Heave Amplitude
                        /// 
                        case CalculationType.MeanWaveAmplitude:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                bool newWaveHeightValue = false;

                                switch (waveMeanAmplitudeWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(waveMeanAmplitudeLast))
                                        {
                                            if (value > waveMeanAmplitudeLast)
                                                waveMeanAmplitudeWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < waveMeanAmplitudeLast)
                                                waveMeanAmplitudeWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < waveMeanAmplitudeLast && value > 0)
                                        {
                                            waveMeanAmplitudeWaveTop = waveMeanAmplitudeLast;

                                            if (!double.IsNaN(waveMeanAmplitudeWaveBottom))
                                            {
                                                waveMeanAmplitudeValue = Math.Abs(waveMeanAmplitudeWaveTop) + Math.Abs(waveMeanAmplitudeWaveBottom);
                                                newWaveHeightValue = true;
                                            }

                                            // På vei ned
                                            waveMeanAmplitudeWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > waveMeanAmplitudeLast && value < 0)
                                        {
                                            waveMeanAmplitudeWaveBottom = waveMeanAmplitudeLast;

                                            if (!double.IsNaN(waveMeanAmplitudeWaveTop))
                                            {
                                                waveMeanAmplitudeValue = Math.Abs(waveMeanAmplitudeWaveTop) + Math.Abs(waveMeanAmplitudeWaveBottom);
                                                newWaveHeightValue = true;
                                            }

                                            // På vei opp igjen
                                            waveMeanAmplitudeWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                waveMeanAmplitudeLast = value;

                                // Ny bølgehøyde funnet?
                                if (newWaveHeightValue)
                                {
                                    // Legge inn periode i data listen
                                    waveMeanAmplitudeDataList.Add(new TimeData()
                                    {
                                        data = waveMeanAmplitudeValue,
                                        timestamp = newTimeStamp
                                    });

                                    waveMeanAmplitudeTotal += waveMeanAmplitudeValue;
                                }

                                // Sjekke om vi skal ta ut gamle verdier
                                if (parameter != 0)
                                {
                                    while (waveMeanAmplitudeDataList.Count > 0 && waveMeanAmplitudeDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                    {
                                        // Trekke fra data fra gammel periode
                                        waveMeanAmplitudeTotal -= waveMeanAmplitudeDataList[0].data;

                                        // Fjerne fra verdiliste
                                        waveMeanAmplitudeDataList.RemoveAt(0);
                                    }
                                }

                                // Finne gjennomsnitt verdi
                                if (waveMeanAmplitudeDataList.Count > 0)
                                    result = waveMeanAmplitudeTotal / (double)waveMeanAmplitudeDataList.Count;
                                else
                                    return double.NaN;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Wave Amplitude
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer høyde i oscilerende data
                        /// 
                        /// Input:
                        /// Bølgehøyde data
                        /// 
                        /// Brukes til:
                        /// Heave Amplitude
                        /// 
                        case CalculationType.WaveAmplitude:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                switch (waveAmplitudeWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(waveAmplitudeLast))
                                        {
                                            if (value > waveAmplitudeLast)
                                                waveAmplitudeWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < waveAmplitudeLast)
                                                waveAmplitudeWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < waveAmplitudeLast && value > 0)
                                        {
                                            waveAmplitudeWaveTop = waveAmplitudeLast;

                                            if (!double.IsNaN(waveAmplitudeWaveBottom))
                                                waveAmplitudeValue = Math.Abs(waveAmplitudeWaveTop) + Math.Abs(waveAmplitudeWaveBottom);

                                            // På vei ned
                                            waveAmplitudeWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > waveAmplitudeLast && value < 0)
                                        {
                                            waveAmplitudeWaveBottom = waveAmplitudeLast;

                                            if (!double.IsNaN(waveAmplitudeWaveTop))
                                                waveAmplitudeValue = Math.Abs(waveAmplitudeWaveTop) + Math.Abs(waveAmplitudeWaveBottom);

                                            // På vei opp igjen
                                            waveAmplitudeWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                waveAmplitudeLast = value;

                                // Returnere siste bølgehøyde
                                return waveAmplitudeValue;
                            }
                            break;


                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Ingen kalkulasjoner
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        default:
                            // Sjekke om data string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = value;
                            }
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                // Sette feilmelding
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.SerialPort,
                        errorMessageCat,
                        string.Format("Serial Port Processing\n(DoCalculations)\n\nSystem Message:\n{0}", ex.Message)));
            }

            return result;
        }

        public void Reset()
        {
            // Time Average
            timeAverageTotal = 0;
            timeAverageDataList.Clear();

            // Time Max Absolute
            timeMaxAbsoluteMaxValue = 0;
            timeMaxAbsoluteDataList.Clear();

            // Time Max Positive
            timeHighestMaxValue = double.MinValue;
            timeHighestDataList.Clear();

            // Time Max Negative
            timeLowestMinValue = double.MaxValue;
            timeLowestDataList.Clear();

            // Time Max Height
            timeMaxWaveHeightMaxValue = 0;
            timeMaxWaveHeightLast = double.NaN;
            timeMaxWaveHeightWavePhase = WavePhase.Init;
            timeMaxWaveHeightWaveTop = double.NaN;
            timeMaxWaveHeightWaveBottom = double.NaN;
            timeMaxWaveHeightDataList.Clear();

            // Wave Amplitude
            waveAmplitudeLast = double.NaN;
            waveAmplitudeWavePhase = WavePhase.Init;
            waveAmplitudeWaveTop = double.NaN;
            waveAmplitudeWaveBottom = double.NaN;
            waveAmplitudeValue = double.NaN;

            // Wave Mean Amplitude
            waveMeanAmplitudeLast = double.NaN;
            waveMeanAmplitudeWavePhase = WavePhase.Init;
            waveMeanAmplitudeWaveTop = double.NaN;
            waveMeanAmplitudeWaveBottom = double.NaN;
            waveMeanAmplitudeValue = double.NaN;
            waveMeanAmplitudeTotal = 0;
            waveMeanAmplitudeDataList.Clear();

        }

        public double BufferCount()
        {
            switch (type)
            {
                case CalculationType.TimeAverage:
                    return timeAverageDataList.Count;

                case CalculationType.TimeMaxAbsolute:
                    return timeMaxAbsoluteDataList.Count;

                case CalculationType.TimeMax:
                    return timeHighestDataList.Count;

                case CalculationType.TimeMin:
                    return timeLowestDataList.Count;

                default:
                    return 0;
            }
        }
    }

    public enum WavePhase
    {
        Init,
        Ascending,
        Descending
    }

    class TimeData
    {
        public double data { get; set; }
        public double data2 { get; set; }
        public DateTime timestamp { get; set; }
    }

    public enum CalculationType
    {
        [Description("None")]
        None,
        [Description("Addition")]
        Addition,
        [Description("Subtraction")]
        Subtraction,
        [Description("Multiplication")]
        Multiplication,
        [Description("Division")]
        Division,
        [Description("Time Average")]
        TimeAverage,
        [Description("Time Max")]
        TimeMax,
        [Description("Time Min")]
        TimeMin,
        [Description("Time Max Absolute")]
        TimeMaxAbsolute,
        [Description("Time Max Height")]
        TimeMaxAmplitude,
        [Description("Wave Height")]
        WaveAmplitude,
        [Description("Mean Wave Height")]
        MeanWaveAmplitude,

        }
}

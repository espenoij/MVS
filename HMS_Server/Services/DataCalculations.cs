using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace HMS_Server
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

        // Significant Heave Rate
        private double significantHeaveRateSquareSum = 0;
        private List<TimeData> significantHeaveRateDataList = new List<TimeData>();

        // Significant Wave Height
        private double swhValue = 0;
        private double swhLast = double.NaN;
        private WavePhase swhWavePhase = WavePhase.Init;
        private double swhWaveTop = double.NaN;
        private double swhWaveBottom = double.NaN;
        private List<TimeData> swhDataList = new List<TimeData>();

        // Time Max Wave Height
        private double timeMaxWaveHeightMaxValue = 0;
        private double timeMaxWaveHeightLast = double.NaN;
        private WavePhase timeMaxWaveHeightWavePhase = WavePhase.Init;
        private double timeMaxWaveHeightWaveTop = double.NaN;
        private double timeMaxWaveHeightWaveBottom = double.NaN;
        private List<TimeData> timeMaxWaveHeightDataList = new List<TimeData>();

        // Wave Mean Height
        private double waveMeanHeightLast = double.NaN;
        private WavePhase waveMeanHeightWavePhase = WavePhase.Init;
        private double waveMeanHeightWaveTop = double.NaN;
        private double waveMeanHeightWaveBottom = double.NaN;
        private double waveMeanHeightValue = 0;
        private double waveMeanHeightTotal = 0;
        private List<TimeData> waveMeanHeightDataList = new List<TimeData>();

        // Period
        private double periodHeightTop = double.NaN;
        private double periodHeightBottom = double.NaN;
        private double periodLast = double.NaN;
        private double periodCurrent = double.NaN;
        private WavePhase periodWavePhase = WavePhase.Init;
        private DateTime periodLastWaveTopTime = DateTime.MinValue;

        // Time Mean Period
        private double timeMeanPeriodWaveHeightTop = double.NaN;
        private double timeMeanPeriodWaveHeightBottom = double.NaN;
        private double timeMeanPeriodTotal = 0;
        private double timeMeanPeriodLast = double.NaN;
        private WavePhase timeMeanPeriodWavePhase = WavePhase.Init;
        private DateTime timeMeanPeriodLastWaveTopTime = DateTime.MinValue;
        private List<TimeData> timeMeanPeriodDataList = new List<TimeData>();

        // Wave Radar
        private double waveRadarLastTop = double.NaN;
        private double waveRadarLastBottom = double.NaN;
        private double waveRadarLast = double.NaN;
        private WavePhase waveRadarWavePhase = WavePhase.Init;

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
                        /// Degrees to Radians
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        case CalculationType.DegToRad:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = HMSCalc.ToRadians(value);
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Radians to Degrees
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        case CalculationType.RadToDeg:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = HMSCalc.ToDegrees(value);
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// GPS Position
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        // Brukes til:
                        // Latitude
                        // Longitude
                        case CalculationType.GPSPosition:

                            // Input format: dddmm.mmx, ddmm.mmx, (d=degrees, m=minutes, x=N/S/E/W)

                            // Posisjon til desimal separator
                            int decimalPointIndex = newData.IndexOf('.');

                            // Sjekker om vi har N/S/E/W i inndata
                            if (decimalPointIndex != -1 ||
                                newData.Contains('N') ||
                                newData.Contains('S') ||
                                newData.Contains('E') ||
                                newData.Contains('W'))
                            {
                                double degrees;
                                double minutes;

                                // ddmm.mmx
                                if (decimalPointIndex == 4)
                                {
                                    double.TryParse(newData.Substring(0, 2), Constants.numberStyle, new CultureInfo("en-US"), out degrees);
                                    double.TryParse(newData.Substring(2, 5), Constants.numberStyle, new CultureInfo("en-US"), out minutes);
                                }
                                // dddmm.mmx
                                else
                                {
                                    double.TryParse(newData.Substring(0, 3), Constants.numberStyle, new CultureInfo("en-US"), out degrees);
                                    double.TryParse(newData.Substring(3, 5), Constants.numberStyle, new CultureInfo("en-US"), out minutes);
                                }

                                result = degrees + (minutes / 60.0);

                                // Sør og vest har negative verdier
                                if (newData.Contains('S') ||
                                    newData.Contains('W'))
                                {
                                    result *= -1.0;
                                }
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Average
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        // Beskrivelse:
                        // Returnerer snittverdien av et data sett innsamlet over en gitt tid

                        // Brukes til:
                        // 

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
                                    while (timeAverageDataList.Count > 0 && timeAverageDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                    {
                                        // Trekke fra i total summen
                                        timeAverageTotal -= timeAverageDataList[0].data;

                                        // Fjerne fra verdi listen
                                        timeAverageDataList.RemoveAt(0);
                                    }
                                    //timeAverageDataList.TrimExcess();
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
                                    //timeMaxAbsoluteDataList.TrimExcess();

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
                        /// Time Heighest
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer høyeste verdi i et datasett innsamlet over en angitt tid.

                        /// Brukes til:
                        /// Max pitch up
                        /// Max roll left

                        case CalculationType.TimeHeighest:

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
                                    //timeMaxPositiveDataList.TrimExcess();

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
                        /// Time Low
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer laveste verdi i et datasett innsamlet over en angitt tid.

                        /// Brukes til:
                        /// Max pitch up
                        /// Max roll left

                        case CalculationType.TimeLowest:

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
                                    //timeMaxNegativeDataList.TrimExcess();

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
                        /// Significant Heave Rate
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer Significant Heave Rate
                        ///
                        /// Input:
                        /// heave velocity

                        case CalculationType.SignificantHeaveRate:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                // Sjekke at ny verdi ikke er lik den forrige som ble lagt inn i datasettet -> unngå duplikater
                                if (!double.IsNaN(value) &&
                                    significantHeaveRateDataList.LastOrDefault()?.timestamp != newTimeStamp)
                                {
                                    // Legge inn den nye absolutte verdien i data settet
                                    significantHeaveRateDataList.Add(new TimeData()
                                    {
                                        data = Math.Abs(value),
                                        timestamp = newTimeStamp
                                    });

                                    // Legge til ny verdi i square total
                                    significantHeaveRateSquareSum += Math.Pow(value, 2);

                                    // Sjekke om vi skal ta ut gamle verdier
                                    while (significantHeaveRateDataList.Count > 0 && significantHeaveRateDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                    {
                                        // Trekke fra gammel verdi i square total
                                        significantHeaveRateSquareSum -= Math.Pow(significantHeaveRateDataList[0].data, 2);

                                        // Fjerne fra verdiliste
                                        significantHeaveRateDataList.RemoveAt(0);
                                    }
                                    //significantHeaveRateDataList.TrimExcess();
                                }

                                // Regne 2 x RMS

                                // Square
                                // -> significantHeaveRateSquareSum

                                // Mean
                                double mean;
                                if (significantHeaveRateDataList.Count() > 0)
                                    mean = significantHeaveRateSquareSum / significantHeaveRateDataList.Count();
                                else
                                    mean = 0;

                                // Root
                                double root = Math.Sqrt(mean);

                                // 2 x
                                result = root * 2;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Significant Wave Height
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer gjennomsnittet av de tredjedel høyeste bølgene siste X tid
                        /// 
                        /// Input:
                        /// Bølgehøyde data
                        /// Tid
                        /// 
                        /// Brukes til:
                        /// Significant Wave Height
                        /// 
                        case CalculationType.SignificantWaveHeight:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                bool top13ops = false;

                                switch (swhWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(swhLast))
                                        {
                                            if (value > swhLast)
                                                swhWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < swhLast)
                                                swhWavePhase = WavePhase.Descending;
                                        }
                                        break;
                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < swhLast && value > 0)
                                        {
                                            swhWaveTop = swhLast;

                                            if (!double.IsNaN(swhWaveBottom))
                                            {
                                                double height = Math.Abs(swhWaveTop) + Math.Abs(swhWaveBottom);

                                                // Legge inn bølgehøyde i data listen
                                                int i = 0;
                                                bool found = false;

                                                // Løpe gjennom listen med bølgehøyde data
                                                while (!found && i < swhDataList.Count)
                                                {
                                                    if (height < swhDataList[i].data)
                                                        found = true;

                                                    i++;
                                                }

                                                // Legge inn bølgehøyde midt i listen
                                                if (i < swhDataList.Count)
                                                {
                                                    swhDataList.Insert(i,
                                                        new TimeData()
                                                        {
                                                            data = height,
                                                            timestamp = newTimeStamp
                                                        });
                                                }
                                                // Legge til bølgehøyde sist i listen
                                                else
                                                {
                                                    swhDataList.Add(new TimeData()
                                                        {
                                                            data = height,
                                                            timestamp = newTimeStamp
                                                        });
                                                }

                                                // Dersom denne bølgehøyden ble lagt inn i høyeste 1/3 av bølger så må vi kalkulere ny SWH
                                                if (i > (swhDataList.Count / 3) * 2)
                                                    top13ops = true;
                                            }

                                            // På vei ned
                                            swhWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > swhLast && value < 0)
                                        {
                                            swhWaveBottom = swhLast;

                                            if (!double.IsNaN(swhWaveTop))
                                            {
                                                double height = Math.Abs(swhWaveTop) + Math.Abs(swhWaveBottom);

                                                // Legge inn bølgehøyde i data listen
                                                int i = 0;
                                                bool found = false;

                                                // Løpe gjennom listen med bølgehøyde data
                                                while (!found && i < swhDataList.Count)
                                                {
                                                    // Finne neste hvor vi skal legge inn bølgen
                                                    if (height < swhDataList[i].data)
                                                        found = true;

                                                    i++;
                                                }

                                                // Legge inn bølgehøyde midt i listen
                                                if (i < swhDataList.Count)
                                                {
                                                    swhDataList.Insert(i,
                                                        new TimeData()
                                                        {
                                                            data = height,
                                                            timestamp = newTimeStamp
                                                        });
                                                }
                                                // Legge til bølgehøyde sist i listen
                                                else
                                                {
                                                    swhDataList.Add(new TimeData()
                                                        {
                                                            data = height,
                                                            timestamp = newTimeStamp
                                                        });
                                                }

                                                // Dersom denne bølgehøyden ble lagt inn i høyeste 1/3 av bølger så må vi kalkulere ny SWH
                                                if (i > (swhDataList.Count / 3) * 2)
                                                    top13ops = true;
                                            }

                                            // På vei opp igjen
                                            swhWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                swhLast = value;

                                // Sjekke om vi skal ta ut gamle verdier.
                                // NB! Må løpe gjennom hele listen og sjekke da disse verdiene ikke er sortert på tid, men på bølgehøyde
                                int deletedValues = 0;
                                for (int i = 0; i < swhDataList.Count && i >= 0; i++)
                                {
                                    if (swhDataList[i]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                    {
                                        // Dersom denne bølgehøyden ble tatt ut fra høyeste 1/3 av bølger så må vi kalkulere ny SWH
                                        if (!top13ops)
                                            if (i > (swhDataList.Count / 3) * 2)
                                                top13ops = true;

                                        // Fjerne fra verdiliste
                                        swhDataList.RemoveAt(i--);

                                        deletedValues++;
                                    }
                                }
                                //swhDataList.TrimExcess();

                                // Finne ny SWH verdi
                                // Ny SWH verdi beregnes kun dersom ny verdi legges inn i topp 1/3 bølgehøyder eller
                                // verdi fjernes fra topp 1/3 bølgehøyder.
                                //
                                // Spesialtilfelle:
                                // Dersom 1 verdi legges inn i bunn 2/3 og 1 verdi fjernes fra bunn 2/3 så endres ikke SWH.
                                // 
                                if (top13ops || deletedValues != 1)
                                {
                                    double waveHeightSum = 0;
                                    int top13startPos = (int)((swhDataList.Count / 3) * 2);

                                    for (int i = top13startPos; i < swhDataList.Count; i++)
                                    {
                                        waveHeightSum += swhDataList[i].data;
                                    }

                                    if (swhDataList.Count - top13startPos > 0) // Kan ikke dele på 0 under
                                        swhValue = waveHeightSum / (swhDataList.Count - top13startPos);
                                }

                                result = swhValue;
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
                        /// Heave Height Max
                        /// 
                        case CalculationType.TimeMaxHeight:

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
                                //timeMaxHeightDataList.TrimExcess();

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
                        /// Mean Heave Height
                        /// 
                        case CalculationType.MeanWaveHeight:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                bool newWaveHeightValue = false;

                                switch (waveMeanHeightWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(waveMeanHeightLast))
                                        {
                                            if (value > waveMeanHeightLast)
                                                waveMeanHeightWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < waveMeanHeightLast)
                                                waveMeanHeightWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < waveMeanHeightLast && value > 0)
                                        {
                                            waveMeanHeightWaveTop = waveMeanHeightLast;

                                            if (!double.IsNaN(waveMeanHeightWaveBottom))
                                            {
                                                waveMeanHeightValue = Math.Abs(waveMeanHeightWaveTop) + Math.Abs(waveMeanHeightWaveBottom);
                                                newWaveHeightValue = true;
                                            }

                                            // På vei ned
                                            waveMeanHeightWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > waveMeanHeightLast && value < 0)
                                        {
                                            waveMeanHeightWaveBottom = waveMeanHeightLast;

                                            if (!double.IsNaN(waveMeanHeightWaveTop))
                                            {
                                                waveMeanHeightValue = Math.Abs(waveMeanHeightWaveTop) + Math.Abs(waveMeanHeightWaveBottom);
                                                newWaveHeightValue = true;
                                            }

                                            // På vei opp igjen
                                            waveMeanHeightWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                waveMeanHeightLast = value;

                                // Ny bølgehøyde funnet?
                                if (newWaveHeightValue)
                                {
                                    // Legge inn periode i data listen
                                    waveMeanHeightDataList.Add(new TimeData()
                                    {
                                        data = waveMeanHeightValue,
                                        timestamp = newTimeStamp
                                    });

                                    waveMeanHeightTotal += waveMeanHeightValue;
                                }

                                // Sjekke om vi skal ta ut gamle verdier
                                while (waveMeanHeightDataList.Count > 0 && waveMeanHeightDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                {
                                    // Trekke fra data fra gammel periode
                                    waveMeanHeightTotal -= waveMeanHeightDataList[0].data;

                                    // Fjerne fra verdiliste
                                    waveMeanHeightDataList.RemoveAt(0);
                                }

                                // Finne gjennomsnitt verdi
                                if (waveMeanHeightDataList.Count > 0)
                                    result = waveMeanHeightTotal / (double)waveMeanHeightDataList.Count;
                                else
                                    return double.NaN;
                            }
                            break;


                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Period
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer målt bølge periode.
                        /// 
                        /// Brukes til:
                        /// Wave Period (TS)
                        /// 
                        case CalculationType.Period:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                switch (periodWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(periodLast))
                                        {
                                            if (value > periodLast)
                                                periodWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < periodLast)
                                                periodWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < periodLast && value > 0)
                                        {
                                            // Bølgehøyde topp
                                            periodHeightTop = periodLast;

                                            if (periodLastWaveTopTime != DateTime.MinValue)
                                            {
                                                // Sjekke cut-off
                                                if (!double.IsNaN(periodHeightTop) &&
                                                    !double.IsNaN(periodHeightBottom))
                                                {
                                                    // Beregner ikke periode dersom bølgehøyden er mindre enn X m
                                                    double waveHeight = Math.Abs(periodHeightTop) + Math.Abs(periodHeightBottom);
                                                    if (waveHeight > adminSettingsVM.waveHeightCutoff)
                                                    {
                                                        periodCurrent = newTimeStamp.Subtract(periodLastWaveTopTime).TotalSeconds;
                                                    }
                                                    else
                                                    {
                                                        periodCurrent = 0;
                                                    }
                                                }

                                                periodLastWaveTopTime = newTimeStamp;
                                            }
                                            else
                                            {
                                                periodLastWaveTopTime = newTimeStamp;
                                            }

                                            // På vei ned
                                            periodWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > periodLast && value < 0)
                                        {
                                            // Bølgehøyde bunn
                                            periodHeightBottom = periodLast;

                                            // På vei opp igjen
                                            periodWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                periodLast = value;

                                // Retur
                                return periodCurrent;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Time Mean Period
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Returnerer gjennomsnitt målt bølge periode i et datasett innsamlet over en angitt tid.
                        /// 
                        /// Brukes til:
                        /// Heave Period Mean
                        /// Mean wave period
                        /// 
                        case CalculationType.TimeMeanPeriod:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                switch (timeMeanPeriodWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(timeMeanPeriodLast))
                                        {
                                            if (value > timeMeanPeriodLast)
                                                timeMeanPeriodWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < timeMeanPeriodLast)
                                                timeMeanPeriodWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < timeMeanPeriodLast && value > 0)
                                        {
                                            // Bølgehøyde topp
                                            timeMeanPeriodWaveHeightTop = timeMeanPeriodLast;

                                            if (timeMeanPeriodLastWaveTopTime != DateTime.MinValue)
                                            {
                                                // Sjekke cut-off
                                                if (!double.IsNaN(timeMeanPeriodWaveHeightTop) &&
                                                    !double.IsNaN(timeMeanPeriodWaveHeightBottom))
                                                {
                                                    // Beregner ikke periode dersom bølgehøyden er mindre enn X m
                                                    double waveHeight = Math.Abs(timeMeanPeriodWaveHeightTop) + Math.Abs(timeMeanPeriodWaveHeightBottom);
                                                    if (waveHeight > adminSettingsVM.waveHeightCutoff)
                                                    {
                                                        TimeSpan period = newTimeStamp.Subtract(timeMeanPeriodLastWaveTopTime);

                                                        // Legge inn periode i data listen
                                                        timeMeanPeriodDataList.Add(new TimeData()
                                                        {
                                                            data = period.TotalSeconds,
                                                            timestamp = newTimeStamp
                                                        });

                                                        timeMeanPeriodTotal += period.TotalSeconds;
                                                    }
                                                    else
                                                    {
                                                        // Legge inn 0 periode i data listen
                                                        timeMeanPeriodDataList.Add(new TimeData()
                                                        {
                                                            data = 0,
                                                            timestamp = newTimeStamp
                                                        });
                                                    }
                                                }

                                                timeMeanPeriodLastWaveTopTime = newTimeStamp;
                                            }
                                            else
                                            {
                                                timeMeanPeriodLastWaveTopTime = newTimeStamp;
                                            }

                                            // På vei ned
                                            timeMeanPeriodWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > timeMeanPeriodLast && value < 0)
                                        {
                                            // Bølgehøyde bunn
                                            timeMeanPeriodWaveHeightBottom = timeMeanPeriodLast;

                                            // På vei opp igjen
                                            timeMeanPeriodWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                timeMeanPeriodLast = value;

                                // Sjekke om vi skal ta ut gamle verdier
                                while (timeMeanPeriodDataList.Count > 0 && timeMeanPeriodDataList[0]?.timestamp.AddSeconds(parameter) < newTimeStamp)
                                {
                                    // Trekke fra data fra gammel periode
                                    timeMeanPeriodTotal -= timeMeanPeriodDataList[0].data;

                                    // Fjerne fra verdiliste
                                    timeMeanPeriodDataList.RemoveAt(0);
                                }

                                // Finne gjennomsnitt verdi
                                if (timeMeanPeriodDataList.Count > 0)
                                    result = timeMeanPeriodTotal / (double)timeMeanPeriodDataList.Count;
                                else
                                    return double.NaN;
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Rounding Decimals
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Avrunder svaret med angitt antall desimaler.
                        /// 
                        case CalculationType.RoundingDecimals:

                            // Sjekke om data string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                if (parameter > 10)
                                    parameter = 10;

                                result = Math.Round(value, (int)parameter, MidpointRounding.AwayFromZero);
                            }
                            break;

                        //////////////////////////////////////////////////////////////////////////////////////////////////
                        ///// NWS Codes (Weather codes)
                        //////////////////////////////////////////////////////////////////////////////////////////////////
                        //// Beskrivelse:
                        //// Leser NWS kode felt
                        ////
                        //// C - NO PRECIPITATION
                        //// P - PRECIPITATION
                        //// L - DRIZZLE
                        //// R - RAIN
                        //// S - SNOW
                        //// IP - ICE PELLETS
                        //// ZL - FREEZING DRIZZLE
                        //// ZR - FREEZING RAIN
                        //// 
                        //// + Heavy
                        //// - Light
                        //// Space - Moderate
                        ////
                        //// Client data:
                        //// 0 - NO PRECIPITATION
                        //// 1 - PRECIPITATION
                        //// 2 - DRIZZLE
                        //// 3 - RAIN
                        //// 4 - SNOW
                        //// 5 - ICE PELLETS
                        //// 6 - FREEZING DRIZZLE
                        //// 7 - FREEZING RAIN
                        ////
                        //// 0 - Light
                        //// 1 - Moderate
                        //// 2 - Heavy
                        ////
                        //case CalculationType.NWSCodes:

                        //    // Vær fenomen
                        //    WeatherPhenomena phenomena = WeatherPhenomena.Clear;

                        //    if (newData.Contains("IP"))
                        //    {
                        //        phenomena = WeatherPhenomena.IcePellets;
                        //    }
                        //    else
                        //    if (newData.Contains("ZL"))
                        //    {
                        //        phenomena = WeatherPhenomena.FreezingDrizzle;
                        //    }
                        //    else
                        //    if (newData.Contains("ZR"))
                        //    {
                        //        phenomena = WeatherPhenomena.FreezingRain;
                        //    }
                        //    else
                        //    if (newData.Contains("C"))
                        //    {
                        //        phenomena = WeatherPhenomena.Clear;
                        //    }
                        //    else
                        //    if (newData.Contains("P"))
                        //    {
                        //        phenomena = WeatherPhenomena.Precipitation;
                        //    }
                        //    else
                        //    if (newData.Contains("R"))
                        //    {
                        //        phenomena = WeatherPhenomena.Rain;
                        //    }
                        //    else
                        //    if (newData.Contains("L"))
                        //    {
                        //        phenomena = WeatherPhenomena.Drizzle;
                        //    }
                        //    else
                        //    if (newData.Contains("S"))
                        //    {
                        //        phenomena = WeatherPhenomena.Snow;
                        //    }

                        //    // Vær intensitet
                        //    WeatherSeverity severity = WeatherSeverity.Moderate;

                        //    if (newData.Contains("+"))
                        //    {
                        //        severity = WeatherSeverity.Heavy;
                        //    }
                        //    else
                        //    if (newData.Contains("-"))
                        //    {
                        //        severity = WeatherSeverity.Light;
                        //    }
                        //    else
                        //    {
                        //        severity = WeatherSeverity.Moderate;
                        //    }

                        //    // Weather encoding
                        //    result = Weather.Encode((int)phenomena, (int)severity);

                        //    break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// METAR Codes (Weather codes)
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        // Beskrivelse:
                        // Leser NWS kode felt
                        //
                        case CalculationType.METARCodes:

                            WeatherSeverity severity = WeatherSeverity.None;
                            WeatherPhenomena phenomena1 = WeatherPhenomena.None;
                            WeatherPhenomena phenomena2 = WeatherPhenomena.None;

                            int phenomena1Pos;
                            int phenomena2Pos;

                            string phenomenaSubString = string.Empty;

                            // Vær intensitet
                            if (newData.Substring(0, 1).Equals("+"))
                            {
                                severity = WeatherSeverity.Heavy;

                                phenomena1Pos = 1;
                                phenomena2Pos = 3;
                            }
                            else
                            if (newData.Substring(0, 1).Equals("-"))
                            {
                                severity = WeatherSeverity.Light;

                                phenomena1Pos = 1;
                                phenomena2Pos = 3;
                            }
                            else
                            {
                                severity = WeatherSeverity.Moderate;

                                phenomena1Pos = 0;
                                phenomena2Pos = 2;
                            }

                            // Vær fenomen
                            if (phenomena1Pos + 2 <= newData.Length)
                                phenomenaSubString = newData.Substring(phenomena1Pos, 2);

                            phenomena1 = Weather.GetPhenomena(phenomenaSubString);

                            if (phenomena2Pos + 2 <= newData.Length)
                                phenomenaSubString = newData.Substring(phenomena2Pos, 2);

                            phenomena2 = Weather.GetPhenomena(phenomenaSubString);

                            // Weather encoding
                            result = Weather.Encode((int)severity, (int)phenomena1, (int)phenomena2);

                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Absolute
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Avrunder svaret med angitt antall desimaler.
                        /// 
                        case CalculationType.Absolute:

                            // Sjekke om data string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = Math.Abs(value);
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Knots to m/s
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Avrunder svaret med angitt antall desimaler.
                        /// 
                        case CalculationType.KnotsToMS:

                            // Sjekke om data string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = HMSCalc.KnotsToMS(value);
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// M/S to Knots
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Avrunder svaret med angitt antall desimaler.
                        /// 
                        case CalculationType.MSToKnots:

                            // Sjekke om data string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                result = HMSCalc.MStoKnots(value);
                            }
                            break;

                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Wave Radar
                        ////////////////////////////////////////////////////////////////////////////////////////////////
                        /// Beskrivelse:
                        /// Konverterer høyde data fra en bølge radar til oscillerende data rundt et nullpunkt
                        /// 
                        /// Input:
                        /// Bølgehøyde data
                        /// 
                        /// Brukes til:
                        /// Bølge data
                        /// 
                        case CalculationType.WaveRadar:

                            // Sjekke om string er numerisk
                            if (double.TryParse(newData, Constants.numberStyle, Constants.cultureInfo, out value))
                            {
                                switch (waveRadarWavePhase)
                                {
                                    // Init
                                    case WavePhase.Init:
                                        if (!double.IsNaN(waveRadarLast))
                                        {
                                            if (value > waveRadarLast)
                                                waveRadarWavePhase = WavePhase.Ascending;
                                            else
                                            if (value < waveRadarLast)
                                                waveRadarWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot topp av bølge
                                    case WavePhase.Ascending:

                                        // Dersom neste verdi er mindre enn forrige -> passert toppen av bølgen
                                        if (value < waveRadarLast)
                                        {
                                            // Lagre ny bølge topp
                                            waveRadarLastTop = value;

                                            // På vei ned
                                            waveRadarWavePhase = WavePhase.Descending;
                                        }
                                        break;

                                    // På vei mot bunn av bølge
                                    case WavePhase.Descending:

                                        // Dersom neste verdi er større enn forrige -> passert bunnen av bølgen
                                        if (value > waveRadarLast)
                                        {
                                            // Lagre ny bølge bunn
                                            waveRadarLastBottom = value;

                                            // På vei opp igjen
                                            waveRadarWavePhase = WavePhase.Ascending;
                                        }
                                        break;
                                }

                                // Oppdatere siste verdi
                                waveRadarLast = value;

                                // Kan kun utføre beregninger når vi har funnet en topp og en bunn
                                if (!double.IsNaN(waveRadarLastTop) &&
                                    !double.IsNaN(waveRadarLastBottom))
                                {
                                    // Finne 0-nivå for bølgedata
                                    double centerWaveHeight = waveRadarLastTop + ((waveRadarLastBottom - waveRadarLastTop) / 2);

                                    // Resultat: bølgehøyde i forhold til 0-nivå
                                    result = value - centerWaveHeight;
                                }
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
            timeHighestMaxValue = 0;
            timeHighestDataList.Clear();

            // Time Max Positive
            timeLowestMinValue = 0;
            timeLowestDataList.Clear();

            // Significant Heave Rate
            significantHeaveRateSquareSum = 0;
            significantHeaveRateDataList.Clear();

            // Significant Wave Height
            swhValue = 0;
            swhLast = double.NaN;
            swhWavePhase = WavePhase.Init;
            swhWaveTop = double.NaN;
            swhWaveBottom = double.NaN;
            swhDataList.Clear();

            // Time Max Height
            timeMaxWaveHeightMaxValue = 0;
            timeMaxWaveHeightLast = double.NaN;
            timeMaxWaveHeightWavePhase = WavePhase.Init;
            timeMaxWaveHeightWaveTop = double.NaN;
            timeMaxWaveHeightWaveBottom = double.NaN;
            timeMaxWaveHeightDataList.Clear();

            // Height
            waveMeanHeightLast = double.NaN;
            waveMeanHeightWavePhase = WavePhase.Init;
            waveMeanHeightWaveTop = double.NaN;
            waveMeanHeightWaveBottom = double.NaN;
            waveMeanHeightValue = 0;

            // Time Mean Period
            timeMeanPeriodTotal = 0;
            timeMeanPeriodLast = double.NaN;
            timeMeanPeriodWavePhase = WavePhase.Init;
            timeMeanPeriodLastWaveTopTime = DateTime.MinValue;
            timeMeanPeriodDataList.Clear();
        }

        public double BufferCount()
        {
            switch (type)
            {
                case CalculationType.TimeAverage:
                    return timeAverageDataList.Count;

                case CalculationType.TimeMaxAbsolute:
                    return timeMaxAbsoluteDataList.Count;

                case CalculationType.TimeHeighest:
                    return timeHighestDataList.Count;

                case CalculationType.TimeLowest:
                    return timeLowestDataList.Count;

                case CalculationType.SignificantHeaveRate:
                    return significantHeaveRateDataList.Count;

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
        [Description("Degrees To Radians")]
        DegToRad,
        [Description("Radians To Degrees")]
        RadToDeg,
        [Description("GPS Position")]
        GPSPosition,
        [Description("Time Average")]
        TimeAverage,
        [Description("Time Highest")]
        TimeHeighest,
        [Description("Time Lowest")]
        TimeLowest,
        [Description("Time Max Absolute")]
        TimeMaxAbsolute,
        [Description("Time Max Height")]
        TimeMaxHeight,
        [Description("Time Mean Period")]
        TimeMeanPeriod,
        [Description("Rounding Decimals")]
        RoundingDecimals,
        [Description("Significant Heave Rate")]
        SignificantHeaveRate,
        [Description("Height")]
        MeanWaveHeight,
        [Description("METAR Codes")]
        METARCodes,
        [Description("Significant Wave Height")]
        SignificantWaveHeight,
        [Description("Absolute Value")]
        Absolute,
        [Description("Knots to m/s")]
        KnotsToMS,
        [Description("m/s to knots")]
        MSToKnots,
        [Description("Wave Radar")]
        WaveRadar,
        [Description("Period")]
        Period,

        //[Description("NWS Codes")]
        //NWSCodes,
    }
}

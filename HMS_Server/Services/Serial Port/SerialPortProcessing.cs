using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HMS_Server
{
    class SerialPortProcessing
    {
        public InputDataType inputType { get; set; }
        public BinaryType binaryType { get; set; }

        public string packetHeader { get; set; }
        public string packetEnd { get; set; }

        public string packetDelimiter { get; set; }
        public SerialPortSetup.CombineFields packetCombineFields { get; set; }
        public bool fixedPosData { get; set; }
        public int fixedPosStart { get; set; }
        public int fixedPosTotal { get; set; }

        public int dataField { get; set; }
        public DecimalSeparator decimalSeparator { get; set; }
        public bool autoExtractValue { get; set; }

        public SerialPortProcessing()
        {
        }

        public List<string> FindSelectedPackets(string dataString)
        {
            // Først må vi dele opp rå data i individuelle pakker i tilfelle vi får mer enn en pakke i en sending.
            // Så må disse pakkene behandles individuelt.

            // Enten header eller end string må være satt, ellers kan vi ikke dele opp dataene eller verifisere at vi har fullstendig data pakke
            // Det burde egentlig være sånn at begge burde være satt, men det finnes sensorer som leverer data uten header og noen uten end...

            // Retur er packets som ble funnet
            List<string> packetString = new List<string>();

            if (packetHeader != string.Empty || packetEnd != string.Empty)
            {
                int startPos = 0;
                int endPos = dataString.Length;
                int packetStartPos = 0;
                int packetEndPos;
                int totalCount;
                bool done = false;

                while ((startPos <= endPos) && (packetStartPos > -1) && !done)
                {
                    totalCount = endPos - startPos;

                    // Sjekker om packet header er satt
                    if (!string.IsNullOrEmpty(packetHeader))
                        // Finne header pos
                        packetStartPos = dataString.IndexOf(packetHeader, startPos, totalCount, StringComparison.Ordinal); // Ytelse-/hastighets-forskjell er stor med/uten StringComparison.Ordinal
                    else
                        // Ingen header satt -> les fra første pos
                        packetStartPos = startPos;

                    // Fant ikke packetStart -> finnes ikke i dataString -> avslutt
                    if (packetStartPos == -1)
                    {
                        done = true;
                    }
                    // startString funnet
                    else
                    {
                        // Så må vi søke etter endString
                        totalCount = endPos - packetStartPos;

                        // Sjekker om packet end er satt
                        if (!string.IsNullOrEmpty(packetEnd))
                            // Finne end pos
                            packetEndPos = dataString.IndexOf(packetEnd, packetStartPos, totalCount, StringComparison.Ordinal);
                        else
                            // Ingen end satt -> les til slutten
                            packetEndPos = packetStartPos + totalCount;

                        // Fant ikke packetEnd -> finnes ikke i dataString -> avslutt
                        if (packetEndPos == -1)
                        {
                            break;
                        }
                        // packetEnd funnet
                        else
                        {
                            if (packetEndPos - packetStartPos > 0)
                            {
                                switch (inputType)
                                {
                                    case InputDataType.Text:
                                        // Korrigerer packetEnd slik at vi får med packetEnd i resultatet
                                        packetEndPos += packetEnd.Length;

                                        // Plukke ut data mellom packetStartPos og packetEndPos for videre prosessering
                                        packetString.Add(dataString.Substring(packetStartPos, packetEndPos - packetStartPos));
                                        break;

                                    case InputDataType.Binary:
                                        // Sender hele pakken videre
                                        packetString.Add(dataString);
                                        break;
                                }

                                // Neste iterasjon
                                startPos = packetEndPos;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return packetString;
        }

        public PacketDataFields FindPacketDataFields(string dataString)
        {
            PacketDataFields packetDataFields = new PacketDataFields();

            // Delimited data packet
            if (!fixedPosData)
            {
                // Delimiter string
                string[] delimiter = new string[] { packetDelimiter };

                // Sjekke start & end
                if ((dataString.StartsWith(packetHeader, StringComparison.Ordinal) || packetHeader == string.Empty) &&
                    (dataString.EndsWith(packetEnd, StringComparison.Ordinal) || packetEnd == string.Empty))
                {
                    string trimmedDataString = dataString;

                    //// Trimme bort start
                    //if (packetHeader != string.Empty && inputType == InputDataType.Text)
                    //    trimmedDataString = trimmedDataString.Substring(packetHeader.Length);

                    //// Trimme bort evt delimiter etter start
                    //if (trimmedDataString.StartsWith(delimiter[0].ToString(), StringComparison.Ordinal) && inputType == InputDataType.Text)
                    //    trimmedDataString = trimmedDataString.Substring(delimiter[0].ToString().Length);

                    // Trimme bort end
                    if (!string.IsNullOrEmpty(packetEnd) && !string.IsNullOrEmpty(trimmedDataString) && inputType == InputDataType.Text)
                    {
                        int length = trimmedDataString.IndexOf(packetEnd, StringComparison.Ordinal);
                        if (length > 0)
                            trimmedDataString = trimmedDataString.Substring(0, length);
                    }

                    // Fordele data ut i datafelt før visning
                    string[] dataFields = trimmedDataString.Split(delimiter, StringSplitOptions.None);

                    // Skal vi inkludere evt sepparat enhetsfelt? F.eks. breddegrad hvor  N/S står i eget felt
                    switch (packetCombineFields)
                    {
                        // Inkludere enhet for heltallsfelt
                        case SerialPortSetup.CombineFields.Even:

                            for (int i = 0; (i * 2) < dataFields.Length && ((i * 2) + 1) < dataFields.Length && i < packetDataFields.dataField.Length; i++)
                            {
                                packetDataFields.dataField[i] = dataFields[i * 2] + dataFields[(i * 2) + 1];
                            }
                            break;

                        // Inkludere enhet for oddetallsfelt
                        case SerialPortSetup.CombineFields.Odd:

                            packetDataFields.dataField[0] = dataFields[0];
                            for (int i = 0; ((i * 2) + 1) < dataFields.Length && ((i * 2) + 2) < dataFields.Length && i + 1 < packetDataFields.dataField.Length; i++)
                            {
                                packetDataFields.dataField[i + 1] = dataFields[(i * 2) + 1] + dataFields[(i * 2) + 2];
                            }
                            break;

                        // Ingen enhet som skal inkluderes i data
                        default:
                            for (int i = 0; i < dataFields.Length && i < packetDataFields.dataField.Length; i++)
                            {
                                packetDataFields.dataField[i] = dataFields[i];
                            }
                            break;
                    }
                }
            }
            // Fixed position data packet
            else
            {
                string str = TextHelper.EscapeControlChars(dataString);
                if (fixedPosStart + fixedPosTotal < str.Length)
                    packetDataFields.dataField[0] = str.Substring(fixedPosStart, fixedPosTotal);
            }

            return packetDataFields;
        }

        public SelectedDataField FindSelectedDataInPacket(PacketDataFields packetDataFields)
        {
            SelectedDataField selectedData = new SelectedDataField();

            if (inputType == InputDataType.Text)
            {
                if (!string.IsNullOrEmpty(packetDataFields.dataField[dataField]))
                    selectedData.selectedDataFieldString = packetDataFields.dataField[dataField];

                // Vise selected data på skjerm
                if (!string.IsNullOrEmpty(selectedData.selectedDataFieldString))
                {
                    // Har vi automatisk number extraction?
                    if (autoExtractValue)
                    {
                        double doubleValue;
                        int intValue;
                        string valueStr;
                        bool valueFound = false;

                        // NB! Viktig å bruke cultureInfo i konvertering til og fra string

                        // 1. Søker først etter desimal-tall verdier

                        // Søker etter substring med desimal-tall
                        CultureInfo cultureInfo;

                        if (decimalSeparator == DecimalSeparator.Point)
                        {
                            valueStr = Regex.Match(selectedData.selectedDataFieldString, @"-?\d*\.\d*").ToString();
                            cultureInfo = new CultureInfo("en-US");
                        }
                        else
                        {
                            valueStr = Regex.Match(selectedData.selectedDataFieldString, @"-?\d*\,\d*").ToString();
                            cultureInfo = new CultureInfo("nb-NO");
                        }

                        // Fant vi substring med tall?
                        if (!string.IsNullOrEmpty(valueStr))
                        {
                            // Konvertere substring til double for å verifisere gyldig double
                            valueFound = double.TryParse(valueStr, NumberStyles.Any, cultureInfo, out doubleValue);

                            // Fant desimal tall
                            if (valueFound)
                            {
                                selectedData.selectedDataFieldString = doubleValue.ToString(Constants.cultureInfo);
                            }
                        }

                        // ...dersom vi ikke fant desimal-tall verdier
                        if (!valueFound)
                        {
                            // 2. Søker etter integer verdier

                            // Søker etter substring med integer tall
                            valueStr = Regex.Match(selectedData.selectedDataFieldString, @"-?\d+").ToString();

                            // Fant vi substring med tall?
                            if (!string.IsNullOrEmpty(valueStr))
                            {
                                // Konvertere substring til int for å verifisere gyldig int
                                valueFound = int.TryParse(valueStr, NumberStyles.Any, cultureInfo, out intValue);

                                // Fant integer tall
                                if (valueFound)
                                {
                                    selectedData.selectedDataFieldString = intValue.ToString(Constants.cultureInfo);
                                }
                            }
                        }
                    }
                    //else
                    //{
                    //    // Sjekke at vi har korrekt desimal separator
                    //    if (decimalSeparator == DecimalSeparator.Point)
                    //    {
                    //        selectedData.selectedDataFieldString.Replace(",", ".");
                    //    }
                    //    else
                    //    {
                    //        selectedData.selectedDataFieldString.Replace(".", ",");
                    //    }
                    //}
                }
            }
            else
            if (inputType == InputDataType.Binary)
            {
                try
                {
                    // Generere en hex string
                    string hexValue = "0x";
                    switch (binaryType)
                    {
                        case BinaryType.Byte:
                            // 1 byte
                            hexValue += packetDataFields.dataField[dataField];
                            break;
                        case BinaryType.Int16:
                        case BinaryType.Uint16:
                            // 2 byte
                            for (int i = 0; i < 2; i++)
                                hexValue += packetDataFields.dataField[dataField + i];
                            break;

                        case BinaryType.Int32:
                        case BinaryType.Uint32:
                        case BinaryType.Float:
                            // 4 byte
                            for (int i = 0; i < 4; i++)
                                hexValue += packetDataFields.dataField[dataField + i];
                            break;

                        case BinaryType.Long:
                        case BinaryType.Ulong:
                        case BinaryType.Int64:
                        case BinaryType.Uint64:
                        case BinaryType.Double:
                            // 8 byte
                            for (int i = 0; i < 4; i++)
                                hexValue += packetDataFields.dataField[dataField + i];
                            break;
                    }

                    // Konvertere hex string til desimal verdi, og lagre som string igjen
                    switch (binaryType)
                    {
                        case BinaryType.Byte:
                            selectedData.selectedDataFieldString = Convert.ToByte(hexValue, 16).ToString();
                            break;

                        case BinaryType.Int16:
                            selectedData.selectedDataFieldString = Convert.ToInt16(hexValue, 16).ToString();
                            break;

                        case BinaryType.Uint16:
                            selectedData.selectedDataFieldString = Convert.ToUInt16(hexValue, 16).ToString();
                            break;

                        case BinaryType.Int32:
                            selectedData.selectedDataFieldString = Convert.ToInt32(hexValue, 16).ToString();
                            break;

                        case BinaryType.Uint32:
                            selectedData.selectedDataFieldString = Convert.ToUInt32(hexValue, 16).ToString();
                            break;

                        case BinaryType.Float:
                            selectedData.selectedDataFieldString = BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(hexValue, 16)), 0).ToString();
                            break;

                        case BinaryType.Long:
                            selectedData.selectedDataFieldString = Convert.ToInt64(hexValue, 16).ToString();
                            break;

                        case BinaryType.Ulong:
                            selectedData.selectedDataFieldString = Convert.ToUInt64(hexValue, 16).ToString();
                            break;

                        case BinaryType.Int64:
                            selectedData.selectedDataFieldString = Convert.ToInt64(hexValue, 16).ToString();
                            break;

                        case BinaryType.Uint64:
                            selectedData.selectedDataFieldString = Convert.ToUInt64(hexValue, 16).ToString();
                            break;

                        case BinaryType.Double:
                            selectedData.selectedDataFieldString = BitConverter.Int64BitsToDouble(Convert.ToInt64(hexValue, 16)).ToString();
                            break;
                    }
                }
                catch (Exception)
                {
                    // Invalid input til int convert
                    // Trenger ikke gjøre noe
                }
            }

            return selectedData;
        }

        public CalculatedData ApplyCalculationsToSelectedData(
            SelectedDataField selectedData, 
            List<DataCalculations> dataCalculations, 
            DateTime timestamp, 
            ErrorHandler errorHandler, 
            ErrorMessageCategory errorMessageCat,
            AdminSettingsVM adminSettingsVM)
        {
            CalculatedData calculatedData = new CalculatedData();

            try
            {
                // Init retur data
                double value;

                // Auto extract value funksjonen konverterer til system culature info
                if (autoExtractValue)
                {
                    if (double.TryParse(selectedData.selectedDataFieldString, Constants.numberStyle, Constants.cultureInfo, out value))
                        calculatedData.data = value;
                    else
                        calculatedData.data = double.NaN;
                }
                else
                // Punktum som desimal separator
                if (decimalSeparator == DecimalSeparator.Point)
                {
                    if (double.TryParse(selectedData.selectedDataFieldString, Constants.numberStyle, new CultureInfo("en-US"), out value))
                        calculatedData.data = value;
                    else
                        calculatedData.data = double.NaN;
                }
                // Komma som desimal separator
                else
                {
                    if (double.TryParse(selectedData.selectedDataFieldString, Constants.numberStyle, new CultureInfo("nb-NO"), out value))
                        calculatedData.data = value;
                    else
                        calculatedData.data = double.NaN;
                }

                // Data Calculations
                for (int i = 0; i < Constants.DataCalculationSteps; i++)
                {
                    // Skal vi utføre kalkulasjoner?
                    if (dataCalculations[i].type != CalculationType.None)
                    {
                        // Utføre valgt prosessering
                        switch (dataCalculations[i].type)
                        {
                            case CalculationType.GPSPosition:
                            //case CalculationType.NWSCodes:
                            case CalculationType.METARCodes:
                                calculatedData.data = dataCalculations[i].DoCalculations(selectedData.selectedDataFieldString, timestamp, errorHandler, errorMessageCat, adminSettingsVM);
                                break;

                            default:
                                calculatedData.data = dataCalculations[i].DoCalculations(calculatedData.data.ToString(Constants.cultureInfo), timestamp, errorHandler, errorMessageCat, adminSettingsVM);
                                break;
                        }
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
                        string.Format("Serial Port Processing\n(ApplyCalculationsToSelectedData)\n\nSystem Message:\n{0}", ex.Message)));
            }

            return calculatedData;
        }
    }
}

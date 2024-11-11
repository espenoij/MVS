using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MVS
{
    class FileReaderProcessing
    {
        public string delimiter { get; set; }
        public bool fixedPosData { get; set; }
        public int fixedPosStart { get; set; }
        public int fixedPosTotal { get; set; }

        public int dataField { get; set; }
        public DecimalSeparator decimalSeparator { get; set; }
        public bool autoExtractValue { get; set; }

        public FileDataFields SplitDataLine(string dataLine)
        {
            FileDataFields fileDataFields = new FileDataFields();
            int i = 0;

            // Splitte data linjen etter delimiter
            if (!fixedPosData)
            {
                if (!string.IsNullOrEmpty(delimiter))
                {
                    foreach (var dataField in dataLine.Split(delimiter[0]))
                    {
                        if (i < fileDataFields.dataField.Length)
                        {
                            // Legge inn nye data felt
                            fileDataFields.dataField[i++] = dataField;
                        }
                    }
                }
            }
            // Fixed pos data
            else
            {
                string str = TextHelper.EscapeControlChars(dataLine);
                if (fixedPosStart + fixedPosTotal < str.Length)
                    fileDataFields.dataField[0] = str.Substring(fixedPosStart, fixedPosTotal);
            }

            return fileDataFields;
        }

        public SelectedDataField FindSelectedData(FileDataFields fileDataFields)
        {
            SelectedDataField selectedData = new SelectedDataField();

            if (!string.IsNullOrEmpty(fileDataFields.dataField[dataField]))
                selectedData.selectedDataFieldString = fileDataFields.dataField[dataField];

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

            return selectedData;
        }

        public CalculatedData ApplyCalculationsToSelectedData(SelectedDataField selectedData, List<DataCalculations> dataCalculations, DateTime timestamp, ErrorHandler errorHandler, ErrorMessageCategory errorMessageCat, AdminSettingsVM adminSettingsVM)
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
                            //case CalculationType.GPSPosition:
                            ////case CalculationType.NWSCodes:
                            //case CalculationType.METARCodes:
                            //    calculatedData.data = dataCalculations[i].DoCalculations(selectedData.selectedDataFieldString, timestamp, errorHandler, errorMessageCat, adminSettingsVM);
                            //    break;

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
                        string.Format("File Reader Processing\n(ApplyCalculationsToSelectedData)\n\nSystem Message:\n{0}", ex.Message)));
            }

            return calculatedData;
        }
    }
}

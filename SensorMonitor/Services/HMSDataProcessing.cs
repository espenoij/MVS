using System;
using System.Collections.Generic;

namespace SensorMonitor
{
    class HMSDataProcessing
    {
        private ErrorHandler errorHandler;
        private ErrorMessageCategory errorMessageCat;

        // Liste med data kalkulasjoner som skal utføres på HMS data
        private List<DataCalculations> dataCalculations = new List<DataCalculations>();

        public void Init(ErrorHandler errorHandler, ErrorMessageCategory errorMessageCat)
        {
            this.errorHandler = errorHandler;
            this.errorMessageCat = errorMessageCat;
        }

        // Legg inn en ny type kalkulasjon
        public void AddProcessing(CalculationType type, double parameter)
        {
            dataCalculations.Add(new DataCalculations()
            {
                type = type,
                parameter = parameter
            });
        }

        // Utfør de innlagte kalkulasjonene
        // NB! Denne metoden returnerer numerisk verdi! Det er mest praktisk ifht bruken her.
        // DoCalculations leverer string fordi ikke alle kalkulasjons typene er av numerisk karakter. F.eks. vær-koder.
        // Mao dersom CalculationType her settes til en type som ikke leverer numeriske verdier så returnes NaN.
        // Dersom det trengs kan man lage en egen versjon av DoProcessing som leverer string.
        public double DoProcessing(HMSData newData)
        {
            string processedData = string.Empty;

            if (newData != null && errorHandler != null)
            {
                // Inn data flyttes til utdata feltet da dette mates i loopen under til vi har kjørt gjennom alle kalkulasjoner
                processedData = newData.data.ToString();

                // Løpe gjennom alle kalkulasjoner
                foreach (var item in dataCalculations)
                {
                    // Tar output fra forrige iterasjon og kjører ny kalkulasjon
                    processedData = item.DoCalculations(processedData, newData.timestamp, errorHandler, errorMessageCat).ToString();
                }
            }

            // Konvertere string til double
            double resultData;
            if (double.TryParse(processedData, out resultData))
                return resultData;
            else
                return double.NaN;
        }
    }
}

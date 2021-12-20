using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    class Verfication
    {
        private UserInputs userInputs;

        private DataCollection testData;
        private DataCollection referenceData;
        private RadObservableCollectionEx<VerificationData> verificationData;
        private List<double> refTimeIDList = new List<double>();

        private double prevID = 0;

        public Verfication(Config config, UserInputs userInputs)
        {
            this.userInputs = userInputs;

            // Test data liste
            testData = new DataCollection();
            testData.LoadTestData(config);

            // Referanse data liste
            referenceData = new DataCollection();
            referenceData.LoadReferenceData(config);

            // Verification data
            verificationData = new RadObservableCollectionEx<VerificationData>();

            // Referanse time ID list (disse skal lagres til DB)
            for (int i = 52200; i <= 57600; i += 60)
                refTimeIDList.Add(i);

            // User input settings
            userInputs.dayNight = DayNight.Day;
            userInputs.helicopterType = HelicopterType.EC225;
            userInputs.helideckCategory = HelideckCategory.Category1;
        }

        public DataCollection GetTestData()
        {
            return testData;
        }

        public DataCollection GetRefData()
        {
            return referenceData;
        }

        public RadObservableCollectionEx<VerificationData> GetVerificationData()
        {
            return verificationData;
        }

        public void Reset()
        {
            // Resette sammenligningsdata
            foreach (var item in verificationData)
                item.Reset();
        }

        public void Update(RadObservableCollectionEx<HMSData> hmsDataList, RadObservableCollectionEx<SensorData> sensorDataList, DatabaseHandler database)
        {
            // Test Data
            testData.TransferData(hmsDataList);

            // Referanse Data
            referenceData.TransferData(sensorDataList);

            // Verification
            // Overføre og regn ut forskjellene mellom test data og referanse data
            // Input til listen er test data og referanse data.
            RadObservableCollectionEx<HMSData> testDataList = testData.GetDataList();
            RadObservableCollectionEx<HMSData> referenceDataList = referenceData.GetDataList();

            // Løper gjennom listen med test data
            foreach (var item in testDataList)
            {
                // Finner tilsvarende data i referanse data listen
                var refData = referenceDataList.Where(x => x.id == item.id);
                if (refData.Count() == 1)
                {
                    // Finne data i verification data listen
                    var veriData = verificationData.Where(x => (int)x.id == item.id);

                    // Dersom verifikasjons data allerede eksisterer -> oppdater
                    if (veriData.Count() == 1)
                    {
                        veriData.First().id = (VerificationType)item.id;
                        veriData.First().name = item.name;
                        veriData.First().testData = item.data;
                        veriData.First().refData = refData.First().data;
                    }
                    // Dersom de ikke eksisterer -> legg inn
                    else
                    {
                        verificationData.Add(new VerificationData()
                        {
                            id = (VerificationType)item.id,
                            name = item.name,
                            testData = item.data,
                            refData = refData.First().data
                        });
                    }
                }
            }

            // Sette on-deck i user inputs
            if (verificationData.First().testData == 56100)
            {
                userInputs.onDeckHelicopterHeading = 388;
                userInputs.onDeckTime = DateTime.UtcNow;
                userInputs.onDeckVesselHeading = testData.GetData(ValueType.VesselHeading).data;
                userInputs.onDeckWindDirection = testData.GetData(ValueType.HelideckWindDirection2m).data;
            }

            // Oppdatere/beregne forskjeller mellom test data og referanse data.
            foreach (var item in verificationData)
            {
                // Sammenligne test og referanse data
                if (item.id != VerificationType.TimeID) // Men aldri for Time ID
                    item.Compare();
                else
                    item.differenceAbs = 0;
            }

            // Legge test data inn i databasen
            // Sjekke time ID mot listen med time ID'er vi er interessert i
            // Matcher time ID settet i referanse data filen fra CAA (OUT_PRE tab i excel ark)
            if (refTimeIDList.Exists(x => x == verificationData.First().testData) && prevID != verificationData.First().testData)
            {
                database.Insert(verificationData);

                // Forhinderer duplikater
                prevID = verificationData.First().testData;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class Verfication
    {
        private UserInputs userInputs;
        private ErrorHandler errorHandler;

        private HMSDataCollection testData;
        private HMSDataCollection referenceData;
        private RadObservableCollection<VerificationData> verificationData;
        private List<double> refTimeIDList = new List<double>();

        private double prevID = 0;

        private bool deviationReset = false;
        private bool helicopterSetInit = false;
        private bool helicopterSetLanded = false;
        private bool helicopterSetTakeoff = false;

        public Verfication(Config config, UserInputs userInputs, ErrorHandler errorHandler)
        {
            this.userInputs = userInputs;
            this.errorHandler = errorHandler;

            // Test data liste
            testData = new HMSDataCollection();
            testData.LoadTestData(config);

            // Referanse data liste
            referenceData = new HMSDataCollection();
            referenceData.LoadReferenceData(config);

            // Verification data
            verificationData = new RadObservableCollection<VerificationData>();

            // Referanse time ID list (disse skal lagres til DB)
            for (int i = 52200; i <= 57600; i += 60)
                refTimeIDList.Add(i);

            // User input settings
            userInputs.helicopterType = HelicopterType.EC225;
            userInputs.helideckCategory = HelideckCategory.Category1;
            userInputs.dayNight = DayNight.Day;
            userInputs.displayMode = DisplayMode.PreLanding;
        }

        public HMSDataCollection GetTestData()
        {
            return testData;
        }

        public HMSDataCollection GetRefData()
        {
            return referenceData;
        }

        public RadObservableCollection<VerificationData> GetVerificationData()
        {
            return verificationData;
        }

        public void Reset()
        {
            // Resette sammenligningsdata
            foreach (var item in verificationData)
                item.Reset();
        }

        public void Update(HMSDataCollection hmsOutputData, RadObservableCollection<SensorData> sensorDataList, DatabaseHandler database)
        {
            // Test Data
            testData.TransferData(hmsOutputData.GetDataList());

            // Referanse Data
            referenceData.TransferData(sensorDataList);

            // Verification
            // Overføre og regn ut forskjellene mellom test data og referanse data
            // Input til listen er test data og referanse data.

            // Løper gjennom listen med test data
            foreach (var item in testData.GetDataList().ToList())
            {
                // Finner tilsvarende data i referanse data listen
                var refData = referenceData.GetDataList().ToList().Where(x => x.id == item.id);
                if (refData.Count() == 1)
                {
                    // Korrigere verdier som skal vises som heltall
                    if (item.id == (int)VerificationType.MSI ||
                        item.id == (int)VerificationType.WSI ||
                        item.id == (int)VerificationType.RelativeWindDirection ||
                        item.id == (int)VerificationType.VesselHeading ||
                        item.id == (int)VerificationType.HelideckHeading ||
                        item.id == (int)VerificationType.HelicopterHeading ||
                        item.id == (int)VerificationType.WindSpeed2m ||
                        item.id == (int)VerificationType.WindDir2m ||
                        item.id == (int)VerificationType.WindGust2m ||
                        item.id == (int)VerificationType.WindSpeed10m ||
                        item.id == (int)VerificationType.WindDir10m ||
                        item.id == (int)VerificationType.WindGust10m)
                    {
                        item.data = Math.Round(item.data, 0, MidpointRounding.AwayFromZero);
                    }

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

            if (verificationData.Count > 0)
            {
                // Stopper alle verification operasjoner når vi passerer siste time ID
                if (verificationData.First().testData <= 57600)
                {
                    // Resetter deviation variablene når vi starter med referanse data fra 54000
                    if (!deviationReset && verificationData.First().testData >= 54000)
                    {
                        Reset();
                        deviationReset = true;
                    }

                    // Helikopter Init
                    if (!helicopterSetInit)
                    {
                        userInputs.displayMode = DisplayMode.PreLanding;
                        userInputs.onDeckHelicopterHeading = -1;
                        userInputs.onDeckTime = DateTime.MinValue;
                        userInputs.onDeckVesselHeading = -1;
                        userInputs.onDeckWindDirection = -1;

                        helicopterSetInit = true;
                    }

                    // Helikopter lander: Sette on-deck i user inputs
                    if (verificationData.First().testData >= 56100 &&
                        !helicopterSetLanded)
                    {
                        userInputs.displayMode = DisplayMode.OnDeck;

                        var time = referenceData.GetDataList().ToList().Where(x => x.id == 12);
                        if (time.Count() == 1)
                            userInputs.onDeckHelicopterHeading = time.First().data;
                        else
                            userInputs.onDeckHelicopterHeading = -1;

                        userInputs.onDeckTime = DateTime.UtcNow;
                        userInputs.onDeckVesselHeading = hmsOutputData.GetData(ValueType.VesselHeading).data;
                        userInputs.onDeckWindDirection = hmsOutputData.GetData(ValueType.HelideckWindDirection2m).data;

                        helicopterSetLanded = true;
                    }

                    // Helikopteret tar av igjen
                    if (verificationData.First().testData >= 57300 &&
                        !helicopterSetTakeoff)
                    {
                        userInputs.displayMode = DisplayMode.PreLanding;

                        helicopterSetTakeoff = true;
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
                        try
                        {
                            // Lagre til databasen
                            database.Insert(verificationData);

                            errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert7_VerificationData);
                        }
                        catch (Exception ex)
                        {
                            errorHandler.Insert(
                                new ErrorMessage(
                                    DateTime.UtcNow,
                                    ErrorMessageType.Database,
                                    ErrorMessageCategory.None,
                                    string.Format("Database Error (Insert verificationData)\n\nSystem Message:\n{0}", ex.Message)));

                            errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert7_VerificationData);
                        }

                        // Forhinderer duplikater
                        prevID = verificationData.First().testData;
                    }
                }
            }
        }
    }
}

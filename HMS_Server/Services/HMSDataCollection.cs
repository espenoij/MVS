using System;
using System.Linq;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class HMSDataCollection
    {
        // Configuration settings
        private Config config;

        // Liste med data
        private RadObservableCollectionEx<HMSData> dataList = new RadObservableCollectionEx<HMSData>();

        public void LoadHMSInput(Config config)
        {
            this.config = config;

            // Hente liste med data fra fil
            HMSDataConfigCollection clientConfigCollection = config.GetClientDataList();

            if (clientConfigCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (HMSDataConfig item in clientConfigCollection)
                {
                    // Nytt data objekt
                    HMSData clientSensorData = new HMSData();

                    // Overføre data
                    clientSensorData.id = item.id;
                    clientSensorData.name = item.name;

                    if (item.dataId == string.Empty)
                        item.dataId = "0";
                    clientSensorData.dataId = int.Parse(item.dataId, Constants.cultureInfo);

                    if (item.sensorId == string.Empty)
                        item.sensorId = "0";
                    clientSensorData.sensorGroupId = int.Parse(item.sensorId, Constants.cultureInfo);

                    clientSensorData.dbColumn = item.dbTableName;

                    // Legge inn i data listen
                    dataList.Add(clientSensorData);
                }
            }
        }

        public void LoadTestData(Config config)
        {
            this.config = config;

            // Hente liste med data fra fil
            TestDataConfigCollection testDataConfigCollection = config.GetTestDataList();

            if (testDataConfigCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (TestDataConfig item in testDataConfigCollection)
                {
                    // Nytt data objekt
                    HMSData clientSensorData = new HMSData();

                    // Overføre data
                    clientSensorData.id = item.id;
                    clientSensorData.name = item.name;

                    if (item.dataId == string.Empty)
                        item.dataId = "0";
                    clientSensorData.dataId = int.Parse(item.dataId, Constants.cultureInfo);

                    // Legge inn i data listen
                    dataList.Add(clientSensorData);
                }
            }
        }

        public void LoadReferenceData(Config config)
        {
            this.config = config;

            // Hente liste med data fra fil
            ReferenceDataConfigCollection referanceDataConfigCollection = config.GetReferenceDataList();

            if (referanceDataConfigCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (ReferenceDataConfig item in referanceDataConfigCollection)
                {
                    // Nytt data objekt
                    HMSData clientSensorData = new HMSData();

                    // Overføre data
                    clientSensorData.id = item.id;
                    clientSensorData.name = item.name;

                    if (item.dataId == string.Empty)
                        item.dataId = "0";
                    clientSensorData.dataId = int.Parse(item.dataId, Constants.cultureInfo);

                    // Legge inn i data listen
                    dataList.Add(clientSensorData);
                }
            }
        }

        // Overføre data fra liste i input to denne data samlingen
        // HMS: Overføre fra sensor data liste til HMS data liste
        // Verification: Overføre fra sensor data liste til referanse data liste
        public void TransferData(RadObservableCollection<SensorData> sensorDataList)
        {
            // Lese timeout fra config
            double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            // Løper gjennom data listen
            foreach (var hmsData in dataList.ToList())
            {
                // Finne match i mottaker data listen
                var serverData = sensorDataList.Where(x => x?.id == hmsData?.dataId);

                // Fant match?
                if (serverData.Count() > 0 && hmsData != null)
                {
                    // Overføre data
                    hmsData.data = serverData.First().data;

                    // Overføre time stamp
                    hmsData.timestamp = serverData.First().timestamp;

                    // Sjekke timestamp for data timeout
                    if (hmsData.timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                        hmsData.status = DataStatus.TIMEOUT_ERROR;
                    else
                        hmsData.status = DataStatus.OK;
                }
                // Ingen verdi i data samlingen knyttet til verdi i mottaker listen
                else
                {
                    hmsData.status = DataStatus.TIMEOUT_ERROR;
                }
            }
        }

        // Overføre data fra liste i input to denne data samlingen
        // Verification: Overføre fra HMS data liste til test data liste
        public void TransferData(RadObservableCollectionEx<HMSData> hmsDataList)
        {
            // Lese timeout fra config
            double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            // Løper gjennom data listen
            foreach (var hmsData in dataList.ToList())
            {
                // Finne match i mottaker data listen
                var serverData = hmsDataList.Where(x => x?.id == hmsData?.dataId);

                // Fant match?
                if (serverData.Count() > 0 && hmsData != null)
                {
                    // Overføre data
                    hmsData.data = serverData.First().data;

                    // Overføre time stamp
                    hmsData.timestamp = serverData.First().timestamp;

                    // Sjekke timestamp for data timeout
                    if (hmsData.timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                        hmsData.status = DataStatus.TIMEOUT_ERROR;
                    else
                        hmsData.status = DataStatus.OK;
                }
                // Ingen verdi i data samlingen knyttet til verdi i mottaker listen
                else
                {
                    hmsData.status = DataStatus.TIMEOUT_ERROR;
                }
            }
        }

        // Hente datalisten
        public RadObservableCollectionEx<HMSData> GetDataList()
        {
            return dataList;
        }

        // Hente data fra samlingen
        public HMSData GetData(ValueType id)
        {
            var sensorData = dataList.Where(x => x.id == (int)id);
            if (sensorData.Count() > 0)
                return sensorData.First();
            else
                return null;
        }
    }
}

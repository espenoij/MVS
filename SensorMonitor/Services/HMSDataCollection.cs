using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Data;

namespace SensorMonitor
{
    public class HMSDataCollection
    {
        // Configuration settings
        private Config config;

        // Liste med HMS data
        private RadObservableCollectionEx<HMSData> hmsDataList = new RadObservableCollectionEx<HMSData>();

        public void LoadHMSInputDefinitions(Config config)
        {
            this.config = config;

            // Hente liste med data fra fil
            HMSDataConfigCollection clientConfigCollection = config.GetClientDataList();

            if (clientConfigCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (HMSDataConfig item in clientConfigCollection)
                {
                    // Nytt HMS data objekt
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

                    clientSensorData.dbTableName = item.dbTableName;

                    // Legge inn i data listen
                    hmsDataList.Add(clientSensorData);
                }
            }
        }

        // Overføre fra sensor data liste til HMS data liste
        public void Transfer(RadObservableCollectionEx<SensorData> sensorDataList)
        {
            // Lese timeout fra config
            double dataTimeout = config.Read(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            lock (sensorDataList)
            {
                // Løper gjennom HMS data listen
                foreach (var hmsData in hmsDataList.ToList())
                {
                    // Finne match i server data listen
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
                    // Ingen server verdi knyttet til klient verdi
                    else
                    {
                        hmsData.status = DataStatus.TIMEOUT_ERROR;
                    }
                }
            }
        }

        public RadObservableCollectionEx<HMSData> GetDataList()
        {
            return hmsDataList;
        }

        public HMSData GetData(ValueType id)
        {
            var sensorData = hmsDataList.Where(x => x.id == (int)id);
            if (sensorData.Count() > 0)
                return sensorData.First();
            else
                return null;
        }
    }
}

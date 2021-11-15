using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace SensorMonitorClient
{
    public class SensorStatusVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Sensor Status
        private SensorStatus sensorStatus;

        public void Init(SensorStatus sensorStatus)
        {
            this.sensorStatus = sensorStatus;

            InitUI();
        }

        private void InitUI()
        {
            sensorName = new List<string>();
            for (int i = 0; i < sensorStatus.GetSensorList()?.Count; i++)
                sensorName.Add(sensorStatus.GetSensorList()[i].name);
        }

        public void Update()
        {
            if (sensorStatus.GetSensorList()?.Count != sensorName.Count)
            {
                sensorName.Clear();

                for (int i = 0; i < sensorStatus.GetSensorList()?.Count; i++)
                    sensorName.Add(sensorStatus.GetSensorList()[i].name);
            }

            foreach (var item in sensorStatus.GetSensorList())
                SetSensorName(item);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Sensor Name
        /////////////////////////////////////////////////////////////////////////////
        private List<string> sensorName { get; set; }

        public void SetSensorName(Sensor sensor)
        {
            if (sensor.id < sensorName.Count)
            {
                sensorName[sensor.id] = sensor.name;

                switch (sensor.id)
                {
                    case 0:
                        OnPropertyChanged(nameof(sensorName1));
                        break;
                    case 1:
                        OnPropertyChanged(nameof(sensorName2));
                        break;
                    case 2:
                        OnPropertyChanged(nameof(sensorName3));
                        break;
                    case 3:
                        OnPropertyChanged(nameof(sensorName4));
                        break;
                    case 4:
                        OnPropertyChanged(nameof(sensorName5));
                        break;
                    case 5:
                        OnPropertyChanged(nameof(sensorName6));
                        break;
                    case 6:
                        OnPropertyChanged(nameof(sensorName7));
                        break;
                    case 7:
                        OnPropertyChanged(nameof(sensorName8));
                        break;
                    case 8:
                        OnPropertyChanged(nameof(sensorName9));
                        break;
                    case 9:
                        OnPropertyChanged(nameof(sensorName10));
                        break;
                    case 10:
                        OnPropertyChanged(nameof(sensorName11));
                        break;
                    case 11:
                        OnPropertyChanged(nameof(sensorName12));
                        break;
                }
            }
        }

        private string GetSensorName(int sensor)
        {
            if (sensor < sensorName?.Count)
                return sensorName[sensor];
            else
                return Constants.NotAvailable;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Sensor Name to string
        /////////////////////////////////////////////////////////////////////////////
        public string sensorName1 { get { return GetSensorName(0); } }
        public string sensorName2 { get { return GetSensorName(1); } }
        public string sensorName3 { get { return GetSensorName(2); } }
        public string sensorName4 { get { return GetSensorName(3); } }
        public string sensorName5 { get { return GetSensorName(4); } }
        public string sensorName6 { get { return GetSensorName(5); } }
        public string sensorName7 { get { return GetSensorName(6); } }
        public string sensorName8 { get { return GetSensorName(7); } }
        public string sensorName9 { get { return GetSensorName(8); } }
        public string sensorName10 { get { return GetSensorName(9); } }
        public string sensorName11 { get { return GetSensorName(10); } }
        public string sensorName12 { get { return GetSensorName(11); } }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

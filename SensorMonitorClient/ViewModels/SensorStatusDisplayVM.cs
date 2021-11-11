using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace SensorMonitorClient
{
    public class SensorStatusDisplayVM
    {
        // Sensor Status
        private SensorGroupStatus sensorStatus;

        public RadObservableCollectionEx<SensorStatusDisplay> sensorStatusDisplayList = new RadObservableCollectionEx<SensorStatusDisplay>();

        public void Init(SensorGroupStatus sensorStatus)
        {
            this.sensorStatus = sensorStatus;

            InitUI();
        }

        private void InitUI()
        {
            for (int i = 0; i < Constants.MaxSensors; i++)
                sensorStatusDisplayList.Add(new SensorStatusDisplay());
        }

        public void Update()
        {
            RadObservableCollectionEx<SensorGroup> sensorStatusList = sensorStatus.GetSensorList();

            // Oppdatere liste med sensor navn/status
            for (int i = 0; i < Constants.MaxSensors; i++)
            {
                if (i < sensorStatusList?.Count)
                {
                    sensorStatusDisplayList[i].sensorName = sensorStatusList[i].name;
                    sensorStatusDisplayList[i].sensorStatus = sensorStatusList[i].status;
                }
                else
                {
                    sensorStatusDisplayList[i].sensorName = string.Empty;
                    sensorStatusDisplayList[i].sensorStatus = DataStatus.NONE;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Sensor Status Display
        /////////////////////////////////////////////////////////////////////////////
        public class SensorStatusDisplay : INotifyPropertyChanged
        {
            // Change notification
            public event PropertyChangedEventHandler PropertyChanged;

            private string _sensorName { get; set; } = string.Empty;
            public string sensorName
            {
                get
                {
                    return _sensorName;
                }
                set
                {
                    _sensorName = value;
                    OnPropertyChanged();
                }
            }

            private DataStatus _sensorStatus { get; set; } = DataStatus.NONE;
            public DataStatus sensorStatus
            {
                get
                {
                    return _sensorStatus;
                }
                set
                {
                    _sensorStatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(sensorImage));
                }
            }
            public BitmapImage sensorImage
            {
                get
                {
                    switch (sensorStatus)
                    {
                        case DataStatus.OK:
                            return new BitmapImage(new Uri("../Icons/outline_check_circle_black_48dp.png", UriKind.Relative));

                        case DataStatus.TIMEOUT_ERROR:
                            return new BitmapImage(new Uri("../Icons/outline_info_black_48dp.png", UriKind.Relative));

                        default:
                            return new BitmapImage();
                    }
                }
            }

            // Variabel oppdatert
            // Dersom navn ikke er satt brukes kallende medlem sitt navn
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }


    }
}

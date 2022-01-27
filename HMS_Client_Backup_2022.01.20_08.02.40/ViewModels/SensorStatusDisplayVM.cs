﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace HMS_Client
{
    public class SensorStatusDisplayVM
    {
        // Sensor Status
        private SensorGroupStatus sensorStatus;

        public RadObservableCollectionEx<SensorStatusDisplay> sensorStatusDisplayList = new RadObservableCollectionEx<SensorStatusDisplay>();

        public void Init(SensorGroupStatus sensorStatus, Config config)
        {
            this.sensorStatus = sensorStatus;

            InitUI(config);
        }

        private void InitUI(Config config)
        {
            for (int i = 0; i < Constants.MaxSensors; i++)
            {
                sensorStatusDisplayList.Add(new SensorStatusDisplay());

                int t = i % 10;
                if (t == 0 ||
                    t == 1 ||
                    t == 4 ||
                    t == 5 ||
                    t == 8 ||
                    t == 9)
                    sensorStatusDisplayList[i].even = true;
                else
                    sensorStatusDisplayList[i].even = false;
            }

            DispatcherTimer statusUpdate = new DispatcherTimer();
            statusUpdate.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            statusUpdate.Tick += UpdateStatus;
            statusUpdate.Start();

            // Oppdatere sensor group status
            void UpdateStatus(object sender, EventArgs e)
            {
                RadObservableCollectionEx<SensorGroup> sensorStatusList = sensorStatus.GetSensorGroupList();

                // Oppdatere liste med sensor navn/status
                for (int i = 0; i < Constants.MaxSensors; i++)
                {
                    if (i < sensorStatusList?.Count)
                    {
                        sensorStatusDisplayList[i].name = sensorStatusList[i].name;
                        sensorStatusDisplayList[i].status = sensorStatusList[i].status;
                        sensorStatusDisplayList[i].active = sensorStatusList[i].active;
                    }
                    else
                    {
                        sensorStatusDisplayList[i].name = string.Empty;
                        sensorStatusDisplayList[i].status = DataStatus.NONE;
                        sensorStatusDisplayList[i].active = false;
                    }
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

            private string _name { get; set; } = string.Empty;
            public string name
            {
                get
                {
                    return _name;
                }
                set
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }

            private bool _active { get; set; }
            public bool active
            {
                get
                {
                    return _active;
                }
                set
                {
                    _active = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(sensorStatusVisibility));
                }
            }
            public Visibility sensorStatusVisibility
            {
                get
                {
                    if (active)
                        return Visibility.Visible;
                    else
                        return Visibility.Hidden;
                }
            }

            private DataStatus _status { get; set; } = DataStatus.NONE;
            public DataStatus status
            {
                get
                {
                    return _status;
                }
                set
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(statusString));
                    OnPropertyChanged(nameof(statusBackgroundColor));
                }
            }
            public string statusString
            {
                get
                {
                    return _status.ToString();
                }
            }

            private bool _even { get; set; }
            public bool even
            {
                get
                {
                    return _even;
                }
                set
                {
                    _even = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(statusBackgroundColor));
                }
            }

            public StatusBackground statusBackgroundColor
            {
                get
                {
                    switch (status)
                    {
                        case DataStatus.OK:
                            if (even)
                                return StatusBackground.BACKGROUND_SEPARATOR;
                            else
                                return StatusBackground.BLANK;

                        case DataStatus.TIMEOUT_ERROR:
                            return StatusBackground.AMBER;

                        default:
                            return StatusBackground.AMBER;
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

    public enum StatusBackground
    {
        BLANK,
        BACKGROUND_SEPARATOR,
        AMBER
    }
}
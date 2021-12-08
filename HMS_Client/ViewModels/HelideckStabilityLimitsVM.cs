using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class HelideckStabilityLimitsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer20m = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer3h = new DispatcherTimer();

        // MSI / WSI Graph buffer / data
        private RadObservableCollectionEx<HMSData> msiwsi20mBuffer = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> msiwsi3hBuffer = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> msiwsi20mDataList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> msiwsi3hDataList = new RadObservableCollectionEx<HMSData>();

        private bool initDone = false;

        public void Init(Config config, SensorGroupStatus sensorStatus)
        {
            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(msi20m)) OnPropertyChanged(nameof(msi20mString));
                if (sensorStatus.TimeoutCheck(wsi20m)) OnPropertyChanged(nameof(wsi20mString));

                if (sensorStatus.TimeoutCheck(msiwsi))
                {
                    OnPropertyChanged(nameof(msiwsiX));
                    OnPropertyChanged(nameof(msiwsiY));
                    OnPropertyChanged(nameof(msiwsiXString));
                    OnPropertyChanged(nameof(msiwsiYString));
                }

                // Oppdatere data som skal ut i grafer
                if (msiwsi.status == DataStatus.OK)
                {
                    GraphBuffer.Update(msiwsi, msiwsi20mBuffer);
                    GraphBuffer.Update(msiwsi, msiwsi3hBuffer);
                }
            }

            // Oppdatere trend data i UI: 20 minutter
            ChartDataUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault));
            ChartDataUpdateTimer20m.Tick += ChartDataUpdate20m;
            ChartDataUpdateTimer20m.Start();

            void ChartDataUpdate20m(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data: 20m
                GraphBuffer.Transfer(msiwsi20mBuffer, msiwsi20mDataList);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(msiwsi20mDataList, Constants.Minutes20);
            }

            // Oppdatere trend data i UI: 3 hours
            ChartDataUpdateTimer3h.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault));
            ChartDataUpdateTimer3h.Tick += ChartDataUpdate3h;
            ChartDataUpdateTimer3h.Start();

            void ChartDataUpdate3h(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data: 20m
                GraphBuffer.Transfer(msiwsi3hBuffer, msiwsi3hDataList);


                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(msiwsi3hDataList, Constants.Hours3);
            }
        }

        private void InitUI()
        {
            _mms_msi = new HMSData();
            _msi20m = new HMSData();
            _windDir = new HMSData();
            _windSpd = new HMSData();
            _vesselDir = new HMSData();
            _vesselSpd = new HMSData();
            _wsit = new HMSData();
            _wsi20m = new HMSData();
            _msiwsi = new HMSData();

            _msi20m.data = 0;

            selectedGraphTime = GraphTime.Minutes20;

            initDone = true;
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            if (initDone)
            {
                msi20m = clientSensorList.GetData(ValueType.MSI);
                wsi20m = clientSensorList.GetData(ValueType.WSI);

                // MSI / WSI Graf
                /////////////////////////////////////////////////
                /// Kombinerer MSI og WSI data
                if (msi20m.status == DataStatus.OK &&
                    wsi20m.status == DataStatus.OK)
                {
                    HMSData tmp = new HMSData();

                    // Sette data
                    tmp.data = msi20m.data;
                    tmp.data2 = wsi20m.data;

                    tmp.status = DataStatus.OK;

                    if (msi20m.timestamp < wsi20m.timestamp)
                        tmp.timestamp = msi20m.timestamp;
                    else
                        tmp.timestamp = wsi20m.timestamp;

                    // Lagre
                    msiwsi = tmp;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Measure of Motion Severity - MMS msi
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _mms_msi { get; set; }
        public HMSData mms_msi
        {
            get
            {
                return _mms_msi;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _mms_msi.data ||
                        value.timestamp != _mms_msi.timestamp)
                    {
                        _mms_msi.Set(value);
                        OnPropertyChanged(nameof(mms_msiString));
                    }
                }
            }
        }

        public string mms_msiString
        {
            get
            {
                if (mms_msi != null)
                {
                    // Sjekke om data er gyldig
                    if (mms_msi.status == DataStatus.OK)
                    {
                        return mms_msi.data.ToString("0.0");
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion Severity Index - MSI 20m
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _msi20m { get; set; }
        public HMSData msi20m
        {
            get
            {
                return _msi20m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _msi20m.data ||
                        value.timestamp != _msi20m.timestamp)
                    {
                        _msi20m.Set(value);
                        OnPropertyChanged(nameof(msi20mString));
                    }
                }
            }
        }

        public string msi20mString
        {
            get
            {
                if (msi20m != null)
                {
                    // Sjekke om data er gyldig
                    if (msi20m.status == DataStatus.OK)
                    {
                        return msi20m.data.ToString("0.0");
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windDir { get; set; }
        public HMSData windDir
        {
            get
            {
                return _windDir;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windDir.data ||
                        value.timestamp != _windDir.timestamp)
                    {
                        _windDir.Set(value);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windSpd { get; set; }
        public HMSData windSpd
        {
            get
            {
                return _windSpd;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windSpd.data ||
                        value.timestamp != _windSpd.timestamp)
                    {
                        _windSpd.Set(value);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Vessel Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselDir { get; set; }
        public HMSData vesselDir
        {
            get
            {
                return _vesselDir;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _vesselDir.data ||
                        value.timestamp != _vesselDir.timestamp)
                    {
                        _vesselDir.Set(value);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Vessel Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselSpd { get; set; }
        public HMSData vesselSpd
        {
            get
            {
                return _vesselSpd;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _vesselSpd.data ||
                        value.timestamp != _vesselSpd.timestamp)
                    {
                        _vesselSpd.Set(value);
                    }
                }
            }
        }


        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: WSI (t)
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _wsit { get; set; }
        public HMSData wsit
        {
            get
            {
                return _wsit;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _wsit.data ||
                        value.timestamp != _wsit.timestamp)
                    {
                        _wsit.Set(value);
                    }
                }
            }
        }

        public string wsitString
        {
            get
            {
                if (wsit != null)
                {
                    // Sjekke om data er gyldig
                    if (wsit.status == DataStatus.OK)
                    {
                        return wsit.data.ToString("0.0");
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Severity Index - WSI 20m
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _wsi20m { get; set; }
        public HMSData wsi20m
        {
            get
            {
                return _wsi20m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _wsi20m.data ||
                        value.timestamp != _wsi20m.timestamp)
                    {
                        _wsi20m.Set(value);
                        OnPropertyChanged(nameof(wsi20mString));
                    }
                }
            }
        }

        public string wsi20mString
        {
            get
            {
                if (wsi20m != null)
                {
                    // Sjekke om data er gyldig
                    if (wsi20m.status == DataStatus.OK)
                    {
                        return wsi20m.data.ToString("0.0");
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // MSI / WSI Graf data
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _msiwsi { get; set; }
        public HMSData msiwsi
        {
            get
            {
                return _msiwsi;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(msiwsiX));
                    OnPropertyChanged(nameof(msiwsiY));
                    OnPropertyChanged(nameof(msiwsiXString));
                    OnPropertyChanged(nameof(msiwsiYString));

                    _msiwsi.Set(value);
                }
            }
        }

        public double msiwsiX
        {
            get
            {
                if (_msiwsi?.status == DataStatus.OK)
                    return Math.Round(_msiwsi.data2, 0, MidpointRounding.AwayFromZero);
                else
                    return 0;
            }
        }

        public string msiwsiXString
        {
            get
            {
                if (msiwsi != null)
                {
                    // Sjekke om data er gyldig
                    if (msiwsi.status == DataStatus.OK)
                    {
                        return msiwsiX.ToString("0");
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        public double msiwsiY
        {
            get
            {
                if (msiwsi?.status == DataStatus.OK)
                    return Math.Round(msiwsi.data, 0, MidpointRounding.AwayFromZero);
                else
                    return 0;
            }
        }

        public string msiwsiYString
        {
            get
            {
                if (msiwsi != null)
                {
                    // Sjekke om data er gyldig
                    if (msiwsi.status == DataStatus.OK)
                    {
                        return msiwsiY.ToString("0");
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Selected Graph Time
        /////////////////////////////////////////////////////////////////////////////
        private GraphTime _selectedGraphTime { get; set; }
        public GraphTime selectedGraphTime
        {
            get
            {
                return _selectedGraphTime;
            }
            set
            {
                _selectedGraphTime = value;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Display Mode
        /////////////////////////////////////////////////////////////////////////////
        private DisplayMode _displayMode { get; set; }
        public DisplayMode displayMode
        {
            get
            {
                return _displayMode;
            }
            set
            {
                _displayMode = value;

                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
            }
        }

        public bool displayModeVisibilityPreLanding
        {
            get
            {
                if (_displayMode == DisplayMode.PreLanding)
                    return true;
                else
                    return false;
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum GraphTime
    {
        Minutes20,
        Hours3
    }
}

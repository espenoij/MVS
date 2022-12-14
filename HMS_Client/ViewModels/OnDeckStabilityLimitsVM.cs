using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace HMS_Client
{
    public class OnDeckStabilityLimitsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        // MSI / WSI Graph buffer / data
        public RadObservableCollection<HMSData> msiwsi20mDataList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> msiwsi3hDataList = new RadObservableCollection<HMSData>();

        private bool initDone = false;

        public void Init(Config config, SensorGroupStatus sensorStatus)
        {
            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(msi)) OnPropertyChanged(nameof(msiString));
                if (sensorStatus.TimeoutCheck(wsi)) OnPropertyChanged(nameof(wsiString));

                if (sensorStatus.TimeoutCheck(msiwsi))
                {
                    OnPropertyChanged(nameof(msiwsiX));
                    OnPropertyChanged(nameof(msiwsiY));
                }

                // Her skal vi ikke ha 0 data i grafen
                if (msiwsi.status == DataStatus.OK)
                {
                    // Oppdatere data som skal ut i grafer
                    GraphBuffer.UpdateWithCull(msiwsi, msiwsi20mDataList, Constants.GraphCullFrequency20m);
                    GraphBuffer.UpdateWithCull(msiwsi, msiwsi3hDataList, Constants.GraphCullFrequency3h);
                }

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(msiwsi20mDataList, Constants.Minutes20);
                GraphBuffer.RemoveOldData(msiwsi3hDataList, Constants.Hours3);
            }
        }

        private void InitUI()
        {
            _mms_msi = new HMSData();
            _msi = new HMSData();
            _windDir = new HMSData();
            _windSpd = new HMSData();
            _vesselDir = new HMSData();
            _vesselSpd = new HMSData();
            _wsit = new HMSData();
            _wsi = new HMSData();
            _msiwsi = new HMSData();

            _msi.data = 0;

            selectedGraphTime = GraphTime.Minutes20;

            initDone = true;
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            if (initDone)
            {
                msi = clientSensorList.GetData(ValueType.MSI);
                wsi = clientSensorList.GetData(ValueType.WSI);

                //// TEST
                //if (clientSensorList.GetData(ValueType.MSI).status == DataStatus.OK)
                //{
                //    OnPropertyChanged(nameof(msiString));
                //}
                //if (clientSensorList.GetData(ValueType.MSI).status == DataStatus.OK_NA)
                //{
                //    OnPropertyChanged(nameof(msiString));
                //}

                // MSI / WSI Graf
                /////////////////////////////////////////////////
                /// Kombinerer MSI og WSI data
                if (msi.status == DataStatus.OK &&
                    wsi.status == DataStatus.OK)
                {
                    HMSData tmp = new HMSData();

                    // Sette data
                    tmp.data = msi.data;
                    tmp.data2 = wsi.data;

                    tmp.status = DataStatus.OK;

                    if (msi.timestamp < wsi.timestamp)
                        tmp.timestamp = msi.timestamp;
                    else
                        tmp.timestamp = wsi.timestamp;

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
        // Motion Severity Index - MSI
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _msi { get; set; }
        public HMSData msi
        {
            get
            {
                return _msi;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _msi.data ||
                        value.timestamp != _msi.timestamp ||
                        value.status != _msi.status)
                    {
                        _msi.Set(value);
                        OnPropertyChanged(nameof(msiString));
                    }
                }
            }
        }

        public string msiString
        {
            get
            {
                if (msi != null)
                {
                    // Sjekke om data er gyldig
                    if (msi.status == DataStatus.OK)
                    {
                        return msi.data.ToString("0");
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
        // Wind Severity Index - WSI
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _wsi { get; set; }
        public HMSData wsi
        {
            get
            {
                return _wsi;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _wsi.data ||
                        value.timestamp != _wsi.timestamp ||
                        value.status != _wsi.status)
                    {
                        _wsi.Set(value);
                        OnPropertyChanged(nameof(wsiString));
                    }
                }
            }
        }

        public string wsiString
        {
            get
            {
                if (wsi != null)
                {
                    // Sjekke om data er gyldig
                    if (wsi.status == DataStatus.OK)
                    {
                        return wsi.data.ToString("0");
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

                    _msiwsi.Set(value);
                }
            }
        }

        public double msiwsiX
        {
            get
            {
                if (_msiwsi?.status == DataStatus.OK)
                    return _msiwsi.data2;
                else
                    return double.NaN; // Settes til NaN slik at graf ikke viser data
            }
        }

        public double msiwsiY
        {
            get
            {
                if (msiwsi?.status == DataStatus.OK)
                    return msiwsi.data;
                else
                    return double.NaN; // Settes til NaN slik at graf ikke viser data
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

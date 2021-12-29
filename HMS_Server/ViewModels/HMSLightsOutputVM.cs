using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Server
{
    public class HMSLightsOutputVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer statusUpdateTimer = new DispatcherTimer();

        public void Init(DataCollection hmsOutputDataList, Config config)
        {
            // Oppdatere UI
            statusUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
            statusUpdateTimer.Tick += UIUpdate;
            statusUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                helideckStatusData = hmsOutputDataList?.GetData(ValueType.HelideckStatus);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckStatusData { get; set; } = new HMSData();
        public HMSData helideckStatusData
        {
            get
            {
                return _helideckStatusData;
            }
            set
            {
                if (value != null)
                {
                    _helideckStatusData.Set(value);

                    OnPropertyChanged(nameof(helideckStatus));
                }
            }
        }

        public HelideckStatusType helideckStatus
        {
            get
            {
                if (_helideckStatusData.status == DataStatus.OK)
                    return (HelideckStatusType)_helideckStatusData.data;
                else
                    // Dersom vi har data timeout skal lyset slåes av
                    return HelideckStatusType.OFF;
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

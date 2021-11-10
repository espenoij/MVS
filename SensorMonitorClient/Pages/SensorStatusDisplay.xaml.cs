using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for NOROG_SensorStatus.xaml
    /// </summary>
    public partial class SensorStatusDisplay : UserControl
    {
        // Configuration settings
        private Config config;

        // Sensor Status
        private SensorStatus sensorStatus;

        private List<DataStatus> sensorStatusPrevious = new List<DataStatus>();

        public SensorStatusDisplay()
        {
            InitializeComponent();
        }

        public void Init(SensorStatusVM sensorStatusVM, Config config, SensorStatus sensorStatus, RadTabItem tabHelicopterOps)
        {
            this.config = config;
            this.sensorStatus = sensorStatus;

            DataContext = sensorStatusVM;

            for (int i = 0; i < Constants.MaxSensors; i++)
                sensorStatusPrevious.Add(DataStatus.OK);

            InitUpdateUI(tabHelicopterOps);
        }

        private void InitUpdateUI(RadTabItem tabHelicopterOps)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            timer.Tick += UpdateUI;
            timer.Start();

            void UpdateUI(object sender, EventArgs e)
            {
                try
                {
                    if (tabHelicopterOps.IsSelected && sensorStatus != null)
                    {
                        /////////////////////////////////////////////////////////////////////////////
                        // Sensor Status
                        /////////////////////////////////////////////////////////////////////////////
                        for (int i = 0; i < Constants.MaxSensors; i++)
                        {
                            Grid statusField;
                            Image statusImage;

                            switch (i)
                            {
                                case 0:
                                    statusField = stSensor1;
                                    statusImage = imgSensor1;
                                    break;
                                case 1:
                                    statusField = stSensor2;
                                    statusImage = imgSensor2;
                                    break;
                                case 2:
                                    statusField = stSensor3;
                                    statusImage = imgSensor3;
                                    break;
                                case 3:
                                    statusField = stSensor4;
                                    statusImage = imgSensor4;
                                    break;
                                case 4:
                                    statusField = stSensor5;
                                    statusImage = imgSensor5;
                                    break;
                                case 5:
                                    statusField = stSensor6;
                                    statusImage = imgSensor6;
                                    break;
                                case 6:
                                    statusField = stSensor7;
                                    statusImage = imgSensor7;
                                    break;
                                case 7:
                                    statusField = stSensor8;
                                    statusImage = imgSensor8;
                                    break;
                                case 8:
                                    statusField = stSensor9;
                                    statusImage = imgSensor9;
                                    break;
                                case 9:
                                    statusField = stSensor10;
                                    statusImage = imgSensor10;
                                    break;
                                default:
                                    statusField = stSensor1;
                                    statusImage = imgSensor1;
                                    break;
                            }

                            // Synlig?
                            if (sensorStatus.IsActive(i) &&
                                statusField != null &&
                                statusImage != null)
                            {
                                // Sette synlig
                                if (statusField.Visibility != Visibility.Visible)
                                    statusField.Visibility = Visibility.Visible;

                                // Sjekke om status er endret
                                if (sensorStatus.StatusChanged(i))
                                {
                                    // Sjekke status
                                    switch (sensorStatus.GetStatus(i))
                                    {
                                        // Feil status
                                        case DataStatus.TIMEOUT_ERROR:
                                            statusField.Background = (Brush)this.FindResource("ColorAmber");
                                            statusImage.Source = new BitmapImage(new Uri("../Icons/outline_info_black_48dp.png", UriKind.Relative));
                                            break;

                                        // OK status
                                        case DataStatus.OK:
                                            statusField.ClearValue(StackPanel.BackgroundProperty);
                                            statusImage.Source = new BitmapImage(new Uri("../Icons/outline_check_circle_black_48dp.png", UriKind.Relative));
                                            break;
                                    }

                                    // Resette status endret variabelen
                                    sensorStatus.StatusChangedReset(i);
                                }
                            }
                            else
                            {
                                // Skjule
                                if (statusField?.Visibility != Visibility.Hidden)
                                    statusField.Visibility = Visibility.Hidden;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Denne kan ikke være her i produksjon. Må fjernes/endres til noe usynlig for kunden!
                    DialogHandler.Warning("Sensor Status", string.Format("UpdateUI exception:\n\n{0}", ex.Message));
                }
            }
        }
    }
}

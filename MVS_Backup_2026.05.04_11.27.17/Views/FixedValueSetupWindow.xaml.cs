using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for FixedValueSetupWindow.xaml
    /// </summary>
    public partial class FixedValueSetupWindow : RadWindow
    {
        // Valgt sensor data
        private SensorData sensorData;

        private DispatcherTimer fixedValueTimer = new DispatcherTimer();

        // Lister for visning
        private RadObservableCollection<FileDataLine> fixedValueDataOutput = new RadObservableCollection<FileDataLine>();

        // View Model
        private FixedValueWindowVM fixedValueWindowVM;

        public FixedValueSetupWindow(SensorData sensorData, Config config)
        {
            InitializeComponent();

            fixedValueWindowVM = new FixedValueWindowVM(config);
            DataContext = fixedValueWindowVM;

            // Lagre valgt sensor data
            this.sensorData = sensorData;

            // Initialisere application settings
            InitializeApplicationSettings();
        }

        // Initialiserer
        private void InitializeApplicationSettings()
        {
            InitFileReader();

            InitBasicInformation();
            InitFileReaderControls();

            InitFixedData();
        }

        private void InitFileReader()
        {
            fixedValueTimer.Tick += runFileReaderTimer;

            void runFileReaderTimer(object sender, EventArgs e)
            {
                // Trinn 1: Hente fixed data
                //////////////////////////////////////////////////////////////////////////////
                string dataLine = sensorData.fixedValue.value;

                // Vise data linje på skjerm
                DisplayDataLines(dataLine);
            }
        }

        private void InitBasicInformation()
        {
            // Basic Information
            lbSensorID.Content = sensorData.id.ToString();
            lbSensorName.Content = sensorData.name;
            lbSensorType.Content = sensorData.type.ToString();
        }

        private void InitFileReaderControls()
        {
            // Status Felt
            ///////////////////////////////////////////////////////////
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += timer_Tick;
            timer.Start();

            void timer_Tick(object sender, EventArgs e)
            {
                if (fixedValueTimer.IsEnabled)
                {
                    bFileReaderStatus.Background = (Brush)this.FindResource("ColorGreen");
                }
                else
                {
                    bFileReaderStatus.Background = (Brush)this.FindResource("ColorRed");
                }
            }
        }

        private void InitFixedData()
        {
            // Binding for data listviews
            lvFixedDataOutput.ItemsSource = fixedValueDataOutput;

            tbFixedValueFrequency.Text = sensorData.fixedValue.frequency.ToString();
            tbFixedValueValue.Text = sensorData.fixedValue.value;
        }

        private void FixedValueSender_Start()
        {
            // Starter reader dispatcher
            FixedValueTimer_Start();

            // Status
            statusBar.Text = "Output started...";

            // Stenge tilgang til settings
            btnFixedValueOutputStart.IsEnabled = false;
            btnFixedValueOutputStop.IsEnabled = true;
        }

        private void FixedValueSender_Stop()
        {
            // Stopper reader dispatcher
            FixedValueTimer_Stop();

            // Status
            statusBar.Text = "Output stopped.";

            // Åpne tilgang til settings
            btnFixedValueOutputStart.IsEnabled = true;
            btnFixedValueOutputStop.IsEnabled = false;
        }

        private void FixedValueTimer_Start()
        {
            // Gjør dette trikset med Interval her for å få dispatchertimer til å kjøre med en gang.
            // Ellers venter den til intervallet er gått før den kjører første gang.
            fixedValueTimer.Interval = TimeSpan.FromMilliseconds(0);
            fixedValueTimer.Start();
            fixedValueTimer.Interval = TimeSpan.FromMilliseconds(sensorData.fixedValue.frequency);
        }

        private void FixedValueTimer_Stop()
        {
            fixedValueTimer.Stop();
        }

        private void DisplayDataLines(string dataString)
        {
            // Legg ut data i raw data listview
            fixedValueDataOutput.Add(new FileDataLine() { data = dataString });

            // Begrense data på skjerm
            while (fixedValueDataOutput.Count > fixedValueWindowVM.totalDataLines)
                fixedValueDataOutput.RemoveAt(0);

            // Status
            statusBar.Text = "Outputing fixed value data...";
        }

        private void btnFixedValueOutputStart_Click(object sender, RoutedEventArgs e)
        {
            FixedValueSender_Start();
        }

        private void btnFixedValueOutputStop_Click(object sender, RoutedEventArgs e)
        {
            FixedValueSender_Stop();
        }

        private void chkClearAllData_Click(object sender, RoutedEventArgs e)
        {
            // Sletter lister
            fixedValueDataOutput.Clear();
        }

        private void tbTotalDataLines_LostFocus(object sender, RoutedEventArgs e)
        {
            tbTotalDataLines_Update(sender);
        }

        private void tbTotalDataLines_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbTotalDataLines_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbTotalDataLines_Update(object sender)
        {
            fixedValueWindowVM.totalDataLinesString = (sender as TextBox).Text;
        }

        private void tbFixedValueFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedValueFrequency_Update(sender);
        }

        private void tbFixedValueFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedValueFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedValueFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    tbFixedValueFrequency.Text,
                    Constants.FixedValueFreqMin,
                    Constants.FixedValueFreqMax,
                    Constants.FixedValueFreqDefault,
                    out double validatedInput);

            sensorData.fixedValue.frequency = validatedInput;

            // Sette tilbake (evt korrigert) verdi til skjerm
            tbFixedValueFrequency.Text = validatedInput.ToString();
        }

        private void tbFixedValueValue_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedValueValue_Update(sender);
        }

        private void tbFixedValueValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedValueValue_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedValueValue_Update(object sender)
        {
            try
            {
                double value;

                if (string.IsNullOrEmpty(tbFixedValueValue.Text))
                    value = 0;
                else
                    value = Convert.ToDouble(tbFixedValueValue.Text);

                // Lagre ny setting
                sensorData.fixedValue.value = value.ToString();

                // Sette tilbake (evt korrigert) verdi til skjerm
                tbFixedValueValue.Text = value.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input must be numeric.\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void RadWindow_Closed(object sender, WindowClosedEventArgs e)
        {
            FixedValueSender_Stop();
        }
    }
}

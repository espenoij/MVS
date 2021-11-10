using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for HelideckReport_CAP.xaml
    /// </summary>
    public partial class HelideckReport_CAP : UserControl
    {
        private HelideckReportVM helideckReportVM;
        private Config config;

        public HelideckReport_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckReportVM helideckReportVM, Config config)
        {
            this.helideckReportVM = helideckReportVM;
            this.config = config;

            DataContext = helideckReportVM;

            helideckReportVM.EmailStatus = EmailStatus.PREVIEW;

            // Helicopter Operator
            HelicopterOperatorConfigCollection helicopterOperatorConfigCollection = config.GetHelicopterOperatorDataList();
        }

        private void tbNameOfHLO_LostFocus(object sender, RoutedEventArgs e)
        {
            tbNameOfHLO_Update(sender);
        }

        private void tbNameOfHLO_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbNameOfHLO_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbNameOfHLO_Update(object sender)
        {
            helideckReportVM.nameOfHLO = (sender as TextBox).Text;
        }

        private void tbEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            tbEmail_Update(sender);
        }

        private void tbEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbEmail_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbEmail_Update(object sender)
        {
            if (DataValidation.IsValidEmailAddress((sender as TextBox).Text) ||
                (sender as TextBox).Text == string.Empty)
            {
                helideckReportVM.emailFrom = (sender as TextBox).Text;
            }
            else
            {
                DialogHandler.Warning("Input Error", "Invalid email address.");
                (sender as TextBox).Text = helideckReportVM.emailFrom;
            }
        }

        private void tbTelephone_LostFocus(object sender, RoutedEventArgs e)
        {
            tbTelephone_Update(sender);
        }

        private void tbTelephone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbTelephone_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbTelephone_Update(object sender)
        {
            helideckReportVM.telephone = (sender as TextBox).Text;
        }

        private void tbAnyUnserviceableSensorsComments_LostFocus(object sender, RoutedEventArgs e)
        {
            tbAnyUnserviceableSensorsComments_Update(sender);
        }

        private void tbAnyUnserviceableSensorsComments_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbAnyUnserviceableSensorsComments_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbAnyUnserviceableSensorsComments_Update(object sender)
        {
            helideckReportVM.anyUnserviceableSensorsComments = (sender as TextBox).Text;
        }

        private void tbFlightNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFlightNumber_Update(sender);
        }

        private void tbFlightNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFlightNumber_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFlightNumber_Update(object sender)
        {
            helideckReportVM.flightNumber = (sender as TextBox).Text;
        }

        private void tbFuelQuantity_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFuelQuantity_Update(sender);
        }

        private void tbFuelQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFuelQuantity_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFuelQuantity_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HelicopterLoadMin,
                Constants.HelicopterLoadMax,
                Constants.HelicopterLoadDefault,
                out double validatedInput);

            helideckReportVM.fuelQuantity = (int)validatedInput;
            tbFuelQuantity.Text = validatedInput.ToString();
        }

        private void tbRouting1_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRouting1_Update(sender);
        }

        private void tbRouting1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRouting1_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRouting1_Update(object sender)
        {
            helideckReportVM.routing1 = (sender as TextBox).Text;
        }

        private void tbRouting2_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRouting2_Update(sender);
        }

        private void tbRouting2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRouting2_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRouting2_Update(object sender)
        {
            helideckReportVM.routing2 = (sender as TextBox).Text;
        }

        private void tbRouting3_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRouting3_Update(sender);
        }

        private void tbRouting3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRouting3_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRouting3_Update(object sender)
        {
            helideckReportVM.routing3 = (sender as TextBox).Text;
        }

        private void tbRouting4_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRouting4_Update(sender);
        }

        private void tbRouting4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRouting4_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRouting4_Update(object sender)
        {
            helideckReportVM.routing4 = (sender as TextBox).Text;
        }

        private void tbLogInfoRemarks_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLogInfoRemarks_Update(sender);
        }

        private void tbLogInfoRemarks_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLogInfoRemarks_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLogInfoRemarks_Update(object sender)
        {
            helideckReportVM.logInfoRemarks = (sender as TextBox).Text;
        }

        private void tbSendTo_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSendTo_Update(sender);
        }

        private void tbSendTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSendTo_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSendTo_Update(object sender)
        {
            if (DataValidation.IsValidEmailAddress((sender as TextBox).Text) ||
                (sender as TextBox).Text == string.Empty)
            {
                helideckReportVM.emailTo = (sender as TextBox).Text;
            }
            else
            {
                DialogHandler.Warning("Input Error", "Invalid email address.");
                (sender as TextBox).Text = helideckReportVM.emailTo;
            }
        }

        private void tbCCTo_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCCTo_Update(sender);
        }

        private void tbCCTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCCTo_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbCCTo_Update(object sender)
        {
            if (DataValidation.IsValidEmailAddress((sender as TextBox).Text) ||
                (sender as TextBox).Text == string.Empty)
            {
                helideckReportVM.emailCC = (sender as TextBox).Text;
            }
            else
            {
                DialogHandler.Warning("Input Error", "Invalid email address.");
                (sender as TextBox).Text = helideckReportVM.emailCC;
            }
        }

        private void btnClearHelideckReport_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.nameOfHLO = string.Empty;
        }

        private void btnClearObservations_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.lightningPresent = false;
            helideckReportVM.coldFlaring = false;
        }

        private void btnClearServices_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.rescueRecoveryAvailable = false;
            helideckReportVM.helifuelAvailable = false;
            helideckReportVM.fuelQuantity = 0;
            helideckReportVM.ndbServiceable = false;
        }

        private void btnClearSensorRemarks_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.anyUnserviceableSensors = false;
            helideckReportVM.anyUnserviceableSensorsComments = string.Empty;
        }

        private void btnClearRemarks_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.otherWeatherInfo = string.Empty;
        }

        private void btnClearEmail_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.emailFrom = string.Empty;
            helideckReportVM.emailTo = string.Empty;
            helideckReportVM.emailCC = string.Empty;
        }

        private void btnSendReport_Click(object sender, RoutedEventArgs e)
        {
            string reportFile = string.Format("{0}_{1}.pdf", Constants.HelideckReportFilename, DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss"));

            // Opprette PDF dokument med Helideck Report
            PDFHandler pdf = new PDFHandler();
            pdf.SaveToFile(helideckReportPreviewContainer, reportFile);

            // Åpne email sending dialog
            DialogEmail dialogEmail = new DialogEmail(helideckReportVM, reportFile);
            dialogEmail.Owner = App.Current.MainWindow;
            dialogEmail.ShowDialog();
        }

        private void btnSavePDF_Click(object sender, RoutedEventArgs e)
        {
            PDFHandler pdf = new PDFHandler();
            pdf.SaveToFileWithDialog(helideckReportPreviewContainer);
        }

        private void btnOpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            string reportFolder = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder);
            Process.Start(reportFolder);
        }

        private void tbSignificantWaveHeight_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void tbSignificantWaveHeight_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void tbRemarks_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRemarks_Update(sender);
        }

        private void tbRemarks_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRemarks_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRemarks_Update(object sender)
        {
            helideckReportVM.otherWeatherInfo = (sender as TextBox).Text;
        }
    }
}

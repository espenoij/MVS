using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckReport_NOROG.xaml
    /// </summary>
    public partial class HelideckReport_NOROG : UserControl
    {
        private HelideckReportVM helideckReportVM;
        private Config config;
        private AdminSettingsVM adminSettingsVM;

        private List<HelicopterOperator> helicopterOperatorList = new List<HelicopterOperator>();

        public HelideckReport_NOROG()
        {
            InitializeComponent();
        }

        public void Init(HelideckReportVM helideckReportVM, Config config, AdminSettingsVM adminSettingsVM)
        {
            this.helideckReportVM = helideckReportVM;
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;

            DataContext = helideckReportVM;

            helideckReportVM.EmailStatus = EmailStatus.PREVIEW;

            // Helicopter Operator
            HelicopterOperatorConfigCollection helicopterOperatorConfigCollection = config.GetHelicopterOperatorDataList();

            if (helicopterOperatorConfigCollection != null)
            {
                foreach (HelicopterOperatorConfig helicopterOperator in helicopterOperatorConfigCollection)
                {
                    helicopterOperatorList.Add(new HelicopterOperator()
                    {
                        id = helicopterOperator.id,
                        name = helicopterOperator.name,
                        email = helicopterOperator.email
                    });

                    cboHelicopterOperator.Items.Add(helicopterOperator.name);
                }
            }

            string helicopterOperatorEmail = config.Read(ConfigKey.EmailTo, ConfigType.Data);
            cboHelicopterOperator.Text = config.Read(ConfigKey.HelicopterOperator, ConfigType.Data);
            helideckReportVM.emailTo = helicopterOperatorEmail;
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

        private void btnClearRemarks_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.otherWeatherInfo = string.Empty;
        }

        private void tbOtherWeatherInfo_LostFocus(object sender, RoutedEventArgs e)
        {
            tbOtherWeatherInfo_Update(sender);
        }

        private void tbOtherWeatherInfo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbOtherWeatherInfo_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbOtherWeatherInfo_Update(object sender)
        {
            helideckReportVM.otherWeatherInfo = (sender as TextBox).Text;
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

        private void tbReturnLoad_LostFocus(object sender, RoutedEventArgs e)
        {
            tbReturnLoad_Update(sender);
        }

        private void tbReturnLoad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbReturnLoad_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbReturnLoad_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.ReturnLoadMin,
                Constants.ReturnLoadMax,
                Constants.ReturnLoadDefault,
                out double validatedInput);

            helideckReportVM.returnLoad = (int)validatedInput;
            tbReturnLoad.Text = validatedInput.ToString();
        }

        private void tbTotalLoad_LostFocus(object sender, RoutedEventArgs e)
        {
            tbTotalLoad_Update(sender);
        }

        private void tbTotalLoad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbTotalLoad_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbTotalLoad_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HelicopterLoadMin,
                Constants.HelicopterLoadMax,
                Constants.HelicopterLoadDefault,
                out double validatedInput);

            helideckReportVM.totalLoad = (int)validatedInput;
            tbTotalLoad.Text = validatedInput.ToString();
        }

        private void tbLuggage_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLuggage_Update(sender);
        }

        private void tbLuggage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLuggage_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLuggage_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HelicopterLoadMin,
                Constants.HelicopterLoadMax,
                Constants.HelicopterLoadDefault,
                out double validatedInput);

            helideckReportVM.luggage = (int)validatedInput;
            tbLuggage.Text = validatedInput.ToString();
        }

        private void tbCargo_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCargo_Update(sender);
        }

        private void tbCargo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCargo_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbCargo_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HelicopterLoadMin,
                Constants.HelicopterLoadMax,
                Constants.HelicopterLoadDefault,
                out double validatedInput);

            helideckReportVM.cargo = (int)validatedInput;
            tbCargo.Text = validatedInput.ToString();
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
            helideckReportVM.emailFrom = string.Empty;
            helideckReportVM.telephone = string.Empty;
            helideckReportVM.dynamicPositioning = false;
            helideckReportVM.accurateMonitoringEquipment = false;
        }

        private void btnClearOtherWeatherInfo_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.seaSprayObserved = false;
            helideckReportVM.otherWeatherInfo = string.Empty;
        }

        private void btnClearLogInfo_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.flightNumber = string.Empty;
            helideckReportVM.returnLoad = 0;
            helideckReportVM.totalLoad = 0;
            helideckReportVM.luggage = 0;
            helideckReportVM.cargo = 0;
            helideckReportVM.helifuelAvailable = false;
            helideckReportVM.fuelQuantity = 0;
            helideckReportVM.routing1 = string.Empty;
            helideckReportVM.routing2 = string.Empty;
            helideckReportVM.routing3 = string.Empty;
            helideckReportVM.routing4 = string.Empty;
            helideckReportVM.logInfoRemarks = string.Empty;
        }

        private void btnClearEmail_Click(object sender, RoutedEventArgs e)
        {
            helideckReportVM.emailTo = string.Empty;
            helideckReportVM.emailCC = string.Empty;
            helideckReportVM.sendHMSScreenCapture = false;
        }

        private void btnSendReport_Click(object sender, RoutedEventArgs e)
        {
            string reportFile = string.Format("{0}_{1}.pdf", Constants.HelideckReportFilename, DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss"));

            // Opprette PDF dokument med Helideck Report
            PDFHandler pdf = new PDFHandler();
            pdf.SaveToFile(helideckReportPreviewContainer, reportFile);

            // Åpne email sending dialog
            DialogEmail dialogEmail = new DialogEmail(helideckReportVM, reportFile, adminSettingsVM);
            dialogEmail.Owner = App.Current.MainWindow;
            dialogEmail.ShowDialog();
        }

        private void btnSavePDF_Click(object sender, RoutedEventArgs e)
        {
            PDFHandler pdf = new PDFHandler();
            pdf.SaveToFileWithDialog(helideckReportPreviewContainer, Constants.HelideckReportFilename);
        }

        private void btnOpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            string reportFolder = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder);
            Process.Start(reportFolder);
        }

        private void cboHelicopterOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Finne match i helikopter operatør data listen
            var helicopterOperator = helicopterOperatorList.Where(x => x?.name == (sender as RadComboBox).Text);

            // Fant match?
            if (helicopterOperator.Count() > 0)
            {
                helideckReportVM.emailTo = helicopterOperator.First().email;

                config.Write(ConfigKey.HelicopterOperator, helicopterOperator.First().name, ConfigType.Data);

                if (helicopterOperator.First().id == 0)
                    tbSendTo.IsEnabled = true;
                else
                    tbSendTo.IsEnabled = false;
            }
        }
    }
}

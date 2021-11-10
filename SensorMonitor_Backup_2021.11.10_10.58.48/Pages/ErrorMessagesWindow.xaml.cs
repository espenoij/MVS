using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace SensorMonitor
{
    /// <summary>
    /// Interaction logic for ErrorMessagesWindow.xaml
    /// </summary>
    public partial class ErrorMessagesWindow : RadWindow
    {
        // Configuration
        private Config config;

        // Error Message Handler
        private ErrorHandler errorHandler;

        // Error Message List
        private RadObservableCollectionEx<ErrorMessage> errorMessageList = new RadObservableCollectionEx<ErrorMessage>();

        public ErrorMessagesWindow(Config config, ErrorHandler errorHandler)
        {
            this.config = config;
            this.errorHandler = errorHandler;

            InitializeComponent();

            InitUI();
        }

        private void InitUI()
        {
            // View
            cboErrorMessageView.Items.Add("Live View");
            cboErrorMessageView.Items.Add("From Database");

            cboErrorMessageView.SelectedIndex = int.Parse(config.Read(ConfigKey.ErrorMessagesView, ConfigSection.ErrorMessages));
            cboErrorMessageView.Text = cboErrorMessageView.SelectedItem.ToString();

            // Type
            foreach (var type in Enum.GetValues(typeof(ErrorMessageType)))
                cboErrorMessageType.Items.Add(type.ToString());

            cboErrorMessageType.SelectedIndex = int.Parse(config.Read(ConfigKey.ErrorMessagesType, ConfigSection.ErrorMessages));
            cboErrorMessageType.Text = cboErrorMessageType.SelectedItem.ToString();

            // Selection
            cboErrorMessageSelection.Items.Add("Last 100");
            cboErrorMessageSelection.Items.Add("Last 500");
            cboErrorMessageSelection.Items.Add("Last 1000");

            cboErrorMessageSelection.SelectedIndex = int.Parse(config.Read(ConfigKey.ErrorMessagesSelection, ConfigSection.ErrorMessages));
            cboErrorMessageSelection.Text = cboErrorMessageSelection.SelectedItem.ToString();

            cboErrorMessageSelection.IsEnabled = false;

            // Liste med feil meldinger
            // ...dersom live view er valgt
            if (cboErrorMessageView.SelectedIndex == 0)
            {
                lvErrorMessagesData.ItemsSource = errorHandler.GetErrorMessageList();
            }
            else
            {
                lvErrorMessagesData.ItemsSource = errorMessageList;
            }
        }

        private void cboErrorMessageView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender as RadComboBox != null)
            {
                switch ((sender as RadComboBox).SelectedIndex)
                {
                    // Live View
                    case 0:
                        cboErrorMessageSelection.IsEnabled = false;
                        btnReadDB.IsEnabled = false;
                        lvErrorMessagesData.ItemsSource = errorHandler.GetErrorMessageList();
                        break;

                    // Stored Messages
                    case 1:
                        cboErrorMessageSelection.IsEnabled = true;
                        btnReadDB.IsEnabled = true;
                        lvErrorMessagesData.ItemsSource = errorMessageList;
                        break;
                }

                // Lagre ny setting til config fil
                config.Write(ConfigKey.ErrorMessagesView, (sender as RadComboBox).SelectedIndex.ToString(), ConfigSection.ErrorMessages);
            }
        }

        private void cboErrorMessageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender as RadComboBox != null)
            {
                // Lagre ny setting til config fil
                config.Write(ConfigKey.ErrorMessagesType, (sender as RadComboBox).SelectedIndex.ToString(), ConfigSection.ErrorMessages);
            }
        }

        private void cboErrorMessageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender as RadComboBox != null)
            {
                // Lagre ny setting til config fil
                config.Write(ConfigKey.ErrorMessagesSelection, (sender as RadComboBox).SelectedIndex.ToString(), ConfigSection.ErrorMessages);
            }
        }

        private void btnReadDB_Click(object sender, RoutedEventArgs e)
        {
            // Antall feil data som skal leses
            int number = 0;
            switch (cboErrorMessageSelection.SelectedIndex)
            {
                case 0:
                    number = 100;
                    break;
                case 1:
                    number = 500;
                    break;
                case 2:
                    number = 1000;
                    break;
            }

            // Type Feil
            ErrorMessageType type = (ErrorMessageType)Enum.Parse(typeof(ErrorMessageType), cboErrorMessageType.Text);

            // Lese feilmeldinggsdata
            List<ErrorMessage> newErrorMessages = errorHandler.ReadLast(type, number);
            if (newErrorMessages != null)
            {
                // Sortere listen
                List<ErrorMessage> sortedErrorMessages = newErrorMessages.OrderBy(x => x.timestamp).ToList();

                // Slette display feil listen
                errorMessageList.Clear();

                // Overføre feil data til display listen/listview
                foreach (var errorMessage in sortedErrorMessages)
                    errorMessageList.Add(errorMessage);
            }
        }

        private void btnDeleteAllErrorMessages_Click(object sender, RoutedEventArgs e)
        {
            RadWindow.Confirm("Delete all error messages data from database?", OnClosed);

            void OnClosed(object sendero, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    // Slette data
                    errorHandler.DeleteErrorMessageData();

                    // Slette display feil listen
                    errorMessageList.Clear();
                }
            }
        }

        private void btnClearDisplay_Click(object sender, RoutedEventArgs e)
        {
            errorHandler.ClearAllMessages();
            errorMessageList.Clear();
        }
    }
}

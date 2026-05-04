using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for ErrorMessagesWindow.xaml
    /// </summary>
    public partial class ErrorMessagesPage : UserControl
    {
        // Configuration
        private Config config;

        // Error Message Handler
        private ErrorHandler errorHandler;

        // Error Message List
        private RadObservableCollection<ErrorMessage> errorMessageDisplayList = new RadObservableCollection<ErrorMessage>();
        private DispatcherTimer errorMessageDisplayListUpdater;

        public ErrorMessagesPage()
        {
            InitializeComponent();
        }

        public void Init(Config config, ErrorHandler errorHandler)
        {
            this.config = config;
            this.errorHandler = errorHandler;

            // Data liste
            lvErrorMessagesData.ItemsSource = errorMessageDisplayList;

            // View
            cboErrorMessageView.Items.Add("Live View");
            cboErrorMessageView.Items.Add("From Database");

            cboErrorMessageView.SelectedIndex = (int)config.ReadWithDefault(ConfigKey.ErrorMessagesView, ConfigSection.ErrorMessages, 0);
            cboErrorMessageView.Text = cboErrorMessageView.SelectedItem.ToString();

            // Type
            foreach (var type in Enum.GetValues(typeof(ErrorMessageType)))
                cboErrorMessageType.Items.Add(type.ToString());

            cboErrorMessageType.SelectedIndex = (int)config.ReadWithDefault(ConfigKey.ErrorMessagesType, ConfigSection.ErrorMessages, 0);
            cboErrorMessageType.Text = cboErrorMessageType.SelectedItem.ToString();

            // Selection
            cboErrorMessageSelection.Items.Add("Last 100");
            cboErrorMessageSelection.Items.Add("Last 200");
            cboErrorMessageSelection.Items.Add("Last 500");

            cboErrorMessageSelection.SelectedIndex = (int)config.ReadWithDefault(ConfigKey.ErrorMessagesSelection, ConfigSection.ErrorMessages, 0);
            cboErrorMessageSelection.Text = cboErrorMessageSelection.SelectedItem.ToString();

            cboErrorMessageSelection.IsEnabled = false;

            // Dispatcher for å oppdatere meldingene i error message list
            errorMessageDisplayListUpdater = new DispatcherTimer();
            errorMessageDisplayListUpdater.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            errorMessageDisplayListUpdater.Tick += UpdateErrorMessageDisplayList;

            void UpdateErrorMessageDisplayList(object sender, EventArgs e)
            {
                TransferNewMessages(errorMessageDisplayList, errorHandler);
            }

            // Starte overføring av error messages til display listen dersom live view er valgt
            if (cboErrorMessageView.SelectedIndex == 0)
                errorMessageDisplayListUpdater.Start();
        }

        private void TransferNewMessages(RadObservableCollection<ErrorMessage> displayList, ErrorHandler errorHandler)
        {
            lock (errorHandler.GetErrorMessageListLock())
            {
                // Gå gjennom meldingslisten
                foreach (var item in errorHandler.GetErrorMessageList().ToList())
                {
                    if (item != null)
                    {
                        // Finne ut om melding ligger inne fra før
                        // Dersom den ikke ligger inne -> legg den inn, ellers gjør vi ingenting
                        if (displayList.Where(x => x.timestamp == item.timestamp).Count() == 0)
                        {
                            // Legge inn my melding
                            displayList.Add(new ErrorMessage(item));

                            // Slette første når listen blir for lang
                            while (displayList.Count > Constants.MaxErrorMessages)
                                displayList.RemoveAt(0);
                        }
                    }
                }

                // Slette alle meldinger når de er overført til skjerm.
                errorHandler.ClearAllMessages();
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
                        errorMessageDisplayListUpdater?.Start();
                        break;

                    // Stored Messages
                    case 1:
                        cboErrorMessageSelection.IsEnabled = true;
                        btnReadDB.IsEnabled = true;
                        errorMessageDisplayListUpdater?.Stop();
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
                errorHandler.SetSelectedType((ErrorMessageType)Enum.Parse(typeof(ErrorMessageType), (sender as RadComboBox).SelectedIndex.ToString()));

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
                    number = 200;
                    break;
                case 2:
                    number = 500;
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
                errorMessageDisplayList.Clear();

                // Overføre feil data til display listen/listview
                foreach (var errorMessage in sortedErrorMessages)
                    errorMessageDisplayList.Add(errorMessage);
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
                    errorMessageDisplayList.Clear();
                }
            }
        }

        private void btnClearDisplay_Click(object sender, RoutedEventArgs e)
        {
            errorHandler.ClearAllMessages();
            errorMessageDisplayList.Clear();
        }
    }
}

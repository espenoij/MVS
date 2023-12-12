using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;
using static MVS.MainWindow;

namespace MVS
{
    /// <summary>
    /// Interaction logic for VerificationSessions.xaml
    /// </summary>
    public partial class Recordings : UserControl
    {
        // Database handler
        private MVSDatabase mvsDatabase;

        // Motion Data Set List
        private RadObservableCollection<RecordingSession> motionVerificationSessionList = new RadObservableCollection<RecordingSession>();

        private MainWindowVM mainWindowVM;
        private Config config;
        private ImportVM importVM;

        private MainWindow.UpdateUIButtonsCallback updateUIButtonsCallback;

        public Recordings()
        {
            InitializeComponent();

            importVM = new ImportVM();
        }

        public void Init(MainWindowVM mainWindowVM, Config config, MVSDatabase mvsDatabase, UpdateUIButtonsCallback updateUIButtonsCallback)
        {
            // Database
            this.mvsDatabase = mvsDatabase;

            // Config
            this.config = config;

            // VM
            this.mainWindowVM = mainWindowVM;

            this.updateUIButtonsCallback = updateUIButtonsCallback;

            InitUI();
        }

        public void InitUI()
        {
            importVM.Init(config);

            // Liste med sensor verdier
            gvVerificationSessions.ItemsSource = motionVerificationSessionList;

            // Fylle input setup combobox
            cboInputMRUs.Items.Add(InputMRUType.ReferenceMRU.GetDescription());
            cboInputMRUs.Items.Add(InputMRUType.ReferenceMRU_TestMRU.GetDescription());

            // Sette default verdi
            cboInputMRUs.Text = cboInputMRUs.Items[0].ToString();

            // Laste sensor data
            LoadVerificationSessions();

            //// Sette første set som selected
            //if (gvVerificationSessions.Items.Count > 0)
            //{
            //    gvVerificationSessions.SelectedItem = gvVerificationSessions.Items[0];
            //    mainWindowVM.SelectedSession = (MotionDataSet)gvVerificationSessions.Items[0];
            //    LoadSelectedItemsDetails();
            //}

            UpdateUIStates(false);
        }

        public void UpdateUIStates(bool serverStarted)
        {
            // Vi kan ikke utføre data set endringer dersom server kjører.
            if (serverStarted)
            {
                btnNew.IsEnabled = false;
                btnDelete.IsEnabled = false;
                btnImport.IsEnabled = false;

                gvVerificationSessions.IsEnabled = false;
            }
            else
            {
                btnNew.IsEnabled = true;

                // Er et data sett valgt?
                if (mainWindowVM.SelectedSession == null)
                {
                    btnDelete.IsEnabled = false;
                    btnImport.IsEnabled = false;
                }
                else
                {
                    btnDelete.IsEnabled = true;

                    if (mainWindowVM.SelectedSession.StartTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                        btnImport.IsEnabled = false;
                    else
                        btnImport.IsEnabled = true;
                }

                gvVerificationSessions.IsEnabled = true;
            }

            if (mainWindowVM.SelectedSession != null)
            {
                tbDataSetName.IsEnabled = true;
                tbDataSetDescription.IsEnabled = true;
                cboInputMRUs.IsEnabled = true;
            }
            else
            {
                tbDataSetName.IsEnabled = false;
                tbDataSetDescription.IsEnabled = false;
                cboInputMRUs.IsEnabled = false;
            }
        }

        private void LoadVerificationSessions()
        {
            // Hente liste med data set fra database
            List<RecordingSession> sessionList = mvsDatabase.GetAllSessions();

            if (sessionList != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (var item in sessionList)
                {
                    // Laste timestamps på data set
                    mvsDatabase.LoadTimestamps(item);

                    motionVerificationSessionList.Insert(0, item);
                }
            }
        }

        public void Start()
        {
            UpdateUIStates(true);
        }

        public void Stop()
        {
            // Laste timestamps på data set
            mvsDatabase.LoadTimestamps(mainWindowVM.SelectedSession);

            UpdateUIStates(false);

            // Legge data i UI
            LoadSelectedItemsDetails();

            // Oppdatere databasen
            mvsDatabase.Update(mainWindowVM.SelectedSession);
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            // Legge inne nytt tomt motion data set objekt
            RecordingSession newSession = new RecordingSession();

            // Store motion data set in database
            newSession.Id = mvsDatabase.Insert(newSession);

            // Legge i listen
            motionVerificationSessionList.Insert(0, newSession);

            // Sette ny item som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
            int index = gvVerificationSessions.Items.IndexOf(newSession);
            gvVerificationSessions.SelectedItem = gvVerificationSessions.Items[index];

            updateUIButtonsCallback();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindowVM.SelectedSession != null)
            {
                RadWindow.Confirm("Delete the selected motion data set and all associated data?", OnClosed);

                void OnClosed(object sendero, WindowClosedEventArgs ea)
                {
                    if ((bool)ea.DialogResult == true)
                    {
                        // Slette fra database
                        mvsDatabase.Remove(mainWindowVM.SelectedSession);

                        // Slette fra listen
                        motionVerificationSessionList.Remove(mainWindowVM.SelectedSession);

                        // Sette item 0 som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
                        gvVerificationSessions.SelectedItem = gvVerificationSessions.Items[0];
                    }
                }
            }

            updateUIButtonsCallback();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            // Åpne HMS data import dialog vindu
            DialogImport importDlg = new DialogImport();
            importDlg.Owner = App.Current.MainWindow;
            importDlg.Init(importVM, config, mainWindowVM.SelectedSession, mvsDatabase, mainWindowVM);
            importDlg.ShowDialog();

            // Laster items data på nytt ettersom de kan ha blitt endret under import over
            LoadSelectedItemsDetails();
        }

        private void gvVerificationSessions_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            mainWindowVM.SelectedSession = (sender as RadGridView).SelectedItem as RecordingSession;

            LoadSelectedItemsDetails();
            UpdateUIStates(false);

            updateUIButtonsCallback();
        }

        private void LoadSelectedItemsDetails()
        {
            if (mainWindowVM.SelectedSession != null)
            {
                lbDataSetID.Content = mainWindowVM.SelectedSession.Id.ToString();
                tbDataSetName.Text = mainWindowVM.SelectedSession.Name;
                tbDataSetDescription.Text = mainWindowVM.SelectedSession.Description;
                cboInputMRUs.Text = mainWindowVM.SelectedSession.InputMRUs.GetDescription();
                lbDataSetDate.Content = mainWindowVM.SelectedSession.DateString;
                lbDataSetStartTime.Content = mainWindowVM.SelectedSession.StartTimeString2;
                lbDataSetEndTime.Content = mainWindowVM.SelectedSession.EndTimeString2;
                lbDataSetDuration.Content = mainWindowVM.SelectedSession.DurationString;
            }
            else
            {
                lbDataSetID.Content = string.Empty;
                tbDataSetName.Text = string.Empty;
                tbDataSetDescription.Text = string.Empty;
                cboInputMRUs.Text = InputMRUType.None.GetDescription();
                lbDataSetDate.Content = string.Empty;
                lbDataSetStartTime.Content = string.Empty;
                lbDataSetEndTime.Content = string.Empty;
                lbDataSetDuration.Content = string.Empty;
            }
        }

        private void tbDataSetName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbDataSetName_Update(sender);
        }

        private void tbDataSetName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbDataSetName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbDataSetName_Update(object sender)
        {
            if (mainWindowVM.SelectedSession != null)
            {
                mainWindowVM.SelectedSession.Name = (sender as TextBox).Text;

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedSession);

                mainWindowVM.SelectedSessionNameUpdated();
            }
        }

        private void tbDataSetDescription_LostFocus(object sender, RoutedEventArgs e)
        {
            tbDataSetDescription_Update(sender);
        }

        private void tbDataSetDescription_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbDataSetDescription_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbDataSetDescription_Update(object sender)
        {
            if (mainWindowVM.SelectedSession != null)
            {
                mainWindowVM.SelectedSession.Description = (sender as TextBox).Text;

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedSession);
            }
        }

        private void cboInputMRUs_DropDownClosed(object sender, EventArgs e)
        {
            if (mainWindowVM.SelectedSession != null)
            {
                // Ny valgt MRU type
                mainWindowVM.SelectedSession.InputMRUs = EnumExtension.GetEnumValueFromDescription<InputMRUType>((sender as RadComboBox).Text);

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedSession);
            }
        }
    }
}

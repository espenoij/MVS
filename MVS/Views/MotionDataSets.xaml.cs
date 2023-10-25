using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for MotionDataSets.xaml
    /// </summary>
    public partial class MotionDataSets : UserControl
    {
        // Database handler
        private MVSDatabase mvsDatabase;

        // Motion Data Set List
        private RadObservableCollection<MotionDataSet> motionDataSetList = new RadObservableCollection<MotionDataSet>();
        private MotionDataSet motionDataSetSelected = new MotionDataSet();

        private MainWindow.StopServerCallback stopServerCallback;

        private MainWindowVM mainWindowVM;

        public MotionDataSets()
        {
            InitializeComponent();
        }

        public void Init(MainWindowVM mainWindowVM, MVSDatabase mvsDatabase, MainWindow.StopServerCallback stopServerCallback)
        {
            // Database
            this.mvsDatabase = mvsDatabase;

            // VM
            this.mainWindowVM = mainWindowVM;

            // Stop server callback
            this.stopServerCallback = stopServerCallback;

            InitUI();
        }

        public void InitUI()
        {
            // Liste med sensor verdier
            gvMotionDataSets.ItemsSource = motionDataSetList;

            // Laste sensor data
            LoadMotionDataSets();
        }

        public void ServerStartedCheck(bool serverStarted)
        {
            // Vi kan ikke utføre sensor input endringer dersom server kjører.
            if (serverStarted)
            {
                btnNew.IsEnabled = false;
                btnDelete.IsEnabled = false;
                btnImport.IsEnabled = false;
            }
            else
            {
                btnNew.IsEnabled = true;
                btnDelete.IsEnabled = true;
                btnImport.IsEnabled = true;
            }
        }

        private void LoadMotionDataSets()
        {
            // Hente liste med data fra database
            List<MotionDataSet> dataSets = mvsDatabase.GetAll();

            // Legge alle data inn i listview for visning på skjerm
            foreach (var item in dataSets)
            {
                motionDataSetList.Insert(0, item);
            }
        }

        public void Stop(OperationsMode operationsMode)
        {
            if (operationsMode == OperationsMode.Recording)
            {
                // Sette timestamps til data set
                mvsDatabase.SetTimestamps(motionDataSetSelected);

                mainWindowVM.SelectedMotionDataSet = motionDataSetSelected;

                // Legge data i UI
                lbDataSetStartTime.Content = motionDataSetSelected.StartTimeString;
                lbDataSetEndTime.Content = motionDataSetSelected.EndTimeString;

                // Oppdatere databasen
                mvsDatabase.Update(motionDataSetSelected);
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            stopServerCallback();
            ServerStartedCheck(false);
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            // Legge inne nytt tomt motion data set objekt
            MotionDataSet newDataSet = new MotionDataSet();

            // Sette data
            newDataSet.Name = "New Data Set";
            newDataSet.Description = "(description)";
            newDataSet.StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            newDataSet.EndTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            newDataSet.Status = MotionDataStatus.Open;

            // Store motion data set in database
            newDataSet.Id = mvsDatabase.Insert(newDataSet);

            // Legge i listen
            motionDataSetList.Insert(0,newDataSet);

            // Sette ny item som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
            int index = gvMotionDataSets.Items.IndexOf(newDataSet);
            gvMotionDataSets.SelectedItem = gvMotionDataSets.Items[index];
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            RadWindow.Confirm("Delete the selected motion data set and all associated data?", OnClosed);

            void OnClosed(object sendero, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    // Slette fra database
                    mvsDatabase.Remove(motionDataSetSelected);

                    // Slette fra listen
                    motionDataSetList.Remove(motionDataSetSelected);

                    // Sette item 0 som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
                    gvMotionDataSets.SelectedItem = gvMotionDataSets.Items[0];
                }
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void gvMotionDataSets_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            motionDataSetSelected = (sender as RadGridView).SelectedItem as MotionDataSet;
            mainWindowVM.SelectedMotionDataSet = motionDataSetSelected;

            if (motionDataSetSelected != null)
            {
                lbDataSetID.Content = motionDataSetSelected.Id.ToString();
                tbDataSetName.Text = motionDataSetSelected.Name;
                tbDataSetDescription.Text = motionDataSetSelected.Description;
                lbDataSetStartTime.Content = motionDataSetSelected.StartTimeString;
                lbDataSetEndTime.Content = motionDataSetSelected.EndTimeString;
                lbDataSetStatus.Content = motionDataSetSelected.StatusString;
            }
            else
            {
                lbDataSetID.Content = string.Empty;
                tbDataSetName.Text = string.Empty;
                tbDataSetDescription.Text = string.Empty;
                lbDataSetStartTime.Content = string.Empty;
                lbDataSetEndTime.Content = string.Empty;
                lbDataSetStatus.Content = string.Empty;
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
            if (motionDataSetSelected != null)
            {
                motionDataSetSelected.Name = (sender as TextBox).Text;

                // Oppdatere database
                mvsDatabase.Update(motionDataSetSelected);
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
            if (motionDataSetSelected != null)
            {
                motionDataSetSelected.Description = (sender as TextBox).Text;

                // Oppdatere database
                mvsDatabase.Update(motionDataSetSelected);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;
using static MVS.MainWindow;

namespace MVS
{
    /// <summary>
    /// Interaction logic for Projects.xaml
    /// </summary>
    public partial class Projects : UserControl
    {
        // Database handler
        private MVSDatabase mvsDatabase;

        // Motion Data Set List
        private RadObservableCollection<Project> mvsProjectList = new RadObservableCollection<Project>();

        private MainWindowVM mainWindowVM;
        private Config config;
        private ImportVM importVM;
        private ProjectVM projectVM;

        // Data View worker
        private readonly BackgroundWorker dataViewWorker = new BackgroundWorker();

        private MainWindow.UpdateUIButtonsCallback updateUIButtonsCallback;

        // Progress dialog
        DialogDataAnalysisProgress progressDlg = new DialogDataAnalysisProgress();

        public Projects()
        {
            InitializeComponent();

            importVM = new ImportVM();
        }

        public void Init(MainWindowVM mainWindowVM, ProjectVM projectVM, Config config, MVSDatabase mvsDatabase, UpdateUIButtonsCallback updateUIButtonsCallback)
        {
            // Database
            this.mvsDatabase = mvsDatabase;

            // Config
            this.config = config;

            // VM
            DataContext = projectVM;
            this.mainWindowVM = mainWindowVM;
            this.projectVM = projectVM;

            this.updateUIButtonsCallback = updateUIButtonsCallback;

            InitUI();
        }

        public void InitUI()
        {
            importVM.Init(config);

            // Liste med sensor verdier
            gvProjects.ItemsSource = mvsProjectList;

            // Fylle input setup combobox
            cboInputMRUs.Items.Add(InputMRUType.ReferenceMRU_TestMRU.GetDescription());
            cboInputMRUs.Items.Add(InputMRUType.ReferenceMRU.GetDescription());

            // Sette default verdi
            cboInputMRUs.Text = cboInputMRUs.Items[0].ToString();

            // Analysis init
            InitDataViewWorker();

            // Laste sensor data
            LoadProjects();

            // Progress dialog
            progressDlg.Owner = App.Current.MainWindow;

            //// Sette første set som selected
            //if (gvProjects.Items.Count > 0)
            //{
            //    gvProjects.SelectedItem = gvProjects.Items[0];
            //    mainWindowVM.SelectedSession = (MotionDataSet)gvProjects.Items[0];
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
                lbImport.IsEnabled = false;

                gvProjects.IsEnabled = false;
            }
            else
            {
                btnNew.IsEnabled = true;

                // Er et data sett valgt?
                if (mainWindowVM.SelectedProject == null)
                {
                    btnDelete.IsEnabled = false;
                    btnImport.IsEnabled = false;
                    lbImport.IsEnabled = false;
                }
                else
                {
                    btnDelete.IsEnabled = true;

                    if (mainWindowVM.SelectedProject.StartTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    {
                        btnImport.IsEnabled = false;
                        lbImport.IsEnabled = false;
                    }
                    else
                    {
                        btnImport.IsEnabled = true;
                        lbImport.IsEnabled = true;
                    }
                }

                gvProjects.IsEnabled = true;
            }

            if (mainWindowVM.SelectedProject != null)
            {
                tbDataSetName.IsEnabled = true;
                tbDataSetComments.IsEnabled = true;
                cboInputMRUs.IsEnabled = true;
            }
            else
            {
                tbDataSetName.IsEnabled = false;
                tbDataSetComments.IsEnabled = false;
                cboInputMRUs.IsEnabled = false;
            }
        }

        private void LoadProjects()
        {
            // Hente liste med data set fra database
            List<Project> sessionList = mvsDatabase.GetAllProjects();

            if (sessionList != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (var item in sessionList)
                {
                    // Laste timestamps på data set
                    mvsDatabase.LoadTimestamps(item);

                    mvsProjectList.Insert(0, item);
                }
            }
        }

        public void Start()
        {
            UpdateUIStates(true);
        }

        public void Stop(OperationsMode mode)
        {
            if (mode != OperationsMode.Test)
            {
                // Laste timestamps på data set
                mvsDatabase.LoadTimestamps(mainWindowVM.SelectedProject);

                // Oppdatere databasen
                mvsDatabase.Update(mainWindowVM.SelectedProject);
            }

            UpdateUIStates(false);

            // Legge data i UI
            LoadSelectedItemsDetails();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            // Legge inne nytt tomt motion data set objekt
            Project newSession = new Project();

            // Store motion data set in database
            newSession.Id = mvsDatabase.Insert(newSession);

            // Legge i listen
            mvsProjectList.Insert(0, newSession);

            // Sette ny item som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
            int index = gvProjects.Items.IndexOf(newSession);
            gvProjects.SelectedItem = gvProjects.Items[index];

            updateUIButtonsCallback();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindowVM.SelectedProject != null)
            {
                RadWindow.Confirm("Delete the selected motion data set and all associated data?", OnClosed);

                void OnClosed(object sendero, WindowClosedEventArgs ea)
                {
                    if ((bool)ea.DialogResult == true)
                    {
                        // Slette fra database
                        mvsDatabase.Remove(mainWindowVM.SelectedProject);

                        // Slette fra listen
                        mvsProjectList.Remove(mainWindowVM.SelectedProject);

                        // Sette item 0 som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
                        gvProjects.SelectedItem = gvProjects.Items[0];
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
            importDlg.Init(importVM, config, mainWindowVM.SelectedProject, mvsDatabase, mainWindowVM);
            importDlg.ShowDialog();

            // Laster items data på nytt ettersom de kan ha blitt endret under import over
            LoadSelectedItemsDetails();
        }

        private void gvProjects_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            mainWindowVM.SelectedProject = (sender as RadGridView).SelectedItem as Project;

            LoadSelectedItemsDetails();
            UpdateUIStates(false);

            if (mainWindowVM.SelectedProject != null &&
                mainWindowVM.SelectedProject.DataSetHasData())
            {
                // Clear display data
                projectVM.ClearDisplayData();

                // Starte data analyse
                dataViewWorker.RunWorkerAsync();

                // Åpne progress dialog
                progressDlg.Start(mainWindowVM);
                progressDlg.ShowDialog();
            }

            updateUIButtonsCallback();
        }

        private void LoadSelectedItemsDetails()
        {
            if (mainWindowVM.SelectedProject != null)
            {
                lbDataSetID.Content = mainWindowVM.SelectedProject.Id.ToString();
                tbDataSetName.Text = mainWindowVM.SelectedProject.Name;
                tbDataSetComments.Text = mainWindowVM.SelectedProject.Comments;
                cboInputMRUs.Text = mainWindowVM.SelectedProject.InputMRUs.GetDescription();
                lbDataSetDate.Content = mainWindowVM.SelectedProject.DateString;
                lbDataSetStartTime.Content = mainWindowVM.SelectedProject.StartTimeString2;
                lbDataSetEndTime.Content = mainWindowVM.SelectedProject.EndTimeString2;
                lbDataSetDuration.Content = mainWindowVM.SelectedProject.DurationString;

                tbDurationWarning.Visibility = mainWindowVM.SelectedProject.DurationWarning();
            }
            else
            {
                lbDataSetID.Content = string.Empty;
                tbDataSetName.Text = string.Empty;
                tbDataSetComments.Text = string.Empty;
                cboInputMRUs.Text = InputMRUType.None.GetDescription();
                lbDataSetDate.Content = string.Empty;
                lbDataSetStartTime.Content = string.Empty;
                lbDataSetEndTime.Content = string.Empty;
                lbDataSetDuration.Content = string.Empty;

                tbDurationWarning.Visibility = Visibility.Collapsed;
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
            if (mainWindowVM.SelectedProject != null)
            {
                mainWindowVM.SelectedProject.Name = (sender as TextBox).Text;

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedProject);

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
            if (mainWindowVM.SelectedProject != null)
            {
                mainWindowVM.SelectedProject.Comments = (sender as TextBox).Text;

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedProject);
            }
        }

        private void cboInputMRUs_DropDownClosed(object sender, EventArgs e)
        {
            if (mainWindowVM.SelectedProject != null)
            {
                // Ny valgt MRU type
                mainWindowVM.SelectedProject.InputMRUs = EnumExtension.GetEnumValueFromDescription<InputMRUType>((sender as RadComboBox).Text);

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedProject);
            }
        }

        private void InitDataViewWorker()
        {
            dataViewWorker.DoWork += dataViewWorker_DoWork;
            dataViewWorker.ProgressChanged += importWorker_ProgressChanged;
            dataViewWorker.RunWorkerCompleted += dataViewWorker_RunWorkerCompleted;
            dataViewWorker.WorkerReportsProgress = true;
        }

        private void dataViewWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Sette ops mode til analyse
            mainWindowVM.OperationsMode = OperationsMode.ViewData;

            // Resetter data listene i dataCalculations
            projectVM.ResetDataCalculations();

            // Laste session data fra databasen
            mvsDatabase.LoadSessionData(mainWindowVM.SelectedProject, projectVM.projectsDataList);

            // Analysere session data
            projectVM.AnalyseProjectData(dataViewWorker.ReportProgress);
        }

        private void importWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            mainWindowVM.dataAnalysisProgress = e.ProgressPercentage;
        }

        private void dataViewWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            projectVM.TransferToDisplay();

            progressDlg.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MVS.Models;
using MVS.Views.Controls;
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

            // Set initial wizard step.
            SetWizardStep(1);
            UpdateWizardNavigation();
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

            // Refresh the wizard widgets (duration banner, cards, step-4 labels).
            durationBanner.Update(mainWindowVM.SelectedProject);
            RefreshResultCards();
            RefreshAppliedCorrectionPanel();
            UpdateWizardNavigation();
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

            // Refresh result cards with newly computed statistics.
            RefreshResultCards();
            RefreshAppliedCorrectionPanel();
            UpdateWizardNavigation();
        }

        // ============================================================
        // Wizard navigation
        // ============================================================

        private int currentStep = 1;

        private void SetWizardStep(int step)
        {
            if (step < 1) step = 1;
            if (step > 4) step = 4;
            currentStep = step;

            panelStep1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            panelStep2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            panelStep3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            panelStep4.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;

            tbStep1.Foreground = step == 1 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep2.Foreground = step == 2 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep3.Foreground = step == 3 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep4.Foreground = step == 4 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;

            tbStep1.FontWeight = step == 1 ? FontWeights.Bold : FontWeights.Normal;
            tbStep2.FontWeight = step == 2 ? FontWeights.Bold : FontWeights.Normal;
            tbStep3.FontWeight = step == 3 ? FontWeights.Bold : FontWeights.Normal;
            tbStep4.FontWeight = step == 4 ? FontWeights.Bold : FontWeights.Normal;

            if (projectVM != null)
                projectVM.CurrentWizardStep = step;

            UpdateWizardNavigation();
        }

        private void UpdateWizardNavigation()
        {
            btnPrev.IsEnabled = currentStep > 1;

            var project = mainWindowVM?.SelectedProject;
            bool hasData = project != null && project.DataSetHasData();

            // Allow forward navigation from step 1/2 always (so the user can preview
            // the workflow), but require captured data before step 3 actually shows results.
            btnNext.IsEnabled = currentStep < 4;

            if (currentStep == 2)
            {
                tbAnalysisStatus.Text = hasData
                    ? "Data captured. Continue to review."
                    : "No data captured yet. Record or import HMS data.";
            }
            else if (currentStep == 3)
            {
                tbAnalysisStatus.Text = hasData
                    ? "Showing per-axis statistics for the selected project."
                    : "Capture data before reviewing.";
            }
            else if (currentStep == 4)
            {
                tbAnalysisStatus.Text = project != null && project.HasCorrectionApplied
                    ? "Corrections applied for this project."
                    : "No corrections applied yet.";
            }
            else
            {
                tbAnalysisStatus.Text = project != null
                    ? "Project selected. Continue to capture."
                    : "Select or create a project to begin.";
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            SetWizardStep(currentStep - 1);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            SetWizardStep(currentStep + 1);
        }

        // ============================================================
        // Result-card refresh and event handlers
        // ============================================================

        private void RefreshResultCards()
        {
            if (projectVM == null)
                return;

            var project = mainWindowVM?.SelectedProject;
            bool hasCorrection = project != null && project.HasCorrectionApplied;

            cardPitch.RefStats = projectVM.RefPitchStats;
            cardPitch.TestStats = projectVM.TestPitchStats;
            cardPitch.DevStats = projectVM.DevPitchStats;
            cardPitch.AppliedCorrection = project?.AppliedCorrectionPitch ?? 0d;
            cardPitch.HasCorrectionApplied = hasCorrection;

            cardRoll.RefStats = projectVM.RefRollStats;
            cardRoll.TestStats = projectVM.TestRollStats;
            cardRoll.DevStats = projectVM.DevRollStats;
            cardRoll.AppliedCorrection = project?.AppliedCorrectionRoll ?? 0d;
            cardRoll.HasCorrectionApplied = hasCorrection;

            cardHeave.RefStats = projectVM.RefHeaveStats;
            cardHeave.TestStats = projectVM.TestHeaveStats;
            cardHeave.DevStats = projectVM.DevHeaveStats;
            cardHeave.AppliedCorrection = project?.AppliedCorrectionHeave ?? 0d;
            cardHeave.HasCorrectionApplied = hasCorrection;
        }

        private void RefreshAppliedCorrectionPanel()
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || !project.HasCorrectionApplied)
            {
                lbAppliedPitch.Content = Constants.NotAvailable;
                lbAppliedRoll.Content = Constants.NotAvailable;
                lbAppliedHeave.Content = Constants.NotAvailable;
                lbAppliedAt.Content = Constants.NotAvailable;
                return;
            }

            lbAppliedPitch.Content = project.AppliedCorrectionPitch.ToString("F4") + " °";
            lbAppliedRoll.Content = project.AppliedCorrectionRoll.ToString("F4") + " °";
            lbAppliedHeave.Content = project.AppliedCorrectionHeave.ToString("F4") + " m";
            lbAppliedAt.Content = project.CorrectionAppliedAtString;
        }

        private void cardPitch_CopyRequested(object sender, RoutedEventArgs e)
        {
            CopyValueToClipboard(projectVM?.RecommendedCorrectionPitch ?? 0d);
        }
        private void cardRoll_CopyRequested(object sender, RoutedEventArgs e)
        {
            CopyValueToClipboard(projectVM?.RecommendedCorrectionRoll ?? 0d);
        }
        private void cardHeave_CopyRequested(object sender, RoutedEventArgs e)
        {
            CopyValueToClipboard(projectVM?.RecommendedCorrectionHeave ?? 0d);
        }

        private static void CopyValueToClipboard(double value)
        {
            try
            {
                Clipboard.SetText(value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            }
            catch
            {
                // Clipboard access can fail intermittently; ignore.
            }
        }

        private void cardPitch_ApplyRequested(object sender, RoutedEventArgs e)
        {
            ApplySingleAxis(axisPitch: true, axisRoll: false, axisHeave: false);
        }
        private void cardRoll_ApplyRequested(object sender, RoutedEventArgs e)
        {
            ApplySingleAxis(axisPitch: false, axisRoll: true, axisHeave: false);
        }
        private void cardHeave_ApplyRequested(object sender, RoutedEventArgs e)
        {
            ApplySingleAxis(axisPitch: false, axisRoll: false, axisHeave: true);
        }

        private void ApplySingleAxis(bool axisPitch, bool axisRoll, bool axisHeave)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null) return;

            double pitch = axisPitch ? projectVM.RecommendedCorrectionPitch : project.AppliedCorrectionPitch;
            double roll = axisRoll ? projectVM.RecommendedCorrectionRoll : project.AppliedCorrectionRoll;
            double heave = axisHeave ? projectVM.RecommendedCorrectionHeave : project.AppliedCorrectionHeave;

            mvsDatabase.SaveCorrection(project, pitch, roll, heave);
            RefreshResultCards();
            RefreshAppliedCorrectionPanel();
            UpdateWizardNavigation();
        }

        private void cardPitch_ResetRequested(object sender, RoutedEventArgs e)
        {
            ResetSingleAxis(true, false, false);
        }
        private void cardRoll_ResetRequested(object sender, RoutedEventArgs e)
        {
            ResetSingleAxis(false, true, false);
        }
        private void cardHeave_ResetRequested(object sender, RoutedEventArgs e)
        {
            ResetSingleAxis(false, false, true);
        }

        private void ResetSingleAxis(bool axisPitch, bool axisRoll, bool axisHeave)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null) return;

            double pitch = axisPitch ? 0d : project.AppliedCorrectionPitch;
            double roll = axisRoll ? 0d : project.AppliedCorrectionRoll;
            double heave = axisHeave ? 0d : project.AppliedCorrectionHeave;

            // If all three would now be zero, treat as a full reset.
            if (pitch == 0d && roll == 0d && heave == 0d)
                mvsDatabase.ResetCorrection(project);
            else
                mvsDatabase.SaveCorrection(project, pitch, roll, heave);

            RefreshResultCards();
            RefreshAppliedCorrectionPanel();
            UpdateWizardNavigation();
        }

        // ============================================================
        // Apply / Reset all + Export
        // ============================================================

        private void btnApplyAll_Click(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null) return;

            projectVM.ApplyRecommendedCorrection(mvsDatabase, project);
            RefreshResultCards();
            RefreshAppliedCorrectionPanel();
            UpdateWizardNavigation();
        }

        private void btnResetAll_Click(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null) return;

            RadWindow.Confirm("Clear the applied corrections for this project?", OnClosed);

            void OnClosed(object _, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    projectVM.ResetAppliedCorrection(mvsDatabase, project);
                    RefreshResultCards();
                    RefreshAppliedCorrectionPanel();
                    UpdateWizardNavigation();
                }
            }
        }

        private void btnExportReport_Click(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null) return;

            var dlg = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = string.Format("verification_{0}_{1:yyyyMMdd_HHmmss}.csv",
                    string.IsNullOrEmpty(project.Name) ? "project" : project.Name,
                    DateTime.UtcNow)
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var report = new Services.VerificationReportExporter();
                    report.ExportCsv(dlg.FileName, project, projectVM);
                    RadWindow.Alert("Report exported successfully.");
                }
                catch (Exception ex)
                {
                    RadWindow.Alert("Failed to export report:\n" + ex.Message);
                }
            }
        }
    }
}

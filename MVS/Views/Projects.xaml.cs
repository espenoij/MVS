 using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using MVS.Models;
using MVS.Views.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;
using Telerik.Windows.Documents.Fixed;
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

        // LiDAR wizard view model (Step 2). Used to gate wizard navigation on correction state.
        private LivoxLidarVM _livoxVM;

        // Currently subscribed project (for live wizard re-evaluation on Name/InputMRUs changes).
        private Project _subscribedProject;

        // Data View worker
        private readonly BackgroundWorker dataViewWorker = new BackgroundWorker();

        private MainWindow.UpdateUIButtonsCallback updateUIButtonsCallback;
        private Action startRecordingCallback;
        private Action stopRecordingCallback;
        private Action testRunCallback;

        // Progress dialog
        DialogDataAnalysisProgress progressDlg = new DialogDataAnalysisProgress();

        // ---- Step 5 report generation / preview state ----
        // The generated PDF is cached in-memory so navigating back to Step 5 and
        // saving to disc do not trigger a fresh (potentially slow) generation.
        private byte[] _reportPdfBytes;
        // The stream backing the on-screen preview; kept alive for the viewer's lifetime.
        private MemoryStream _reportPreviewStream;
        // Id of the project the cached report belongs to (used to detect staleness).
        private int? _reportProjectId;
        // Guards against concurrent/re-entrant generation while a report is building.
        private bool _reportGenerating;
        // Tracks whether a report has been generated for the current project session.
        private bool _reportGenerated;
        // Tracks whether input fields changed after the last report generation.
        private bool _reportNeedsUpdate;
        // Guards TextChanged handlers during LoadSelectedItemsDetails.
        private bool _loadingDetails;

        // Live-update timer for the duration banner while recording is active.
        private readonly DispatcherTimer _recordingBannerTimer;
        private DateTime _recordingStartTime;

        public Projects()
        {
            InitializeComponent();

            importVM = new ImportVM();

            _recordingBannerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _recordingBannerTimer.Tick += (s, e) => durationBanner.UpdateLive(_recordingStartTime);
        }

        public void Init(MainWindowVM mainWindowVM, ProjectVM projectVM, Config config, MVSDatabase mvsDatabase, UpdateUIButtonsCallback updateUIButtonsCallback, Action startRecordingCallback, Action stopRecordingCallback, Action testRunCallback)
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
            this.startRecordingCallback = startRecordingCallback;
            this.stopRecordingCallback = stopRecordingCallback;
            this.testRunCallback = testRunCallback;

            // Wire the MainWindowVM bridge so recording-button bindings resolve.
            mainWindowVMBridge.DataContext = mainWindowVM;

            InitUI();
        }

        public void InitUI()
        {
            importVM.Init(config);

            // Wire up the embedded Data Analysis charts (Step 3).
            ucDataAnalysis.Init(projectVM);

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

        /// <summary>
        /// Wires the embedded Livox LiDAR wizard page (wizard step 2) to its view model.
        /// Called from MainWindow after the LivoxLidarVM has been constructed.
        /// </summary>
        public void InitLidar(LivoxLidarVM livoxVM)
        {
            _livoxVM = livoxVM;

            // Re-evaluate wizard navigation whenever the LiDAR correction state changes
            // (e.g. when the user applies or clears the correction in Step 2).
            if (livoxVM?.Correction != null)
                livoxVM.Correction.PropertyChanged += (s, e) => Dispatcher.Invoke(UpdateWizardNavigation);

            ucWizardLivoxLidarPage.Init(livoxVM);

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
                btnClearRecording.IsEnabled = false;

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
                    btnClearRecording.IsEnabled = false;
                }
                else
                {
                    btnDelete.IsEnabled = true;

                    if (mainWindowVM.SelectedProject.StartTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    {
                        btnImport.IsEnabled = false;
                        lbImport.IsEnabled = false;
                        btnClearRecording.IsEnabled = false;
                    }
                    else
                    {
                        btnImport.IsEnabled = true;
                        lbImport.IsEnabled = true;
                        btnClearRecording.IsEnabled = true;
                    }
                }

                gvProjects.IsEnabled = true;
            }

            if (mainWindowVM.SelectedProject != null)
                {
                    tbDataSetName.IsEnabled = true;
                    tbDataSetComments.IsEnabled = true;
                    tbOperator.IsEnabled = true;
                    tbVesselName.IsEnabled = true;
                    tbLocation.IsEnabled = true;
                    chkCorrectionApplied.IsEnabled = true;
                    cboInputMRUs.IsEnabled = true;
                }
                else
                {
                    tbDataSetName.IsEnabled = false;
                    tbDataSetComments.IsEnabled = false;
                    tbOperator.IsEnabled = false;
                    tbVesselName.IsEnabled = false;
                    tbLocation.IsEnabled = false;
                    chkCorrectionApplied.IsEnabled = false;
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

            // Start live banner updates.
            _recordingStartTime = DateTime.UtcNow;
            durationBanner.UpdateLive(_recordingStartTime);
            _recordingBannerTimer.Start();
        }

        public void Stop(OperationsMode mode)
        {
            // Stop live banner updates.
            _recordingBannerTimer.Stop();

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

            // Load and analyse the freshly-recorded session data so the Review
            // step shows current statistics as soon as the user navigates there.
            if (mode == OperationsMode.Recording &&
                mainWindowVM.SelectedProject != null &&
                mainWindowVM.SelectedProject.DataSetHasData())
            {
                dataViewWorker.RunWorkerAsync();
                progressDlg.Start(mainWindowVM);
                progressDlg.ShowDialog();
            }
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

        private void btnStartRecording_Click(object sender, RoutedEventArgs e)
        {
            startRecordingCallback?.Invoke();
        }

        private void btnStopRecording_Click(object sender, RoutedEventArgs e)
        {
            stopRecordingCallback?.Invoke();
        }

        private void btnTestRun_Click(object sender, RoutedEventArgs e)
        {
            testRunCallback?.Invoke();
        }

        private void btnClearRecording_Click(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || !project.DataSetHasData())
                return;

            RadWindow.Confirm("This will permanently delete all recorded motion data for this project.\n\nThis action cannot be undone.", OnClosed);

            void OnClosed(object sendero, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    mvsDatabase.DeleteData(project);
                    project.ClearTimestamps();
                    projectVM?.ResetAnalysisResults();
                    LoadSelectedItemsDetails();
                }
            }
        }

        private void gvProjects_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            mainWindowVM.SelectedProject = (sender as RadGridView).SelectedItem as Project;

            // Reset all downstream wizard data (LiDAR correction, analysis results and
            // the wizard step) so nothing from the previously selected project carries
            // over to the newly created or selected one.
            ResetDownstreamData();

            LoadSelectedItemsDetails();
            UpdateUIStates(false);

            if (mainWindowVM.SelectedProject != null &&
                mainWindowVM.SelectedProject.DataSetHasData())
            {
                // Starte data analyse
                dataViewWorker.RunWorkerAsync();

                // Åpne progress dialog
                progressDlg.Start(mainWindowVM);
                progressDlg.ShowDialog();
            }

            updateUIButtonsCallback();
        }

        /// <summary>
        /// Resets all downstream wizard data so that state from a previously selected
        /// project does not carry over when a new project is created or selected.
        /// Clears the shared LiDAR correction (step 2), the captured/analysis results
        /// (steps 3-4) and returns the wizard to the first step.
        /// </summary>
        private void ResetDownstreamData()
        {
            // Step 2 — clear the shared in-memory LiDAR correction state.
            _livoxVM?.ResetCorrection();

            // Steps 3-4 — clear analysis data and statistics from the previous project.
            projectVM?.ResetAnalysisResults();

            // Step 5 — drop any cached report and reset generation state for the new project.
            _reportGenerated = false;
            _reportNeedsUpdate = false;
            InvalidateReportCache();

            // Return the wizard to the first step.
            SetWizardStep(1);
        }

        private void LoadSelectedItemsDetails()
        {
            _loadingDetails = true;
            if (mainWindowVM.SelectedProject != null)
            {
                lbDataSetID.Content = mainWindowVM.SelectedProject.Id.ToString();
                tbDataSetName.Text = mainWindowVM.SelectedProject.Name;
                tbDataSetComments.Text = mainWindowVM.SelectedProject.Comments;
                tbOperator.Text = mainWindowVM.SelectedProject.Operator;
                tbVesselName.Text = mainWindowVM.SelectedProject.VesselName;
                tbLocation.Text = mainWindowVM.SelectedProject.Location;
                SetCorrectionCheckbox(mainWindowVM.SelectedProject.HasCorrectionApplied);
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
                tbOperator.Text = string.Empty;
                tbVesselName.Text = string.Empty;
                tbLocation.Text = string.Empty;
                SetCorrectionCheckbox(false);
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

            // Subscribe to the selected project so Step 1 gating reacts live to
            // Name / Input MRU changes.
            SubscribeToProject(mainWindowVM.SelectedProject);

            UpdateWizardNavigation();
            _loadingDetails = false;
        }

        /// <summary>
        /// Subscribes to the selected project's PropertyChanged so the wizard navigation
        /// (and requirements banner) re-evaluate live as the user edits Name / Input MRUs.
        /// </summary>
        private void SubscribeToProject(Project project)
        {
            if (ReferenceEquals(_subscribedProject, project))
                return;

            if (_subscribedProject != null)
                _subscribedProject.PropertyChanged -= SelectedProject_PropertyChanged;

            _subscribedProject = project;

            if (_subscribedProject != null)
                _subscribedProject.PropertyChanged += SelectedProject_PropertyChanged;
        }

        private void SelectedProject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If the applied corrections change, the cached report no longer matches
            // the project state, so drop it and let Step 5 regenerate on next visit.
            if (e.PropertyName == nameof(Project.HasCorrectionApplied) ||
                (e.PropertyName != null && e.PropertyName.StartsWith("AppliedCorrection", StringComparison.Ordinal)))
            {
                InvalidateReportCache();

                // Keep the checkbox on Step 5 in sync with the project's correction state.
                var project = mainWindowVM?.SelectedProject;
                if (project != null)
                    SetCorrectionCheckbox(project.HasCorrectionApplied);
            }

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

        private void tbOperator_LostFocus(object sender, RoutedEventArgs e)
        {
            tbOperator_Update(sender);
        }

        private void tbOperator_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbOperator_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbOperator_Update(object sender)
        {
            if (mainWindowVM.SelectedProject != null)
            {
                mainWindowVM.SelectedProject.Operator = (sender as TextBox).Text;
                mvsDatabase.Update(mainWindowVM.SelectedProject);
                InvalidateReportCache();
            }
        }

        private void tbVesselName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbVesselName_Update(sender);
        }

        private void tbVesselName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbVesselName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbVesselName_Update(object sender)
        {
            if (mainWindowVM.SelectedProject != null)
            {
                mainWindowVM.SelectedProject.VesselName = (sender as TextBox).Text;
                mvsDatabase.Update(mainWindowVM.SelectedProject);
                InvalidateReportCache();
            }
        }

        private void tbLocation_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLocation_Update(sender);
        }

        private void tbLocation_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLocation_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLocation_Update(object sender)
        {
            if (mainWindowVM.SelectedProject != null)
            {
                mainWindowVM.SelectedProject.Location = (sender as TextBox).Text;
                mvsDatabase.Update(mainWindowVM.SelectedProject);
                InvalidateReportCache();
            }
        }

        private void SetCorrectionCheckbox(bool isChecked)
        {
            chkCorrectionApplied.Checked -= chkCorrectionApplied_Changed;
            chkCorrectionApplied.Unchecked -= chkCorrectionApplied_Changed;
            chkCorrectionApplied.IsChecked = isChecked;
            chkCorrectionApplied.Checked += chkCorrectionApplied_Changed;
            chkCorrectionApplied.Unchecked += chkCorrectionApplied_Changed;
        }

        private void chkCorrectionApplied_Changed(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null)
                return;

            bool apply = chkCorrectionApplied.IsChecked == true;

            if (apply)
                projectVM.ApplyRecommendedCorrection(mvsDatabase, project);
            else
                projectVM.ResetAppliedCorrection(mvsDatabase, project);

            RefreshResultCards();
            InvalidateReportCache();
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

            // Automatically apply the recommended corrections found during capture.
            var project = mainWindowVM?.SelectedProject;
            if (project != null && projectVM != null)
                projectVM.ApplyRecommendedCorrection(mvsDatabase, project);

            // Refresh result cards with newly computed statistics.
            RefreshResultCards();

            // Advance the wizard to the step that best reflects the loaded project's state
            // so the user can move freely through steps 2 and 3 without being blocked.
            var loadedProject = mainWindowVM?.SelectedProject;
            if (loadedProject != null && loadedProject.DataSetHasData())
                SetWizardStep(loadedProject.HasCorrectionApplied ? 4 : 3);
            else
                UpdateWizardNavigation();
        }

        // ============================================================
        // Wizard navigation
        // ============================================================

        private int currentStep = 1;

        private void SetWizardStep(int step)
        {
            if (step < 1) step = 1;
            if (step > 5) step = 5;
            currentStep = step;

            panelStep1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            panelStep2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            panelStep3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            panelStep4.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
            panelStep5.Visibility = step == 5 ? Visibility.Visible : Visibility.Collapsed;

            tbStep1.Foreground = step == 1 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep2.Foreground = step == 2 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep3.Foreground = step == 3 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep4.Foreground = step == 4 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;
            tbStep5.Foreground = step == 5 ? System.Windows.Media.Brushes.SteelBlue : System.Windows.Media.Brushes.DimGray;

            tbStep1.FontWeight = step == 1 ? FontWeights.Bold : FontWeights.Normal;
            tbStep2.FontWeight = step == 2 ? FontWeights.Bold : FontWeights.Normal;
            tbStep3.FontWeight = step == 3 ? FontWeights.Bold : FontWeights.Normal;
            tbStep4.FontWeight = step == 4 ? FontWeights.Bold : FontWeights.Normal;
            tbStep5.FontWeight = step == 5 ? FontWeights.Bold : FontWeights.Normal;

            if (projectVM != null)
                projectVM.CurrentWizardStep = step;

            UpdateWizardNavigation();

            // On entering the report step, show the cached report if one is already
            // available for this project; otherwise prompt the user to generate it.
            // Generation itself is triggered explicitly via the Generate Report button.
            if (step == 5)
            {
                RefreshReportPreviewState();
            }
        }

        private void UpdateWizardNavigation()
        {
            btnPrev.IsEnabled = currentStep > 1;

            var project = mainWindowVM?.SelectedProject;
            bool hasData = project != null && project.DataSetHasData();
            bool lidarApplied = _livoxVM?.Correction != null && _livoxVM.Correction.IsActive;

            // Evaluate the minimum requirements for the current step. The user may only
            // proceed to the next step once these are met.
            bool canProceed;
            string requirementsText;
            bool requirementsMet;

            switch (currentStep)
            {
                case 1:
                    // Step 1 — Setup: project selected, named, and input MRUs chosen.
                    bool hasProject = project != null;
                    bool hasName = project != null && !string.IsNullOrWhiteSpace(project.Name);
                    bool hasInputMRUs = project != null && project.InputMRUs != InputMRUType.None;
                    canProceed = hasProject && hasName && hasInputMRUs;
                    requirementsMet = canProceed;
                    requirementsText = canProceed
                        ? "Step 1 complete — project setup is ready. Click Next to continue to LiDAR correction."
                        : "To continue you must:  " +
                          (hasProject ? "\u2713 Select a project" : "\u2717 Select a project") + "    " +
                          (hasName ? "\u2713 Enter a project name" : "\u2717 Enter a project name") + "    " +
                          (hasInputMRUs ? "\u2713 Choose the Input MRUs" : "\u2717 Choose the Input MRUs");
                    break;

                case 2:
                    // Step 2 — LiDAR correction: work through all four sub-steps then apply.
                    // Also satisfied when the project already has captured data (correction was applied during the original capture session).
                    bool lidarSetup    = _livoxVM != null && (_livoxVM.CanDisconnect || _livoxVM.CanFit);
                    bool lidarScanned  = _livoxVM?.CanFit == true;
                    bool lidarAnalysed = _livoxVM?.HasFitResult == true;
                    canProceed = lidarApplied || hasData;
                    requirementsMet = canProceed;
                    requirementsText = lidarApplied
                        ? "Step 2 complete — the LiDAR correction is applied to the Reference MRU. Click Next to continue to capture."
                        : hasData
                            ? "Step 2 — project already has captured data. Click Next to continue."
                            : "To continue you must:  " +
                              (lidarSetup    ? "\u2713 1: Set up the LiDAR"    : "\u2717 1: Set up the LiDAR")    + "    " +
                              (lidarScanned  ? "\u2713 2: Perform a scan"       : "\u2717 2: Perform a scan")       + "    " +
                              (lidarAnalysed ? "\u2713 3: Analyse"              : "\u2717 3: Analyse")              + "    " +
                              (lidarApplied  ? "\u2713 4: Apply the correction" : "\u2717 4: Apply the correction");
                    break;

                case 3:
                    // Step 3 — Capture: the project must contain recorded or imported data.
                    canProceed = hasData;
                    requirementsMet = canProceed;
                    requirementsText = canProceed
                        ? "Step 3 complete — motion data captured. Click Next to review the results."
                        : "To continue you must:  \u2717 Record live motion data or import an existing HMS recording for this project.";
                    break;

                case 4:
                    // Step 4 — Review: informational only; the user may always continue to reporting.
                    canProceed = true;
                    requirementsMet = true;
                    requirementsText = "Step 4 — review the results if needed, then click Next to continue to reporting.";
                    break;

                default:
                    // Step 5 — Apply & Report: final step, no further navigation.
                    canProceed = false;
                    requirementsMet = true;
                    requirementsText = string.Empty;
                    break;
            }

            btnNext.IsEnabled = canProceed;

            // Update the requirements banner with success / outstanding-requirement feedback.
            if (currentStep >= 5 || string.IsNullOrEmpty(requirementsText))
            {
                requirementsBanner.Visibility = Visibility.Collapsed;
            }
            else
            {
                requirementsBanner.Visibility = Visibility.Visible;
                tbRequirements.Text = requirementsText;

                if (requirementsMet)
                {
                    // Green — requirements met.
                    requirementsBanner.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xD1, 0xE7, 0xDD));
                    requirementsBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xA3, 0xCF, 0xBB));
                    tbRequirements.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x0A, 0x36, 0x22));
                }
                else
                {
                    // Amber — requirements outstanding.
                    requirementsBanner.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0xF3, 0xCD));
                    requirementsBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0xE6, 0x9C));
                    tbRequirements.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x66, 0x4D, 0x03));
                }
            }

            // Show the selected project name in the wizard header (steps 2-5).
            if (project != null && currentStep > 1)
            {
                tbWizardProjectName.Text = project.Name;
                tbWizardProjectName.Visibility = Visibility.Visible;
            }
            else
            {
                tbWizardProjectName.Visibility = Visibility.Collapsed;
            }

            if (currentStep == 2)
            {
                tbAnalysisStatus.Text = lidarApplied
                    ? "LiDAR correction applied to the Reference MRU."
                    : "Apply the LiDAR-derived correction to the Reference MRU before capturing data.";
            }
            else if (currentStep == 3)
            {
                tbAnalysisStatus.Text = hasData
                    ? "Data captured. Continue to review."
                    : "No data captured yet. Record or import HMS data.";
            }
            else if (currentStep == 4)
            {
                tbAnalysisStatus.Text = hasData
                    ? "Showing per-axis statistics for the selected project."
                    : "Capture data before reviewing.";
            }
            else if (currentStep == 5)
            {
                tbAnalysisStatus.Text = project != null && project.HasCorrectionApplied
                    ? "Corrections applied for this project."
                    : "No corrections applied yet.";
            }
            else
            {
                tbAnalysisStatus.Text = project != null
                    ? "Project selected. Continue to LiDAR correction."
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

        // ============================================================
        // Report generation & preview (Step 5)
        // ============================================================

        /// <summary>
        /// Updates the report area when entering Step 5 without triggering generation.
        /// If a valid report is already cached for the current project it is shown;
        /// otherwise the user is prompted to click Generate Report.
        /// </summary>
        private void RefreshReportPreviewState()
        {
            var project = mainWindowVM?.SelectedProject;

            if (project == null || projectVM == null)
            {
                btnGenerateReport.IsEnabled = false;
                btnUpdateReport.IsEnabled = false;
                btnUpdateReport.Visibility = Visibility.Collapsed;
                ShowReportPreviewEmpty("Select a project to generate its report.");
                return;
            }

            UpdateReportButtonState();

            if (_reportPdfBytes != null && _reportProjectId == project.Id)
            {
                ApplyReportPreview(_reportPdfBytes);
            }
            else
            {
                ShowReportPreviewEmpty(_reportGenerated && _reportNeedsUpdate
                    ? "Input data has changed — click Update Report to refresh the preview."
                    : "Click Generate Report to build the verification report.");
            }
        }

        /// <summary>
        /// Ensures a report exists for the current project and shows it in the
        /// on-screen preview. Generation runs on a background thread behind a modal
        /// progress dialog. A cached report for the same project is reused so the
        /// (potentially slow) generation only happens once per project/data change.
        /// </summary>
        private async Task EnsureReportPreviewAsync()
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null)
            {
                ShowReportPreviewEmpty("Select a project to generate its report.");
                return;
            }

            // Reuse a valid cached report for this project.
            if (_reportPdfBytes != null && _reportProjectId == project.Id)
            {
                ApplyReportPreview(_reportPdfBytes);
                return;
            }

            // Guard against re-entrancy (e.g. rapid step navigation).
            if (_reportGenerating)
                return;

            _reportGenerating = true;
            btnGenerateReport.IsEnabled = false;
            btnSaveReportToDisc.IsEnabled = false;
            btnOpenPdfReport.IsEnabled = false;

            var progress = new DialogReportProgress();

            try
            {
                // Build the report model on the UI thread — it reads view-model state.
                projectVM.ComputeExtendedStatistics();
                var model = Services.Reporting.VerificationReportModel.FromProject(project, projectVM);

                progress.Show();

                // The heavy work (chart rendering + PDF build) only touches the model
                // POCO and Telerik document objects, so it is safe off the UI thread.
                byte[] bytes = await Task.Run(() => GenerateReportBytes(model));

                _reportPdfBytes = bytes;
                _reportProjectId = project.Id;
                _reportGenerated = true;
                _reportNeedsUpdate = false;

                ApplyReportPreview(bytes);
            }
            catch (Exception ex)
            {
                _reportPdfBytes = null;
                _reportProjectId = null;
                ShowReportPreviewEmpty("The report could not be generated.");
                RadWindow.Alert("Failed to generate the report:\n" + ex.Message);
            }
            finally
            {
                progress.Close();
                _reportGenerating = false;
                UpdateReportButtonState();
            }
        }

        private async void btnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            await EnsureReportPreviewAsync();
        }

        /// <summary>
        /// Renders the result charts and exports the report to PDF bytes.
        /// Runs on a background thread; must not touch WPF UI objects.
        /// </summary>
        private static byte[] GenerateReportBytes(Services.Reporting.VerificationReportModel model)
        {
            if (model.HasData)
            {
                model.DeviationChartPng = Services.Reporting.ReportChartRenderer.RenderDeviationChart(model);
                model.MeansChartPng = Services.Reporting.ReportChartRenderer.RenderMeansChart(model);
            }

            using (var ms = new MemoryStream())
            {
                Services.Reporting.VerificationPdfReportExporter.Export(ms, model);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Loads the given PDF bytes into the on-screen preview and enables saving.
        /// </summary>
        private void ApplyReportPreview(byte[] pdfBytes)
        {
            // Replace the previous preview stream (the viewer keeps it open).
            _reportPreviewStream?.Dispose();
            _reportPreviewStream = new MemoryStream(pdfBytes, writable: false);

            pdfReportPreview.DocumentSource = new PdfDocumentSource(_reportPreviewStream);
            pdfReportPreview.Visibility = Visibility.Visible;
            tbReportPreviewEmpty.Visibility = Visibility.Collapsed;

            btnSaveReportToDisc.IsEnabled = true;
            btnOpenPdfReport.IsEnabled = true;
        }

        /// <summary>
        /// Hides the preview and shows an empty-state message.
        /// </summary>
        private void ShowReportPreviewEmpty(string message)
        {
            pdfReportPreview.DocumentSource = null;
            pdfReportPreview.Visibility = Visibility.Collapsed;
            tbReportPreviewEmpty.Text = message;
            tbReportPreviewEmpty.Visibility = Visibility.Visible;
            btnSaveReportToDisc.IsEnabled = false;
            btnOpenPdfReport.IsEnabled = false;
        }

        /// <summary>
        /// Discards any cached report so the next visit to Step 5 regenerates it.
        /// Call when the selected project or its corrections change.
        /// </summary>
        private void InvalidateReportCache()
        {
            _reportPdfBytes = null;
            _reportProjectId = null;

            if (_reportGenerated)
                _reportNeedsUpdate = true;

            UpdateReportButtonState();
        }

        private bool IsInputValid()
        {
            var project = mainWindowVM?.SelectedProject;
            return project != null && projectVM != null &&
                   !string.IsNullOrWhiteSpace(tbOperator.Text) &&
                   !string.IsNullOrWhiteSpace(tbVesselName.Text) &&
                   !string.IsNullOrWhiteSpace(tbLocation.Text);
        }

        private void UpdateReportButtonState()
        {
            bool valid = IsInputValid();
            btnGenerateReport.IsEnabled = valid;
            btnUpdateReport.IsEnabled = valid;
            btnUpdateReport.Visibility = (_reportGenerated && _reportNeedsUpdate)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void tbReportField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_loadingDetails) return;
            UpdateReportButtonState();
        }

        // ============================================================
        // Export
        // ============================================================

        private void btnSaveReportToDisc_Click(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null) return;

            // The report is generated on entering Step 5; if for some reason it is
            // not available, fall back to generating it synchronously here.
            if (_reportPdfBytes == null || _reportProjectId != project.Id)
            {
                try
                {
                    projectVM.ComputeExtendedStatistics();
                    var model = Services.Reporting.VerificationReportModel.FromProject(project, projectVM);
                    _reportPdfBytes = GenerateReportBytes(model);
                    _reportProjectId = project.Id;
                }
                catch (Exception ex)
                {
                    RadWindow.Alert("Failed to generate the report:\n" + ex.Message);
                    return;
                }
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = string.Format("verification_{0}_{1:yyyyMMdd_HHmmss}.pdf",
                    string.IsNullOrEmpty(project.Name) ? "project" : project.Name,
                    DateTime.UtcNow)
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, _reportPdfBytes);
                    RadWindow.Alert("PDF report saved successfully.");
                }
                catch (Exception ex)
                {
                    RadWindow.Alert("Failed to save PDF report:\n" + ex.Message);
                }
            }
        }

        private void btnOpenPdfReport_Click(object sender, RoutedEventArgs e)
        {
            var project = mainWindowVM?.SelectedProject;
            if (project == null || projectVM == null) return;

            // The report is generated on entering Step 5; if for some reason it is
            // not available, fall back to generating it synchronously here.
            if (_reportPdfBytes == null || _reportProjectId != project.Id)
            {
                try
                {
                    projectVM.ComputeExtendedStatistics();
                    var model = Services.Reporting.VerificationReportModel.FromProject(project, projectVM);
                    _reportPdfBytes = GenerateReportBytes(model);
                    _reportProjectId = project.Id;
                }
                catch (Exception ex)
                {
                    RadWindow.Alert("Failed to generate the report:\n" + ex.Message);
                    return;
                }
            }

            try
            {
                // Write the report to a temp file and open it with the system's
                // default PDF viewer.
                string fileName = string.Format("verification_{0}_{1:yyyyMMdd_HHmmss}.pdf",
                    string.IsNullOrEmpty(project.Name) ? "project" : project.Name,
                    DateTime.UtcNow);
                string path = Path.Combine(Path.GetTempPath(), fileName);
                File.WriteAllBytes(path, _reportPdfBytes);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                RadWindow.Alert("Failed to open PDF report:\n" + ex.Message);
            }
        }
    }
}

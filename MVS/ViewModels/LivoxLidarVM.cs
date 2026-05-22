using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MVS
{
    public class LivoxLidarVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly LivoxLidarSubsystem   _subsystem;
        private readonly LivoxLidarCorrection  _correction;
        private readonly ErrorHandler          _errorHandler;
        private readonly Config                _config;

        // Last fit result — shown in the UI before the user decides to apply
        private LivoxLidarPlaneFitResult _lastFit;

        // Last deck edge result
        private LivoxLidarDeckEdgeResult _lastEdge;

        private bool _simulationInProgress;

        public LivoxLidarVM(LivoxLidarSubsystem subsystem, LivoxLidarCorrection correction,
                            ErrorHandler errorHandler, Config config)
        {
            _subsystem   = subsystem;
            _correction  = correction;
            _errorHandler = errorHandler;
            _config      = config;

            // Load persisted settings
            LoadConfig();

            // Wire subsystem events → UI
            _subsystem.StatusChanged     += OnStatusChanged;
            _subsystem.PointCountUpdated += OnPointCountUpdated;
            _subsystem.ErrorOccurred     += msg => AppendStatus(msg);

            // Expose the shared correction object for direct binding
            Correction = correction;

            // Commands
            ConnectCommand          = new RelayCommand(_ => Connect(),        _ => CanConnect);
            DisconnectCommand       = new RelayCommand(_ => Disconnect(),      _ => CanDisconnect);
            StartScanCommand        = new RelayCommand(_ => StartScan(),       _ => CanScan);
            StopScanCommand         = new RelayCommand(_ => StopScan(),        _ => IsScanning);
            FitPlaneCommand         = new RelayCommand(_ => FitPlane(),        _ => CanFit);
            ApplyCorrectionCommand  = new RelayCommand(_ => ApplyCorrection(), _ => HasFitResult);
            ClearCorrectionCommand  = new RelayCommand(_ => ClearCorrection());
            SimulateScanCommand     = new RelayCommand(_ => SimulateScan(),    _ => CanSimulate);
            AnalyseCommand          = new RelayCommand(_ => Analyse(),         _ => CanFit);
        }

        // ── Bound properties ─────────────────────────────────────────────────

        public LivoxLidarCorrection Correction { get; }

        private string _configFilePath;
        public string ConfigFilePath
        {
            get { return _configFilePath; }
            set { _configFilePath = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxConfigFilePath, value ?? ""); }
        }

        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxIpAddress, value ?? ""); }
        }

        private float _rangeMinMm = 500f;
        public float RangeMinMm
        {
            get { return _rangeMinMm; }
            set { _rangeMinMm = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxRangeMinMm, value.ToString()); }
        }

        private float _rangeMaxMm = 30000f;
        public float RangeMaxMm
        {
            get { return _rangeMaxMm; }
            set { _rangeMaxMm = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxRangeMaxMm, value.ToString()); }
        }

        private float _azimuthMinDeg = -180f;
        public float AzimuthMinDeg
        {
            get { return _azimuthMinDeg; }
            set { _azimuthMinDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxAzimuthMinDeg, value.ToString()); }
        }

        private float _azimuthMaxDeg = 180f;
        public float AzimuthMaxDeg
        {
            get { return _azimuthMaxDeg; }
            set { _azimuthMaxDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxAzimuthMaxDeg, value.ToString()); }
        }

        private float _elevationMinDeg = -90f;
        public float ElevationMinDeg
        {
            get { return _elevationMinDeg; }
            set { _elevationMinDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxElevationMinDeg, value.ToString()); }
        }

        private float _elevationMaxDeg = 90f;
        public float ElevationMaxDeg
        {
            get { return _elevationMaxDeg; }
            set { _elevationMaxDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxElevationMaxDeg, value.ToString()); }
        }

        private int _maxBufferPoints = 500000;
        public int MaxBufferPoints
        {
            get { return _maxBufferPoints; }
            set { _maxBufferPoints = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxMaxBufferPoints, value.ToString()); }
        }

        private int _maxDisplayPoints = 20000;
        public int MaxDisplayPoints
        {
            get { return _maxDisplayPoints; }
            set { _maxDisplayPoints = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxMaxDisplayPoints, value.ToString()); }
        }

        private double _simPitchDeg = 2.0;
        public double SimPitchDeg
        {
            get { return _simPitchDeg; }
            set 
            { 
                _simPitchDeg = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(EffectivePitchDeg));
                _config.Write(ConfigKey.LivoxSimPitchDeg, value.ToString()); 
            }
        }

        private double _simRollDeg = 1.5;
        public double SimRollDeg
        {
            get { return _simRollDeg; }
            set 
            { 
                _simRollDeg = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(EffectiveRollDeg));
                _config.Write(ConfigKey.LivoxSimRollDeg, value.ToString()); 
            }
        }

        private double _simDeckSlantDeg = 0.0;
        public double SimDeckSlantDeg
        {
            get { return _simDeckSlantDeg; }
            set 
            { 
                _simDeckSlantDeg = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(EffectivePitchDeg));
                OnPropertyChanged(nameof(EffectiveRollDeg));
                _config.Write(ConfigKey.LivoxSimDeckSlantDeg, value.ToString());
            }
        }

        private double _simDeckSlantDirDeg = 0.0;
        public double SimDeckSlantDirDeg
        {
            get { return _simDeckSlantDirDeg; }
            set 
            { 
                _simDeckSlantDirDeg = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(EffectivePitchDeg));
                OnPropertyChanged(nameof(EffectiveRollDeg));
                _config.Write(ConfigKey.LivoxSimDeckSlantDirDeg, value.ToString());
            }
        }

        private DeckSlantType _simDeckSlantType = DeckSlantType.Flat;
        public DeckSlantType SimDeckSlantType
        {
            get { return _simDeckSlantType; }
            set
            {
                _simDeckSlantType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFlatSlant));
                OnPropertyChanged(nameof(EffectivePitchDeg));
                OnPropertyChanged(nameof(EffectiveRollDeg));
                _config.Write(ConfigKey.LivoxSimDeckSlantType, value.ToString());
            }
        }

        public IEnumerable<DeckSlantType> DeckSlantTypeOptions => (DeckSlantType[])Enum.GetValues(typeof(DeckSlantType));

        public bool IsFlatSlant => _simDeckSlantType == DeckSlantType.Flat;

        // Computed effective angles combining lidar orientation and deck slant
        public double EffectivePitchDeg
        {
            get
            {
                double result = _simPitchDeg;

                if (_simDeckSlantType == DeckSlantType.Flat)
                {
                    // Flat slant: use direction to compute pitch component
                    double dirRad = _simDeckSlantDirDeg * Math.PI / 180.0;
                    result += _simDeckSlantDeg * Math.Cos(dirRad);
                }
                // For Ridge and CenterHigh, the slant varies across the deck,
                // so we don't add a constant pitch offset here

                return Math.Round(result, 3);
            }
        }

        public double EffectiveRollDeg
        {
            get
            {
                double result = _simRollDeg;

                if (_simDeckSlantType == DeckSlantType.Flat)
                {
                    // Flat slant: use direction to compute roll component
                    double dirRad = _simDeckSlantDirDeg * Math.PI / 180.0;
                    result += _simDeckSlantDeg * Math.Sin(dirRad);
                }
                // For Ridge and CenterHigh, the slant varies across the deck,
                // so we don't add a constant roll offset here

                return Math.Round(result, 3);
            }
        }

        private double _simNoiseMm = 10.0;
        public double SimNoiseMm
        {
            get { return _simNoiseMm; }
            set { _simNoiseMm = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimNoiseMm, value.ToString()); }
        }

        private double _simLidarYawDeg = 0.0;
        public double SimLidarYawDeg
        {
            get { return _simLidarYawDeg; }
            set { _simLidarYawDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimLidarYawDeg, value.ToString()); }
        }

        private double _simLidarHeightM = 0.8;
        public double SimLidarHeightM
        {
            get { return _simLidarHeightM; }
            set { _simLidarHeightM = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimLidarHeightM, value.ToString()); }
        }

        private int _simPointCount = 50000;
        public int SimPointCount
        {
            get { return _simPointCount; }
            set { _simPointCount = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimPointCount, value.ToString()); }
        }

        private bool _simShowCube1 = true;
        public bool SimShowCube1
        {
            get { return _simShowCube1; }
            set { _simShowCube1 = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimShowCube1, value.ToString()); }
        }

        private bool _simShowCube2 = true;
        public bool SimShowCube2
        {
            get { return _simShowCube2; }
            set { _simShowCube2 = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimShowCube2, value.ToString()); }
        }

        private HelideckShape _simHelideckShape = HelideckShape.Hexagon;
        public HelideckShape SimHelideckShape
        {
            get { return _simHelideckShape; }
            set { _simHelideckShape = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimHelideckShape, value.ToString()); }
        }

        private HelideckShape _helideckShape = HelideckShape.Hexagon;
        public HelideckShape HelideckShape
        {
            get { return _helideckShape; }
            set { _helideckShape = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxHelideckShape, value.ToString()); }
        }

        public IEnumerable<HelideckShape> HelideckShapeOptions => (HelideckShape[])Enum.GetValues(typeof(HelideckShape));

        // Vessel Forward method
        private VesselForwardMethod _vesselFwdMethod = VesselForwardMethod.Automatic;
        public VesselForwardMethod VesselFwdMethod
        {
            get { return _vesselFwdMethod; }
            set { _vesselFwdMethod = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsManualVesselFwd)); _config.Write(ConfigKey.LivoxVesselFwdMethod, value.ToString()); }
        }

        public IEnumerable<VesselForwardMethod> VesselForwardMethodOptions => (VesselForwardMethod[])Enum.GetValues(typeof(VesselForwardMethod));

        public bool IsManualVesselFwd => _vesselFwdMethod == VesselForwardMethod.Manual;

        private double _vesselFwdManualDeg = 0.0;
        public double VesselFwdManualDeg
        {
            get { return _vesselFwdManualDeg; }
            set { _vesselFwdManualDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxVesselFwdManualDeg, value.ToString()); }
        }

        // Minimum requirements for calculations
        private int _minFitPoints = 1000;
        public int MinFitPoints
        {
            get { return _minFitPoints; }
            set { _minFitPoints = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxMinFitPoints, value.ToString()); }
        }

        private int _minEdgePoints = 200;
        public int MinEdgePoints
        {
            get { return _minEdgePoints; }
            set { _minEdgePoints = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxMinEdgePoints, value.ToString()); }
        }

        // Perspective view rotation angles
        private double _perspectiveRotX = -45.0;
        public double PerspectiveRotX
        {
            get { return _perspectiveRotX; }
            set { _perspectiveRotX = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxPerspectiveRotX, value.ToString()); }
        }

        private double _perspectiveRotY = -45.0;
        public double PerspectiveRotY
        {
            get { return _perspectiveRotY; }
            set { _perspectiveRotY = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxPerspectiveRotY, value.ToString()); }
        }

        private double _perspectiveRotZ = 0.0;
        public double PerspectiveRotZ
        {
            get { return _perspectiveRotZ; }
            set { _perspectiveRotZ = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxPerspectiveRotZ, value.ToString()); }
        }

        private bool _enableEmissiveColors = false;
        public bool EnableEmissiveColors
        {
            get { return _enableEmissiveColors; }
            set { _enableEmissiveColors = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxEnableEmissiveColors, value.ToString()); }
        }

        // Resolved vessel forward angle (read-only, for display)
        private string _resolvedVesselFwd = "—";
        public string ResolvedVesselFwd
        {
            get { return _resolvedVesselFwd; }
            private set { _resolvedVesselFwd = value; OnPropertyChanged(); }
        }

        // Analysis busy flag
        private bool _isAnalysing;
        public bool IsAnalysing
        {
            get { return _isAnalysing; }
            private set { _isAnalysing = value; OnPropertyChanged(); }
        }

        private int _analyseProgress;
        public int AnalyseProgress
        {
            get { return _analyseProgress; }
            private set { _analyseProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(AnalyseProgressText)); }
        }
        public string AnalyseProgressText => $"{_analyseProgress} %";

        private string _analyseStepText = "";
        public string AnalyseStepText
        {
            get { return _analyseStepText; }
            private set { _analyseStepText = value; OnPropertyChanged(); }
        }

        public void AppendStatus(string msg)
        {
            _errorHandler.Insert(new ErrorMessage(
                DateTime.Now,
                ErrorMessageType.LivoxLidar,
                ErrorMessageCategory.None,
                msg));
        }

        private string _sdkStatus = "Disconnected";
        public string SdkStatus
        {
            get { return _sdkStatus; }
            set { _sdkStatus = value; OnPropertyChanged(); }
        }

        private int _accumulatedPoints;
        public int AccumulatedPoints
        {
            get { return _accumulatedPoints; }
            set { _accumulatedPoints = value; OnPropertyChanged(); OnPropertyChanged(nameof(AccumulatedPointsString)); }
        }
        public string AccumulatedPointsString => _accumulatedPoints.ToString("N0");

        // Last fit result display
        private string _fitPitch = "—";
        public string FitPitch { get { return _fitPitch; } set { _fitPitch = value; OnPropertyChanged(); } }

        private string _fitRoll = "—";
        public string FitRoll  { get { return _fitRoll;  } set { _fitRoll  = value; OnPropertyChanged(); } }

        private string _fitRmse = "—";
        public string FitRmse  { get { return _fitRmse;  } set { _fitRmse  = value; OnPropertyChanged(); } }

        private string _fitRmseQualityString = "—";
        public string FitRmseQualityString
        {
            get { return _fitRmseQualityString; }
            set { _fitRmseQualityString = value; OnPropertyChanged(); }
        }

        public string FitRmseQualityTooltip => LivoxLidarFitQuality.ThresholdDescription;

        private string _fitPoints = "—";
        public string FitPoints { get { return _fitPoints; } set { _fitPoints = value; OnPropertyChanged(); } }

        private string _fitSlantType = "—";
        public string FitSlantType { get { return _fitSlantType; } set { _fitSlantType = value; OnPropertyChanged(); } }

        private string _fitSlantAngle = "—";
        public string FitSlantAngle { get { return _fitSlantAngle; } set { _fitSlantAngle = value; OnPropertyChanged(); } }

        // Deck edge result display
        private string _edgeDirection = "—";
        public string EdgeDirection { get { return _edgeDirection; } set { _edgeDirection = value; OnPropertyChanged(); } }

        private string _edgeAngle = "—";
        public string EdgeAngle { get { return _edgeAngle; } set { _edgeAngle = value; OnPropertyChanged(); } }

        private string _edgeForwardAngle = "—";
        public string EdgeForwardAngle { get { return _edgeForwardAngle; } set { _edgeForwardAngle = value; OnPropertyChanged(); } }

        private string _edgePointsStr = "—";
        public string EdgePointsStr { get { return _edgePointsStr; } set { _edgePointsStr = value; OnPropertyChanged(); } }

        private string _edgeMethod = "—";
        public string EdgeMethod { get { return _edgeMethod; } set { _edgeMethod = value; OnPropertyChanged(); } }

        // State flags for button enable logic
        public bool CanConnect    => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Disconnected;
        public bool CanDisconnect => _subsystem.CurrentStatus != LivoxLidarSubsystem.Status.Disconnected;
        public bool CanScan       => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Connected;
        public bool IsScanning    => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Scanning;
        public bool CanFit        => _accumulatedPoints >= 20;
        public bool HasFitResult  => _lastFit != null && _lastFit.IsValid;
        public bool HasEdgeResult => _lastEdge != null && _lastEdge.IsValid;
        public bool CanSimulate   => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Disconnected ||
                                     _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Connected;
        public bool IsScanActive  => IsScanning || _simulationInProgress;

        // ── Commands ─────────────────────────────────────────────────────────

        public ICommand ConnectCommand         { get; }
        public ICommand DisconnectCommand      { get; }
        public ICommand StartScanCommand       { get; }
        public ICommand StopScanCommand        { get; }
        public ICommand FitPlaneCommand        { get; }
        public ICommand ApplyCorrectionCommand { get; }
        public ICommand ClearCorrectionCommand { get; }
        public ICommand SimulateScanCommand    { get; }
        public ICommand AnalyseCommand         { get; }

        // ── Command implementations ───────────────────────────────────────────

        private void Connect()
        {
            ApplyFiltersToSubsystem();
            AppendStatus("Connecting...");
            _subsystem.Connect(ConfigFilePath, _errorHandler);
        }

        private void Disconnect()
        {
            _subsystem.Disconnect();
            AppendStatus("Disconnected");
        }

        private void StartScan()
        {
            ApplyFiltersToSubsystem();
            _subsystem.StartScan();
            AppendStatus("Scanning...");
            _lastFit = null;
            _lastEdge = null;
            RefreshFitDisplay();
            RefreshEdgeDisplay();
        }

        private void StopScan()
        {
            _simulationInProgress = false;
            OnPropertyChanged(nameof(IsScanActive));
            _subsystem.StopScan();
            AppendStatus($"Scan stopped. {_accumulatedPoints:N0} points accumulated.");
        }

        private void SimulateScan()
        {
            // Clear all previous scan data
            _lastPointCloudUpdate = DateTime.MinValue;
            AccumulatedPoints = 0;
            _lastFit  = null;
            _lastEdge = null;
            RefreshFitDisplay();
            RefreshEdgeDisplay();
            ScanCleared?.Invoke();

            ApplyFiltersToSubsystem();
            _subsystem.SimPitchDeg    = EffectivePitchDeg;
            _subsystem.SimRollDeg     = EffectiveRollDeg;
            _subsystem.SimDeckSlantDeg = SimDeckSlantDeg;
            _subsystem.SimDeckSlantDirDeg = SimDeckSlantDirDeg;
            _subsystem.SimDeckSlantType = SimDeckSlantType;
            _subsystem.SimNoiseMm     = SimNoiseMm;
            _subsystem.SimLidarYawDeg = SimLidarYawDeg;
            _subsystem.SimLidarHeightMm = SimLidarHeightM * 1000.0; // Convert meters to millimeters
            _subsystem.SimPointCount  = SimPointCount;
            _subsystem.SimShowCube1   = SimShowCube1;
            _subsystem.SimShowCube2   = SimShowCube2;
            _subsystem.SimHelideckShape = SimHelideckShape;
            _subsystem.StartSimulation();
            _simulationInProgress = true;
            OnPropertyChanged(nameof(IsScanActive));
            SimulationStarted?.Invoke();
            AppendStatus($"Simulating helideck scan (pitch={SimPitchDeg:F3}°, roll={SimRollDeg:F3}°, lidar yaw={SimLidarYawDeg:F3}°)...");
        }

        private void FitPlane()
        {
            if (_accumulatedPoints < _minFitPoints)
            {
                string msg = $"Insufficient point cloud: {_accumulatedPoints:N0} pts accumulated, minimum required is {_minFitPoints:N0} pts.\n\nContinue scanning and try again.";
                AppendStatus($"Fit aborted — {_accumulatedPoints:N0} pts, need ≥ {_minFitPoints:N0}.");
                // Clear any prior result so stale values are not shown and Analyse() aborts.
                _lastFit  = null;
                _lastEdge = null;
                RefreshFitDisplay();
                RefreshEdgeDisplay();
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    Telerik.Windows.Controls.RadWindow.Alert(TextHelper.Wrap(msg))));
                return;
            }

            AppendStatus("Fitting plane...");
            _lastFit = _subsystem.FitPlane();

            if (_lastFit == null || !_lastFit.IsValid)
            {
                AppendStatus("Fit failed — not enough points or degenerate surface.");
                _lastFit  = null;
                _lastEdge = null;
                RefreshFitDisplay();
                RefreshEdgeDisplay();
                string msg = "Plane fit failed — not enough points or degenerate surface.\n\nTry scanning longer or adjusting filters.";
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    Telerik.Windows.Controls.RadWindow.Alert(TextHelper.Wrap(msg))));
                return;
            }

            // Validate the post-filter point count against the user-defined minimum.
            // FilterPlaneInliers may discard outliers, dropping the fitted count below the threshold
            // even when the raw accumulated count was sufficient.
            if (_lastFit.PointCount < _minFitPoints)
            {
                string msg = $"Insufficient inlier points after filtering: {_lastFit.PointCount:N0} pts used, minimum required is {_minFitPoints:N0} pts.\n\nContinue scanning and try again.";
                AppendStatus($"Fit rejected — {_lastFit.PointCount:N0} inlier pts, need ≥ {_minFitPoints:N0}.");
                _lastFit  = null;
                _lastEdge = null;
                RefreshFitDisplay();
                RefreshEdgeDisplay();
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    Telerik.Windows.Controls.RadWindow.Alert(TextHelper.Wrap(msg))));
                return;
            }

            RefreshFitDisplay();
            AppendStatus($"Fit OK — RMSE {_lastFit.SurfaceRmse:F1} mm  ({_lastFit.PointCount:N0} pts)");
            OnPropertyChanged(nameof(HasFitResult));

            FitResultReady?.Invoke(_lastFit);
        }

        private void ApplyCorrection()
        {
            if (_lastFit == null || !_lastFit.IsValid) return;

            double heading = ResolveVesselForwardAngle();
            _correction.Apply(_lastFit.PitchDeg, _lastFit.RollDeg,
                              heading, _lastFit.SurfaceRmse, _lastFit.PointCount);

            PersistCorrection();
            AppendStatus("Correction applied to Reference MRU.");
        }

        private void ClearCorrection()
        {
            _correction.Clear();
            PersistCorrection();
            AppendStatus("Correction cleared.");
        }

        private async void Analyse()
        {
            IsAnalysing = true;
            AnalyseProgress = 0;
            AnalyseStepText = $"Fitting plane to {_accumulatedPoints:N0} points…";
            try
            {
                // Yield to let the UI render the busy overlay before blocking
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

                FitPlane();
                AnalyseProgress = 50;
                if (_lastFit == null || !_lastFit.IsValid) return;

                if (_vesselFwdMethod == VesselForwardMethod.Automatic)
                {
                    AnalyseStepText = "Detecting deck edge…";
                    // Yield again so the UI can update between steps
                    await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

                    FindDeckEdge();
                }
                else
                {
                    // Not automatic — clear edge results and refresh display
                    _lastEdge = null;
                    RefreshEdgeDisplay();
                    AppendStatus($"Vessel forward method is {_vesselFwdMethod} — skipping edge detection.");
                }
                AnalyseProgress = 100;
            }
            finally
            {
                IsAnalysing = false;
            }
        }

        private void FindDeckEdge()
        {
            AppendStatus("Detecting deck edge...");
            var snapshot = _subsystem.GetPointCloudSnapshot();
            int snapshotCount = snapshot != null ? snapshot.Count : 0;

            // Early gate: the snapshot itself must hold at least the user-defined
            // minimum number of points before edge detection is feasible.
            if (snapshotCount < _minEdgePoints)
            {
                string msg = $"Insufficient point cloud for edge detection: {snapshotCount:N0} pts available, minimum required is {_minEdgePoints:N0} pts.\n\nContinue scanning and try again.";
                AppendStatus($"Edge detection aborted — {snapshotCount:N0} pts, need ≥ {_minEdgePoints:N0}.");
                _lastEdge = null;
                RefreshEdgeDisplay();
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    Telerik.Windows.Controls.RadWindow.Alert(TextHelper.Wrap(msg))));
                return;
            }

            _lastEdge = LivoxLidarDeckEdgeFinder.FindEdge(snapshot, HelideckShape, _minEdgePoints);

            if (_lastEdge == null || !_lastEdge.IsValid)
            {
                string msg = $"Edge detection failed — not enough edge points.";
                if (_lastEdge != null)
                    msg = $"Edge detection failed: only {_lastEdge.EdgePointCount:N0} edge pts found, minimum required is {_minEdgePoints:N0}.\n\nTry scanning closer to the deck edge or increasing scan duration.";
                AppendStatus("Edge detection failed — not enough forward deck points or degenerate geometry.");
                _lastEdge = null;
                RefreshEdgeDisplay();
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    Telerik.Windows.Controls.RadWindow.Alert(TextHelper.Wrap(msg))));
                return;
            }

            if (_lastEdge.EdgePointCount < _minEdgePoints)
            {
                string msg = $"Edge detection rejected: only {_lastEdge.EdgePointCount:N0} edge pts found, minimum required is {_minEdgePoints:N0}.\n\nTry scanning closer to the deck edge or increasing scan duration.";
                AppendStatus($"Edge rejected — {_lastEdge.EdgePointCount:N0} edge pts, need ≥ {_minEdgePoints:N0}.");
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    Telerik.Windows.Controls.RadWindow.Alert(TextHelper.Wrap(msg))));
                _lastEdge = null;
                RefreshEdgeDisplay();
                return;
            }

            RefreshEdgeDisplay();
            AppendStatus($"Edge OK — {_lastEdge.EdgePointCount:N0} edge pts, angle {_lastEdge.EdgeAngleDeg:F3}°, " +
                         $"vessel fwd {_lastEdge.VesselForwardAngleDeg:F3}° " +
                         $"({_lastEdge.DeckInlierCount:N0} deck inliers, hull {_lastEdge.HullVertexCount} vertices)");
            OnPropertyChanged(nameof(HasEdgeResult));

            // Refine pitch/roll using the edge detector's more accurate vessel forward.
            // The plane fitter's internal bow-edge PCA can be imprecise; the deck edge
            // finder uses RANSAC + convex hull and produces a better yaw estimate.
            if (_lastFit != null && _lastFit.IsValid)
            {
                double fwdX = _lastEdge.VesselForwardX;
                double fwdY = _lastEdge.VesselForwardY;
                double fhLen = Math.Sqrt(fwdX * fwdX + fwdY * fwdY);
                if (fhLen > 1e-6)
                {
                    double fhx = fwdX / fhLen, fhy = fwdY / fhLen;
                    double lhx = -fhy, lhy = fhx; // horizontal lateral (90° CCW of forward)
                    double nx = _lastFit.NormalX, ny = _lastFit.NormalY, nz = _lastFit.NormalZ;
                    const double R2D = 180.0 / Math.PI;
                    _lastFit.PitchDeg = Math.Atan2(nx * fhx + ny * fhy, nz) * R2D;
                    _lastFit.RollDeg  = Math.Atan2(nx * lhx + ny * lhy, nz) * R2D;
                    RefreshFitDisplay();
                    AppendStatus($"Plane fit (refined) — Pitch: {_lastFit.PitchDeg:+0.000;-0.000;0.000}°" +
                                 $"  Roll: {_lastFit.RollDeg:+0.000;-0.000;0.000}°");
                }
            }

            DeckEdgeResultReady?.Invoke(_lastEdge);
        }

        // ── Fit result event (for 3D view) ────────────────────────────────────

        public event Action<LivoxLidarPlaneFitResult> FitResultReady;
        public event Action<LivoxLidarDeckEdgeResult> DeckEdgeResultReady;
        public event Action ScanCleared;
        public event Action SimulationStarted;
        public event Action<List<(float x, float y, float z)>> PointCloudUpdated;

        private DateTime _lastPointCloudUpdate = DateTime.MinValue;
        private static readonly TimeSpan PointCloudUpdateInterval = TimeSpan.FromMilliseconds(500);

        public List<(float x, float y, float z)> GetPointCloudSnapshot()
            => _subsystem.GetPointCloudSnapshot();

        // ── Subsystem event handlers ──────────────────────────────────────────

        private void OnStatusChanged(LivoxLidarSubsystem.Status status)
        {
            Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                SdkStatus = status.ToString();
                    if (_simulationInProgress && status == LivoxLidarSubsystem.Status.Disconnected)
                    {
                        _simulationInProgress = false;
                        AppendStatus($"Simulation complete. {_accumulatedPoints:N0} points accumulated.");
                        if (!string.IsNullOrEmpty(_subsystem.LastSimulationDiagnostics))
                            AppendStatus($"  Face distribution: {_subsystem.LastSimulationDiagnostics}");

                        // Force a final point-cloud update so the 3D view always
                        // reflects the complete scan, even if the throttle skipped
                        // the last batch during simulation.
                        var finalSnapshot = _subsystem.GetPointCloudSnapshot();
                        if (finalSnapshot != null && finalSnapshot.Count > 0)
                            PointCloudUpdated?.Invoke(finalSnapshot);
                    }
                    RefreshCommandStates();
            }));
        }

        private void OnPointCountUpdated(int count)
        {
            var now = DateTime.UtcNow;
            bool fireUpdate = (now - _lastPointCloudUpdate) >= PointCloudUpdateInterval;
            if (fireUpdate) _lastPointCloudUpdate = now;

            List<(float x, float y, float z)> snapshot = fireUpdate ? _subsystem.GetPointCloudSnapshot() : null;

            Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AccumulatedPoints = count;
                OnPropertyChanged(nameof(CanFit));
                if (snapshot != null)
                    PointCloudUpdated?.Invoke(snapshot);
            }));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshFitDisplay()
        {
            if (_lastFit != null && _lastFit.IsValid)
            {
                FitPitch   = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3}°", _lastFit.PitchDeg);
                FitRoll    = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3}°", _lastFit.RollDeg);
                FitRmse    = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1} mm", _lastFit.SurfaceRmse);
                FitPoints  = _lastFit.PointCount.ToString("N0");
                FitRmseQualityString = LivoxLidarFitQuality.Label(LivoxLidarFitQuality.Classify(_lastFit.SurfaceRmse));
                FitSlantType = _lastFit.DetectedSlantType.ToString();
                FitSlantAngle = _lastFit.DetectedSlantType == DeckSlantType.Flat
                    ? "—"
                    : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3}°", _lastFit.DetectedSlantDeg);
            }
            else
            {
                FitPitch = FitRoll = FitRmse = FitPoints = "—";
                FitSlantType = FitSlantAngle = "—";
                FitRmseQualityString = "—";
            }
            OnPropertyChanged(nameof(HasFitResult));
            // CommandManager.RequerySuggested only fires on UI focus changes, so commands
            // bound via HasFitResult (Apply Correction) wouldn't re-evaluate after Analyse
            // completes until the user interacts with the window. Force a requery here.
            CommandManager.InvalidateRequerySuggested();
        }

        private void RefreshEdgeDisplay()
        {
            if (_lastEdge != null && _lastEdge.IsValid)
            {
                EdgeDirection    = string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0:F3}, {1:F3}, {2:F3})", _lastEdge.DirectionX, _lastEdge.DirectionY, _lastEdge.DirectionZ);
                EdgeAngle        = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3}°", _lastEdge.EdgeAngleDeg);
                EdgeForwardAngle = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3}°", _lastEdge.VesselForwardAngleDeg);
                EdgePointsStr    = _lastEdge.EdgePointCount.ToString("N0");
                EdgeMethod       = _lastEdge.DetectionMethod;
            }
            else
            {
                EdgeDirection = EdgeAngle = EdgeForwardAngle = EdgePointsStr = EdgeMethod = "—";
            }
            OnPropertyChanged(nameof(HasEdgeResult));
            RefreshResolvedVesselFwd();
        }

        private double ResolveVesselForwardAngle()
        {
            switch (_vesselFwdMethod)
            {
                case VesselForwardMethod.LidarForward:
                    return 0.0;
                case VesselForwardMethod.Manual:
                    return _vesselFwdManualDeg;
                default: // Automatic
                    return _lastEdge != null && _lastEdge.IsValid
                        ? _lastEdge.VesselForwardAngleDeg
                        : 0.0;
            }
        }

        private void RefreshResolvedVesselFwd()
        {
            double angle = ResolveVesselForwardAngle();
            string source;
            switch (_vesselFwdMethod)
            {
                case VesselForwardMethod.LidarForward:
                    source = "lidar fwd";
                    break;
                case VesselForwardMethod.Manual:
                    source = "manual";
                    break;
                default:
                    source = _lastEdge != null && _lastEdge.IsValid ? "auto" : "default";
                    break;
            }
            ResolvedVesselFwd = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3}° ({1})", angle, source);
        }

        private void RefreshCommandStates()
        {
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanDisconnect));
            OnPropertyChanged(nameof(CanScan));
            OnPropertyChanged(nameof(IsScanning));
            OnPropertyChanged(nameof(CanSimulate));
            OnPropertyChanged(nameof(IsScanActive));
        }

        private void ApplyFiltersToSubsystem()
        {
            _subsystem.RangeMinMm      = RangeMinMm;
            _subsystem.RangeMaxMm      = RangeMaxMm;
            _subsystem.AzimuthMinDeg   = AzimuthMinDeg;
            _subsystem.AzimuthMaxDeg   = AzimuthMaxDeg;
            _subsystem.ElevationMinDeg = ElevationMinDeg;
            _subsystem.ElevationMaxDeg = ElevationMaxDeg;
            _subsystem.MaxBufferPoints = MaxBufferPoints;
        }

        // ── Config persistence ────────────────────────────────────────────────

        private void LoadConfig()
        {
            ConfigFilePath  = _config.ReadWithDefault(ConfigKey.LivoxConfigFilePath, @"Config\LivoxLidar\mid360_config.json");
            IpAddress       = _config.ReadWithDefault(ConfigKey.LivoxIpAddress,      "192.168.1.3");

            RangeMinMm      = (float)_config.ReadWithDefault(ConfigKey.LivoxRangeMinMm,      500.0);
            RangeMaxMm      = (float)_config.ReadWithDefault(ConfigKey.LivoxRangeMaxMm,      30000.0);
            AzimuthMinDeg   = (float)_config.ReadWithDefault(ConfigKey.LivoxAzimuthMinDeg,   -180.0);
            AzimuthMaxDeg   = (float)_config.ReadWithDefault(ConfigKey.LivoxAzimuthMaxDeg,   180.0);
            ElevationMinDeg = (float)_config.ReadWithDefault(ConfigKey.LivoxElevationMinDeg, -90.0);
            ElevationMaxDeg = (float)_config.ReadWithDefault(ConfigKey.LivoxElevationMaxDeg, 90.0);
            MaxBufferPoints = _config.ReadWithDefault(ConfigKey.LivoxMaxBufferPoints, 500000);

            SimPitchDeg    = _config.ReadWithDefault(ConfigKey.LivoxSimPitchDeg,    2.0);
            SimRollDeg     = _config.ReadWithDefault(ConfigKey.LivoxSimRollDeg,     1.5);
            SimDeckSlantDeg = _config.ReadWithDefault(ConfigKey.LivoxSimDeckSlantDeg, 0.0);
            SimDeckSlantDirDeg = _config.ReadWithDefault(ConfigKey.LivoxSimDeckSlantDirDeg, 0.0);
            var slantTypeStr = _config.ReadWithDefault(ConfigKey.LivoxSimDeckSlantType, "Flat");
            DeckSlantType parsedSlantType;
            SimDeckSlantType = Enum.TryParse(slantTypeStr, out parsedSlantType) ? parsedSlantType : DeckSlantType.Flat;
            SimNoiseMm     = _config.ReadWithDefault(ConfigKey.LivoxSimNoiseMm,     10.0);
            SimLidarYawDeg = _config.ReadWithDefault(ConfigKey.LivoxSimLidarYawDeg, 0.0);
            SimLidarHeightM = _config.ReadWithDefault(ConfigKey.LivoxSimLidarHeightM, 0.8);
            SimPointCount  = _config.ReadWithDefault(ConfigKey.LivoxSimPointCount,  50000);
            bool cube1;
            SimShowCube1   = bool.TryParse(_config.ReadWithDefault(ConfigKey.LivoxSimShowCube1, "True"), out cube1) ? cube1 : true;
            bool cube2;
            SimShowCube2   = bool.TryParse(_config.ReadWithDefault(ConfigKey.LivoxSimShowCube2, "True"), out cube2) ? cube2 : true;
            var shapeStr   = _config.ReadWithDefault(ConfigKey.LivoxSimHelideckShape, "Hexagon");
            HelideckShape parsedShape;
            SimHelideckShape = Enum.TryParse(shapeStr, out parsedShape) ? parsedShape : HelideckShape.Hexagon;
            MaxDisplayPoints = _config.ReadWithDefault(ConfigKey.LivoxMaxDisplayPoints, 20000);
            var deckShapeStr = _config.ReadWithDefault(ConfigKey.LivoxHelideckShape, "Hexagon");
            HelideckShape parsedDeckShape;
            HelideckShape = Enum.TryParse(deckShapeStr, out parsedDeckShape) ? parsedDeckShape : HelideckShape.Hexagon;

            var fwdMethodStr = _config.ReadWithDefault(ConfigKey.LivoxVesselFwdMethod, "Automatic");
            VesselForwardMethod parsedFwdMethod;
            VesselFwdMethod = Enum.TryParse(fwdMethodStr, out parsedFwdMethod) ? parsedFwdMethod : VesselForwardMethod.Automatic;
            VesselFwdManualDeg = _config.ReadWithDefault(ConfigKey.LivoxVesselFwdManualDeg, 0.0);
            MinFitPoints  = _config.ReadWithDefault(ConfigKey.LivoxMinFitPoints,  1000);
            MinEdgePoints = _config.ReadWithDefault(ConfigKey.LivoxMinEdgePoints, 200);
            PerspectiveRotX = _config.ReadWithDefault(ConfigKey.LivoxPerspectiveRotX, -45.0);
            PerspectiveRotY = _config.ReadWithDefault(ConfigKey.LivoxPerspectiveRotY, -45.0);
            PerspectiveRotZ = _config.ReadWithDefault(ConfigKey.LivoxPerspectiveRotZ, 0.0);
            bool emissive;
            EnableEmissiveColors = bool.TryParse(_config.ReadWithDefault(ConfigKey.LivoxEnableEmissiveColors, "False"), out emissive) ? emissive : false;

            // Do not restore persisted correction on startup — start with a clean state.
        }

        private void PersistCorrection()
        {
            _config.Write(ConfigKey.LivoxCorrectionActive,    _correction.IsActive.ToString());
            _config.Write(ConfigKey.LivoxCorrectionPitch,     _correction.PitchOffset.ToString());
            _config.Write(ConfigKey.LivoxCorrectionRoll,      _correction.RollOffset.ToString());
            _config.Write(ConfigKey.LivoxCorrectionHeading,   _correction.HeadingOffset.ToString());
            _config.Write(ConfigKey.LivoxCorrectionTimestamp, _correction.IsActive ? _correction.Timestamp.ToString("O") : "");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Minimal RelayCommand implementation to avoid adding MVVM framework dependency
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        public event EventHandler CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object p) => _canExecute == null || _canExecute(p);
        public void Execute(object p)    => _execute(p);
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            set { _simPitchDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimPitchDeg, value.ToString()); }
        }

        private double _simRollDeg = 1.5;
        public double SimRollDeg
        {
            get { return _simRollDeg; }
            set { _simRollDeg = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimRollDeg, value.ToString()); }
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

        private int _simPointCount = 50000;
        public int SimPointCount
        {
            get { return _simPointCount; }
            set { _simPointCount = value; OnPropertyChanged(); _config.Write(ConfigKey.LivoxSimPointCount, value.ToString()); }
        }

        // Status / readouts
        private readonly System.Text.StringBuilder _statusLog = new System.Text.StringBuilder();
        public string StatusMessage => _statusLog.ToString();

        private void AppendStatus(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            if (_statusLog.Length > 0) _statusLog.AppendLine();
            _statusLog.Append(line);
            OnPropertyChanged(nameof(StatusMessage));
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

        private string _fitPoints = "—";
        public string FitPoints { get { return _fitPoints; } set { _fitPoints = value; OnPropertyChanged(); } }

        // Deck edge result display
        private string _edgeDirection = "—";
        public string EdgeDirection { get { return _edgeDirection; } set { _edgeDirection = value; OnPropertyChanged(); } }

        private string _edgeAngle = "—";
        public string EdgeAngle { get { return _edgeAngle; } set { _edgeAngle = value; OnPropertyChanged(); } }

        private string _edgeForwardAngle = "—";
        public string EdgeForwardAngle { get { return _edgeForwardAngle; } set { _edgeForwardAngle = value; OnPropertyChanged(); } }

        private string _edgePointsStr = "—";
        public string EdgePointsStr { get { return _edgePointsStr; } set { _edgePointsStr = value; OnPropertyChanged(); } }

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
            _statusLog.Clear();
            OnPropertyChanged(nameof(StatusMessage));
            RefreshFitDisplay();
            RefreshEdgeDisplay();
            ScanCleared?.Invoke();

            ApplyFiltersToSubsystem();
            _subsystem.SimPitchDeg    = SimPitchDeg;
            _subsystem.SimRollDeg     = SimRollDeg;
            _subsystem.SimNoiseMm     = SimNoiseMm;
            _subsystem.SimLidarYawDeg = SimLidarYawDeg;
            _subsystem.SimPointCount  = SimPointCount;
            _subsystem.StartSimulation();
            _simulationInProgress = true;
            AppendStatus($"Simulating helideck scan (pitch={SimPitchDeg:F1}°, roll={SimRollDeg:F1}°, lidar yaw={SimLidarYawDeg:F1}°)...");
        }

        private void FitPlane()
        {
            AppendStatus("Fitting plane...");
            _lastFit = _subsystem.FitPlane();

            if (_lastFit == null || !_lastFit.IsValid)
            {
                AppendStatus("Fit failed — not enough points or degenerate surface.");
                RefreshFitDisplay();
                return;
            }

            RefreshFitDisplay();
            AppendStatus($"Fit OK — RMSE {_lastFit.FitRmse:F1} mm  ({_lastFit.PointCount:N0} pts)");
            OnPropertyChanged(nameof(HasFitResult));

            FitResultReady?.Invoke(_lastFit);
        }

        private void ApplyCorrection()
        {
            if (_lastFit == null || !_lastFit.IsValid) return;

            double heading = _lastEdge != null && _lastEdge.IsValid
                           ? _lastEdge.VesselForwardAngleDeg
                           : 0.0;
            _correction.Apply(_lastFit.PitchDeg, _lastFit.RollDeg,
                              heading, _lastFit.FitRmse, _lastFit.PointCount);

            PersistCorrection();
            AppendStatus("Correction applied to Reference MRU.");
        }

        private void ClearCorrection()
        {
            _correction.Clear();
            PersistCorrection();
            AppendStatus("Correction cleared.");
        }

        private void Analyse()
        {
            FitPlane();
            if (_lastFit == null || !_lastFit.IsValid) return;
            FindDeckEdge();
        }

        private void FindDeckEdge()
        {
            AppendStatus("Detecting deck edge...");
            var snapshot = _subsystem.GetPointCloudSnapshot();
            _lastEdge = LivoxLidarDeckEdgeFinder.FindEdge(snapshot);

            if (_lastEdge == null || !_lastEdge.IsValid)
            {
                AppendStatus("Edge detection failed — not enough forward deck points or degenerate geometry.");
                RefreshEdgeDisplay();
                return;
            }

            RefreshEdgeDisplay();
            AppendStatus($"Edge OK — {_lastEdge.EdgePointCount:N0} edge pts, angle {_lastEdge.EdgeAngleDeg:F1}°, " +
                         $"vessel fwd {_lastEdge.VesselForwardAngleDeg:F1}° " +
                         $"({_lastEdge.DeckInlierCount:N0} deck inliers, hull {_lastEdge.HullVertexCount} vertices)");
            OnPropertyChanged(nameof(HasEdgeResult));

            DeckEdgeResultReady?.Invoke(_lastEdge);
        }

        // ── Fit result event (for 3D view) ────────────────────────────────────

        public event Action<LivoxLidarPlaneFitResult> FitResultReady;
        public event Action<LivoxLidarDeckEdgeResult> DeckEdgeResultReady;
        public event Action ScanCleared;
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
                FitPitch   = $"{_lastFit.PitchDeg:F3}°";
                FitRoll    = $"{_lastFit.RollDeg:F3}°";
                FitRmse    = $"{_lastFit.FitRmse:F1} mm";
                FitPoints  = _lastFit.PointCount.ToString("N0");
            }
            else
            {
                FitPitch = FitRoll = FitRmse = FitPoints = "—";
            }
            OnPropertyChanged(nameof(HasFitResult));
        }

        private void RefreshEdgeDisplay()
        {
            if (_lastEdge != null && _lastEdge.IsValid)
            {
                EdgeDirection    = $"({_lastEdge.DirectionX:F3}, {_lastEdge.DirectionY:F3}, {_lastEdge.DirectionZ:F3})";
                EdgeAngle        = $"{_lastEdge.EdgeAngleDeg:F3}°";
                EdgeForwardAngle = $"{_lastEdge.VesselForwardAngleDeg:F3}°";
                EdgePointsStr    = _lastEdge.EdgePointCount.ToString("N0");
            }
            else
            {
                EdgeDirection = EdgeAngle = EdgeForwardAngle = EdgePointsStr = "—";
            }
            OnPropertyChanged(nameof(HasEdgeResult));
        }

        private void RefreshCommandStates()
        {
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanDisconnect));
            OnPropertyChanged(nameof(CanScan));
            OnPropertyChanged(nameof(IsScanning));
            OnPropertyChanged(nameof(CanSimulate));
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
            SimNoiseMm     = _config.ReadWithDefault(ConfigKey.LivoxSimNoiseMm,     10.0);
            SimLidarYawDeg = _config.ReadWithDefault(ConfigKey.LivoxSimLidarYawDeg, 0.0);
            SimPointCount  = _config.ReadWithDefault(ConfigKey.LivoxSimPointCount,  50000);
            MaxDisplayPoints = _config.ReadWithDefault(ConfigKey.LivoxMaxDisplayPoints, 20000);

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

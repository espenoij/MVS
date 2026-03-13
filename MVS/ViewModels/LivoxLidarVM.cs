using System;
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
            _subsystem.ErrorOccurred     += msg => StatusMessage = msg;

            // Expose the shared correction object for direct binding
            Correction = correction;

            // Commands
            ConnectCommand          = new RelayCommand(_ => Connect(),     _ => CanConnect);
            DisconnectCommand       = new RelayCommand(_ => Disconnect(),   _ => CanDisconnect);
            StartScanCommand        = new RelayCommand(_ => StartScan(),    _ => CanScan);
            StopScanCommand         = new RelayCommand(_ => StopScan(),     _ => IsScanning);
            FitPlaneCommand         = new RelayCommand(_ => FitPlane(),     _ => CanFit);
            ApplyCorrectionCommand  = new RelayCommand(_ => ApplyCorrection(), _ => HasFitResult);
            ClearCorrectionCommand  = new RelayCommand(_ => ClearCorrection());
        }

        // ── Bound properties ─────────────────────────────────────────────────

        public LivoxLidarCorrection Correction { get; }

        private string _configFilePath;
        public string ConfigFilePath
        {
            get { return _configFilePath; }
            set { _configFilePath = value; OnPropertyChanged(); SaveConfig(); }
        }

        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; OnPropertyChanged(); SaveConfig(); }
        }

        private float _rangeMinMm = 500f;
        public float RangeMinMm
        {
            get { return _rangeMinMm; }
            set { _rangeMinMm = value; OnPropertyChanged(); SaveConfig(); }
        }

        private float _rangeMaxMm = 30000f;
        public float RangeMaxMm
        {
            get { return _rangeMaxMm; }
            set { _rangeMaxMm = value; OnPropertyChanged(); SaveConfig(); }
        }

        private float _azimuthMinDeg = -180f;
        public float AzimuthMinDeg
        {
            get { return _azimuthMinDeg; }
            set { _azimuthMinDeg = value; OnPropertyChanged(); SaveConfig(); }
        }

        private float _azimuthMaxDeg = 180f;
        public float AzimuthMaxDeg
        {
            get { return _azimuthMaxDeg; }
            set { _azimuthMaxDeg = value; OnPropertyChanged(); SaveConfig(); }
        }

        private float _elevationMinDeg = -90f;
        public float ElevationMinDeg
        {
            get { return _elevationMinDeg; }
            set { _elevationMinDeg = value; OnPropertyChanged(); SaveConfig(); }
        }

        private float _elevationMaxDeg = 90f;
        public float ElevationMaxDeg
        {
            get { return _elevationMaxDeg; }
            set { _elevationMaxDeg = value; OnPropertyChanged(); SaveConfig(); }
        }

        // Status / readouts
        private string _statusMessage = "Disconnected";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged(); }
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

        private string _fitHeading = "—";
        public string FitHeading { get { return _fitHeading; } set { _fitHeading = value; OnPropertyChanged(); } }

        private string _fitRmse = "—";
        public string FitRmse  { get { return _fitRmse;  } set { _fitRmse  = value; OnPropertyChanged(); } }

        private string _fitPoints = "—";
        public string FitPoints { get { return _fitPoints; } set { _fitPoints = value; OnPropertyChanged(); } }

        // State flags for button enable logic
        public bool CanConnect    => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Disconnected;
        public bool CanDisconnect => _subsystem.CurrentStatus != LivoxLidarSubsystem.Status.Disconnected;
        public bool CanScan       => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Connected;
        public bool IsScanning    => _subsystem.CurrentStatus == LivoxLidarSubsystem.Status.Scanning;
        public bool CanFit        => _accumulatedPoints >= 20;
        public bool HasFitResult  => _lastFit != null && _lastFit.IsValid;

        // ── Commands ─────────────────────────────────────────────────────────

        public ICommand ConnectCommand         { get; }
        public ICommand DisconnectCommand      { get; }
        public ICommand StartScanCommand       { get; }
        public ICommand StopScanCommand        { get; }
        public ICommand FitPlaneCommand        { get; }
        public ICommand ApplyCorrectionCommand { get; }
        public ICommand ClearCorrectionCommand { get; }

        // ── Command implementations ───────────────────────────────────────────

        private void Connect()
        {
            ApplyFiltersToSubsystem();
            StatusMessage = "Connecting...";
            _subsystem.Connect(ConfigFilePath, _errorHandler);
        }

        private void Disconnect()
        {
            _subsystem.Disconnect();
            StatusMessage = "Disconnected";
        }

        private void StartScan()
        {
            ApplyFiltersToSubsystem();
            _subsystem.StartScan();
            StatusMessage = "Scanning...";
            _lastFit = null;
            RefreshFitDisplay();
        }

        private void StopScan()
        {
            _subsystem.StopScan();
            StatusMessage = $"Scan stopped. {_accumulatedPoints:N0} points accumulated.";
        }

        private void FitPlane()
        {
            StatusMessage = "Fitting plane...";
            _lastFit = _subsystem.FitPlane();

            if (_lastFit == null || !_lastFit.IsValid)
            {
                StatusMessage = "Fit failed — not enough points or degenerate surface.";
                RefreshFitDisplay();
                return;
            }

            RefreshFitDisplay();
            StatusMessage = $"Fit OK — RMSE {_lastFit.FitRmse:F1} mm  ({_lastFit.PointCount:N0} pts)";
            OnPropertyChanged(nameof(HasFitResult));

            FitResultReady?.Invoke(_lastFit);
        }

        private void ApplyCorrection()
        {
            if (_lastFit == null || !_lastFit.IsValid) return;

            _correction.Apply(_lastFit.PitchDeg, _lastFit.RollDeg,
                              _lastFit.HeadingDeg, _lastFit.FitRmse, _lastFit.PointCount);

            PersistCorrection();
            StatusMessage = "Correction applied to Reference MRU.";
        }

        private void ClearCorrection()
        {
            _correction.Clear();
            PersistCorrection();
            StatusMessage = "Correction cleared.";
        }

        // ── Fit result event (for 3D view) ────────────────────────────────────

        public event Action<LivoxLidarPlaneFitResult> FitResultReady;

        // ── Subsystem event handlers ──────────────────────────────────────────

        private void OnStatusChanged(LivoxLidarSubsystem.Status status)
        {
            Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                SdkStatus = status.ToString();
                RefreshCommandStates();
            }));
        }

        private void OnPointCountUpdated(int count)
        {
            Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AccumulatedPoints = count;
                OnPropertyChanged(nameof(CanFit));
            }));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshFitDisplay()
        {
            if (_lastFit != null && _lastFit.IsValid)
            {
                FitPitch   = $"{_lastFit.PitchDeg:F3}°";
                FitRoll    = $"{_lastFit.RollDeg:F3}°";
                FitHeading = $"{_lastFit.HeadingDeg:F1}°";
                FitRmse    = $"{_lastFit.FitRmse:F1} mm";
                FitPoints  = _lastFit.PointCount.ToString("N0");
            }
            else
            {
                FitPitch = FitRoll = FitHeading = FitRmse = FitPoints = "—";
            }
            OnPropertyChanged(nameof(HasFitResult));
        }

        private void RefreshCommandStates()
        {
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanDisconnect));
            OnPropertyChanged(nameof(CanScan));
            OnPropertyChanged(nameof(IsScanning));
        }

        private void ApplyFiltersToSubsystem()
        {
            _subsystem.RangeMinMm      = RangeMinMm;
            _subsystem.RangeMaxMm      = RangeMaxMm;
            _subsystem.AzimuthMinDeg   = AzimuthMinDeg;
            _subsystem.AzimuthMaxDeg   = AzimuthMaxDeg;
            _subsystem.ElevationMinDeg = ElevationMinDeg;
            _subsystem.ElevationMaxDeg = ElevationMaxDeg;
        }

        // ── Config persistence ────────────────────────────────────────────────

        private void LoadConfig()
        {
            try
            {
                var cfg = _config.GetLivoxLidarSystemConfig();
                if (cfg == null) { SetDefaults(); return; }

                ConfigFilePath  = cfg.ConfigFilePath;
                IpAddress       = cfg.IpAddress;

                float v;
                if (float.TryParse(cfg.RangeMinMm,      out v)) RangeMinMm      = v;
                if (float.TryParse(cfg.RangeMaxMm,      out v)) RangeMaxMm      = v;
                if (float.TryParse(cfg.AzimuthMinDeg,   out v)) AzimuthMinDeg   = v;
                if (float.TryParse(cfg.AzimuthMaxDeg,   out v)) AzimuthMaxDeg   = v;
                if (float.TryParse(cfg.ElevationMinDeg, out v)) ElevationMinDeg = v;
                if (float.TryParse(cfg.ElevationMaxDeg, out v)) ElevationMaxDeg = v;

                // Restore persisted correction
                if (bool.TryParse(cfg.CorrectionActive, out bool active) && active)
                {
                    double p, r, h;
                    if (double.TryParse(cfg.CorrectionPitch,   System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out p) &&
                        double.TryParse(cfg.CorrectionRoll,    System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out r) &&
                        double.TryParse(cfg.CorrectionHeading, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out h))
                    {
                        _correction.Apply(p, r, h, 0, 0);
                        if (DateTime.TryParse(cfg.CorrectionTimestamp, out DateTime ts))
                            _correction.Timestamp = ts;
                    }
                }
            }
            catch { SetDefaults(); }
        }

        private void SetDefaults()
        {
            ConfigFilePath = @"Config\LivoxLidar\mid360_config.json";
            IpAddress      = "192.168.1.3";
        }

        private void SaveConfig()
        {
            try { _config.SetLivoxLidarSystemConfig(BuildConfigObj()); }
            catch { }
        }

        private void PersistCorrection()
        {
            try
            {
                var cfg = BuildConfigObj();
                cfg.CorrectionActive    = _correction.IsActive.ToString();
                cfg.CorrectionPitch     = _correction.PitchOffset.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture);
                cfg.CorrectionRoll      = _correction.RollOffset.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture);
                cfg.CorrectionHeading   = _correction.HeadingOffset.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture);
                cfg.CorrectionTimestamp = _correction.Timestamp.ToString("O");
                _config.SetLivoxLidarSystemConfig(cfg);
            }
            catch { }
        }

        private LivoxLidarSystemConfig BuildConfigObj()
        {
            var cfg = new LivoxLidarSystemConfig
            {
                ConfigFilePath  = ConfigFilePath ?? "",
                IpAddress       = IpAddress ?? "",
                RangeMinMm      = RangeMinMm.ToString(System.Globalization.CultureInfo.InvariantCulture),
                RangeMaxMm      = RangeMaxMm.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AzimuthMinDeg   = AzimuthMinDeg.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AzimuthMaxDeg   = AzimuthMaxDeg.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ElevationMinDeg = ElevationMinDeg.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ElevationMaxDeg = ElevationMaxDeg.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            return cfg;
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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    /// <summary>
    /// Holds the active helideck correction derived from a LiDAR plane fit.
    /// Shared between LivoxLidarSubsystem (writer) and MVSProcessingMotion (reader).
    /// </summary>
    public class LivoxLidarCorrection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Correction values ────────────────────────────────────────────────

        private double _pitchOffset;
        /// <summary>Pitch offset to subtract from the corrected Reference MRU (degrees).</summary>
        public double PitchOffset
        {
            get { return _pitchOffset; }
            set { _pitchOffset = value; OnPropertyChanged(); OnPropertyChanged(nameof(PitchOffsetString)); }
        }
        public string PitchOffsetString => IsActive ? $"{_pitchOffset:F3}°" : "—";

        private double _rollOffset;
        /// <summary>Roll offset to subtract from the corrected Reference MRU (degrees).</summary>
        public double RollOffset
        {
            get { return _rollOffset; }
            set { _rollOffset = value; OnPropertyChanged(); OnPropertyChanged(nameof(RollOffsetString)); }
        }
        public string RollOffsetString => IsActive ? $"{_rollOffset:F3}°" : "—";

        private double _headingOffset;
        /// <summary>
        /// Heading offset (degrees). Rotates the MRU pitch/roll axes to align with the
        /// helideck bow–stern / port–starboard axes before the pitch/roll offsets are applied.
        /// </summary>
        public double HeadingOffset
        {
            get { return _headingOffset; }
            set { _headingOffset = value; OnPropertyChanged(); OnPropertyChanged(nameof(HeadingOffsetString)); }
        }
        public string HeadingOffsetString => IsActive ? $"{_headingOffset:F3}°" : "—";

        // ── Fit quality metadata ─────────────────────────────────────────────

        private double _fitRmse;
        /// <summary>RMSE of the plane fit in millimetres (= √λ_min).</summary>
        public double FitRmse
        {
            get { return _fitRmse; }
            set { _fitRmse = value; OnPropertyChanged(); OnPropertyChanged(nameof(FitRmseString)); }
        }
        public string FitRmseString => IsActive ? $"{_fitRmse:F1} mm" : "—";

        private int _pointCount;
        /// <summary>Number of points used in the fit.</summary>
        public int PointCount
        {
            get { return _pointCount; }
            set { _pointCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(PointCountString)); }
        }
        public string PointCountString => IsActive ? _pointCount.ToString("N0") : "—";

        // ── State ────────────────────────────────────────────────────────────

        private bool _isActive;
        /// <summary>True when a valid correction has been applied and is in use.</summary>
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusString));
                OnPropertyChanged(nameof(PitchOffsetString));
                OnPropertyChanged(nameof(RollOffsetString));
                OnPropertyChanged(nameof(HeadingOffsetString));
                OnPropertyChanged(nameof(FitRmseString));
                OnPropertyChanged(nameof(PointCountString));
                OnPropertyChanged(nameof(TimestampString));
            }
        }
        public string StatusString => _isActive ? "Active" : "Not active";

        private DateTime _timestamp = DateTime.MinValue;
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimestampString)); }
        }
        public string TimestampString =>
            (_isActive && _timestamp != DateTime.MinValue)
                ? _timestamp.ToString(Constants.TimestampFormat, Constants.cultureInfo)
                : "—";

        // ── Mutators ─────────────────────────────────────────────────────────

        public void Apply(double pitchOffset, double rollOffset, double headingOffset,
                          double fitRmse, int pointCount)
        {
            _isActive     = true;
            PitchOffset   = pitchOffset;
            RollOffset    = rollOffset;
            HeadingOffset = headingOffset;
            FitRmse       = fitRmse;
            PointCount    = pointCount;
            Timestamp     = DateTime.UtcNow;
            IsActive      = true;
        }

        public void Clear()
        {
            PitchOffset   = 0;
            RollOffset    = 0;
            HeadingOffset = 0;
            FitRmse       = 0;
            PointCount    = 0;
            Timestamp     = DateTime.MinValue;
            IsActive      = false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

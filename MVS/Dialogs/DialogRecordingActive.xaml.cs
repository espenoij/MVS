using System;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Modeless popup shown while a recording session is active. Hosts the live
    /// capture-duration status banner and a Stop button so the operator always has
    /// a prominent recording indicator and can stop the session from the popup.
    /// </summary>
    public partial class DialogRecordingActive : RadWindow
    {
        private readonly DispatcherTimer timer;
        private DateTime recordingStartUtc;
        private Action stopAction;

        // Guards so the stop flow and the window close only run once.
        private bool sessionEnded;
        private bool closed;

        public DialogRecordingActive()
        {
            InitializeComponent();

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => banner.UpdateLive(recordingStartUtc);

            Closed += OnClosed;
        }

        /// <summary>
        /// Begin showing the live recording status. <paramref name="startUtc"/> is
        /// the UTC time recording began; <paramref name="onStop"/> is invoked when
        /// the operator requests a stop from this popup.
        /// </summary>
        public void StartSession(DateTime startUtc, Action onStop)
        {
            recordingStartUtc = startUtc;
            stopAction = onStop;

            banner.UpdateLive(recordingStartUtc);
            timer.Start();
        }

        /// <summary>
        /// Closes the popup in response to the recording session ending. Safe to
        /// call multiple times.
        /// </summary>
        public void CloseSession()
        {
            sessionEnded = true;
            timer.Stop();

            if (!closed)
                Close();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            RequestStop();
        }

        private void OnClosed(object sender, WindowClosedEventArgs e)
        {
            closed = true;
            timer.Stop();

            // If the operator closed the popup directly (window chrome), make sure
            // the recording session is stopped as well.
            RequestStop();
        }

        private void RequestStop()
        {
            if (sessionEnded)
                return;

            sessionEnded = true;
            timer.Stop();
            btnStop.IsEnabled = false;

            stopAction?.Invoke();
        }
    }
}

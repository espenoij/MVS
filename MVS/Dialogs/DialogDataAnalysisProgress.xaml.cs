using System.Windows;
using System.Windows.Threading;
using System;
using System.ComponentModel;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogDataAnalysisProgress
    /// .xaml
    /// </summary>
    public partial class DialogDataAnalysisProgress : Window
    {
        private DispatcherTimer timer;

        public DialogDataAnalysisProgress()
        {
            InitializeComponent();
            Closed += OnClosed;
        }

        public void Start(MainWindowVM mainWindowVM)
        {
            DataContext = mainWindowVM;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += runTimer;

            timer.Start();

            void runTimer(Object source, EventArgs e)
            {
                dataAnalysisProgressBar.Value = mainWindowVM.dataAnalysisProgress;

                //if (mainWindowVM.dataAnalysisProgress >= 99 ||
                //    mainWindowVM.Result.code != ImportResultCode.OK) 
                //{
                //    Close();
                //}
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            timer?.Stop();
            timer = null;
        }
    }
}

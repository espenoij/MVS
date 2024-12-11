using Telerik.Windows.Controls;
using System.Windows.Threading;
using System;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogDataAnalysisProgress
    /// .xaml
    /// </summary>
    public partial class DialogDataAnalysisProgress : RadWindow
    {
        private DispatcherTimer timer;

        public DialogDataAnalysisProgress()
        {
            InitializeComponent();
        }

        public void Init(MainWindowVM mainWindowVM)
        {
            DataContext = mainWindowVM;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += runTimer;

            timer.Start();

            void runTimer(Object source, EventArgs e)
            {
                dataAnalysisProgressBar.Value = mainWindowVM.dataAnalysisProgress;

                if (mainWindowVM.dataAnalysisProgress >= 99 ||
                    mainWindowVM.Result.code != ImportResultCode.OK) 
                {
                    Close();
                }
            }
        }
    }
}

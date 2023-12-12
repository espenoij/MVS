using Telerik.Windows.Controls;
using System.Windows.Threading;
using System;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogImportProgress
    /// .xaml
    /// </summary>
    public partial class DialogImportProgress : RadWindow
    {
        private DispatcherTimer timer;

        public DialogImportProgress()
        {
            InitializeComponent();
        }

        public void Init(ImportVM importVM)
        {
            DataContext = importVM;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += runTimer;

            timer.Start();

            void runTimer(Object source, EventArgs e)
            {
                importProgressBar.Value = importVM.importProgress;

                if (importVM.importProgress >= 99 ||
                    importVM.Result.code != ImportResultCode.OK) 
                {
                    Close();
                }
            }
        }
    }
}

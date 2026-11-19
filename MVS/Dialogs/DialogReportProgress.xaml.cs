using System;
using System.ComponentModel;
using System.Windows;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogReportProgress.xaml.
    /// A simple modal, indeterminate progress window shown while the verification
    /// report is being generated (chart rendering and PDF build can take a moment).
    /// </summary>
    public partial class DialogReportProgress : Window
    {
        private bool _allowClose;

        public DialogReportProgress()
        {
            InitializeComponent();
            Closing += PreventUserClose;
        }

        public void CloseProgress()
        {
            _allowClose = true;
            Close();
        }

        private void PreventUserClose(object sender, CancelEventArgs e)
        {
            e.Cancel = !_allowClose;
        }
    }
}

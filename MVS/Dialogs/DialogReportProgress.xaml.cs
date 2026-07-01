using Telerik.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogReportProgress.xaml.
    /// A simple modal, indeterminate progress window shown while the verification
    /// report is being generated (chart rendering and PDF build can take a moment).
    /// </summary>
    public partial class DialogReportProgress : RadWindow
    {
        public DialogReportProgress()
        {
            InitializeComponent();
        }
    }
}

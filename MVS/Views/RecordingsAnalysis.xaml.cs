using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for RecordingsAnalysis.xaml
    /// </summary>
    public partial class RecordingsAnalysis : UserControl
    {
        public RecordingsAnalysis()
        {
            InitializeComponent();
        }

        public void Init(RecordingsAnalysisVM recordingsAnalysisVM)
        {
            // Context
            DataContext = recordingsAnalysisVM;

        }
    }
}

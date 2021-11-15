using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingReadouts_NOROG.xaml
    /// </summary>
    public partial class WindHeadingReadouts_NOROG : UserControl
    {
        public WindHeadingReadouts_NOROG()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}

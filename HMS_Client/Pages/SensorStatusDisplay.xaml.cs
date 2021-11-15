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
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for NOROG_SensorStatus.xaml
    /// </summary>
    public partial class SensorStatusDisplay : UserControl
    {
        public SensorStatusDisplay()
        {
            InitializeComponent();
        }

        public void Init(SensorStatusDisplayVM sensorStatusVM)
        {
            DataContext = sensorStatusVM;

            lbSensorStatus.ItemsSource = sensorStatusVM.sensorStatusDisplayList;
        }
    }
}

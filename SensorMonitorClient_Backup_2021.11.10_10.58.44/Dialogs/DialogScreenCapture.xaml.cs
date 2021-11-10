using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for DialogScreenCapture.xaml
    /// </summary>
    public partial class DialogScreenCapture : RadWindow
    {
        public DialogScreenCapture()
        {
            InitializeComponent();
        }

        private void btnOpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            string reportFolder = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder);
            Process.Start(reportFolder);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

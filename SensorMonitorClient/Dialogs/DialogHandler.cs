using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SensorMonitorClient
{
    public static class DialogHandler
    {
        public static void Warning(string header, string text)
        {
            DialogHandlerWarning dialog = new DialogHandlerWarning(header, text);
            dialog.Owner = App.Current.MainWindow;
            dialog.ShowDialog();
        }
    }
}

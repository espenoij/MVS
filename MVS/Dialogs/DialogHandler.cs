using System;
using System.Windows;

namespace MVS
{
    public static class DialogHandler
    {
        public static void Warning(string header, string text)
        {
            ShowMessage("Warning", header, text);
        }

        public static void Alert(string text)
        {
            ShowMessage("Alert", "Alert", text);
        }

        public static bool Confirm(string text)
        {
            bool confirmed = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var owner = App.Current.MainWindow;
                MessageBoxResult result = owner == null
                    ? MessageBox.Show(text, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    : MessageBox.Show(owner, text, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                confirmed = result == MessageBoxResult.Yes;
            });

            return confirmed;
        }

        private static void ShowMessage(string title, string header, string text)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                DialogHandlerWarning dialog = new DialogHandlerWarning(header, text);
                dialog.Owner = App.Current.MainWindow;
                dialog.Title = title;
                dialog.ShowDialog();
            });
        }
    }
}

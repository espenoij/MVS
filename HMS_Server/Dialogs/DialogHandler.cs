namespace HMS_Server
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

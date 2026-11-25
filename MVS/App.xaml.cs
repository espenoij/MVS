using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace MVS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex = null;

        public App()
        {
            // Sette culture info globalt
            FrameworkElement.LanguageProperty.OverrideMetadata(
                   typeof(FrameworkElement),
                   new FrameworkPropertyMetadata(
                       XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Mutex for å hindre at flere instanser av server programmet kjører samtidig
            bool mutexCreated;
            mutex = new Mutex(true, Constants.appName, out mutexCreated);
            if (!mutexCreated)
            {
                DialogHandler.Warning("The application is already running.", "Running two or more simultaneous sessions is not allowed.");

                // Programmet kjører allerede -> avslutt
                Application.Current.Shutdown();
            }

            base.OnStartup(e);

            // Starte server vinduet
            MainWindow sensorMonitorServer = new MainWindow();
            sensorMonitorServer.Show();

            // Egen kode for å få et icon på taskbar
            var window = sensorMonitorServer;
            window.ShowInTaskbar = true;

            // Tittel i taskbar
            window.Title = "Motion Verification System"; // Må matche det som står i MainWindow.xaml

        }

    }
}

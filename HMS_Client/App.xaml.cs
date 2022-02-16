using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Mutex for å hindre at flere instanser av server programmet kjører samtidig
            bool mutexCreated;
            mutex = new Mutex(true, Constants.appNameClient, out mutexCreated);
            if (!mutexCreated)
            {
                DialogHandler.Warning("The application is already running.", "Running two or more simultaneous sessions is not allowed.");

                // Programmet kjører allerede -> avslutt
                Application.Current.Shutdown();
            }

            base.OnStartup(e);

            // Generell dispatcher exception handler
            this.Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs ex)
            {
                DialogHandler.Warning("Dispatcher Unhandled Exception", ex.Exception.Message);
                ex.Handled = true;
            }

            string version = Application.ResourceAssembly.GetName().Version.ToString();

            // Starte klient vinduet
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Egen kode for å få et icon på taskbar
            var window = mainWindow.ParentOfType<Window>();
            window.ShowInTaskbar = true;

            // Tittel i taskbar
            window.Title = "Helideck Monitoring System";

            // Theme Colors
            //////////////////////////////////////////////////////

            // Accent (tab indicator, etc)
            MaterialPalette.Palette.AccentNormalColor = (Color)ColorConverter.ConvertFromString("#ffffff");
            MaterialPalette.Palette.AccentHoverColor = (Color)ColorConverter.ConvertFromString("#ff818181");
            MaterialPalette.Palette.AccentPressedColor = (Color)ColorConverter.ConvertFromString("#FFD3F069");

            // Divider & inputfelt understrek
            MaterialPalette.Palette.DividerColor = (Color)ColorConverter.ConvertFromString("#44000000");

            MaterialPalette.Palette.IconColor = (Color)ColorConverter.ConvertFromString("#FF000000");
            MaterialPalette.Palette.MainColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            MaterialPalette.Palette.MarkerColor = (Color)ColorConverter.ConvertFromString("#FF000000");
            MaterialPalette.Palette.ValidationColor = (Color)ColorConverter.ConvertFromString("#FFD50000");
            MaterialPalette.Palette.ComplementaryColor = (Color)ColorConverter.ConvertFromString("#FFE0E0E0");
            MaterialPalette.Palette.AlternativeColor = (Color)ColorConverter.ConvertFromString("#FFF5F5F5");
            MaterialPalette.Palette.MarkerInvertedColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            MaterialPalette.Palette.PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFFAFAFA");

            // Window Bar
            MaterialPalette.Palette.PrimaryNormalColor = (Color)ColorConverter.ConvertFromString("#FF000000");

            // Focus
            MaterialPalette.Palette.PrimaryFocusColor = (Color)ColorConverter.ConvertFromString("#ff999999");

            // Tab hover
            MaterialPalette.Palette.PrimaryHoverColor = (Color)ColorConverter.ConvertFromString("#FF797979");

            MaterialPalette.Palette.PrimaryPressedColor = (Color)ColorConverter.ConvertFromString("#FF263238");
            MaterialPalette.Palette.RippleColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            MaterialPalette.Palette.ReadOnlyBackgroundColor = (Color)ColorConverter.ConvertFromString("#00FFFFFF");
            MaterialPalette.Palette.ReadOnlyBorderColor = (Color)ColorConverter.ConvertFromString("#FFABABAB");

            MaterialPalette.Palette.PrimaryOpacity = 0.87;
            MaterialPalette.Palette.SecondaryOpacity = 0.54;
            MaterialPalette.Palette.DisabledOpacity = 0.26;
            MaterialPalette.Palette.DividerOpacity = 0.38;

            base.OnStartup(e);
        }
    }
}

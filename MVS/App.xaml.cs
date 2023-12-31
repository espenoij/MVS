﻿using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Telerik.Windows.Controls;

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

            // Generell exception handler
            //Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        // Generell exception handler
        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //RadWindow.Alert(string.Format("(HMS Server) An unhandled general exception occurred:\n\n{0}", TextHelper.Wrap(e.Exception.Message)));
            e.Handled = true;
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
            var window = sensorMonitorServer.ParentOfType<Window>();
            window.ShowInTaskbar = true;

            // Tittel i taskbar
            window.Title = "Motion Verification System"; // Må matche det som står i MainWindow.xaml

            // Theme Colors
            //////////////////////////////////////////////////////
            ///

            // Accent (tab indicator, etc)
            MaterialPalette.Palette.AccentNormalColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
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
        }

        //protected override void OnClosed(EventArgs e)
        //{
        //    base.OnClosed(e);

        //    Application.Current.Shutdown();
        //}
    }
}

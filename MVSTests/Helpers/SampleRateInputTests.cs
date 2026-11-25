using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Views.Controls;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class SampleRateInputTests
    {
        [TestMethod]
        public void SampleRateInput_CommitsNullableFractionalValuesAndBounds()
        {
            StaTestHelper.Run(() =>
            {
                var previousCulture = CultureInfo.CurrentCulture;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
                try
                {
                    if (Application.Current == null)
                    {
                        var app = new App();
                        app.InitializeComponent();
                    }

                    var metadata = new MruReportMetadata { SampleRateHz = 2 };
                    var spinner = new SampleRateInput { DataContext = metadata };
                    spinner.SetBinding(SampleRateInput.ValueProperty,
                        new Binding(nameof(MruReportMetadata.SampleRateHz)) { Mode = BindingMode.TwoWay });
                    var other = new Button { Content = "Other" };
                    var panel = new StackPanel();
                    panel.Children.Add(spinner);
                    panel.Children.Add(other);
                    var window = new Window { Content = panel, Width = 400, Height = 140, ShowInTaskbar = false };
                    window.Show();
                    try
                    {
                        var input = (TextBox)spinner.FindName("Input");
                        var increase = (RepeatButton)spinner.FindName("Increase");
                        var decrease = (RepeatButton)spinner.FindName("Decrease");
                        int changes = 0;
                        spinner.ValueChanged += (_, __) => changes++;

                        Commit(input, other, "3,75");
                        Assert.AreEqual(3.75, metadata.SampleRateHz);
                        Assert.AreEqual(1, changes);
                        Commit(input, other, "");
                        Assert.IsNull(metadata.SampleRateHz);
                        Commit(input, other, "10001");
                        Assert.AreEqual(10000d, metadata.SampleRateHz);
                        increase.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.AreEqual(10000d, metadata.SampleRateHz);
                        Commit(input, other, "-1");
                        Assert.AreEqual(0d, metadata.SampleRateHz);
                        decrease.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.AreEqual(0d, metadata.SampleRateHz);
                        Commit(input, other, "invalid");
                        Assert.AreEqual(0d, metadata.SampleRateHz);
                        Assert.AreEqual(0d.ToString("N2", CultureInfo.CurrentCulture), input.Text);

                        Commit(input, other, "2,5");
                        increase.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.AreEqual(3.5, metadata.SampleRateHz);
                        decrease.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.AreEqual(2.5, metadata.SampleRateHz);
                        input.Focus();
                        input.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                            PresentationSource.FromVisual(input), 0, Key.Up) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                        Assert.AreEqual(3.5, metadata.SampleRateHz);
                        spinner.Value = 0.001;
                        Assert.IsTrue(other.Focus());
                        Assert.IsTrue(input.Focus());
                        Assert.IsTrue(other.Focus());
                        Assert.AreEqual(0.001, metadata.SampleRateHz);
                        increase.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        Assert.IsTrue(metadata.SampleRateHz.HasValue);
                        Assert.AreEqual(1.001, metadata.SampleRateHz.Value, 0.0000001);
                        CheckMetadataEditorNotifications(window);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    CultureInfo.CurrentCulture = previousCulture;
                }
            });
        }

        private static void CheckMetadataEditorNotifications(Window window)
        {
                var metadata = new MruReportMetadata { SampleRateHz = 2 };
                var editor = new ReportMetadataEditor { Metadata = metadata };
                var scroll = new ScrollViewer { Content = editor };
                var other = new Button { Content = "Other" };
                var panel = new DockPanel();
                DockPanel.SetDock(other, Dock.Bottom);
                panel.Children.Add(other);
                panel.Children.Add(scroll);
                var previousContent = window.Content;
                window.Content = panel;
                window.Height = 600;
                window.UpdateLayout();
                try
                {
                    var spinner = (SampleRateInput)editor.FindName("SampleRate");
                    spinner.BringIntoView();
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    var input = (TextBox)spinner.FindName("Input");
                    double? savedValue = null;
                    double? validationValue = null;
                    editor.MetadataChanged += (_, __) => savedValue = metadata.SampleRateHz;
                    editor.ValidationStateChanged += (_, __) => validationValue = metadata.SampleRateHz;
                    Commit(input, other, 3.5.ToString(CultureInfo.CurrentCulture));
                    Assert.AreEqual(3.5, metadata.SampleRateHz);
                    Assert.AreEqual(3.5, savedValue);
                    Assert.AreEqual(3.5, validationValue);
                    savedValue = null;
                    ((RepeatButton)spinner.FindName("Increase")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.AreEqual(4.5, metadata.SampleRateHz);
                    Assert.AreEqual(4.5, savedValue);
                }
                finally
                {
                    window.Content = previousContent;
                }
        }

        private static void Commit(TextBox input, Button other, string value)
        {
            Assert.IsTrue(input.Focus());
            input.Text = value;
            Assert.IsTrue(other.Focus());
        }
    }
}

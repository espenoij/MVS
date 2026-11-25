using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MVS.Views.Controls
{
    public partial class SampleRateInput : UserControl
    {
        private const double Minimum = 0;
        private const double Maximum = 10000;
        private bool updatingText;
        private bool textChanged;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double?), typeof(SampleRateInput),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged, CoerceValue));

        public SampleRateInput()
        {
            InitializeComponent();
            UpdateText();
        }

        public double? Value
        {
            get => (double?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public event EventHandler ValueChanged;
        public event EventHandler Spun;

        private static object CoerceValue(DependencyObject sender, object value)
        {
            if (value is double number)
            {
                if (double.IsNaN(number) || double.IsInfinity(number))
                    return null;
                return Math.Max(Minimum, Math.Min(Maximum, number));
            }
            return null;
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var input = (SampleRateInput)sender;
            input.UpdateText();
            input.ValueChanged?.Invoke(input, EventArgs.Empty);
        }

        private void UpdateText()
        {
            if (Input != null)
            {
                updatingText = true;
                Input.Text = Value?.ToString("N2", CultureInfo.CurrentCulture) ?? string.Empty;
                updatingText = false;
                textChanged = false;
            }
        }

        private void CommitText()
        {
            if (!textChanged)
                return;

            string text = Input.Text.Trim();
            if (text.Length == 0)
                SetCurrentValue(ValueProperty, null);
            else if (double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out double value) &&
                     !double.IsNaN(value) && !double.IsInfinity(value))
                SetCurrentValue(ValueProperty, Math.Max(Minimum, Math.Min(Maximum, value)));
            UpdateText();
        }

        private void ChangeValue(double amount)
        {
            CommitText();
            double? previous = Value;
            SetCurrentValue(ValueProperty, Math.Max(Minimum, Math.Min(Maximum, (Value ?? 0) + amount)));
            if (Value != previous)
                Spun?.Invoke(this, EventArgs.Empty);
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!updatingText)
                textChanged = true;
        }

        private void Input_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitText();

        private void Increase_Click(object sender, RoutedEventArgs e) => ChangeValue(1);

        private void Decrease_Click(object sender, RoutedEventArgs e) => ChangeValue(-1);

        private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            double change;
            switch (e.Key)
            {
                case Key.Up: change = 1; break;
                case Key.Down: change = -1; break;
                case Key.PageUp: change = 10; break;
                case Key.PageDown: change = -10; break;
                case Key.Enter: CommitText(); e.Handled = true; return;
                default: return;
            }
            ChangeValue(change);
            e.Handled = true;
        }
    }
}

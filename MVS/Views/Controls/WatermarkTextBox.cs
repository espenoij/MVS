using System.Windows;
using System.Windows.Controls;

namespace MVS.Views.Controls
{
    public class WatermarkTextBox : TextBox
    {
        public static readonly DependencyProperty WatermarkContentProperty =
            DependencyProperty.Register(
                nameof(WatermarkContent),
                typeof(string),
                typeof(WatermarkTextBox),
                new PropertyMetadata(string.Empty));

        public string WatermarkContent
        {
            get => (string)GetValue(WatermarkContentProperty);
            set => SetValue(WatermarkContentProperty, value);
        }
    }
}

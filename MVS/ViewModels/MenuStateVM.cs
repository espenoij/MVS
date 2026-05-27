using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    // Singleton view model for shared main-menu UI state (e.g. label visibility).
    // Used as a binding source from XAML data templates where the DataContext
    // is the tab item header content (a string), not the window itself.
    public class MenuStateVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly MenuStateVM _instance = new MenuStateVM();
        public static MenuStateVM Instance => _instance;

        private MenuStateVM()
        {
        }

        private bool _showMenuLabels = true;
        public bool showMenuLabels
        {
            get
            {
                return _showMenuLabels;
            }
            set
            {
                if (_showMenuLabels != value)
                {
                    _showMenuLabels = value;
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

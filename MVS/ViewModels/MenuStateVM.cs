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

        private bool _hasNewMessages = false;
        public bool hasNewMessages
        {
            get
            {
                return _hasNewMessages;
            }
            set
            {
                if (_hasNewMessages != value)
                {
                    _hasNewMessages = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool messagesPageActive { get; set; } = false;

        // True when the user is on wizard Step 2 (LiDAR page) inside the Projects tab.
        // LivoxLidar messages are shown inline on that page, so the main-menu badge
        // should not fire for them while the page is active.
        public bool lidarPageActive { get; set; } = false;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

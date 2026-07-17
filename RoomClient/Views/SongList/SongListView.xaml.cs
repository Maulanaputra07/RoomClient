using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoomClient.Views.SongList
{
    /// <summary>
    /// Interaction logic for SongListView.xaml
    /// </summary>
    public partial class SongListView : UserControl
    {
        public Grid ContentGridControl => ContentGrid;
        public Button ToggleButtonControl => ToggleButton;

        private bool _isExpanded = true;
        private bool _isCollapsed;

        public event EventHandler<bool>? ToggleRequested;
        public SongListView()
        {
            InitializeComponent();
            ToggleButton.Content = "▲ Hide";
        }

        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            _isCollapsed = !_isCollapsed;

            ContentGrid.Visibility =
                _isCollapsed
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            ToggleButton.Content =
                _isCollapsed
                    ? "▼ Show"
                    : "▲ Hide";

            ToggleRequested?.Invoke(this, _isCollapsed);
        }

        //private void OnToggleClick(object sender, RoutedEventArgs e)
        //{
        //    ToggleRequested?.Invoke(this, EventArgs.Empty);
        //}

        //private void OnToggleClick(object sender, RoutedEventArgs e)
        //{
        //    _isExpanded = !_isExpanded;
        //    ContentGrid.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        //    ToggleButton.Content = _isExpanded ? "▲ Hide" : "▼ Show";
        //}
    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using RoomClient.Core.Models;
using RoomClient.ViewModels;

namespace RoomClient.Views.SongList
{
    public partial class SongListView : UserControl
    {
        public Grid ContentGridControl => ContentGrid;

        public event EventHandler<bool>? ToggleRequested;

        public SongListView()
        {
            InitializeComponent();
        }
    }
}
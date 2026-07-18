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
        public Button ToggleButtonControl => ToggleButton;

        private bool _isCollapsed;
        private ObservableCollection<Song>? _observedResults;

        public event EventHandler<bool>? ToggleRequested;

        public SongListView()
        {
            InitializeComponent();
            ToggleButton.Content = "▲ Hide";

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachResultsCollection();

            if (DataContext is MainWindowViewModel mainWindowViewModel)
            {
                AttachResultsCollection(mainWindowViewModel.Search.Results);
            }
        }

        private void AttachResultsCollection(ObservableCollection<Song> results)
        {
            _observedResults = results;
            _observedResults.CollectionChanged += OnResultsCollectionChanged;
            ApplyAutoState(results.Count);
        }

        private void DetachResultsCollection()
        {
            if (_observedResults is not null)
            {
                _observedResults.CollectionChanged -= OnResultsCollectionChanged;
                _observedResults = null;
            }
        }

        private void OnResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_observedResults is not null)
            {
                ApplyAutoState(_observedResults.Count);
            }
        }

        private void ApplyAutoState(int count)
        {
            _isCollapsed = count == 0;
            ContentGrid.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            ToggleButton.Content = _isCollapsed ? "▼ Show" : "▲ Hide";
        }

        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            _isCollapsed = !_isCollapsed;
            ContentGrid.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            ToggleButton.Content = _isCollapsed ? "▼ Show" : "▲ Hide";
            ToggleRequested?.Invoke(this, _isCollapsed);
        }
    }
}
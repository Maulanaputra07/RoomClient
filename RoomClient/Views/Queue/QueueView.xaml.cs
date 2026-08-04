using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using RoomClient.Core.Models;
using RoomClient.ViewModels;

namespace RoomClient.Views.Queue
{
    public partial class QueueView : UserControl
    {
        public Grid ContentGridControl => ContentGrid;
        //public Button ToggleButtonControl => ToggleButton;

        private bool _isCollapsed;
        private ObservableCollection<QueueSong>? _observedQueue;

        public event EventHandler<bool>? ToggleRequested;

        public QueueView()
        {
            InitializeComponent();
            //ToggleButton.Content = "▲ Hide";

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachQueueCollection();

            if (DataContext is MainWindowViewModel mainWindowViewModel)
            {
                AttachQueueCollection(mainWindowViewModel.Queue.Items);
            }
        }

        private void AttachQueueCollection(ObservableCollection<QueueSong> queue)
        {
            _observedQueue = queue;
            _observedQueue.CollectionChanged += OnQueueCollectionChanged;
            ApplyAutoState(queue.Count);
        }

        private void DetachQueueCollection()
        {
            if (_observedQueue is not null)
            {
                _observedQueue.CollectionChanged -= OnQueueCollectionChanged;
                _observedQueue = null;
            }
        }


        private void OnQueueCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_observedQueue is not null)
            {
                ApplyAutoState(_observedQueue.Count);
            }
        }

        private void ApplyAutoState(int count)
        {
            _isCollapsed = count == 0;
            ContentGrid.Visibility = _isCollapsed
                ? Visibility.Collapsed
                : Visibility.Visible;

            //ToggleButton.Content = _isCollapsed
            //    ? "▼ Show"
            //    : "▲ Hide";
        }

        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            _isCollapsed = !_isCollapsed;

            ContentGrid.Visibility = _isCollapsed
                ? Visibility.Collapsed
                : Visibility.Visible;

            //ToggleButton.Content = _isCollapsed
            //    ? "▼ Show"
            //    : "▲ Hide";

            ToggleRequested?.Invoke(this, _isCollapsed);
        }
    }
}
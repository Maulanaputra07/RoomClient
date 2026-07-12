using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoomClient.Core.Interfaces;
using RoomClient.Services.Api;
using RoomClient.Services.Player;
using RoomClient.Services.Queue;
using RoomClient.Services.SignalR;
using RoomClient.Services.Youtube;
using RoomClient.ViewModels;
using RoomClient.Views.Windows;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui;

namespace RoomClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(AppContext.BaseDirectory); })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindow>();

                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<SearchViewModel>();

                services.AddSingleton<PlayerViewModel>();

                services.AddSingleton<QueueViewModel>();

                services.AddSingleton<SongListViewModel>();

                services.AddSingleton<StatusViewModel>();

                services.AddSingleton<IYoutubeService, YoutubeService>();

                services.AddSingleton<IPlayerService, PlayerService>();

                services.AddSingleton<IQueueService, QueueService>();

                services.AddSingleton<IApiService, ApiService>();

                services.AddSingleton<ISignalRService, SignalRService>();
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
            var mainWindow = Services.GetRequiredService<MainWindow>();

            MainWindow = mainWindow;
            MainWindow.Show();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }


    }
}

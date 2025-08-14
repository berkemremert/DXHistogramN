using System.Windows;
using DXHistogram.Services;
using DXHistogram.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DXHistogram
{
    public partial class App : System.Windows.Application
    {
        private IHost _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Create and configure the host
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddSingleton<IDataService, DataService>();
                    services.AddSingleton<IHistogramService, HistogramService>();

                    // Register ViewModels
                    services.AddTransient<MainViewModel>();

                    // Register Views
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // Start the host
            _host.Start();

            // Get the main window and show it
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}
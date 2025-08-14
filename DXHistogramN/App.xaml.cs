using System.Windows;
using DXHistogramN.Services;
using DXHistogramN.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application = System.Windows.Application;

namespace DXHistogramN
{
    public partial class App : Application
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
                    services.AddSingleton<IChartLayoutService, ChartLayoutService>();

                    // Register ViewModels
                    services.AddTransient<MainViewModel>();

                    // Register Views - Note: Don't register as Singleton since we want fresh instances
                    services.AddTransient<MainWindow>();
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
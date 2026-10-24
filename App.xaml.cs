using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RamTracking.Services;
using RamTracking.ViewModels;

namespace RamTracking;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Servicios
                services.AddSingleton<IMemoryService, MemoryService>();
                services.AddSingleton<IModuleService, ModuleService>();
                services.AddSingleton<IMonitorService, MonitorService>();
                services.AddSingleton<IReportService, ReportService>();

                // ViewModels
                services.AddTransient<MainViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        
        // Inyectar ViewModel en MainWindow
        var mainWindow = new MainWindow
        {
            DataContext = _host.Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
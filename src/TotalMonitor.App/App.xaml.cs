using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Diagnostics;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Production.json", optional: true)
            .AddEnvironmentVariables("TOTALMONITOR_")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(configuration["Api:BaseUrl"] ?? "http://localhost:5080/"),
            Timeout = TimeSpan.FromSeconds(15)
        });
        services.AddSingleton<IApiClient, ApiClient>();
        services.AddSingleton<ICurrentUserService, ClientCurrentUserService>();
        services.AddSingleton<IAuthenticationService, ApiAuthenticationService>();
        services.AddSingleton<IMeterService, ApiMeterService>();
        services.AddSingleton<ICommunicationSettingsService, ApiCommunicationSettingsService>();
        services.AddSingleton<IHistoricalDataService, ApiHistoricalDataService>();
        services.AddSingleton<IDataAcquisitionService, ApiAcquisitionService>();
        services.AddSingleton<IDataAggregationService, ClientDataAggregationService>();
        services.AddSingleton<IHistoricalDataExporter, ClientCsvExporter>();
        services.AddSingleton<IUserAdminService, ApiUserAdminService>();
        services.AddSingleton<IReportService, ApiReportService>();
        services.AddSingleton<ServerRealtimeClient>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<LoginViewModel>();

        Services = services.BuildServiceProvider();
        try
        {
            Debug.WriteLine("LOGIN → esperando resultado del diálogo");
            var login = Services.GetRequiredService<LoginWindow>();
            if (login.ShowDialog() == true)
            {
                Debug.WriteLine("LOGIN → navegación al dashboard");

                var realtime = Services.GetRequiredService<ServerRealtimeClient>();
                var acq = Services.GetRequiredService<IDataAcquisitionService>() as ApiAcquisitionService;
                if (acq is not null)
                {
                    realtime.MeasurementUpdated += (_, m) =>
                    {
                        acq.OnMeasurementReceived(new TotalMonitor.Core.Entities.Measurement(m.MeterId, "", m.Timestamp, m.Variable, m.Value, m.Unit));
                    };
                    realtime.AcquisitionEventReceived += (_, evt) =>
                    {
                        acq.OnAcquisitionEvent(evt);
                    };
                }
                _ = realtime.StartAsync();

                var mainWindow = Services.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnLastWindowClose;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOGIN/DASHBOARD ERROR: {ex}");
            MessageBox.Show("No fue posible abrir el dashboard después del inicio de sesión. Revise el registro de errores.", "TOTAL MONITOR", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
        base.OnStartup(e);
    }
}

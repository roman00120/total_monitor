using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Infrastructure.Persistence;
using TotalMonitor.Infrastructure.Services;
using TotalMonitor.Infrastructure.Modbus;
using TotalMonitor.Core.Modbus;
using TotalMonitor.Infrastructure.Repositories;
using TotalMonitor.Infrastructure.Acquisition;
using TotalMonitor.Infrastructure.Historical;
using TotalMonitor.Infrastructure.Security;

namespace TotalMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? configuration["Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Database connection string is not configured.");
        services.AddDbContext<TotalMonitorDbContext>(options => options.UseMySql(connectionString, ServerVersion.Parse("8.0.0-mysql")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TotalMonitorDbContext>());
        services.AddScoped<IMeterService, MeterService>();
        services.AddScoped<ICommunicationSettingsService, CommunicationSettingsService>();
        services.AddScoped<IMeasurementService, MeasurementService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();
        services.AddScoped<IMeterConnectionStatusRepository, MeterConnectionStatusRepository>();
        services.AddScoped<IHistoricalDataService, HistoricalDataService>();
        services.AddSingleton<IDataAggregationService, DataAggregationService>();
        services.AddSingleton<IHistoricalDataExporter, CsvHistoricalDataExporter>();
        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<PasswordHasher>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IdentitySeeder>();
        services.AddSingleton<IMeterConnectionService, MeterConnectionService>();
        services.AddSingleton(new ModbusConnectionOptions
        {
            ComPort = configuration["Modbus:ComPort"] ?? "",
            BaudRate = Int(configuration, "BaudRate", 9600),
            DataBits = Int(configuration, "DataBits", 8),
            Parity = configuration["Modbus:Parity"] ?? "None",
            StopBits = configuration["Modbus:StopBits"] ?? "One",
            ReadTimeout = Int(configuration, "ReadTimeout", 1000),
            WriteTimeout = Int(configuration, "WriteTimeout", 1000),
            RetryCount = Int(configuration, "RetryCount", 1),
            RetryDelay = Int(configuration, "RetryDelay", 100),
            PollingInterval = Int(configuration, "PollingInterval", 1000)
        });
        services.AddTransient<ISerialPortService, SerialPortService>();
        services.AddTransient<IModbusTransport, SerialModbusTransport>();
        services.AddTransient<IModbusClient, ModbusClient>();
        services.AddTransient<IModbusRegisterReader, ModbusRegisterReader>();
        services.AddTransient<IModbusService, ModbusClient>();
        services.AddSingleton<TOV452RegisterMap>(_ => TOV452RegisterMap.Empty);
        services.AddSingleton<MeterRegisterMap>(_ => MeterRegisterMap.Empty);
        services.AddSingleton<IModbusClientFactory, ModbusClientFactory>();
        services.AddSingleton<IDataAcquisitionService, DataAcquisitionService>();
        return services;
    }
    private static int Int(IConfiguration configuration, string key, int fallback) => int.TryParse(configuration[$"Modbus:{key}"], out var value) ? value : fallback;
}

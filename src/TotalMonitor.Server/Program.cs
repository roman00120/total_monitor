using TotalMonitor.Infrastructure;
using TotalMonitor.Core.Security;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Server;
using TotalMonitor.Server.Hubs;
using TotalMonitor.Infrastructure.Persistence;
using TotalMonitor.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();
builder.Configuration.AddEnvironmentVariables("TOTALMONITOR_");
var jwtSecret = builder.Configuration["Authentication:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 || jwtSecret.StartsWith("CHANGE_ME", StringComparison.Ordinal))
    throw new InvalidOperationException("Authentication:SecretKey is required and must contain at least 32 characters. Configure it through appsettings.Production.json or TOTALMONITOR_ environment variables.");
await DatabaseBootstrapper.EnsureDatabaseExistsAsync(
    builder.Configuration,
    LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("DatabaseBootstrap"));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, ServerCurrentUserService>();
builder.Services.AddScoped<IAuthorizationService, TotalMonitor.Infrastructure.Security.AuthorizationService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret.PadRight(32, '0'))), ValidateIssuer = true, ValidIssuer = builder.Configuration["Authentication:Issuer"] ?? "TotalMonitor", ValidateAudience = true, ValidAudience = builder.Configuration["Authentication:Audience"] ?? "TotalMonitor.Client", ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30) }; options.Events = new JwtBearerEvents { OnMessageReceived = context => { var token = context.Request.Query["access_token"]; if (!string.IsNullOrWhiteSpace(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/monitoring")) context.Token = token; return Task.CompletedTask; } }; });
builder.Services.AddAuthorization(options => { foreach (var permission in new[] { PermissionNames.DashboardView, PermissionNames.MetersView, PermissionNames.MetersCreate, PermissionNames.MetersEdit, PermissionNames.MetersDelete, PermissionNames.MonitoringView, PermissionNames.HistoryView, PermissionNames.ReportsView, PermissionNames.ReportsExport, PermissionNames.UsersView, PermissionNames.UsersCreate, PermissionNames.UsersEdit, PermissionNames.UsersDelete, PermissionNames.SettingsView, PermissionNames.SettingsEdit }) options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission))); });
builder.Services.AddSignalR();
builder.Services.AddHostedService<AcquisitionHostedService>();

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

await InitializeDatabaseAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "server_error", message = "Ocurrió un error interno." });
    }));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TotalMonitorDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
    if (!await db.Database.CanConnectAsync())
        throw new InvalidOperationException("The database is not available. Verify ConnectionStrings:Default before starting TotalMonitor.Server.");

    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("No se pudieron aplicar las migraciones EF Core pendientes.", ex);
    }
    logger.LogInformation("Database connectivity verified and pending migrations applied.");

    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    try
    {
        await seeder.EnsureInitialAdministratorAsync(
            app.Configuration["Authentication:InitialAdminUsername"] ?? string.Empty,
            app.Configuration["Authentication:InitialAdminName"] ?? "Administrador",
            app.Configuration["Authentication:InitialAdminPassword"] ?? string.Empty);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("No se pudo crear o verificar el administrador inicial.", ex);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace TotalMonitor.Infrastructure.Persistence;

public static class DatabaseBootstrapper
{
    private const string DatabaseName = "totalmonitor";

    public static async Task EnsureDatabaseExistsAsync(IConfiguration configuration, ILogger logger, CancellationToken cancellationToken = default)
    {
        var configured = configuration.GetConnectionString("Default") ?? configuration["Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("MySQL no está configurado. Configure ConnectionStrings:Default.");

        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(configured);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("La cadena de conexión MySQL no es válida.", ex);
        }

        builder.Database = string.Empty;
        await using var connection = new MySqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (MySqlException ex)
        {
            throw new InvalidOperationException(
                "No se pudo conectar a MySQL en " + builder.Server + ":" + builder.Port + ". Verifique que MySQL esté iniciado y que las credenciales sean correctas.", ex);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE DATABASE IF NOT EXISTS totalmonitor CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("MySQL disponible; base de datos {DatabaseName} verificada o creada.", DatabaseName);
        }
        catch (MySqlException ex)
        {
            throw new InvalidOperationException(
                "MySQL aceptó la conexión, pero no se pudo crear o verificar la base de datos " + DatabaseName + ". Verifique permisos del usuario.", ex);
        }
    }
}

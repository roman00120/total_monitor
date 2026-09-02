using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Collections.Concurrent;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Security;

namespace TotalMonitor.App;

public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public interface IApiClient
{
    string? Token { get; }
    Task<T> GetAsync<T>(string path, CancellationToken ct = default);
    Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default);
    Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default);
    Task<byte[]> PostBytesAsync(string path, object body, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    void SetToken(string token);
    void ClearToken();
}

public sealed class ApiClient(HttpClient http) : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string? Token { get; private set; }

    public async Task<T> GetAsync<T>(string path, CancellationToken ct = default) => await Send<T>(HttpMethod.Get, path, null, ct);
    public async Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default) => await Send<T>(HttpMethod.Post, path, body, ct);
    public async Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default) => await Send<T>(HttpMethod.Put, path, body, ct);
    public async Task<byte[]> PostBytesAsync(string path, object body, CancellationToken ct = default)
    {
        using var response = await SendResponse(HttpMethod.Post, path, body, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var response = await SendResponse(HttpMethod.Delete, path, null, ct);
    }

    public void SetToken(string token) => Token = token;
    public void ClearToken() => Token = null;

    private async Task<T> Send<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await SendResponse(method, path, body, ct);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }

    private async Task<HttpResponseMessage> SendResponse(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (Token is not null) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        try
        {
            var response = await http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) ClearToken();
            if (!response.IsSuccessStatusCode)
            {
                string msg = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden => "No tienes permisos para realizar esta acción.",
                    HttpStatusCode.Unauthorized => "Sesión expirada o no autorizada.",
                    _ => "Error en la comunicación con el servidor."
                };
                try
                {
                    var err = await response.Content.ReadFromJsonAsync<ApiErrorPayload>(JsonOptions, ct);
                    if (!string.IsNullOrWhiteSpace(err?.Message)) msg = err.Message;
                }
                catch { }
                throw new ApiException(response.StatusCode, msg);
            }
            return response;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, "No fue posible conectar con el servidor.") { Source = ex.Source };
        }
    }
}

public sealed record ApiErrorPayload(string? Code, string? Message);

public sealed class ApiAuthenticationService(IApiClient api, ICurrentUserService current) : IAuthenticationService
{
    public bool IsAuthenticated => current.IsAuthenticated;
    public async Task<(bool Success, string Message, AuthenticatedUser? User)> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        try
        {
            var result = await api.PostAsync<ApiLoginResponse>("api/v1/auth/login", new { username, password }, ct);
            api.SetToken(result.Token);
            var user = new AuthenticatedUser(result.UserId, result.Username, result.DisplayName, result.Role, result.Permissions.ToHashSet());
            current.SetUser(user);
            return (true, "Sesión iniciada correctamente.", user);
        }
        catch (ApiException ex)
        {
            return (false, ex.StatusCode == HttpStatusCode.Unauthorized ? "Usuario o contraseña incorrectos." : ex.Message, null);
        }
    }
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try { await api.PostAsync<object>("api/v1/auth/logout", new { }, ct); }
        catch (ApiException) { }
        finally { api.ClearToken(); current.Clear(); }
    }
    public Task<AuthenticatedUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default) => Task.FromResult(current.User);
}

public sealed record ApiLoginResponse(string Token, int UserId, string Username, string DisplayName, string Role, IReadOnlyList<string> Permissions, DateTimeOffset ExpiresAt);

public sealed class ApiMeterService(IApiClient api) : IMeterService
{
    public async Task<IReadOnlyList<Meter>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await api.GetAsync<List<ApiMeter>>("api/v1/meters", ct);
        return items.Select(x =>
        {
            var meter = new Meter(x.Name, x.ModbusAddress, x.ComPort, x.BaudRate, x.Parity, x.IsEnabled, x.Model ?? "TOV452");
            typeof(Meter).GetProperty("Id")?.SetValue(meter, x.Id);
            return meter;
        }).ToList();
    }

    public async Task<Meter> CreateAsync(Meter meter, CancellationToken ct = default)
    {
        var created = await api.PostAsync<ApiMeter>("api/v1/meters", new
        {
            name = meter.Name,
            modbusAddress = meter.ModbusAddress,
            comPort = meter.ComPort,
            baudRate = meter.BaudRate,
            parity = meter.Parity,
            isEnabled = meter.IsEnabled,
            model = meter.Model
        }, ct);
        var res = new Meter(created.Name, created.ModbusAddress, created.ComPort, created.BaudRate, created.Parity, created.IsEnabled, created.Model ?? "TOV452");
        typeof(Meter).GetProperty("Id")?.SetValue(res, created.Id);
        return res;
    }

    public async Task<bool> UpdateAsync(int id, string name, byte address, string comPort, int baudRate, string parity, bool enabled, string model = "TOV452", CancellationToken ct = default)
    {
        await api.PutAsync<object>($"api/v1/meters/{id}", new
        {
            name,
            modbusAddress = address,
            comPort,
            baudRate,
            parity,
            isEnabled = enabled,
            model
        }, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await api.DeleteAsync($"api/v1/meters/{id}", ct);
        return true;
    }
}

public sealed record ApiMeter(int Id, string Name, string? Model, byte ModbusAddress, string ComPort, int BaudRate, string Parity, bool IsEnabled);

public sealed class ApiCommunicationSettingsService(IApiClient api) : ICommunicationSettingsService
{
    public async Task<CommunicationSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var dto = await api.GetAsync<ApiCommunicationSettings>("api/v1/settings/communication", ct);
        return new CommunicationSettings(
            dto.ComPort,
            dto.BaudRate,
            dto.DataBits,
            dto.Parity,
            dto.StopBits,
            dto.ReadTimeout,
            dto.WriteTimeout,
            dto.PollingInterval);
    }

    public async Task<CommunicationSettings> SaveSettingsAsync(CommunicationSettings settings, CancellationToken ct = default)
    {
        var dto = await api.PutAsync<ApiCommunicationSettings>("api/v1/settings/communication", new
        {
            comPort = settings.ComPort,
            baudRate = settings.BaudRate,
            dataBits = settings.DataBits,
            parity = settings.Parity,
            stopBits = settings.StopBits,
            readTimeout = settings.ReadTimeout,
            writeTimeout = settings.WriteTimeout,
            pollingInterval = settings.PollingInterval
        }, ct);
        return new CommunicationSettings(
            dto.ComPort,
            dto.BaudRate,
            dto.DataBits,
            dto.Parity,
            dto.StopBits,
            dto.ReadTimeout,
            dto.WriteTimeout,
            dto.PollingInterval);
    }

    public async Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken ct = default)
    {
        return await api.GetAsync<List<string>>("api/v1/settings/ports", ct);
    }

    public async Task<(bool Success, string Message, MeterHardwareState State)> TestConnectionAsync(
        string? comPort,
        byte slaveAddress,
        int baudRate,
        string? parity,
        int dataBits,
        string? stopBits,
        int timeout,
        CancellationToken ct = default)
    {
        var result = await api.PostAsync<ApiTestConnectionResult>("api/v1/settings/test-connection", new
        {
            comPort,
            slaveAddress,
            baudRate,
            parity,
            dataBits,
            stopBits,
            timeout
        }, ct);

        Enum.TryParse<MeterHardwareState>(result.HardwareState, true, out var state);
        return (result.Success, result.Message, state);
    }
}

public sealed record ApiCommunicationSettings(
    int Id,
    string ComPort,
    int BaudRate,
    int DataBits,
    string Parity,
    string StopBits,
    int ReadTimeout,
    int WriteTimeout,
    int PollingInterval,
    DateTimeOffset UpdatedAt);

public sealed record ApiTestConnectionResult(bool Success, string Message, string? HardwareState);

public sealed class ApiHistoricalDataService(IApiClient api) : IHistoricalDataService
{
    public async Task<HistoricalPage> QueryAsync(HistoricalQueryFilter filter, CancellationToken ct = default)
    {
        var query = $"api/v1/measurements?From={Uri.EscapeDataString(filter.From.ToString("O"))}&To={Uri.EscapeDataString(filter.To.ToString("O"))}&MeterId={filter.MeterId}&Variable={Uri.EscapeDataString(filter.Variable ?? "")}&Resolution={filter.Resolution}&PageNumber={filter.PageNumber}&PageSize={filter.PageSize}";
        var result = await api.GetAsync<ApiPage>(query, ct);
        return new HistoricalPage(result.Items.Select(x => new HistoricalDataPoint(x.Id, x.MeterId, x.MeterName, x.Timestamp, x.Variable, x.Value, x.Unit)).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
    }
    public Task<IReadOnlyList<string>> GetVariablesAsync(int? meterId = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);
}

public sealed record ApiPage(List<ApiPoint> Items, int TotalCount, int PageNumber, int PageSize);
public sealed record ApiPoint(long Id, int MeterId, string MeterName, DateTimeOffset Timestamp, string Variable, decimal Value, string Unit);

public sealed class ApiReportService(IApiClient api) : IReportService
{
    public async Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken ct = default)
    {
        var result = await api.PostAsync<ApiReport>("api/v1/reports", request, ct);
        var items = result.Items.Select(x => new HistoricalDataPoint(x.Id, x.MeterId, x.MeterName, x.Timestamp, x.Variable, x.Value, x.Unit)).ToList();
        return new ReportResult(request, new HistoricalPage(items, result.TotalCount, 1, request.PageSize), result.GeneratedAt, null);
    }
    public Task ExportCsvAsync(ReportRequest request, string filePath, CancellationToken ct = default) => throw new NotSupportedException("La exportación de reportes debe implementarse mediante el endpoint autorizado del servidor.");
    public Task<byte[]> CreateAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());
}

public sealed record ApiReport(int TotalCount, DateTimeOffset GeneratedAt, List<ApiPoint> Items);

public sealed class ApiUserAdminService(IApiClient api) : IUserAdminService
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await api.GetAsync<List<ApiUser>>("api/v1/users", ct);
        return users.Select(x => new User(x.Username, x.DisplayName, "", "", x.IsActive)).ToList();
    }
    public Task<User> CreateAsync(string username, string displayName, string password, string role, CancellationToken ct = default) => throw new NotSupportedException();
    public Task SetActiveAsync(int userId, bool active, CancellationToken ct = default) => throw new NotSupportedException();
    public Task ResetPasswordAsync(int userId, string temporaryPassword, CancellationToken ct = default) => throw new NotSupportedException();
}

public sealed record ApiUser(string Username, string DisplayName, bool IsActive, DateTimeOffset? LastLoginAt);

public sealed class ApiAcquisitionService(IApiClient api) : IDataAcquisitionService
{
    private readonly ConcurrentDictionary<int, Measurement> latest = new();
    private readonly ConcurrentDictionary<int, MeterConnectionStatus> meterStatuses = new();

    public AcquisitionState State { get; private set; } = AcquisitionState.Stopped;
    public MeterHardwareState HardwareState { get; private set; } = MeterHardwareState.ESPERANDO_MEDIDOR;
    public string CurrentPort { get; private set; } = string.Empty;
    public int ActiveMetersCount { get; private set; } = 0;
    public DateTimeOffset? LastAcquisitionTime { get; private set; }
    public string? LastError { get; private set; }
    public long TotalReadingsProcessed { get; private set; } = 0;

    public IReadOnlyDictionary<int, Measurement> LastMeasurements => latest;
    public IReadOnlyDictionary<int, MeterConnectionStatus> MeterStatuses => meterStatuses;

    public event EventHandler<AcquisitionEvent>? EventRaised;
    public event EventHandler<Measurement>? MeasurementReceived;

    public async Task RefreshStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = await api.GetAsync<ApiAcquisitionStatus>("api/v1/acquisition/status", ct);
            UpdateFromDto(dto);
        }
        catch { }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var dto = await api.PostAsync<ApiAcquisitionStatus>("api/v1/acquisition/start", new { }, cancellationToken);
        UpdateFromDto(dto);
        EventRaised?.Invoke(this, new AcquisitionEvent("AcquisitionStarted", null, "Adquisición iniciada en el servidor.", DateTimeOffset.UtcNow));
    }

    public async Task StopAsync()
    {
        var dto = await api.PostAsync<ApiAcquisitionStatus>("api/v1/acquisition/stop", new { });
        UpdateFromDto(dto);
        EventRaised?.Invoke(this, new AcquisitionEvent("AcquisitionStopped", null, "Adquisición detenida en el servidor.", DateTimeOffset.UtcNow));
    }

    public AcquisitionStatusSummary GetStatusSummary() =>
        new(State, HardwareState, HardwareState.ToDisplayString(), CurrentPort, ActiveMetersCount, LastAcquisitionTime, LastError, TotalReadingsProcessed);

    public void OnMeasurementReceived(Measurement measurement)
    {
        latest[measurement.MeterId] = measurement;
        TotalReadingsProcessed++;
        LastAcquisitionTime = measurement.Timestamp;
        MeasurementReceived?.Invoke(this, measurement);
    }

    public void OnAcquisitionEvent(AcquisitionEvent evt)
    {
        EventRaised?.Invoke(this, evt);
    }

    private void UpdateFromDto(ApiAcquisitionStatus dto)
    {
        if (Enum.TryParse<AcquisitionState>(dto.State, true, out var parsedState))
            State = parsedState;
        if (Enum.TryParse<MeterHardwareState>(dto.HardwareState, true, out var parsedHwState))
            HardwareState = parsedHwState;
        CurrentPort = dto.CurrentPort;
        ActiveMetersCount = dto.ActiveMetersCount;
        LastAcquisitionTime = dto.LastAcquisitionTime;
        LastError = dto.LastError;
        TotalReadingsProcessed = dto.TotalReadingsProcessed;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed record ApiAcquisitionStatus(
    string State,
    string HardwareState,
    string HardwareStatusText,
    string CurrentPort,
    int ActiveMetersCount,
    DateTimeOffset? LastAcquisitionTime,
    string? LastError,
    long TotalReadingsProcessed);

public sealed class ClientCurrentUserService : ICurrentUserService
{
    public AuthenticatedUser? User { get; private set; }
    public bool IsAuthenticated => User is not null;
    public void SetUser(AuthenticatedUser user) => User = user;
    public void Clear() => User = null;
}

public sealed class ClientDataAggregationService : IDataAggregationService
{
    public IReadOnlyList<AggregatedPoint> Aggregate(IEnumerable<HistoricalDataPoint> points, HistoricalResolution resolution, AggregationOperation operation = AggregationOperation.Average)
    {
        var items = points.OrderBy(x => x.Timestamp).ToArray();
        if (resolution is HistoricalResolution.Raw or HistoricalResolution.Automatic) return items.Select(x => new AggregatedPoint(x.Timestamp, x.Value, x.Unit)).ToArray();
        var span = resolution switch
        {
            HistoricalResolution.FiveMinutes => TimeSpan.FromMinutes(5),
            HistoricalResolution.FifteenMinutes => TimeSpan.FromMinutes(15),
            HistoricalResolution.Hour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromMinutes(1)
        };
        return items.GroupBy(x => (x.Timestamp.UtcTicks / span.Ticks) * span.Ticks).Select(x => new AggregatedPoint(new DateTimeOffset(x.Key, TimeSpan.Zero), x.Average(p => p.Value), x.First().Unit)).ToList();
    }
}

public sealed class ClientCsvExporter : IHistoricalDataExporter
{
    public async Task ExportCsvAsync(IEnumerable<HistoricalDataPoint> points, string path, CancellationToken ct = default)
    {
        await using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        await writer.WriteLineAsync("Timestamp,Meter,Variable,Value,Unit");
        foreach (var x in points)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync($"{x.Timestamp:O},{Escape(x.MeterName)},{Escape(x.Variable)},{x.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Escape(x.Unit)}");
        }
    }
    private static string Escape(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}

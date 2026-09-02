using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    protected void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class AsyncCommand(Func<Task> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public async void Execute(object? parameter) => await execute();
}

public sealed class MainViewModel : ObservableObject
{
    private readonly IMeterService meterService;
    private readonly IDataAcquisitionService acquisition;
    private string section = "Dashboard";
    private object currentViewModel;

    public DashboardViewModel Dashboard { get; }
    public MetersViewModel Meters { get; }
    public RealtimeMonitoringViewModel Monitoring { get; }
    public HistoricalViewModel Historical { get; }
    public UsersViewModel Users { get; }
    public ReportsViewModel Reports { get; }
    public SettingsViewModel Settings { get; }

    public object CurrentViewModel { get => currentViewModel; private set => Set(ref currentViewModel, value); }
    public string CurrentSection { get => section; private set => Set(ref section, value); }

    public ICommand DashboardCommand { get; }
    public ICommand MetersCommand { get; }
    public ICommand MonitoringCommand { get; }
    public ICommand HistoryCommand { get; }
    public ICommand ReportsCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }

    public MainViewModel(
        IMeterService meterService,
        ICommunicationSettingsService settingsService,
        IDataAcquisitionService acquisition,
        IHistoricalDataService historical,
        IDataAggregationService aggregation,
        IHistoricalDataExporter exporter,
        IUserAdminService userAdmin,
        IReportService reports)
    {
        this.meterService = meterService;
        this.acquisition = acquisition;

        Dashboard = new(acquisition);
        Meters = new(meterService, acquisition);
        Monitoring = new(acquisition);
        Historical = new(meterService, historical, aggregation, exporter);
        Users = new(userAdmin);
        Reports = new(reports);
        Settings = new(settingsService, meterService, acquisition);

        currentViewModel = Dashboard;

        DashboardCommand = new AsyncCommand(() => NavigateAsync("Dashboard", Dashboard));
        MetersCommand = new AsyncCommand(() => NavigateAsync("Medidores", Meters));
        MonitoringCommand = new AsyncCommand(() => NavigateAsync("Monitoreo", Monitoring));
        HistoryCommand = new AsyncCommand(() => NavigateAsync("Históricos", Historical));
        ReportsCommand = new AsyncCommand(() => NavigateAsync("Reportes", Reports));
        SettingsCommand = new AsyncCommand(() => NavigateAsync("Configuración", Settings));

        StartCommand = new AsyncCommand(async () =>
        {
            await acquisition.StartAsync();
            Dashboard.Refresh();
            Settings.RefreshLiveStatus();
        });

        StopCommand = new AsyncCommand(async () =>
        {
            await acquisition.StopAsync();
            Dashboard.Refresh();
            Settings.RefreshLiveStatus();
        });
    }

    public async Task LoadAsync()
    {
        try { await Settings.LoadAsync(); } catch { }
        try
        {
            await Meters.LoadAsync();
            Dashboard.SetMeterCount(Meters.Rows.Count);
        }
        catch
        {
            Dashboard.SetMeterCount(0);
        }
        try { await Historical.LoadAsync(); } catch { }
        try { await Users.LoadAsync(); } catch { }
    }

    private Task NavigateAsync(string name, object view)
    {
        CurrentSection = name;
        CurrentViewModel = view;
        return Task.CompletedTask;
    }
}

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IDataAcquisitionService acquisition;
    private int total;
    private string state = "Adquisición detenida";

    public int TotalMeters { get => total; private set => Set(ref total, value); }
    public int ConnectedMeters => acquisition.MeterStatuses.Values.Count(x => x.IsConnected);
    public int DisconnectedMeters => Math.Max(0, TotalMeters - ConnectedMeters);
    public int CommunicationErrors => acquisition.MeterStatuses.Values.Count(x => !string.IsNullOrWhiteSpace(x.LastError));
    public string LastUpdate => acquisition.LastAcquisitionTime?.ToLocalTime().ToString("HH:mm:ss")
        ?? acquisition.MeterStatuses.Values.Select(x => x.LastCheckedAt).Max()?.ToLocalTime().ToString("HH:mm:ss")
        ?? "--";
    public string AcquisitionState { get => state; private set => Set(ref state, value); }
    public string OperationMode => "Hardware Real • RS485 / Modbus RTU";
    public string HardwareStatusBadge => acquisition.HardwareState.ToDisplayString();
    public string HardwareStatusColor => acquisition.HardwareState.ToBadgeColor();
    public string SubtitleMessage => acquisition.TotalReadingsProcessed == 0 ? "Esperando datos del TOV452" : "Telemetría eléctrica en vivo";

    public ObservableCollection<LiveMetricCard> MetricCards { get; } = [];

    public DashboardViewModel(IDataAcquisitionService acquisition)
    {
        this.acquisition = acquisition;
        InitializeMetricCards();
        acquisition.EventRaised += OnEvent;
        acquisition.MeasurementReceived += OnMeasurement;
    }

    private void InitializeMetricCards()
    {
        MetricCards.Add(new LiveMetricCard("Voltaje Promedio", "--", "V", "--"));
        MetricCards.Add(new LiveMetricCard("Corriente Promedio", "--", "A", "--"));
        MetricCards.Add(new LiveMetricCard("Potencia Activa", "--", "W", "--"));
        MetricCards.Add(new LiveMetricCard("Factor de Potencia", "--", "", "--"));
        MetricCards.Add(new LiveMetricCard("Frecuencia", "--", "Hz", "--"));
        MetricCards.Add(new LiveMetricCard("Energía Acumulada", "--", "kWh", "--"));
        MetricCards.Add(new LiveMetricCard("THD Tensión", "--", "%", "--"));
    }

    public void SetMeterCount(int count)
    {
        TotalMeters = count;
        Refresh();
    }

    public void Refresh()
    {
        Notify(nameof(ConnectedMeters));
        Notify(nameof(DisconnectedMeters));
        Notify(nameof(CommunicationErrors));
        Notify(nameof(LastUpdate));
        Notify(nameof(OperationMode));
        Notify(nameof(HardwareStatusBadge));
        Notify(nameof(HardwareStatusColor));
        Notify(nameof(SubtitleMessage));
    }

    private void OnEvent(object? sender, AcquisitionEvent e)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.BeginInvoke(Refresh);
        else
            Refresh();

        AcquisitionState = e.EventType switch
        {
            "AcquisitionStarted" => "Esperando conexión del medidor",
            "AcquisitionStopped" => "Adquisición detenida",
            "CycleError" => "Adquisición con errores",
            _ => AcquisitionState
        };
    }

    private void OnMeasurement(object? sender, Measurement m)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.BeginInvoke(() => UpdateMetric(m));
        else
            UpdateMetric(m);
    }

    private void UpdateMetric(Measurement m)
    {
        Refresh();
        var key = m.Variable switch
        {
            "Voltage" or "V" => "Voltaje Promedio",
            "Current" or "I" => "Corriente Promedio",
            "ActivePower" or "P" or "Psum" => "Potencia Activa",
            "PowerFactor" or "PF" => "Factor de Potencia",
            "Frequency" or "F" => "Frecuencia",
            "Energy" => "Energía Acumulada",
            "VoltageThd" or "THD" => "THD Tensión",
            _ => null
        };

        if (key is not null)
        {
            var card = MetricCards.FirstOrDefault(c => c.Variable == key);
            var unit = string.IsNullOrEmpty(m.Unit) ? (key == "Factor de Potencia" ? "" : m.Unit) : m.Unit;
            if (card is null)
            {
                MetricCards.Add(new LiveMetricCard(key, m.Value.ToString("0.##"), unit, m.Timestamp.ToLocalTime().ToString("HH:mm:ss")));
            }
            else
            {
                card.Update(m.Value.ToString("0.##"), m.Timestamp.ToLocalTime().ToString("HH:mm:ss"));
            }
        }
    }
}

public sealed class MeterRowViewModel(Meter meter, IDataAcquisitionService acquisition) : ObservableObject
{
    public int MeterId => meter.Id;
    public string Name => meter.Name;
    public string Model => meter.Model;
    public string Address => meter.ModbusAddress.ToString();
    public string Port => meter.ComPort;
    public int BaudRate => meter.BaudRate;
    public string Parity => meter.Parity;
    public bool IsEnabled => meter.IsEnabled;
    public string IsEnabledText => meter.IsEnabled ? "Sí" : "No";

    public string State => acquisition.State == TotalMonitor.Core.Interfaces.AcquisitionState.Active
        ? (acquisition.MeterStatuses.TryGetValue(meter.Id, out var status) && status.IsConnected ? "Conectado" : "Esperando medidor")
        : (acquisition.MeterStatuses.TryGetValue(meter.Id, out var st) ? st.State : "Desconectado");

    public string LastCommunication => acquisition.MeterStatuses.TryGetValue(meter.Id, out var status)
        ? status.LastSuccessfulCommunication?.ToLocalTime().ToString("HH:mm:ss") ?? "--"
        : "--";

    public string ResponseTime => acquisition.MeterStatuses.TryGetValue(meter.Id, out var status) && status.IsConnected
        ? $"{status.LastResponseTimeMilliseconds?.ToString() ?? "--"} ms"
        : "--";

    public string Errors => acquisition.MeterStatuses.TryGetValue(meter.Id, out var status) ? status.ConsecutiveFailures.ToString() : "0";
    public string LastError => acquisition.MeterStatuses.TryGetValue(meter.Id, out var status) ? status.LastError ?? "--" : "--";

    public void Refresh()
    {
        Notify(nameof(State));
        Notify(nameof(LastCommunication));
        Notify(nameof(ResponseTime));
        Notify(nameof(Errors));
        Notify(nameof(LastError));
        Notify(nameof(IsEnabled));
        Notify(nameof(IsEnabledText));
    }
}

public sealed class RealtimeMonitoringViewModel : ObservableObject
{
    private readonly IDataAcquisitionService acquisition;
    public ObservableCollection<LiveMetricCard> LiveCards { get; } = [];

    public string EmptyMessage => LiveCards.Count == 0 && acquisition.LastMeasurements.Count == 0
        ? "Sin mediciones disponibles. Esperando conexión del medidor TOV452..."
        : "Telemetría en tiempo real activa.";

    public string ChartMessage => LiveCards.Count > 0
        ? "Monitoreo en vivo conectado al flujo de datos."
        : "Sin datos reales para graficar actualmente.";

    public string SimulatorIndicator => acquisition.State == TotalMonitor.Core.Interfaces.AcquisitionState.Active
        ? $"● {acquisition.HardwareState.ToDisplayString().ToUpperInvariant()}"
        : "○ ADQUISICIÓN DETENIDA";

    public string IndicatorColor => acquisition.HardwareState.ToBadgeColor();

    public RealtimeMonitoringViewModel(IDataAcquisitionService acquisition)
    {
        this.acquisition = acquisition;
        acquisition.EventRaised += (_, _) => DispatchUpdate();
        acquisition.MeasurementReceived += (_, m) => OnMeasurement(m);
    }

    private void OnMeasurement(Measurement m)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.BeginInvoke(() => UpdateCard(m));
        else
            UpdateCard(m);
    }

    private void UpdateCard(Measurement m)
    {
        var existing = LiveCards.FirstOrDefault(c => c.Variable == m.Variable);
        if (existing is null)
        {
            LiveCards.Add(new LiveMetricCard(m.Variable, m.Value.ToString("0.##"), m.Unit, m.Timestamp.ToLocalTime().ToString("HH:mm:ss")));
        }
        else
        {
            existing.Update(m.Value.ToString("0.##"), m.Timestamp.ToLocalTime().ToString("HH:mm:ss"));
        }
        Notify(nameof(EmptyMessage));
        Notify(nameof(ChartMessage));
        Notify(nameof(SimulatorIndicator));
        Notify(nameof(IndicatorColor));
    }

    private void DispatchUpdate()
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
        {
            dispatcher.BeginInvoke(() =>
            {
                Notify(nameof(EmptyMessage));
                Notify(nameof(ChartMessage));
                Notify(nameof(SimulatorIndicator));
                Notify(nameof(IndicatorColor));
            });
        }
    }
}

public sealed class LiveMetricCard(string variable, string value, string unit, string time) : ObservableObject
{
    public string Variable { get; } = variable;
    public string Unit { get; } = unit;
    private string val = value;
    public string Value { get => val; private set => Set(ref val, value); }
    private string timestamp = time;
    public string Timestamp { get => timestamp; private set => Set(ref timestamp, value); }

    public void Update(string newValue, string newTimestamp)
    {
        Value = newValue;
        Timestamp = newTimestamp;
    }
}

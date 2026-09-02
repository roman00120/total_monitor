using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public sealed class HistoricalViewModel : ObservableObject
{
    private readonly IMeterService meters;
    private readonly IHistoricalDataService history;
    private readonly IDataAggregationService aggregation;
    private readonly IHistoricalDataExporter exporter;
    private CancellationTokenSource? queryCancellation;

    private string message = "Seleccione filtros y pulse Consultar.";
    private HistoricalViewState state = HistoricalViewState.Idle;
    private int page = 1;

    public ObservableCollection<MeterRowViewModel> MeterOptions { get; } = [];
    public ObservableCollection<string> Variables { get; } = [];
    public ObservableCollection<HistoricalRowViewModel> Results { get; } = [];
    public ObservableCollection<AggregatedPoint> ChartPoints { get; } = [];

    private MeterRowViewModel? selectedMeter;
    public MeterRowViewModel? SelectedMeter { get => selectedMeter; set => Set(ref selectedMeter, value); }

    private string? selectedVariable;
    public string? SelectedVariable { get => selectedVariable; set => Set(ref selectedVariable, value); }

    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-1);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public HistoricalResolution Resolution { get; set; } = HistoricalResolution.Automatic;
    public int PageSize { get; set; } = 500;
    public int PageNumber { get => page; private set => Set(ref page, value); }
    public int TotalCount { get; private set; }
    public string Message { get => message; private set => Set(ref message, value); }
    public HistoricalViewState State { get => state; private set => Set(ref state, value); }

    public bool CanGoPrevious => PageNumber > 1;
    public bool CanGoNext => PageNumber * PageSize < TotalCount;

    public ICommand QueryCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand LastHourCommand { get; }
    public ICommand TodayCommand { get; }
    public ICommand YesterdayCommand { get; }
    public ICommand Last7DaysCommand { get; }
    public ICommand ExportCommand { get; }

    public HistoricalViewModel(
        IMeterService meters,
        IHistoricalDataService history,
        IDataAggregationService aggregation,
        IHistoricalDataExporter exporter)
    {
        this.meters = meters;
        this.history = history;
        this.aggregation = aggregation;
        this.exporter = exporter;

        QueryCommand = new AsyncCommand(QueryAsync);
        PreviousCommand = new AsyncCommand(async () =>
        {
            if (CanGoPrevious)
            {
                PageNumber--;
                await QueryAsync(false);
            }
        });
        NextCommand = new AsyncCommand(async () =>
        {
            if (CanGoNext)
            {
                PageNumber++;
                await QueryAsync(false);
            }
        });

        LastHourCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Now.AddHours(-1), DateTime.Now));
        TodayCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Today, DateTime.Today));
        YesterdayCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1)));
        Last7DaysCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Today.AddDays(-7), DateTime.Today));
        ExportCommand = new AsyncCommand(ExportAsync);
    }

    public async Task LoadAsync()
    {
        MeterOptions.Clear();
        foreach (var meter in await meters.GetAllAsync())
            MeterOptions.Add(new MeterRowViewModel(meter, new EmptyAcquisitionService()));

        Variables.Clear();
        foreach (var variable in await history.GetVariablesAsync())
            Variables.Add(variable);
    }

    private async Task SetRangeAsync(DateTime from, DateTime to)
    {
        FromDate = from;
        ToDate = to;
        Notify(nameof(FromDate));
        Notify(nameof(ToDate));
        await QueryAsync();
    }

    private async Task QueryAsync() => await QueryAsync(true);

    private async Task QueryAsync(bool resetPage)
    {
        queryCancellation?.Cancel();
        queryCancellation?.Dispose();
        queryCancellation = new CancellationTokenSource();
        if (resetPage) PageNumber = 1;
        var ct = queryCancellation.Token;

        try
        {
            var from = new DateTimeOffset(FromDate, TimeZoneInfo.Local.GetUtcOffset(FromDate));
            var to = new DateTimeOffset(ToDate.Date.AddDays(1).AddTicks(-1), TimeZoneInfo.Local.GetUtcOffset(ToDate));
            var filter = new HistoricalQueryFilter(
                from.ToUniversalTime(),
                to.ToUniversalTime(),
                SelectedMeter?.MeterId,
                SelectedVariable,
                Resolution,
                PageNumber,
                PageSize);

            State = HistoricalViewState.Loading;
            Message = "Cargando históricos desde base de datos...";

            var result = await history.QueryAsync(filter, ct);
            Results.Clear();
            foreach (var item in result.Items)
            {
                Results.Add(new HistoricalRowViewModel(item, "REAL"));
            }

            TotalCount = result.TotalCount;
            ChartPoints.Clear();
            foreach (var point in aggregation.Aggregate(result.Items, Resolution))
                ChartPoints.Add(point);

            State = Results.Count == 0 ? HistoricalViewState.Empty : HistoricalViewState.Loaded;
            Message = Results.Count == 0
                ? "No hay mediciones registradas para el periodo y filtros seleccionados."
                : $"{TotalCount} mediciones reales encontradas.";

            Notify(nameof(CanGoPrevious));
            Notify(nameof(CanGoNext));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            State = HistoricalViewState.Error;
            Message = $"No fue posible consultar el histórico: {ex.Message}";
        }
    }

    private async Task ExportAsync()
    {
        if (Results.Count == 0)
        {
            Message = "No hay datos consultados para exportar.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"total-monitor-historico-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            var dataPoints = Results.Select(r => r.Item).ToList();
            await exporter.ExportCsvAsync(dataPoints, dialog.FileName);
            Message = $"✓ Archivo CSV exportado correctamente ({Results.Count} registros).";
        }
    }
}

public sealed class HistoricalRowViewModel(HistoricalDataPoint item, string origin)
{
    public HistoricalDataPoint Item => item;
    public string Timestamp => item.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string MeterName => item.MeterName;
    public string Variable => item.Variable;
    public string Value => item.Value.ToString("0.##");
    public string Unit => item.Unit;
    public string Origin => origin;
}

internal sealed class EmptyAcquisitionService : IDataAcquisitionService
{
    public AcquisitionState State => AcquisitionState.Stopped;
    public MeterHardwareState HardwareState => MeterHardwareState.NO_CONFIGURADO;
    public string CurrentPort => "";
    public int ActiveMetersCount => 0;
    public DateTimeOffset? LastAcquisitionTime => null;
    public string? LastError => null;
    public long TotalReadingsProcessed => 0;
    public event EventHandler<AcquisitionEvent>? EventRaised { add { } remove { } }
    public event EventHandler<Measurement>? MeasurementReceived { add { } remove { } }
    public IReadOnlyDictionary<int, Measurement> LastMeasurements => new Dictionary<int, Measurement>();
    public IReadOnlyDictionary<int, MeterConnectionStatus> MeterStatuses => new Dictionary<int, MeterConnectionStatus>();
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public AcquisitionStatusSummary GetStatusSummary() => new(AcquisitionState.Stopped, MeterHardwareState.NO_CONFIGURADO, "No configurado", "", 0, null, null, 0);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

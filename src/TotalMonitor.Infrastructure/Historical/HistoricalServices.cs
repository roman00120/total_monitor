using System.Text;
using Microsoft.Extensions.Logging;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.Infrastructure.Historical;

public sealed class HistoricalDataService(IMeasurementRepository repository, ILogger<HistoricalDataService> logger) : IHistoricalDataService
{
    public async Task<HistoricalPage> QueryAsync(HistoricalQueryFilter filter, CancellationToken ct = default)
    { filter.Validate(DateTimeOffset.UtcNow); logger.LogInformation("Historical query started. MeterId={MeterId}, Variable={Variable}, From={From}, To={To}, Page={Page}.", filter.MeterId, filter.Variable, filter.From, filter.To, filter.PageNumber); var result = await repository.GetRangeAsync(filter, ct); logger.LogInformation("Historical query returned {Count} of {Total} records.", result.Items.Count, result.TotalCount); return result; }
    public Task<IReadOnlyList<string>> GetVariablesAsync(int? meterId = null, CancellationToken ct = default) => repository.GetVariablesAsync(meterId, ct);
}

public sealed class DataAggregationService : IDataAggregationService
{
    public IReadOnlyList<AggregatedPoint> Aggregate(IEnumerable<HistoricalDataPoint> source, HistoricalResolution resolution, AggregationOperation operation = AggregationOperation.Average)
    {
        var points = source.ToArray(); if (resolution is HistoricalResolution.Automatic) resolution = points.Length > 2000 ? HistoricalResolution.Minute : HistoricalResolution.Raw; if (resolution is HistoricalResolution.Raw) return points.OrderBy(x => x.Timestamp).Select(x => new AggregatedPoint(x.Timestamp, x.Value, x.Unit)).ToArray(); var span = resolution switch { HistoricalResolution.Minute => TimeSpan.FromMinutes(1), HistoricalResolution.FiveMinutes => TimeSpan.FromMinutes(5), HistoricalResolution.FifteenMinutes => TimeSpan.FromMinutes(15), HistoricalResolution.Hour => TimeSpan.FromHours(1), _ => TimeSpan.FromMinutes(1) }; return points.GroupBy(x => (x.Timestamp.UtcTicks / span.Ticks) * span.Ticks).OrderBy(x => x.Key).Select(group => new AggregatedPoint(new DateTimeOffset(group.Key, TimeSpan.Zero), Apply(group.Select(x => x.Value), operation), group.First().Unit)).ToArray();
    }
    private static decimal Apply(IEnumerable<decimal> values, AggregationOperation operation) { var a = values.ToArray(); return operation switch { AggregationOperation.Minimum => a.Min(), AggregationOperation.Maximum => a.Max(), AggregationOperation.Sum => a.Sum(), AggregationOperation.First => a[0], AggregationOperation.Last => a[^1], _ => a.Average() }; }
}

public sealed class CsvHistoricalDataExporter(ILogger<CsvHistoricalDataExporter> logger) : IHistoricalDataExporter
{
    public async Task ExportCsvAsync(IEnumerable<HistoricalDataPoint> points, string filePath, CancellationToken ct = default)
    { logger.LogInformation("Historical CSV export started."); await using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)); await writer.WriteLineAsync("Timestamp,Meter,Variable,Value,Unit"); foreach (var point in points) { ct.ThrowIfCancellationRequested(); await writer.WriteLineAsync(string.Join(',', Escape(point.Timestamp.ToString("O")), Escape(point.MeterName), Escape(point.Variable), point.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), Escape(point.Unit))); } logger.LogInformation("Historical CSV export finished."); }
    private static string Escape(string value) => value.Contains(',', StringComparison.Ordinal) || value.Contains('"') || value.Contains('\n') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}

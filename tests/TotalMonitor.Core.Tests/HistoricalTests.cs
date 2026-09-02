using TotalMonitor.Core.Historical;
using TotalMonitor.Infrastructure.Historical;

namespace TotalMonitor.Core.Tests;
public sealed class HistoricalTests
{
    [Fact] public void Filter_rejects_reversed_dates() => Assert.Throws<ArgumentException>(() => new HistoricalQueryFilter(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1)).Validate(DateTimeOffset.UtcNow));
    [Fact] public void Aggregation_groups_points_without_changing_raw_values()
    {
        var start = DateTimeOffset.UtcNow.Date; var input = new[] { new HistoricalDataPoint(1, 1, "Simulator", start.AddSeconds(1), "Configured", 2, "u"), new HistoricalDataPoint(2, 1, "Simulator", start.AddSeconds(2), "Configured", 4, "u") };
        var result = new DataAggregationService().Aggregate(input, HistoricalResolution.Minute);
        Assert.Single(result); Assert.Equal(3, result[0].Value);
    }
    [Fact] public async Task Csv_export_escapes_special_characters()
    {
        var file = Path.Combine(Path.GetTempPath(), $"total-monitor-{Guid.NewGuid():N}.csv"); try { await new CsvHistoricalDataExporter(Microsoft.Extensions.Logging.Abstractions.NullLogger<CsvHistoricalDataExporter>.Instance).ExportCsvAsync([new HistoricalDataPoint(1, 1, "Meter, 1", DateTimeOffset.UtcNow, "Configured", 1.5m, "u")], file); var csv = await File.ReadAllTextAsync(file); Assert.Contains("\"Meter, 1\"", csv); } finally { if (File.Exists(file)) File.Delete(file); }
    }
}

using TotalMonitor.Core.Historical;
namespace TotalMonitor.Core.Interfaces;
public interface IHistoricalDataService { Task<HistoricalPage> QueryAsync(HistoricalQueryFilter filter, CancellationToken cancellationToken = default); Task<IReadOnlyList<string>> GetVariablesAsync(int? meterId = null, CancellationToken cancellationToken = default); }
public interface IDataAggregationService { IReadOnlyList<AggregatedPoint> Aggregate(IEnumerable<HistoricalDataPoint> points, HistoricalResolution resolution, AggregationOperation operation = AggregationOperation.Average); }
public interface IHistoricalDataExporter { Task ExportCsvAsync(IEnumerable<HistoricalDataPoint> points, string filePath, CancellationToken cancellationToken = default); }

using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Historical;

public enum HistoricalResolution { Automatic, Raw, Minute, FiveMinutes, FifteenMinutes, Hour }
public enum AggregationOperation { Average, Minimum, Maximum, Sum, First, Last }
public enum HistoricalViewState { Idle, Loading, Loaded, Empty, Error }
public sealed record HistoricalQueryFilter(DateTimeOffset From, DateTimeOffset To, int? MeterId = null, string? Variable = null, HistoricalResolution Resolution = HistoricalResolution.Automatic, int PageNumber = 1, int PageSize = 500)
{
    public void Validate(DateTimeOffset now)
    { if (From > To) throw new ArgumentException("La fecha inicial no puede ser mayor que la fecha final."); if (From > now) throw new ArgumentException("La fecha inicial no puede estar en el futuro."); if (PageNumber < 1) throw new ArgumentOutOfRangeException(nameof(PageNumber)); if (PageSize is < 1 or > 5000) throw new ArgumentOutOfRangeException(nameof(PageSize)); }
}
public sealed record HistoricalDataPoint(long Id, int MeterId, string MeterName, DateTimeOffset Timestamp, string Variable, decimal Value, string Unit);
public sealed record HistoricalPage(IReadOnlyList<HistoricalDataPoint> Items, int TotalCount, int PageNumber, int PageSize);
public sealed record AggregatedPoint(DateTimeOffset Timestamp, decimal Value, string Unit);
public sealed record HistoricalChartData(string Variable, string Unit, DateTimeOffset From, DateTimeOffset To, IReadOnlyList<AggregatedPoint> Points);

using Microsoft.EntityFrameworkCore;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Infrastructure.Persistence;

namespace TotalMonitor.Infrastructure.Repositories;

public sealed class MeasurementRepository(TotalMonitorDbContext db) : IMeasurementRepository
{
    public async Task AddRangeAsync(IEnumerable<Measurement> measurements, CancellationToken ct = default) => await db.Measurements.AddRangeAsync(measurements, ct);
    public async Task<HistoricalPage> GetRangeAsync(HistoricalQueryFilter filter, CancellationToken ct = default)
    {
        var query = Project(db.Measurements.AsNoTracking().Where(x => x.Timestamp >= filter.From && x.Timestamp <= filter.To));
        if (filter.MeterId is not null) query = query.Where(x => x.MeterId == filter.MeterId);
        if (!string.IsNullOrWhiteSpace(filter.Variable)) query = query.Where(x => x.Variable == filter.Variable);
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.Timestamp).Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        return new HistoricalPage(items, total, filter.PageNumber, filter.PageSize);
    }
    public async Task<IReadOnlyList<HistoricalDataPoint>> GetLatestAsync(int meterId, string? variable = null, int take = 100, CancellationToken ct = default)
    { var query = Project(db.Measurements.AsNoTracking().Where(x => x.MeterId == meterId)); if (!string.IsNullOrWhiteSpace(variable)) query = query.Where(x => x.Variable == variable); return await query.OrderByDescending(x => x.Timestamp).Take(Math.Clamp(take, 1, 5000)).ToListAsync(ct); }
    public async Task<IReadOnlyList<HistoricalDataPoint>> GetByMeterAsync(int meterId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) => await Project(db.Measurements.AsNoTracking().Where(x => x.MeterId == meterId && x.Timestamp >= from && x.Timestamp <= to)).OrderBy(x => x.Timestamp).ToListAsync(ct);
    public async Task<IReadOnlyList<HistoricalDataPoint>> GetByVariableAsync(string variable, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) => await Project(db.Measurements.AsNoTracking().Where(x => x.Variable == variable && x.Timestamp >= from && x.Timestamp <= to)).OrderBy(x => x.Timestamp).ToListAsync(ct);
    public async Task<IReadOnlyList<string>> GetVariablesAsync(int? meterId = null, CancellationToken ct = default) { var query = db.Measurements.AsNoTracking().AsQueryable(); if (meterId is not null) query = query.Where(x => x.MeterId == meterId); return await query.Select(x => x.Variable).Distinct().OrderBy(x => x).ToListAsync(ct); }
    private IQueryable<HistoricalDataPoint> Project(IQueryable<Measurement> query) => query.Join(db.Meters.AsNoTracking(), measurement => measurement.MeterId, meter => meter.Id, (measurement, meter) => new HistoricalDataPoint(measurement.Id, measurement.MeterId, meter.Name, measurement.Timestamp, measurement.Variable, measurement.Value, measurement.Unit));
}
public sealed class MeterConnectionStatusRepository(TotalMonitorDbContext db) : IMeterConnectionStatusRepository
{
    public async Task<MeterConnectionStatus> GetOrCreateAsync(int meterId, CancellationToken ct = default) => await db.MeterConnectionStatuses.SingleOrDefaultAsync(x => x.MeterId == meterId, ct) ?? new MeterConnectionStatus(meterId, false);
    public Task SaveAsync(MeterConnectionStatus status, CancellationToken ct = default) { if (status.Id == 0) db.MeterConnectionStatuses.Add(status); return Task.CompletedTask; }
}

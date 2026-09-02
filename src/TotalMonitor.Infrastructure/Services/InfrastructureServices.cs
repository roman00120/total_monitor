using Microsoft.EntityFrameworkCore;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Infrastructure.Persistence;

namespace TotalMonitor.Infrastructure.Services;

public sealed class MeterService(TotalMonitorDbContext db) : IMeterService
{
    public async Task<IReadOnlyList<Meter>> GetAllAsync(CancellationToken ct = default) =>
        await db.Meters.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<Meter> CreateAsync(Meter meter, CancellationToken ct = default)
    {
        db.Meters.Add(meter);
        await db.SaveChangesAsync(ct);
        return meter;
    }

    public async Task<bool> UpdateAsync(int id, string name, byte address, string comPort, int baudRate, string parity, bool enabled, string model = "TOV452", CancellationToken ct = default)
    {
        var meter = await db.Meters.FindAsync([id], ct);
        if (meter is null) return false;
        meter.Update(name, address, comPort, baudRate, parity, enabled, model);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var meter = await db.Meters.FindAsync([id], ct);
        if (meter is null) return false;
        meter.Update(meter.Name, meter.ModbusAddress, meter.ComPort, meter.BaudRate, meter.Parity, false, meter.Model);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class MeasurementService(TotalMonitorDbContext db) : IMeasurementService
{
    public async Task<IReadOnlyList<Measurement>> GetByMeterAsync(int meterId, CancellationToken ct = default) =>
        await db.Measurements.AsNoTracking().Where(x => x.MeterId == meterId).OrderByDescending(x => x.Timestamp).ToListAsync(ct);
}

public sealed class UserService(TotalMonitorDbContext db) : IUserService
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync(ct);
}

namespace TotalMonitor.Core.Entities;

public sealed class Measurement
{
    public long Id { get; private set; }
    public int MeterId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string Variable { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string Unit { get; private set; } = string.Empty;

    private Measurement() { }

    public Measurement(int meterId, DateTimeOffset timestamp, string variable, decimal value, string unit)
    {
        if (meterId <= 0) throw new ArgumentOutOfRangeException(nameof(meterId));
        if (string.IsNullOrWhiteSpace(variable)) throw new ArgumentException("A variable name is required.", nameof(variable));
        MeterId = meterId;
        Timestamp = timestamp;
        Variable = variable.Trim();
        Value = value;
        Unit = unit?.Trim() ?? string.Empty;
    }

    public Measurement(int meterId, string meterName, DateTimeOffset timestamp, string variable, decimal value, string unit)
        : this(meterId, timestamp, variable, value, unit)
    {
    }
}

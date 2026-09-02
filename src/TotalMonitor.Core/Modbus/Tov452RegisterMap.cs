namespace TotalMonitor.Core.Modbus;

/// <summary>
/// Configurable TOV452 map shell. The official register map must provide every
/// nullable protocol field before this map can be used for real acquisition.
/// </summary>
public sealed class TOV452RegisterMap
{
    public IReadOnlyList<Tov452RegisterEntry> Entries { get; }
    public bool IsConfigured => Entries.Count > 0 && Entries.All(x => x.IsComplete);

    public TOV452RegisterMap(IEnumerable<Tov452RegisterEntry>? entries = null)
    {
        Entries = (entries ?? []).ToArray();
    }

    public static TOV452RegisterMap Empty { get; } = new();
}

public sealed record Tov452RegisterEntry(
    string ParameterKey,
    ushort? Address = null,
    byte? FunctionCode = null,
    string? DataType = null,
    ushort? Length = null,
    decimal? Scale = null,
    decimal? Offset = null,
    string? Unit = null,
    string? Endianness = null)
{
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ParameterKey) &&
        Address.HasValue &&
        FunctionCode.HasValue &&
        !string.IsNullOrWhiteSpace(DataType) &&
        Length.HasValue &&
        Scale.HasValue &&
        Offset.HasValue &&
        !string.IsNullOrWhiteSpace(Endianness);
}

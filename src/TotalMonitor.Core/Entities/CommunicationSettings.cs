namespace TotalMonitor.Core.Entities;

public sealed class CommunicationSettings
{
    public int Id { get; private set; } = 1;
    public string ComPort { get; private set; } = string.Empty;
    public int BaudRate { get; private set; } = 9600;
    public int DataBits { get; private set; } = 8;
    public string Parity { get; private set; } = "None";
    public string StopBits { get; private set; } = "One";
    public int ReadTimeout { get; private set; } = 1000;
    public int WriteTimeout { get; private set; } = 1000;
    public int PollingInterval { get; private set; } = 1000;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private CommunicationSettings() { }

    public CommunicationSettings(
        string comPort,
        int baudRate = 9600,
        int dataBits = 8,
        string parity = "None",
        string stopBits = "One",
        int readTimeout = 1000,
        int writeTimeout = 1000,
        int pollingInterval = 1000)
    {
        Update(comPort, baudRate, dataBits, parity, stopBits, readTimeout, writeTimeout, pollingInterval);
    }

    public void Update(
        string comPort,
        int baudRate,
        int dataBits,
        string parity,
        string stopBits,
        int readTimeout,
        int writeTimeout,
        int pollingInterval)
    {
        if (baudRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(baudRate), "El baud rate debe ser mayor a 0.");

        if (dataBits is < 5 or > 8)
            throw new ArgumentOutOfRangeException(nameof(dataBits), "Data bits debe estar entre 5 y 8.");

        if (string.IsNullOrWhiteSpace(parity))
            throw new ArgumentException("La paridad es obligatoria.", nameof(parity));

        if (string.IsNullOrWhiteSpace(stopBits))
            throw new ArgumentException("Los stop bits son obligatorios.", nameof(stopBits));

        if (readTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(readTimeout), "El timeout de lectura debe ser mayor a 0.");

        if (writeTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(writeTimeout), "El timeout de escritura debe ser mayor a 0.");

        if (pollingInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(pollingInterval), "El intervalo de adquisición debe ser mayor a 0.");

        ComPort = comPort?.Trim() ?? string.Empty;
        BaudRate = baudRate;
        DataBits = dataBits;
        Parity = parity.Trim();
        StopBits = stopBits.Trim();
        ReadTimeout = readTimeout;
        WriteTimeout = writeTimeout;
        PollingInterval = pollingInterval;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static CommunicationSettings CreateDefault() =>
        new("", 9600, 8, "None", "One", 1000, 1000, 1000);
}

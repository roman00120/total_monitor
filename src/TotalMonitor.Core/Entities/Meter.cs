namespace TotalMonitor.Core.Entities;

public sealed class Meter
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Model { get; private set; } = "TOV452";
    public byte ModbusAddress { get; private set; }
    public string ComPort { get; private set; } = string.Empty;
    public int BaudRate { get; private set; }
    public string Parity { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }

    private Meter() { }

    public Meter(string name, byte modbusAddress, string comPort, int baudRate, string parity, bool isEnabled = true, string model = "TOV452")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A meter name is required.", nameof(name));
        if (modbusAddress is 0 or > 247) throw new ArgumentOutOfRangeException(nameof(modbusAddress));
        if (string.IsNullOrWhiteSpace(comPort)) throw new ArgumentException("A COM port is required.", nameof(comPort));
        if (baudRate <= 0) throw new ArgumentOutOfRangeException(nameof(baudRate));
        if (string.IsNullOrWhiteSpace(parity)) throw new ArgumentException("Parity is required.", nameof(parity));
        Name = name.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? "TOV452" : model.Trim();
        ComPort = comPort.Trim();
        BaudRate = baudRate;
        Parity = parity.Trim();
        ModbusAddress = modbusAddress;
        IsEnabled = isEnabled;
    }

    public void Update(string name, byte modbusAddress, string comPort, int baudRate, string parity, bool isEnabled, string model = "TOV452")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A meter name is required.", nameof(name));
        if (modbusAddress is 0 or > 247) throw new ArgumentOutOfRangeException(nameof(modbusAddress));
        Name = name.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? "TOV452" : model.Trim();
        ModbusAddress = modbusAddress;
        ComPort = comPort.Trim();
        BaudRate = baudRate;
        Parity = parity.Trim();
        IsEnabled = isEnabled;
    }
}

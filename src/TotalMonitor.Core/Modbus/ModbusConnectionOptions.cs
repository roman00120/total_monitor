namespace TotalMonitor.Core.Modbus;

public sealed class ModbusConnectionOptions
{
    public string ComPort { get; set; } = "";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public string Parity { get; set; } = "None";
    public string StopBits { get; set; } = "One";
    public int ReadTimeout { get; set; } = 1000;
    public int WriteTimeout { get; set; } = 1000;
    public int RetryCount { get; set; } = 1;
    public int RetryDelay { get; set; } = 100;
    public int PollingInterval { get; set; } = 1000;
    public void Validate()
    { if (BaudRate <= 0 || DataBits is < 5 or > 8 || ReadTimeout <= 0 || WriteTimeout <= 0 || RetryCount < 0 || RetryDelay < 0 || PollingInterval < 0) throw new ArgumentException("Invalid serial communication options."); }
}

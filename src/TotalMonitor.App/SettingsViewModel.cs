using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ICommunicationSettingsService settingsService;
    private readonly IMeterService meterService;
    private readonly IDataAcquisitionService acquisition;

    // Communication Settings
    public ObservableCollection<string> AvailablePorts { get; } = [];
    private string selectedPort = "COM3";
    public string SelectedPort
    {
        get => selectedPort;
        set
        {
            Set(ref selectedPort, value);
            Notify(nameof(MeterPortDisplay));
            Notify(nameof(CurrentPortText));
            Notify(nameof(HardwareDetectionText));
        }
    }

    public IReadOnlyList<int> AvailableBaudRates { get; } = [9600, 19200, 38400, 57600, 115200];
    private int selectedBaudRate = 9600;
    public int SelectedBaudRate { get => selectedBaudRate; set => Set(ref selectedBaudRate, value); }

    public IReadOnlyList<string> AvailableParities { get; } = ["None", "Even", "Odd", "Mark", "Space"];
    private string selectedParity = "None";
    public string SelectedParity { get => selectedParity; set => Set(ref selectedParity, value); }

    private int dataBits = 8;
    public int DataBits { get => dataBits; set => Set(ref dataBits, value); }

    public IReadOnlyList<string> AvailableStopBits { get; } = ["One", "OnePointFive", "Two"];
    private string selectedStopBits = "One";
    public string SelectedStopBits { get => selectedStopBits; set => Set(ref selectedStopBits, value); }

    private int readTimeout = 1000;
    public int ReadTimeout { get => readTimeout; set => Set(ref readTimeout, value); }

    private int pollingInterval = 1000;
    public int PollingInterval { get => pollingInterval; set => Set(ref pollingInterval, value); }

    // Detection feedback
    public string HardwareDetectionText => AvailablePorts.Count > 0 && SelectedPort != "No se encontraron puertos COM."
        ? $"Convertidor RS485 detectado: {SelectedPort}"
        : "No se detectaron dispositivos RS485.";

    // Communication test state
    private string testResult = "";
    public string TestResult { get => testResult; set => Set(ref testResult, value); }

    private bool? isTestSuccess;
    public bool? IsTestSuccess { get => isTestSuccess; set => Set(ref isTestSuccess, value); }

    private bool isTesting;
    public bool IsTesting { get => isTesting; set => Set(ref isTesting, value); }

    // Meter Settings
    private int meterId = 0;
    public int MeterId { get => meterId; set => Set(ref meterId, value); }

    private string meterName = "TOV452 Principal";
    public string MeterName { get => meterName; set => Set(ref meterName, value); }

    public IReadOnlyList<string> AvailableModels { get; } = ["TOV452"];
    private string selectedModel = "TOV452";
    public string SelectedModel { get => selectedModel; set => Set(ref selectedModel, value); }

    private byte meterAddress = 1;
    public byte MeterAddress { get => meterAddress; set => Set(ref meterAddress, value); }

    public string MeterPortDisplay => SelectedPort;

    private bool isMeterEnabled = true;
    public bool IsMeterEnabled { get => isMeterEnabled; set => Set(ref isMeterEnabled, value); }

    // Save state
    private string saveMessage = "";
    public string SaveMessage { get => saveMessage; set => Set(ref saveMessage, value); }

    private bool? isSaveSuccess;
    public bool? IsSaveSuccess { get => isSaveSuccess; set => Set(ref isSaveSuccess, value); }

    // Acquisition live state
    public bool IsAcquisitionRunning => acquisition.State == AcquisitionState.Active || acquisition.State == AcquisitionState.Starting;
    public string AcquisitionStateBadge => IsAcquisitionRunning ? "● ADQUISICIÓN ACTIVA" : "○ ADQUISICIÓN DETENIDA";
    public string AcquisitionBadgeColor => IsAcquisitionRunning ? "#138A72" : "#627D98";
    public string HardwareStateText => acquisition.HardwareState.ToDisplayString();
    public string HardwareStateColor => acquisition.HardwareState.ToBadgeColor();
    public string OperationModeText => "Hardware Real • RS485 / Modbus RTU";
    public string CurrentPortText => string.IsNullOrWhiteSpace(SelectedPort) ? "No seleccionado" : SelectedPort;
    public int ActiveMetersCount => acquisition.ActiveMetersCount > 0 ? acquisition.ActiveMetersCount : (IsMeterEnabled ? 1 : 0);
    public string LastReadingText => acquisition.LastAcquisitionTime?.ToLocalTime().ToString("HH:mm:ss") ?? "--";
    public long ProcessedReadingsCount => acquisition.TotalReadingsProcessed;
    public string LastErrorText => string.IsNullOrWhiteSpace(acquisition.LastError) ? "Ninguno" : acquisition.LastError;

    // Commands
    public ICommand RefreshPortsCommand { get; }
    public ICommand TestCommunicationCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand StartAcquisitionCommand { get; }
    public ICommand StopAcquisitionCommand { get; }

    public SettingsViewModel(
        ICommunicationSettingsService settingsService,
        IMeterService meterService,
        IDataAcquisitionService acquisition)
    {
        this.settingsService = settingsService;
        this.meterService = meterService;
        this.acquisition = acquisition;

        RefreshPortsCommand = new AsyncCommand(RefreshPortsAsync);
        TestCommunicationCommand = new AsyncCommand(TestCommunicationAsync);
        SaveSettingsCommand = new AsyncCommand(SaveSettingsAsync);
        StartAcquisitionCommand = new AsyncCommand(StartAcquisitionAsync);
        StopAcquisitionCommand = new AsyncCommand(StopAcquisitionAsync);

        acquisition.EventRaised += (_, _) => DispatchRefreshStatus();
        acquisition.MeasurementReceived += (_, _) => DispatchRefreshStatus();
    }

    public async Task LoadAsync()
    {
        await RefreshPortsAsync();
        try
        {
            var s = await settingsService.GetSettingsAsync();
            if (s is not null)
            {
                if (!string.IsNullOrWhiteSpace(s.ComPort))
                {
                    if (!AvailablePorts.Contains(s.ComPort))
                        AvailablePorts.Insert(0, s.ComPort);
                    SelectedPort = s.ComPort;
                }
                SelectedBaudRate = s.BaudRate;
                DataBits = s.DataBits;
                SelectedParity = AvailableParities.Contains(s.Parity) ? s.Parity : "None";
                SelectedStopBits = AvailableStopBits.Contains(s.StopBits) ? s.StopBits : "One";
                ReadTimeout = s.ReadTimeout;
                PollingInterval = s.PollingInterval;
            }
        }
        catch { }

        try
        {
            var meters = await meterService.GetAllAsync();
            var tovMeter = meters.FirstOrDefault(m => m.Model == "TOV452" || m.Name.Contains("TOV452", StringComparison.OrdinalIgnoreCase))
                ?? meters.FirstOrDefault();

            if (tovMeter is not null)
            {
                MeterId = tovMeter.Id;
                MeterName = tovMeter.Name;
                SelectedModel = tovMeter.Model;
                MeterAddress = tovMeter.ModbusAddress;
                IsMeterEnabled = tovMeter.IsEnabled;
            }
        }
        catch { }

        RefreshLiveStatus();
    }

    public async Task RefreshPortsAsync()
    {
        try
        {
            var ports = await settingsService.GetAvailablePortsAsync();
            AvailablePorts.Clear();
            foreach (var p in ports)
            {
                AvailablePorts.Add(p);
            }
            if (AvailablePorts.Count > 0)
            {
                if (!AvailablePorts.Contains(SelectedPort))
                    SelectedPort = AvailablePorts[0];
            }
            else
            {
                AvailablePorts.Add("No se encontraron puertos COM.");
                SelectedPort = AvailablePorts[0];
            }
        }
        catch
        {
            if (AvailablePorts.Count == 0)
            {
                AvailablePorts.Add("No se encontraron puertos COM.");
                SelectedPort = AvailablePorts[0];
            }
        }

        Notify(nameof(HardwareDetectionText));
    }

    public async Task TestCommunicationAsync()
    {
        IsTesting = true;
        TestResult = "Enviando trama Modbus RTU al medidor...";
        IsTestSuccess = null;

        try
        {
            var portToTest = SelectedPort == "No se encontraron puertos COM." ? "" : SelectedPort;
            var (success, message, state) = await settingsService.TestConnectionAsync(
                portToTest,
                MeterAddress,
                SelectedBaudRate,
                SelectedParity,
                DataBits,
                SelectedStopBits,
                ReadTimeout);

            IsTestSuccess = success;
            TestResult = message;
        }
        catch (Exception ex)
        {
            IsTestSuccess = false;
            TestResult = $"✕ Error al probar comunicación: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var port = SelectedPort == "No se encontraron puertos COM." ? "" : SelectedPort;

            if (string.IsNullOrWhiteSpace(MeterName))
            {
                IsSaveSuccess = false;
                SaveMessage = "✕ El nombre del medidor es obligatorio.";
                return;
            }

            if (MeterAddress == 0 || MeterAddress > 247)
            {
                IsSaveSuccess = false;
                SaveMessage = "✕ La dirección Modbus debe estar entre 1 y 247.";
                return;
            }

            if (PollingInterval <= 0)
            {
                IsSaveSuccess = false;
                SaveMessage = "✕ El intervalo de adquisición debe ser mayor a 0 ms.";
                return;
            }

            if (ReadTimeout <= 0)
            {
                IsSaveSuccess = false;
                SaveMessage = "✕ El timeout debe ser mayor a 0 ms.";
                return;
            }

            // Save Communication Settings
            var comm = new CommunicationSettings(
                port,
                SelectedBaudRate,
                DataBits,
                SelectedParity,
                SelectedStopBits,
                ReadTimeout,
                ReadTimeout,
                PollingInterval);

            await settingsService.SaveSettingsAsync(comm);

            // Save/Update Meter
            var effectivePort = string.IsNullOrWhiteSpace(port) ? "COM3" : port;
            if (MeterId > 0)
            {
                await meterService.UpdateAsync(
                    MeterId,
                    MeterName,
                    MeterAddress,
                    effectivePort,
                    SelectedBaudRate,
                    SelectedParity,
                    IsMeterEnabled,
                    SelectedModel);
            }
            else
            {
                var newMeter = new Meter(
                    MeterName,
                    MeterAddress,
                    effectivePort,
                    SelectedBaudRate,
                    SelectedParity,
                    IsMeterEnabled,
                    SelectedModel);
                var created = await meterService.CreateAsync(newMeter);
                MeterId = created.Id;
            }

            IsSaveSuccess = true;
            SaveMessage = "✓ Configuración guardada correctamente en MySQL.";
            RefreshLiveStatus();
        }
        catch (Exception ex)
        {
            IsSaveSuccess = false;
            SaveMessage = $"✕ Error al guardar configuración: {ex.Message}";
        }
    }

    public async Task StartAcquisitionAsync()
    {
        try
        {
            await SaveSettingsAsync();
            if (IsSaveSuccess == false)
                return;

            await acquisition.StartAsync();
            RefreshLiveStatus();
        }
        catch (Exception ex)
        {
            SaveMessage = $"✕ No se pudo iniciar la adquisición: {ex.Message}";
            IsSaveSuccess = false;
        }
    }

    public async Task StopAcquisitionAsync()
    {
        try
        {
            await acquisition.StopAsync();
            RefreshLiveStatus();
        }
        catch (Exception ex)
        {
            SaveMessage = $"✕ Error al detener la adquisición: {ex.Message}";
            IsSaveSuccess = false;
        }
    }

    private void DispatchRefreshStatus()
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.BeginInvoke(RefreshLiveStatus);
        else
            RefreshLiveStatus();
    }

    public void RefreshLiveStatus()
    {
        Notify(nameof(IsAcquisitionRunning));
        Notify(nameof(AcquisitionStateBadge));
        Notify(nameof(AcquisitionBadgeColor));
        Notify(nameof(HardwareStateText));
        Notify(nameof(HardwareStateColor));
        Notify(nameof(OperationModeText));
        Notify(nameof(CurrentPortText));
        Notify(nameof(ActiveMetersCount));
        Notify(nameof(LastReadingText));
        Notify(nameof(ProcessedReadingsCount));
        Notify(nameof(LastErrorText));
        Notify(nameof(HardwareDetectionText));
    }
}

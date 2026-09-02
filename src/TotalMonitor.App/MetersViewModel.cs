using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public sealed class MetersViewModel : ObservableObject
{
    private readonly IMeterService meterService;
    private readonly IDataAcquisitionService acquisition;

    public ObservableCollection<MeterRowViewModel> Rows { get; } = [];

    private MeterRowViewModel? selectedMeter;
    public MeterRowViewModel? SelectedMeter
    {
        get => selectedMeter;
        set
        {
            Set(ref selectedMeter, value);
            Notify(nameof(HasSelectedMeter));
        }
    }

    public bool HasSelectedMeter => SelectedMeter is not null;

    // Form state
    private bool isFormOpen;
    public bool IsFormOpen
    {
        get => isFormOpen;
        set => Set(ref isFormOpen, value);
    }

    private bool isEditing;
    public bool IsEditing
    {
        get => isEditing;
        set
        {
            Set(ref isEditing, value);
            Notify(nameof(FormTitle));
        }
    }

    public string FormTitle => IsEditing ? "Editar Medidor" : "Nuevo Medidor";

    private int formId;
    public int FormId { get => formId; set => Set(ref formId, value); }

    private string formName = "TOV452 Principal";
    public string FormName { get => formName; set => Set(ref formName, value); }

    public IReadOnlyList<string> AvailableModels { get; } = ["TOV452"];
    private string formModel = "TOV452";
    public string FormModel { get => formModel; set => Set(ref formModel, value); }

    private byte formModbusAddress = 1;
    public byte FormModbusAddress { get => formModbusAddress; set => Set(ref formModbusAddress, value); }

    private string formComPort = "COM3";
    public string FormComPort { get => formComPort; set => Set(ref formComPort, value); }

    public IReadOnlyList<int> AvailableBaudRates { get; } = [9600, 19200, 38400, 57600, 115200];
    private int formBaudRate = 9600;
    public int FormBaudRate { get => formBaudRate; set => Set(ref formBaudRate, value); }

    public IReadOnlyList<string> AvailableParities { get; } = ["None", "Even", "Odd", "Mark", "Space"];
    private string formParity = "None";
    public string FormParity { get => formParity; set => Set(ref formParity, value); }

    private int formDataBits = 8;
    public int FormDataBits { get => formDataBits; set => Set(ref formDataBits, value); }

    public IReadOnlyList<string> AvailableStopBits { get; } = ["One", "OnePointFive", "Two"];
    private string formStopBits = "One";
    public string FormStopBits { get => formStopBits; set => Set(ref formStopBits, value); }

    private int formTimeout = 1000;
    public int FormTimeout { get => formTimeout; set => Set(ref formTimeout, value); }

    private bool formIsEnabled = true;
    public bool FormIsEnabled { get => formIsEnabled; set => Set(ref formIsEnabled, value); }

    private string formMessage = "";
    public string FormMessage { get => formMessage; set => Set(ref formMessage, value); }

    private bool? isFormSuccess;
    public bool? IsFormSuccess { get => isFormSuccess; set => Set(ref isFormSuccess, value); }

    // Commands
    public ICommand NewMeterCommand { get; }
    public ICommand EditMeterCommand { get; }
    public ICommand SaveMeterCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    public ICommand DeleteMeterCommand { get; }
    public ICommand RefreshCommand { get; }

    public MetersViewModel(IMeterService meterService, IDataAcquisitionService acquisition)
    {
        this.meterService = meterService;
        this.acquisition = acquisition;

        NewMeterCommand = new AsyncCommand(() =>
        {
            FormId = 0;
            FormName = $"TOV452 #{Rows.Count + 1}";
            FormModel = "TOV452";
            FormModbusAddress = (byte)Math.Clamp(Rows.Count + 1, 1, 247);
            FormComPort = Rows.FirstOrDefault()?.Port ?? "COM3";
            FormBaudRate = 9600;
            FormParity = "None";
            FormDataBits = 8;
            FormStopBits = "One";
            FormTimeout = 1000;
            FormIsEnabled = true;
            FormMessage = "";
            IsFormSuccess = null;
            IsEditing = false;
            IsFormOpen = true;
            return Task.CompletedTask;
        });

        EditMeterCommand = new AsyncCommand(() =>
        {
            if (SelectedMeter is null) return Task.CompletedTask;
            FormId = SelectedMeter.MeterId;
            FormName = SelectedMeter.Name;
            FormModel = SelectedMeter.Model;
            if (byte.TryParse(SelectedMeter.Address, out var addr)) FormModbusAddress = addr;
            FormComPort = SelectedMeter.Port;
            FormBaudRate = SelectedMeter.BaudRate;
            FormParity = SelectedMeter.Parity;
            FormDataBits = 8;
            FormStopBits = "One";
            FormTimeout = 1000;
            FormIsEnabled = SelectedMeter.IsEnabled;
            FormMessage = "";
            IsFormSuccess = null;
            IsEditing = true;
            IsFormOpen = true;
            return Task.CompletedTask;
        });

        SaveMeterCommand = new AsyncCommand(SaveMeterAsync);

        CancelFormCommand = new AsyncCommand(() =>
        {
            IsFormOpen = false;
            FormMessage = "";
            return Task.CompletedTask;
        });

        ToggleActiveCommand = new AsyncCommand(ToggleActiveAsync);
        DeleteMeterCommand = new AsyncCommand(DeleteMeterAsync);
        RefreshCommand = new AsyncCommand(LoadAsync);

        acquisition.EventRaised += (_, _) => RefreshRows();
        acquisition.MeasurementReceived += (_, _) => RefreshRows();
    }

    public async Task LoadAsync()
    {
        Rows.Clear();
        var meters = await meterService.GetAllAsync();
        foreach (var meter in meters)
        {
            Rows.Add(new MeterRowViewModel(meter, acquisition));
        }
        if (SelectedMeter is not null)
        {
            SelectedMeter = Rows.FirstOrDefault(r => r.MeterId == SelectedMeter.MeterId);
        }
    }

    private async Task SaveMeterAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            IsFormSuccess = false;
            FormMessage = "✕ El nombre del medidor es obligatorio.";
            return;
        }

        if (FormModbusAddress == 0 || FormModbusAddress > 247)
        {
            IsFormSuccess = false;
            FormMessage = "✕ La dirección Modbus debe estar entre 1 y 247.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FormComPort))
        {
            FormComPort = "COM3";
        }

        try
        {
            if (IsEditing && FormId > 0)
            {
                await meterService.UpdateAsync(
                    FormId,
                    FormName,
                    FormModbusAddress,
                    FormComPort,
                    FormBaudRate,
                    FormParity,
                    FormIsEnabled,
                    FormModel);
                FormMessage = "✓ Medidor actualizado correctamente.";
            }
            else
            {
                var newMeter = new Meter(
                    FormName,
                    FormModbusAddress,
                    FormComPort,
                    FormBaudRate,
                    FormParity,
                    FormIsEnabled,
                    FormModel);
                await meterService.CreateAsync(newMeter);
                FormMessage = "✓ Medidor creado correctamente.";
            }

            IsFormSuccess = true;
            await LoadAsync();
            await Task.Delay(500);
            IsFormOpen = false;
        }
        catch (Exception ex)
        {
            IsFormSuccess = false;
            FormMessage = $"✕ Error al guardar: {ex.Message}";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedMeter is null) return;
        try
        {
            var newStatus = !SelectedMeter.IsEnabled;
            await meterService.UpdateAsync(
                SelectedMeter.MeterId,
                SelectedMeter.Name,
                byte.Parse(SelectedMeter.Address),
                SelectedMeter.Port,
                SelectedMeter.BaudRate,
                SelectedMeter.Parity,
                newStatus,
                SelectedMeter.Model);
            await LoadAsync();
        }
        catch { }
    }

    private async Task DeleteMeterAsync()
    {
        if (SelectedMeter is null) return;
        try
        {
            await meterService.DeleteAsync(SelectedMeter.MeterId);
            await LoadAsync();
            SelectedMeter = null;
        }
        catch { }
    }

    private void RefreshRows()
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.BeginInvoke(() => Rows.ToList().ForEach(row => row.Refresh()));
        else
            Rows.ToList().ForEach(row => row.Refresh());
    }
}

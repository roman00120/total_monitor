using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public sealed class UsersViewModel(IUserAdminService service) : ObservableObject
{
    public ObservableCollection<User> Users { get; } = [];
    private string message = "Usuarios protegidos por permisos del sistema.";
    public string Message { get => message; private set => Set(ref message, value); }

    public async Task LoadAsync()
    {
        Users.Clear();
        foreach (var user in await service.GetAllAsync())
            Users.Add(user);
        if (Users.Count == 0)
            Message = "No hay usuarios configurados.";
    }
}

public sealed class ReportsViewModel : ObservableObject
{
    private readonly IReportService service;
    private string message = "Configure el rango de fechas y genere el reporte de mediciones.";
    public string Message { get => message; private set => Set(ref message, value); }

    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-1);
    public DateTime ToDate { get; set; } = DateTime.Today;

    public ObservableCollection<HistoricalDataPoint> Results { get; } = [];

    public ICommand GenerateCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand TodayCommand { get; }
    public ICommand YesterdayCommand { get; }
    public ICommand Last7DaysCommand { get; }

    private ReportRequest? request;

    public ReportsViewModel(IReportService service)
    {
        this.service = service;
        GenerateCommand = new AsyncCommand(GenerateAsync);
        ExportCommand = new AsyncCommand(ExportAsync);

        TodayCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Today, DateTime.Today));
        YesterdayCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1)));
        Last7DaysCommand = new AsyncCommand(() => SetRangeAsync(DateTime.Today.AddDays(-7), DateTime.Today));
    }

    private async Task SetRangeAsync(DateTime from, DateTime to)
    {
        FromDate = from;
        ToDate = to;
        Notify(nameof(FromDate));
        Notify(nameof(ToDate));
        await GenerateAsync();
    }

    private async Task GenerateAsync()
    {
        try
        {
            var from = new DateTimeOffset(FromDate, TimeZoneInfo.Local.GetUtcOffset(FromDate)).ToUniversalTime();
            var to = new DateTimeOffset(ToDate.Date.AddDays(1).AddTicks(-1), TimeZoneInfo.Local.GetUtcOffset(ToDate)).ToUniversalTime();
            request = new ReportRequest(from, to);

            Message = "Consultando y procesando reporte desde base de datos...";
            var report = await service.GenerateAsync(request);
            Results.Clear();
            foreach (var item in report.Data.Items)
                Results.Add(item);

            Message = Results.Count == 0
                ? "No se encontraron registros para el periodo seleccionado."
                : $"✓ Reporte generado exitosamente: {report.Data.TotalCount} mediciones encontradas.";
        }
        catch (UnauthorizedAccessException)
        {
            Message = "✕ No tienes permisos para consultar reportes.";
        }
        catch (Exception ex)
        {
            Message = $"✕ No fue posible generar el reporte: {ex.Message}";
        }
    }

    private async Task ExportAsync()
    {
        if (request is null || Results.Count == 0)
        {
            Message = "Primero genere un reporte con datos válidos para exportar.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"total-monitor-reporte-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            await service.ExportCsvAsync(request, dialog.FileName);
            Message = $"✓ Reporte exportado correctamente a {dialog.FileName}.";
        }
    }
}

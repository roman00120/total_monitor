namespace TotalMonitor.Core.Entities;

public enum MeterHardwareState
{
    NO_CONFIGURADO,
    ESPERANDO_COM,
    ESPERANDO_MEDIDOR,
    CONECTANDO,
    CONECTADO,
    ADQUIRIENDO,
    ERROR,
    DESCONECTADO
}

public static class MeterHardwareStateExtensions
{
    public static string ToDisplayString(this MeterHardwareState state) => state switch
    {
        MeterHardwareState.NO_CONFIGURADO => "No configurado",
        MeterHardwareState.ESPERANDO_COM => "Esperando COM",
        MeterHardwareState.ESPERANDO_MEDIDOR => "Esperando medidor",
        MeterHardwareState.CONECTANDO => "Conectando...",
        MeterHardwareState.CONECTADO => "Medidor conectado",
        MeterHardwareState.ADQUIRIENDO => "Adquiriendo",
        MeterHardwareState.ERROR => "Error de comunicación",
        MeterHardwareState.DESCONECTADO => "Conexión perdida",
        _ => "Desconocido"
    };

    public static string ToBadgeColor(this MeterHardwareState state) => state switch
    {
        MeterHardwareState.ADQUIRIENDO => "#138A72",
        MeterHardwareState.CONECTADO => "#138A72",
        MeterHardwareState.CONECTANDO => "#0B69A3",
        MeterHardwareState.ESPERANDO_MEDIDOR => "#B05A00",
        MeterHardwareState.ESPERANDO_COM => "#B05A00",
        MeterHardwareState.DESCONECTADO => "#C5221F",
        MeterHardwareState.ERROR => "#C5221F",
        _ => "#627D98"
    };
}

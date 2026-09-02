namespace TotalMonitor.Core.Modbus;

/// <summary>
/// Variables que el sistema puede modelar. No contiene direcciones Modbus.
/// Las direcciones y la codificación pertenecen al mapa oficial del TOV452.
/// </summary>
public sealed record MeasurementVariableDescriptor(
    string Key,
    string DisplayName,
    string Unit,
    string Description);

public static class Tov452MeasurementCatalog
{
    public static IReadOnlyList<MeasurementVariableDescriptor> Expected { get; } =
    [
        new("V1", "Voltaje V1", "V", "Voltaje de fase 1"),
        new("V2", "Voltaje V2", "V", "Voltaje de fase 2"),
        new("V3", "Voltaje V3", "V", "Voltaje de fase 3"),
        new("Voltage", "Voltaje", "V", "Tensión eléctrica RMS"),
        new("I1", "Corriente I1", "A", "Corriente de fase 1"),
        new("I2", "Corriente I2", "A", "Corriente de fase 2"),
        new("I3", "Corriente I3", "A", "Corriente de fase 3"),
        new("Current", "Corriente", "A", "Corriente eléctrica RMS"),
        new("P1", "Potencia activa P1", "W", "Potencia activa de fase 1"),
        new("P2", "Potencia activa P2", "W", "Potencia activa de fase 2"),
        new("P3", "Potencia activa P3", "W", "Potencia activa de fase 3"),
        new("ActivePower", "Potencia activa", "W", "Potencia activa"),
        new("ReactivePower", "Potencia reactiva", "var", "Potencia reactiva"),
        new("ApparentPower", "Potencia aparente", "VA", "Potencia aparente"),
        new("PF", "Factor de potencia PF", "", "Factor de potencia"),
        new("PowerFactor", "Factor de potencia", "", "Factor de potencia"),
        new("Frequency", "Frecuencia", "Hz", "Frecuencia eléctrica"),
        new("Energy", "Energía", "Wh", "Energía acumulada"),
        new("Demand", "Demanda", "W", "Demanda eléctrica"),
        new("VoltageThd", "THD de voltaje", "%", "Distorsión armónica total de voltaje"),
        new("CurrentThd", "THD de corriente", "%", "Distorsión armónica total de corriente"),
        new("THD", "THD", "%", "Distorsión armónica total")
    ];
}

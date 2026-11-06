namespace Payment.Application.Common;

/// <summary>Resultado vacío para commands que no devuelven entidad.</summary>
public readonly struct Unit
{
    public static Unit Value => default;
}

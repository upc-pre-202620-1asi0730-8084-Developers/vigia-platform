using System.Text.RegularExpressions;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

/// <summary>Código de negocio de la orden, p. ej. "SOL-104".</summary>
public partial record OrderCode
{
    public string Value { get; }

    public OrderCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
            throw new ArgumentException("Order code must follow the format SOL-### (e.g. SOL-104).", nameof(value));

        Value = normalized;
    }

    [GeneratedRegex(@"^SOL-\d{3,}$")]
    private static partial Regex CodePattern();

    public override string ToString() => Value;
}

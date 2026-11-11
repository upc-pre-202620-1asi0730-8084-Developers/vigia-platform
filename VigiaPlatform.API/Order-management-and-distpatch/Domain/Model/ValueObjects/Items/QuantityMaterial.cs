namespace VigiaPlatform.API.Order_management_and_dispatch.Domain.Model.ValueObjects;

public record MaterialQuantity
{
    public int Value { get; }

    public MaterialQuantity(int value)
    {
        if (value <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString();
}
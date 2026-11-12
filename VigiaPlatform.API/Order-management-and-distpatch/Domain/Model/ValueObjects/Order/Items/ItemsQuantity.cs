namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Items;

public record ItemsQuantity
{
    public int Value { get; }

    private const int MinQuantity = 1;
    private const int MaxQuantity = 100_000;

    public ItemsQuantity(int value)
    {
        if (value < MinQuantity)
            throw new ArgumentException($"Value cannot be less than {MinQuantity}.", nameof(value));

        if (value > MaxQuantity)
            throw new ArgumentException($"Value cannot be greater than {MaxQuantity}.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString();
}

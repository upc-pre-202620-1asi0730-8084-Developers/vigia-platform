namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

public record DispatchCreatedAt
{
    public DateTime Value { get; }

    public DispatchCreatedAt(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Creation date is required", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd HH:mm");
}

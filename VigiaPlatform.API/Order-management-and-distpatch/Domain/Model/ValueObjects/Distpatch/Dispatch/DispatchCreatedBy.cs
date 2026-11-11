namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;

public record DispatchCreatedBy
{
    public string Value { get; }

    public DispatchCreatedBy(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}

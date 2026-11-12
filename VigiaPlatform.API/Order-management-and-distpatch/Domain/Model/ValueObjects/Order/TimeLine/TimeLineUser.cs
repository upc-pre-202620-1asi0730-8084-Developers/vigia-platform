namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

public record TimeLineUser
{
    public string Value { get; }

    public TimeLineUser(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}

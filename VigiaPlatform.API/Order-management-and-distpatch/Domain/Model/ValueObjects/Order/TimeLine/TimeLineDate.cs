namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.TimeLine;

public record TimeLineDate
{
    public DateTime Value { get; }

    public TimeLineDate(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Event date is required.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd HH:mm");
}


    
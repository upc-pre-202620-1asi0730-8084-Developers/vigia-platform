namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

public record OrderRequesterName
{
    public string Value { get; }

    public OrderRequesterName(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Items requester name cannot be null or whitespace.", nameof(value));
        }

        Value = value.Trim();

    }
    
}
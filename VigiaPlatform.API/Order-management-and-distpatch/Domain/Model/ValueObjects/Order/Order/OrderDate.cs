namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.Order;

public record OrderDate
{
    public DateTime Value { get; }
    
    private static readonly DateTime MinDate = new(2023, 1, 1);
    
    public OrderDate(DateTime value)
    {
        if(value < MinDate )
            throw new ArgumentException($"The date must be greater than or equal to {MinDate.ToShortDateString()}.", nameof(value));
        
        Value = value.Date;
    }
    
}
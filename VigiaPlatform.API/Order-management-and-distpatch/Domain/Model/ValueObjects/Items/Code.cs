namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public record Code
{
    public string Value { get; }

    public Code(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
        
        //trim se utiliza para eliminar espacios en blanco al inicio y al final del texto
        //toupperinvariant se utiliza para que todo el texto quede en mayusculas y sin tildes
        
        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < 7)
        {
            throw new ArgumentException("Value cannot be shorter than 7 characters", nameof(value));
        }
        //get string 
        Value = normalized;
    }
    
}
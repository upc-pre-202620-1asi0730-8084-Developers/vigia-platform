using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects;

public readonly record struct DispatchId(int Value) : IEntityId<int>
{
    //se genera un operador implicito para convertir de DispatchId a int 
    public static implicit operator int(DispatchId dispatchId) => dispatchId.Value;
    public static implicit operator DispatchId(int value) => new(value);
    
    public override string ToString() => Value.ToString();
}
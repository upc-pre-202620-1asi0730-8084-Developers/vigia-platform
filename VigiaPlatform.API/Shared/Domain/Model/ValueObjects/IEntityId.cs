namespace VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

public interface IEntityId<T> where T : struct
{
    T Value { get; }
}
using VigiaPlatform.API.Shared.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;

public readonly record struct AttachedFileId(int Value) : IEntityId<int>
{
    public static implicit operator int(AttachedFileId id) => id.Value;
    public static implicit operator AttachedFileId(int value) => new(value);

    public override string ToString() => Value.ToString();
}

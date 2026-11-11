using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Entities;

/// <summary>File attached to an order (work program, material list, ...).</summary>
public class OrderAttachedFile
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public AttachedFilename Name { get; private set; } = null!;
    public AttachedFileType Type { get; private set; }
    public AttachedFileSize Size { get; private set; } = null!;

    // Required by EF Core
    protected OrderAttachedFile()
    {
    }

    public OrderAttachedFile(string name, AttachedFileType type, long sizeInBytes)
    {
        Name = new AttachedFilename(name);
        Type = type;
        Size = new AttachedFileSize(sizeInBytes);
    }
}

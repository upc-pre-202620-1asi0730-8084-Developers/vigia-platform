using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Commands;

public record AddOrderAttachedFileCommand(
    int OrderId,
    string Name,
    AttachedFileType Type,
    long SizeInBytes,
    string PerformedBy);

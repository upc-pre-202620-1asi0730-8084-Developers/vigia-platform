namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

// Type: PDF, XLSX, IMAGE, VIDEO, AUDIO, DOCUMENT or OTHER | SizeInBytes: size of the file in bytes
public record CreateOrderAttachedFileResource(string Name, string Type, long SizeInBytes);

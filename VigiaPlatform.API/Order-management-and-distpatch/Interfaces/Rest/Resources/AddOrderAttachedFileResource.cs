namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record AddOrderAttachedFileResource(string Name, string Type, long SizeInBytes, string PerformedBy);

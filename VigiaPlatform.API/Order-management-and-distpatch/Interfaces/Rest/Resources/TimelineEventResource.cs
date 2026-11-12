namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Resources;

public record TimelineEventResource(
    int Id,
    string Event,
    string User,
    string Description,
    string Date);

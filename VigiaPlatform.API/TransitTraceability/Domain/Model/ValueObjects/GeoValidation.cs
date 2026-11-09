using VigiaPlatform.API.TransitTraceability.Domain.Exceptions;

namespace VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

internal static class GeoValidation
{
    public static void EnsureCoordinates(double latitude, double longitude, string errorCode)
    {
        if (double.IsNaN(latitude) || latitude < -90 || latitude > 90)
            throw new TransitTraceabilityException(errorCode, $"Invalid latitude: {latitude}");
        if (double.IsNaN(longitude) || longitude < -180 || longitude > 180)
            throw new TransitTraceabilityException(errorCode, $"Invalid longitude: {longitude}");
    }
}

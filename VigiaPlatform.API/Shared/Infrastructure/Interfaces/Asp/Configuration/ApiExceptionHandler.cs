namespace VigiaPlatform.API.Shared.Infrastructure.Interfaces.Asp.Configuration;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, message) = exception switch
        {
            ArgumentException => (400, "VALIDATION_ERROR", exception.Message),
            InvalidOperationException when exception.TargetSite?.DeclaringType?.Namespace?.StartsWith("VigiaPlatform.API.") == true =>
                (409, "INVALID_OPERATION", exception.Message),
            VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions.SiteReceptionException reception =>
                (400, reception.Code, reception.Message),
            VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Exceptions.FleetException fleet =>
                (400, fleet.Code, fleet.Message),
            DbUpdateException { InnerException: MySqlException { Number: 1062 } } =>
                (409, "DUPLICATE_RECORD", "A record with the same unique identifier already exists."),
            _ => (500, "INTERNAL_ERROR", "The request could not be completed.")
        };
        if (status == 500) logger.LogError(exception, "Request {Method} {Path} failed", context.Request.Method, context.Request.Path);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { code, message }, cancellationToken);
        return true;
    }
}

namespace VigiaPlatform.API.FleetAndDeviceRegistry.Interfaces.REST;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.FleetAndDeviceRegistry.Domain.Model.ValueObjects;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Domain.Repositories;

public record TelemetryReport(string GpsDeviceId, double Latitude, double Longitude, DateTimeOffset? Timestamp);

[ApiController]
[Route("api/v1/telemetry/reports")]
[Authorize(Roles = "ADMIN,WAREHOUSE_MANAGER")]
public class TelemetryController(AppDbContext db, IUnitOfWork unitOfWork, ILogger<TelemetryController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Report(TelemetryReport input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.GpsDeviceId) || input.GpsDeviceId.Length > 64)
            throw new ArgumentException("GPS device ID is required and must not exceed 64 characters.");
        var position = new Location(input.Latitude, input.Longitude);
        var device = input.GpsDeviceId.Trim().ToUpperInvariant();
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.GpsDeviceId == device, ct);
        if (vehicle is null)
        {
            db.Add(new Domain.Model.Entities.UnassociatedTelemetryEvent(device, position, input.Timestamp ?? DateTimeOffset.UtcNow));
            await unitOfWork.CompleteAsync(ct);
            logger.LogInformation("Telemetry from unpaired device {DeviceId} was retained for inspection", device);
            return Ok(new { accepted = false, recorded = true, reason = "UNPAIRED_DEVICE" });
        }
        vehicle.UpdatePosition(position);
        await unitOfWork.CompleteAsync(ct);
        return Ok(new { accepted = true, vehicleId = vehicle.Id });
    }
}

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
        ArgumentException.ThrowIfNullOrWhiteSpace(input.GpsDeviceId);
        var position = new Location(input.Latitude, input.Longitude);
        var device = input.GpsDeviceId.Trim().ToUpperInvariant();
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.GpsDeviceId == device, ct);
        if (vehicle is null)
        {
            logger.LogInformation("Telemetry from unpaired device {DeviceId} was ignored", device);
            return Ok(new { accepted = false, reason = "UNPAIRED_DEVICE" });
        }
        vehicle.UpdatePosition(position);
        await unitOfWork.CompleteAsync(ct);
        return Ok(new { accepted = true, vehicleId = vehicle.Id });
    }
}

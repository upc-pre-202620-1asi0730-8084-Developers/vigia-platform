using VigiaPlatform.API.Shared.Application.Patterns;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.TransitTraceability.Domain.Exceptions;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;
using VigiaPlatform.API.TransitTraceability.Domain.Repositories;
using VigiaPlatform.API.TransitTraceability.Domain.Services;

namespace VigiaPlatform.API.TransitTraceability.Application.Internal.CommandServices;

public class RouteCommandService(IRouteRepository routeRepository, IUnitOfWork unitOfWork) : IRouteCommandService
{
    public async Task<Result<TransitRoute, TransitTraceabilityError>> Handle(CreateRouteCommand command, CancellationToken cancellationToken = default)
    {
        if (await routeRepository.ExistsByDispatchIdAsync(command.DispatchId.Trim(), cancellationToken))
            return Fail<TransitRoute>(TransitTraceabilityErrorCodes.DispatchAlreadyTracked,
                $"Dispatch {command.DispatchId} already has a route.");

        TransitRoute route;
        try
        {
            var destination = new Destination(command.DestinationName, command.DestinationLatitude,
                command.DestinationLongitude, command.DestinationRadiusMeters);
            route = new TransitRoute(command.DispatchId, command.VehicleId, command.VehiclePlate, command.OriginName,
                command.DestinationGeofenceId, destination, command.PlannedPath, command.StartedAt,
                command.EstimatedArrival, command.DeviationThresholdMeters ?? 300, command.StopThresholdMinutes ?? 15);
        }
        catch (TransitTraceabilityException e)
        {
            return Fail<TransitRoute>(e.Code, e.Message);
        }

        await routeRepository.AddAsync(route, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return new Result<TransitRoute, TransitTraceabilityError>.Success(route);
    }

    public async Task<Result<TransitOutcome, TransitTraceabilityError>> Handle(RegisterPositionCommand command, CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.FindByIdWithDetailsAsync(command.RouteId, cancellationToken);
        if (route is null) return RouteNotFound<TransitOutcome>(command.RouteId);

        IReadOnlyList<TransitAlert> raised;
        try
        {
            raised = route.RegisterPosition(command.Latitude, command.Longitude, command.SpeedKmh,
                command.ReportedAt ?? DateTimeOffset.UtcNow);
        }
        catch (TransitTraceabilityException e)
        {
            return Fail<TransitOutcome>(e.Code, e.Message);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return new Result<TransitOutcome, TransitTraceabilityError>.Success(new TransitOutcome(route, raised));
    }

    public async Task<Result<TransitOutcome, TransitTraceabilityError>> Handle(ReportSignalLostCommand command, CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.FindByIdWithDetailsAsync(command.RouteId, cancellationToken);
        if (route is null) return RouteNotFound<TransitOutcome>(command.RouteId);

        TransitAlert alert;
        try
        {
            alert = route.ReportSignalLost(command.DetectedAt ?? DateTimeOffset.UtcNow);
        }
        catch (TransitTraceabilityException e)
        {
            return Fail<TransitOutcome>(e.Code, e.Message);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return new Result<TransitOutcome, TransitTraceabilityError>.Success(new TransitOutcome(route, [alert]));
    }

    public async Task<Result<TransitAlert, TransitTraceabilityError>> Handle(NotifyDestinationSiteCommand command, CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.FindByIdWithDetailsAsync(command.RouteId, cancellationToken);
        if (route is null) return RouteNotFound<TransitAlert>(command.RouteId);

        TransitAlert alert;
        try
        {
            alert = route.NotifyDestinationSite(command.AlertId, DateTimeOffset.UtcNow);
        }
        catch (TransitTraceabilityException e)
        {
            return Fail<TransitAlert>(e.Code, e.Message);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return new Result<TransitAlert, TransitTraceabilityError>.Success(alert);
    }

    private static Result<T, TransitTraceabilityError> Fail<T>(string code, string message) =>
        new Result<T, TransitTraceabilityError>.Failure(new TransitTraceabilityError(code, message));

    private static Result<T, TransitTraceabilityError> RouteNotFound<T>(int routeId) =>
        Fail<T>(TransitTraceabilityErrorCodes.RouteNotFound, $"Route {routeId} not found.");
}

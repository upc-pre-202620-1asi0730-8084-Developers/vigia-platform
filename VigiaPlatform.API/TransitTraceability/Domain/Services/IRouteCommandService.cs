using VigiaPlatform.API.Shared.Application.Patterns;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Commands;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;
using VigiaPlatform.API.TransitTraceability.Domain.Model.ValueObjects;

namespace VigiaPlatform.API.TransitTraceability.Domain.Services;

public interface IRouteCommandService
{
    /// <summary>Starts tracking the route of a dispatch.</summary>
    Task<Result<TransitRoute, TransitTraceabilityError>> Handle(CreateRouteCommand command, CancellationToken cancellationToken = default);

    /// <summary>Registers a GPS position and evaluates deviation, prolonged stop and arrival.</summary>
    Task<Result<TransitOutcome, TransitTraceabilityError>> Handle(RegisterPositionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Registers that the GPS device stopped reporting.</summary>
    Task<Result<TransitOutcome, TransitTraceabilityError>> Handle(ReportSignalLostCommand command, CancellationToken cancellationToken = default);

    /// <summary>Marks a deviation or prolonged-stop alert as notified to the destination site.</summary>
    Task<Result<TransitAlert, TransitTraceabilityError>> Handle(NotifyDestinationSiteCommand command, CancellationToken cancellationToken = default);
}

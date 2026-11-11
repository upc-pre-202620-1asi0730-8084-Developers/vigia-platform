using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

namespace VigiaPlatform.API.TransitTraceability.Domain.Services;

/// <summary>
/// Route after a tracking command, together with the alerts that command raised.
/// </summary>
public record TransitOutcome(TransitRoute Route, IReadOnlyList<TransitAlert> RaisedAlerts);

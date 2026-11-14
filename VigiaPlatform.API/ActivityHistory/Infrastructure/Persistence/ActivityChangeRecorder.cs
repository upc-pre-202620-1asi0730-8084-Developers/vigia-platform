namespace VigiaPlatform.API.ActivityHistory.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.ActivityHistory.Domain.Model;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Aggregates;

public class ActivityChangeRecorder(AppDbContext context, IHttpContextAccessor accessor) : IChangeRecorder
{
    private List<(object Entity, EntityState State)> _changes = [];

    public void CaptureChanges()
    {
        context.ChangeTracker.DetectChanges();
        _changes = context.ChangeTracker.Entries()
            .Where(e => e.Entity is Dispatch or TransitRoute or Reception or DiscrepancyCase)
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted ||
                e.Collections.Any(c => c.CurrentValue is not null && c.CurrentValue.Cast<object>()
                    .Any(child => context.Entry(child).State is EntityState.Added or EntityState.Modified or EntityState.Deleted)))
            .Select(e => (e.Entity, e.State == EntityState.Unchanged ? EntityState.Modified : e.State)).ToList();
    }

    public void AppendChanges()
    {
        foreach (var (entity, state) in _changes)
        {
            var added = state == EntityState.Added;
            var deleted = state == EntityState.Deleted;
            var values = entity switch
            {
                Dispatch dispatch => ("DISPATCH", added ? "DISPATCH_CREATED" : "DISPATCH_UPDATED",
                    dispatch.ProjectName.Value, dispatch.Id.ToString(), dispatch.Status switch {
                        Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch.DispatchStatus.InTransit => "IN_TRANSIT",
                        Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch.DispatchStatus.Completed => "COMPLETED",
                        Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch.DispatchStatus.Cancelled => "CLOSED", _ => "PENDING" }),
                TransitRoute route => ("TRANSPORT", added ? "ROUTE_STARTED" : "ROUTE_UPDATED",
                    route.Destination.Name, route.Id.ToString(), route.IsActive ? "IN_TRANSIT" : "COMPLETED"),
                Reception reception => ("RECEPTION", added ? "RECEPTION_REGISTERED" : "RECEPTION_VERIFIED",
                    reception.SiteName, reception.Id.ToString(), reception.Discrepancy is not null ? "WITH_DISCREPANCY" : reception.ClosedAt is not null ? "COMPLETED" : "PENDING"),
                DiscrepancyCase discrepancy => ("DISCREPANCY", added ? "DISCREPANCY_REPORTED" : "CASE_UPDATED",
                    discrepancy.CaseNumber, discrepancy.Id.ToString(), discrepancy.Status == "Closed" ? "CLOSED" : "WITH_DISCREPANCY"),
                _ => throw new InvalidOperationException("Unsupported activity source.")
            };
            context.Add(new ActivityRecord(values.Item1, deleted ? values.Item1 + "_DELETED" : values.Item2,
                values.Item3, accessor.HttpContext?.User.Identity?.Name ?? "System", values.Item4,
                deleted ? "CLOSED" : values.Item5));
        }
        _changes.Clear();
    }
}

namespace VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;
using VigiaPlatform.API.TransitTraceability.Domain.Model.Entities;

public record PhysicalItem(string MaterialName, double ReceivedQuantity);
public record PhysicalChecklist(bool MaterialGoodCondition, bool QuantityVerified, bool DeliveryGuideReceived,
    bool PhotographicEvidence, bool ObservationsRecorded);
public record PhysicalArrival(string DispatchId, string DeliveryGuideNumber, string DriverName, List<PhysicalItem> Items,
    PhysicalChecklist Checklist, string? Observations, Guid? RequestId = null);
public record PhysicalVerification(string Status, PhysicalChecklist Checklist, string? Observations, Guid? RequestId = null);
public record PhysicalQuantity(int Id, double ReceivedQuantity);
public record PhysicalComparison(List<PhysicalQuantity> Items);

public class PhysicalReceptionService(AppDbContext db, IUnitOfWork unitOfWork)
{
    public async Task<(Reception Reception, bool Replayed)> RegisterAsync(
        PhysicalArrival input, ClaimsPrincipal user, CancellationToken ct)
    {
        var (actorId, actorName, isAdmin) = ResolveActor(user);
        if (input.RequestId == Guid.Empty)
            throw new ArgumentException("The request ID cannot be empty.");
        var normalizedDispatch = input.DispatchId?.Replace("DS-", "", StringComparison.OrdinalIgnoreCase);
        if (!int.TryParse(normalizedDispatch, out var dispatchId))
            throw new ArgumentException("Select a valid dispatch.");

        var dispatch = await db.Set<Dispatch>()
            .Include(d => d.Items)
            .Include(d => d.DeliveryGuide)
            .SingleOrDefaultAsync(d => d.Id == dispatchId, ct)
            ?? throw new ArgumentException("Dispatch does not exist.");

        var project = (await db.Set<ConstructionProject>().ToListAsync(ct))
            .SingleOrDefault(p => p.Id.ToString() == dispatch.SiteId.Value)
            ?? throw new ArgumentException("The dispatch must reference an existing construction project.");

        if (!isAdmin && (!user.IsInRole("SITE_MANAGER") || project.ManagerId != actorId))
            throw new UnauthorizedAccessException("You are not authorized to register a reception for this project.");

        var existing = await FindByDispatchIdAsync(dispatchId.ToString(), ct);
        if (existing is not null)
            return ResolveExistingRegistration(existing, input, actorId);

        if (dispatch.Status != DispatchStatus.InTransit)
            throw new InvalidOperationException("The dispatch must be in transit.");
        if (dispatch.DeliveryGuide is null)
            throw new InvalidOperationException("The dispatch does not have an issued delivery guide.");

        var guideNumber = input.DeliveryGuideNumber?.Trim();
        if (string.IsNullOrWhiteSpace(guideNumber) ||
            !string.Equals(guideNumber, dispatch.DeliveryGuide.GuideNumber, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Delivery guide number does not match the dispatch delivery guide.");

        if (input.Items is null || input.Items.Count != dispatch.Items.Count)
            throw new ArgumentException("Received items must match the dispatch guide.");

        var reception = new Reception(
            dispatchId.ToString(),
            dispatch.VehiclePlate?.Value ?? throw new InvalidOperationException("Vehicle is missing."),
            input.DriverName,
            project.Id,
            project.Name,
            actorName);
        reception.BeginPhysicalVerification(dispatch.DeliveryGuide.GuideNumber, actorId, actorName);
        reception.RecordRegisterRequestId(input.RequestId);

        var remaining = input.Items.ToList();
        foreach (var expected in dispatch.Items)
        {
            var received = remaining.FirstOrDefault(i =>
                string.Equals(i.MaterialName, expected.Material.Value, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("A received material is absent from the dispatch guide.");
            if (!double.IsFinite(received.ReceivedQuantity) || received.ReceivedQuantity < 0)
                throw new ArgumentException("Received quantity must be finite and non-negative.");
            remaining.Remove(received);
            reception.AddItem(expected.Id.ToString(), expected.Material.Value, expected.Unit.ToString(), expected.Quantity.Value);
            reception.VerifyMaterial(expected.Id.ToString(), received.ReceivedQuantity);
        }

        reception.SavePhysicalChecklist(
            input.Checklist.MaterialGoodCondition,
            input.Checklist.QuantityVerified,
            input.Checklist.DeliveryGuideReceived,
            input.Checklist.ObservationsRecorded,
            input.Observations);

        db.Add(reception);
        try
        {
            await unitOfWork.CompleteAsync(ct);
            return (reception, false);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { Number: 1062 })
        {
            db.ChangeTracker.Clear();
            var concurrent = await FindByDispatchIdAsync(dispatchId.ToString(), ct);
            if (concurrent is not null)
                return ResolveExistingRegistration(concurrent, input, actorId);
            throw new InvalidOperationException("This dispatch already has a reception.");
        }
    }

    public async Task<Reception?> FindAsync(int id, CancellationToken ct) => await db.Set<Reception>()
        .Include(r => r.Items).Include(r => r.Evidences).AsSplitQuery().SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Reception?> UpdateAsync(int id, PhysicalArrival input, ClaimsPrincipal user, CancellationToken ct)
    {
        var reception = await FindAsync(id, ct);
        if (reception is null || !await CanAccessReceptionAsync(reception, user, ct))
            return null;

        var (actorId, _, isAdmin) = ResolveActor(user);
        EnsureEditableBy(reception, actorId, isAdmin);

        if (input.DispatchId != reception.DispatchId || input.Items is null || input.Items.Count != reception.Items.Count)
            throw new ArgumentException("Reception must retain its dispatch and expected material lines.");

        if (!string.IsNullOrWhiteSpace(input.DeliveryGuideNumber) &&
            !string.Equals(input.DeliveryGuideNumber.Trim(), reception.DeliveryNoteNumber, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Delivery guide number does not match the dispatch delivery guide.");

        var remaining = input.Items.ToList();
        foreach (var item in reception.Items)
        {
            var received = remaining.FirstOrDefault(i =>
                string.Equals(i.MaterialName, item.MaterialName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("Received materials must match the dispatch guide.");
            if (!double.IsFinite(received.ReceivedQuantity) || received.ReceivedQuantity < 0)
                throw new ArgumentException("Received quantity must be finite and non-negative.");
            remaining.Remove(received);
            reception.VerifyMaterial(item.MaterialId, received.ReceivedQuantity);
        }

        reception.SavePhysicalChecklist(
            input.Checklist.MaterialGoodCondition,
            input.Checklist.QuantityVerified,
            input.Checklist.DeliveryGuideReceived,
            input.Checklist.ObservationsRecorded,
            input.Observations);
        await unitOfWork.CompleteAsync(ct);
        return reception;
    }

    public async Task<Reception?> CompareAsync(int id, PhysicalComparison input, ClaimsPrincipal user, CancellationToken ct)
    {
        var reception = await FindAsync(id, ct);
        if (reception is null || !await CanAccessReceptionAsync(reception, user, ct))
            return null;

        var (actorId, _, isAdmin) = ResolveActor(user);
        EnsureEditableBy(reception, actorId, isAdmin);

        if (input.Items is null || input.Items.Count != reception.Items.Count ||
            input.Items.Select(i => i.Id).Distinct().Count() != input.Items.Count)
            throw new ArgumentException("Verify every reception item exactly once.");

        foreach (var item in input.Items)
        {
            if (!double.IsFinite(item.ReceivedQuantity) || item.ReceivedQuantity < 0)
                throw new ArgumentException("Received quantity must be finite and non-negative.");
            var expected = reception.Items.SingleOrDefault(i => i.Id == item.Id)
                ?? throw new ArgumentException("Item does not belong to this reception.");
            reception.VerifyMaterial(expected.MaterialId, item.ReceivedQuantity);
        }

        await unitOfWork.CompleteAsync(ct);
        return reception;
    }

    public async Task<Reception?> VerifyAsync(int id, PhysicalVerification input, ClaimsPrincipal user, CancellationToken ct)
    {
        if (input.RequestId == Guid.Empty)
            throw new ArgumentException("The request ID cannot be empty.");
        if (input.Status is not ("VERIFIED_CONFORMANT" or "VERIFIED_DISCREPANT"))
            throw new ArgumentException("Unknown verification status.");

        var reception = await FindAsync(id, ct);
        if (reception is null || !await CanAccessReceptionAsync(reception, user, ct))
            return null;

        var (actorId, actorName, isAdmin) = ResolveActor(user);
        if (!isAdmin && reception.VerifiedByUserId != actorId)
            throw new InvalidOperationException("Only the resident who registered this verification can complete it.");

        if (reception.ClosedAt is not null ||
            reception.Status is ReceptionStatus.ConformantClosed or ReceptionStatus.DiscrepancyReported or ReceptionStatus.Rejected)
        {
            if (input.RequestId.HasValue)
            {
                if (reception.VerifyRequestId == input.RequestId.Value.ToString("D") && MatchesVerification(reception, input))
                    return reception;
                throw new InvalidOperationException("This reception verification was already completed with different values or request ID.");
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        reception.RecordVerifyRequestId(input.RequestId);
        reception.CompletePhysicalVerification(
            input.Checklist.MaterialGoodCondition,
            input.Checklist.QuantityVerified,
            input.Checklist.DeliveryGuideReceived,
            input.Checklist.ObservationsRecorded,
            input.Observations,
            input.Status == "VERIFIED_DISCREPANT");

        if (reception.HasAnyDiscrepancy && !await db.Set<DiscrepancyCase>().AnyAsync(c => c.ReceptionId == id.ToString(), ct))
            db.Add(new DiscrepancyCase(reception.DispatchId, id.ToString(), $"CASE-{Guid.NewGuid():N}"));

        await CloseConformantDispatchAsync(reception, actorName, ct);
        await unitOfWork.CompleteAsync(ct);
        await transaction.CommitAsync(ct);
        return reception;
    }

    /// <summary>
    /// Closes the linked dispatch and active transit trip inside the active transaction when reception closes conformant.
    /// </summary>
    public async Task CloseConformantDispatchAsync(Reception reception, string actorName, CancellationToken ct)
    {
        if (reception.Status != ReceptionStatus.ConformantClosed)
            return;

        if (!int.TryParse(reception.DispatchId, out var dispatchId))
            return;

        var dispatch = await db.Set<Dispatch>()
            .Include(d => d.Items)
            .Include(d => d.Timeline)
            .Include(d => d.DeliveryGuide)
            .SingleOrDefaultAsync(d => d.Id == dispatchId, ct);
        if (dispatch is not null && dispatch.Status == DispatchStatus.InTransit)
        {
            dispatch.ChangeStatus(
                DispatchStatus.Completed,
                actorName,
                "Reception verified conformant; dispatch completed.",
                DateTime.UtcNow);
        }

        var trip = await db.Set<DispatchTrip>()
            .Include(t => t.Route)
            .SingleOrDefaultAsync(t => t.DispatchId == dispatchId, ct);
        if (trip?.ActiveVehicleId is not null)
        {
            trip.End("DISPATCH_CLOSED", DateTimeOffset.UtcNow);
            trip.Route.CloseTracking();
        }
    }

    private async Task<Reception?> FindByDispatchIdAsync(string dispatchId, CancellationToken ct) =>
        await db.Set<Reception>()
            .Include(r => r.Items)
            .Include(r => r.Evidences)
            .AsSplitQuery()
            .SingleOrDefaultAsync(r => r.DispatchId == dispatchId, ct);

    private async Task<bool> CanAccessReceptionAsync(Reception reception, ClaimsPrincipal user, CancellationToken ct)
    {
        if (user.IsInRole("ADMIN")) return true;
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return user.IsInRole("SITE_MANAGER") && !string.IsNullOrWhiteSpace(actorId) &&
            await db.Set<ConstructionProject>().AnyAsync(p => p.Id == reception.SiteId && p.ManagerId == actorId, ct);
    }

    private static (string ActorId, string ActorName, bool IsAdmin) ResolveActor(ClaimsPrincipal user)
    {
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Authenticated user identifier is required.");
        var actorName = user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(actorName))
            throw new UnauthorizedAccessException("Authenticated user name is required.");
        return (actorId, actorName, user.IsInRole("ADMIN"));
    }

    private static void EnsureEditableBy(Reception reception, string actorId, bool isAdmin)
    {
        if (!reception.IsPhysicalVerification || reception.ClosedAt is not null ||
            (!isAdmin && reception.VerifiedByUserId != actorId))
            throw new InvalidOperationException("Only the registering resident can edit an open physical reception.");
    }

    private static (Reception Reception, bool Replayed) ResolveExistingRegistration(
        Reception existing, PhysicalArrival input, string actorId)
    {
        if (input.RequestId.HasValue && existing.RegisterRequestId == input.RequestId.Value.ToString("D"))
        {
            if (MatchesRegistration(existing, input, actorId))
                return (existing, true);
            throw new InvalidOperationException("The request ID was already used with different reception details.");
        }
        throw new InvalidOperationException("This dispatch already has a reception.");
    }

    private static bool MatchesRegistration(Reception existing, PhysicalArrival input, string actorId)
    {
        if (existing.VerifiedByUserId != actorId ||
            !string.Equals(existing.DeliveryNoteNumber, input.DeliveryGuideNumber?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(existing.DriverName, input.DriverName?.Trim(), StringComparison.Ordinal) ||
            existing.MaterialGoodCondition != input.Checklist.MaterialGoodCondition ||
            existing.QuantityVerified != input.Checklist.QuantityVerified ||
            existing.DeliveryGuideReceived != input.Checklist.DeliveryGuideReceived ||
            existing.ObservationsRecorded != input.Checklist.ObservationsRecorded ||
            !string.Equals(existing.ReceiverNotes ?? "", input.Observations?.Trim() ?? "", StringComparison.Ordinal) ||
            input.Items is null || input.Items.Count != existing.Items.Count)
            return false;

        foreach (var item in existing.Items)
        {
            var match = input.Items.FirstOrDefault(i =>
                string.Equals(i.MaterialName, item.MaterialName, StringComparison.OrdinalIgnoreCase));
            if (match is null || Math.Abs(match.ReceivedQuantity - item.ReceivedQuantity) > 1e-9)
                return false;
        }
        return true;
    }

    private static bool MatchesVerification(Reception existing, PhysicalVerification input)
    {
        var expectedStatus = input.Status == "VERIFIED_CONFORMANT"
            ? ReceptionStatus.ConformantClosed
            : ReceptionStatus.DiscrepancyReported;
        return existing.Status == expectedStatus &&
            existing.MaterialGoodCondition == input.Checklist.MaterialGoodCondition &&
            existing.QuantityVerified == input.Checklist.QuantityVerified &&
            existing.DeliveryGuideReceived == input.Checklist.DeliveryGuideReceived &&
            existing.ObservationsRecorded == input.Checklist.ObservationsRecorded &&
            string.Equals(existing.ReceiverNotes ?? "", input.Observations?.Trim() ?? "", StringComparison.Ordinal);
    }
}

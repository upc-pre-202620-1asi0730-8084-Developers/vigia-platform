namespace VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration;

using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.Aggregates;
using VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Distpatch.Dispatch;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;

public record PhysicalItem(string MaterialName, double ReceivedQuantity);
public record PhysicalChecklist(bool MaterialGoodCondition, bool QuantityVerified, bool DeliveryGuideReceived,
    bool PhotographicEvidence, bool ObservationsRecorded);
public record PhysicalArrival(string DispatchId, string DeliveryGuideNumber, string DriverName, List<PhysicalItem> Items,
    PhysicalChecklist Checklist, string? Observations);
public record PhysicalVerification(string Status, PhysicalChecklist Checklist, string? Observations);
public record PhysicalQuantity(int Id, double ReceivedQuantity);
public record PhysicalComparison(List<PhysicalQuantity> Items);

public class PhysicalReceptionService(AppDbContext db, IUnitOfWork unitOfWork)
{
    public async Task<Reception> RegisterAsync(PhysicalArrival input, string actorId, string actor, CancellationToken ct)
    {
        if (!int.TryParse(input.DispatchId.Replace("DS-", "", StringComparison.OrdinalIgnoreCase), out var dispatchId))
            throw new ArgumentException("Select a valid dispatch.");
        var dispatch = await db.Set<Dispatch>().Include(d => d.Items).SingleOrDefaultAsync(d => d.Id == dispatchId, ct)
            ?? throw new ArgumentException("Dispatch does not exist.");
        if (dispatch.Status != DispatchStatus.InTransit) throw new InvalidOperationException("The dispatch must be in transit.");
        if (await db.Set<Reception>().AnyAsync(r => r.DispatchId == dispatchId.ToString(), ct))
            throw new InvalidOperationException("This dispatch already has a reception.");
        var project = (await db.Set<ConstructionProject>().ToListAsync(ct)).SingleOrDefault(p => p.Id.ToString() == dispatch.SiteId.Value)
            ?? throw new ArgumentException("The dispatch must reference an existing construction project.");
        if (input.Items is null || input.Items.Count != dispatch.Items.Count)
            throw new ArgumentException("Received items must match the dispatch guide.");
        var reception = new Reception(dispatchId.ToString(), dispatch.VehiclePlate?.Value ?? throw new InvalidOperationException("Vehicle is missing."),
            input.DriverName, project.Id, project.Name, actor);
        reception.BeginPhysicalVerification(input.DeliveryGuideNumber, actorId, actor);
        var remaining = input.Items.ToList();
        foreach (var expected in dispatch.Items)
        {
            var received = remaining.FirstOrDefault(i => string.Equals(i.MaterialName, expected.Material.Value, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("A received material is absent from the dispatch guide.");
            if (!double.IsFinite(received.ReceivedQuantity)) throw new ArgumentException("Received quantity must be finite.");
            remaining.Remove(received);
            reception.AddItem(expected.Id.ToString(), expected.Material.Value, expected.Unit.ToString(), expected.Quantity.Value);
            reception.VerifyMaterial(expected.Id.ToString(), received.ReceivedQuantity);
        }
        reception.SavePhysicalChecklist(input.Checklist.MaterialGoodCondition, input.Checklist.QuantityVerified,
            input.Checklist.DeliveryGuideReceived, input.Checklist.ObservationsRecorded, input.Observations);
        db.Add(reception);
        await unitOfWork.CompleteAsync(ct);
        return reception;
    }

    public async Task<Reception?> FindAsync(int id, CancellationToken ct) => await db.Set<Reception>()
        .Include(r => r.Items).Include(r => r.Evidences).AsSplitQuery().SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Reception?> UpdateAsync(int id, PhysicalArrival input, string actorId, CancellationToken ct)
    {
        var reception = await FindAsync(id, ct);
        if (reception is null) return null;
        EnsureEditableBy(reception, actorId);
        if (input.DispatchId != reception.DispatchId || input.Items.Count != reception.Items.Count)
            throw new ArgumentException("Reception must retain its dispatch and expected material lines.");
        var remaining = input.Items.ToList();
        foreach (var item in reception.Items)
        {
            var received = remaining.FirstOrDefault(i => string.Equals(i.MaterialName, item.MaterialName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("Received materials must match the dispatch guide.");
            remaining.Remove(received);
            reception.VerifyMaterial(item.MaterialId, received.ReceivedQuantity);
        }
        reception.SavePhysicalChecklist(input.Checklist.MaterialGoodCondition, input.Checklist.QuantityVerified,
            input.Checklist.DeliveryGuideReceived, input.Checklist.ObservationsRecorded, input.Observations);
        await unitOfWork.CompleteAsync(ct);
        return reception;
    }

    public async Task<Reception?> CompareAsync(int id, PhysicalComparison input, string actorId, CancellationToken ct)
    {
        var reception = await FindAsync(id, ct);
        if (reception is null) return null;
        EnsureEditableBy(reception, actorId);
        if (input.Items.Count != reception.Items.Count || input.Items.Select(i => i.Id).Distinct().Count() != input.Items.Count)
            throw new ArgumentException("Verify every reception item exactly once.");
        foreach (var item in input.Items)
        {
            var expected = reception.Items.SingleOrDefault(i => i.Id == item.Id)
                ?? throw new ArgumentException("Item does not belong to this reception.");
            reception.VerifyMaterial(expected.MaterialId, item.ReceivedQuantity);
        }
        await unitOfWork.CompleteAsync(ct);
        return reception;
    }

    private static void EnsureEditableBy(Reception reception, string actorId)
    {
        if (!reception.IsPhysicalVerification || reception.VerifiedByUserId != actorId || reception.ClosedAt is not null)
            throw new InvalidOperationException("Only the registering resident can edit an open physical reception.");
    }

    public async Task<Reception?> VerifyAsync(int id, PhysicalVerification input, string actorId, CancellationToken ct)
    {
        var reception = await FindAsync(id, ct);
        if (reception is null) return null;
        if (reception.VerifiedByUserId != actorId) throw new InvalidOperationException("Only the resident who registered this verification can complete it.");
        if (input.Status is not ("VERIFIED_CONFORMANT" or "VERIFIED_DISCREPANT")) throw new ArgumentException("Unknown verification status.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        reception.CompletePhysicalVerification(input.Checklist.MaterialGoodCondition, input.Checklist.QuantityVerified,
            input.Checklist.DeliveryGuideReceived, input.Checklist.ObservationsRecorded, input.Observations,
            input.Status == "VERIFIED_DISCREPANT");
        if (reception.HasAnyDiscrepancy && !await db.Set<DiscrepancyCase>().AnyAsync(c => c.ReceptionId == id.ToString(), ct))
            db.Add(new DiscrepancyCase(reception.DispatchId, id.ToString(), $"CASE-{Guid.NewGuid():N}"));
        await unitOfWork.CompleteAsync(ct);
        await transaction.CommitAsync(ct);
        return reception;
    }
}

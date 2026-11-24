namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.ProjectManagement.Domain.Model;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class EvidenceAccess(AppDbContext db)
{
    public async Task<bool> ReceptionAsync(int id, ClaimsPrincipal user, CancellationToken ct)
    {
        var receipt = await db.Set<Reception>().AsNoTracking().Where(r=>r.Id==id)
            .Select(r=>new {r.SiteId}).SingleOrDefaultAsync(ct);
        if(receipt is null) return false;
        if(user.IsInRole("ADMIN")) return true;
        var accountId=user.FindFirstValue(ClaimTypes.NameIdentifier);
        return user.IsInRole("SITE_MANAGER") && accountId is not null &&
            await db.Set<ConstructionProject>().AnyAsync(p=>p.Id==receipt.SiteId && p.ManagerId==accountId,ct);
    }

    public async Task<int?> CaseReceptionAsync(int id, ClaimsPrincipal user, CancellationToken ct)
    {
        var item=await db.Set<DiscrepancyCase>().AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id,ct);
        if(item is null || !int.TryParse(item.ReceptionId,out var receiptId)) return null;
        var receipt=await db.Set<Reception>().AsNoTracking().Where(r=>r.Id==receiptId)
            .Select(r=>new {r.DispatchId}).SingleOrDefaultAsync(ct);
        return receipt is not null && receipt.DispatchId==item.DispatchId && await ReceptionAsync(receiptId,user,ct) ? receiptId : null;
    }

    public async Task<bool> CaseAsync(int id,ClaimsPrincipal user,CancellationToken ct) =>
        user.IsInRole("ADMIN") ? await db.Set<DiscrepancyCase>().AnyAsync(c=>c.Id==id,ct) :
        await CaseReceptionAsync(id,user,ct) is not null;

    public async Task<HashSet<int>> CaseIdsAsync(ClaimsPrincipal user,CancellationToken ct)
    {
        if(user.IsInRole("ADMIN")) return (await db.Set<DiscrepancyCase>().Select(c=>c.Id).ToListAsync(ct)).ToHashSet();
        var accountId=user.FindFirstValue(ClaimTypes.NameIdentifier);
        var sites=await db.Set<ConstructionProject>().Where(p=>p.ManagerId==accountId).Select(p=>p.Id).ToListAsync(ct);
        var receipts=await db.Set<Reception>().Where(r=>sites.Contains(r.SiteId)).Select(r=>new {r.Id,r.DispatchId}).ToListAsync(ct);
        var allowed=receipts.ToDictionary(r=>r.Id.ToString(),r=>r.DispatchId);
        var cases=await db.Set<DiscrepancyCase>().Select(c=>new {c.Id,c.ReceptionId,c.DispatchId}).ToListAsync(ct);
        return cases.Where(c=>allowed.TryGetValue(c.ReceptionId,out var dispatchId)&&dispatchId==c.DispatchId).Select(c=>c.Id).ToHashSet();
    }
}

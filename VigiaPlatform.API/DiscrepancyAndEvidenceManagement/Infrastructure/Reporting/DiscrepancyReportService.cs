namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Reporting;

using System.Text;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;

public class DiscrepancyReportService(AppDbContext db)
{
    public async Task<byte[]?> GenerateAsync(int id, CancellationToken ct)
    {
        var item = await db.Set<DiscrepancyCase>().AsNoTracking().Include(c => c.Events).SingleOrDefaultAsync(c => c.Id == id, ct);
        if (item is null) return null;
        var receptionId = int.TryParse(item.ReceptionId, out var value) ? value : 0;
        var reception = await db.Set<Reception>().AsNoTracking().Include(r => r.Items).Include(r => r.Evidences)
            .AsSplitQuery().SingleOrDefaultAsync(r => r.Id == receptionId, ct);
        var lines = new List<string> { "VIGIA - DISCREPANCY SUPPORTING REPORT", "", $"Case: {item.CaseNumber}",
            $"Dispatch: {item.DispatchId} | Reception: {item.ReceptionId}", $"Opened (UTC): {item.OpenedAt:O}",
            $"Status: {item.Status} | Responsibility: {item.DeterminedLiability}", $"Project: {reception?.SiteName ?? "Unlinked"}",
            $"Description: {reception?.Discrepancy?.Description ?? "Not recorded"}", $"Resolution: {item.ResolutionSummary ?? "Not recorded"}", "", "MATERIAL VERIFICATION" };
        foreach (var material in reception?.Items ?? [])
            lines.Add($"{material.MaterialName}: dispatched {material.DispatchedQuantity}, received {material.ReceivedQuantity} {material.Unit}; difference {material.DiscrepancyQuantity}");
        lines.AddRange(["", "EVIDENCE REFERENCES"]);
        foreach (var evidence in reception?.Evidences ?? []) lines.Add($"{evidence.AttachedAt:O} | {evidence.Description} | {evidence.FileUrl}");
        lines.AddRange(["", "CASE TIMELINE"]);
        foreach (var entry in item.Events.OrderBy(e => e.OccurredAt)) lines.Add($"{entry.OccurredAt:O} | {entry.Title} | {entry.Description}");
        lines.AddRange(["", $"Generated (UTC): {DateTimeOffset.UtcNow:O}", "Physical verification records an authenticated resident attestation.",
            "Scale measurements and graphic signatures are included only when recorded in the weighed-receipt workflow."]);
        return Render(lines);
    }

    private static byte[] Render(IEnumerable<string> source)
    {
        var lines = source.SelectMany(line => Wrap(line.Replace('\r', ' ').Replace('\n', ' '), 82)).ToArray();
        var pages = lines.Chunk(48).ToArray();
        using var output = new MemoryStream();
        void Write(string text) => output.Write(Encoding.Latin1.GetBytes(text));
        var offsets = new List<long> { 0 };
        void Object(int id, string value) { offsets.Add(output.Position); Write($"{id} 0 obj\n{value}\nendobj\n"); }
        Write("%PDF-1.4\n");
        Object(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Object(2, $"<< /Type /Pages /Count {pages.Length} /Kids [{string.Join(" ", Enumerable.Range(0, pages.Length).Select(i => $"{4 + 2 * i} 0 R"))}] >>");
        Object(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>");
        for (var i = 0; i < pages.Length; i++)
        {
            var content = "BT /F1 10 Tf 14 TL 40 800 Td\n" + string.Join("\n", pages[i].Select((line, index) =>
                $"{(index == 0 ? "" : "T* ")}({Escape(line)}) Tj")) + "\nET";
            Object(4 + 2 * i, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {5 + 2 * i} 0 R >>");
            Object(5 + 2 * i, $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }
        var xref = output.Position;
        Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:D10} 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static IEnumerable<string> Wrap(string line, int width)
    {
        if (line.Length == 0) { yield return ""; yield break; }
        for (var start = 0; start < line.Length; start += width) yield return line.Substring(start, Math.Min(width, line.Length - start));
    }
}

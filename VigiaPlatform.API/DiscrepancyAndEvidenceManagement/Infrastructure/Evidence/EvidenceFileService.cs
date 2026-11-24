namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.Shared.Domain.Repositories;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public record StoredPhotoResource(Guid Id,int EvidenceId,int ReceptionId,string DispatchId,string FileUrl,string Description,
    string ContentType,string FileName,int SizeBytes,string Sha256,DateTimeOffset UploadedAt,Guid RequestId);

public class EvidenceFileService(AppDbContext db,IUnitOfWork work,EvidenceAccess access)
{
    public async Task<StoredPhotoResource> UploadAsync(int receptionId,IFormFile file,string description,Guid requestId,ClaimsPrincipal user,CancellationToken ct)
    {
        if(requestId==Guid.Empty || string.IsNullOrWhiteSpace(description) || description.Trim().Length>250 || description.Any(char.IsControl))
            throw new ArgumentException("A request UUID and a description of 1 to 250 characters are required.");
        if(file.Length<=0 || file.Length>EvidencePhotoValidator.MaximumBytes)throw new ArgumentException("Select a photo of at most 5 MiB.");
        await using var stream=file.OpenReadStream();using var buffer=new MemoryStream();
        var chunk=new byte[65536];int read;
        while((read=await stream.ReadAsync(chunk,ct))>0) {if(buffer.Length+read>EvidencePhotoValidator.MaximumBytes)throw new ArgumentException("Photo exceeds 5 MiB.");await buffer.WriteAsync(chunk.AsMemory(0,read),ct);}
        var bytes=buffer.ToArray();var type=EvidencePhotoValidator.Validate(bytes,file.ContentType);
        var name=Path.GetFileName(file.FileName.Replace('\\','/'));
        if(string.IsNullOrWhiteSpace(name) || name.Length>120 || name.Any(char.IsControl))throw new ArgumentException("The file name is invalid or longer than 120 characters.");
        var extension=Path.GetExtension(name).ToLowerInvariant();
        if(type=="image/png" ? extension!=".png" : extension is not (".jpg" or ".jpeg"))
            throw new ArgumentException("The file extension must match the JPEG or PNG content.");
        var caption=description.Trim();var hash=Convert.ToHexString(SHA256.HashData(bytes));
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {name,caption,hash,type}))));
        await using var transaction=await db.Database.BeginTransactionAsync(ct);
        // Serialize upload/retry and receipt closure on the same parent row, before reading its current state.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM receptions WHERE id={receptionId} FOR UPDATE",ct);
        if(!await access.ReceptionAsync(receptionId,user,ct))throw new UnauthorizedAccessException();
        var replay=await db.Set<StoredEvidenceFile>().Include(f=>f.Evidence).SingleOrDefaultAsync(f=>f.ReceptionId==receptionId&&f.RequestId==requestId,ct);
        var receipt=await db.Set<Reception>().Include(r=>r.Evidences).SingleAsync(r=>r.Id==receptionId,ct);
        if(replay is not null)
        {
            if(replay.Fingerprint!=fingerprint)throw new InvalidOperationException("The request UUID was already used for another photo or description.");
            await transaction.CommitAsync(ct);return Resource(replay,receipt.DispatchId);
        }
        if(await db.Set<StoredEvidenceFile>().CountAsync(f=>f.ReceptionId==receptionId,ct)>=20)throw new InvalidOperationException("A reception supports at most 20 stored photos.");
        var id=Guid.NewGuid();var url=$"/api/v1/receptions/{receptionId}/evidence/files/{id}";
        var evidence=receipt.AttachEvidence("PHOTO",url,caption);
        var at=new DateTimeOffset(DateTimeOffset.UtcNow.UtcTicks/10*10,TimeSpan.Zero);
        var stored=new StoredEvidenceFile(id,receptionId,evidence,requestId,fingerprint,bytes,type,name,hash,
            int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),at);
        db.Add(stored);await work.CompleteAsync(ct);await transaction.CommitAsync(ct);
        return Resource(stored,receipt.DispatchId);
    }
    public static StoredPhotoResource Resource(StoredEvidenceFile f,string dispatchId)=>new(f.Id,f.EvidenceId,f.ReceptionId,dispatchId,
        f.Evidence.FileUrl,f.Evidence.Description,f.ContentType,f.FileName,f.Content.Length,f.Sha256,f.UploadedAt,f.RequestId);
}

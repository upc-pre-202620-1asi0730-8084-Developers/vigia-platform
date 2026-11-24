namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;
using VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence;
using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Exceptions;
using VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Integration;
using VigiaPlatform.API.SiteReceptionAndVerification.Interfaces.REST.Transform;

public class UploadPhotoForm
{
    public IFormFile File {get;set;}=default!;
    public string Description {get;set;}="";
    public Guid RequestId {get;set;}
}
[ApiController,Authorize(Roles="ADMIN,SITE_MANAGER"),Tags("Evidence")]
[Route("api/v1/receptions/{receptionId:int}")]
public class EvidenceFilesController(EvidenceAccess access,EvidenceFileService files,AppDbContext db,PhysicalReceptionService receipts):ControllerBase
{
    [HttpGet("evidence-context")]
    public async Task<IActionResult> Context(int receptionId,CancellationToken ct)
    {
        if(!await access.ReceptionAsync(receptionId,User,ct))return NotFound();
        return Ok(ReceptionAssembler.ToResourceFromEntity((await receipts.FindAsync(receptionId,ct))!));
    }
    [HttpPost("evidence/files"),Consumes("multipart/form-data")]
    [RequestSizeLimit(6*1024*1024),RequestFormLimits(MultipartBodyLengthLimit=6*1024*1024)]
    [ProducesResponseType(typeof(StoredPhotoResource),201)]
    [ProducesResponseType(400),ProducesResponseType(404),ProducesResponseType(409),ProducesResponseType(413)]
    public async Task<IActionResult> Upload(int receptionId,[FromForm] UploadPhotoForm input,CancellationToken ct)
    {
        if(!await access.ReceptionAsync(receptionId,User,ct))return NotFound();
        try
        {
            if(input.File is null)return BadRequest(new {code="PHOTO_REQUIRED",message="Select a photo."});
            var photo=await files.UploadAsync(receptionId,input.File,input.Description,input.RequestId,User,ct);
            return Created(photo.FileUrl,photo);
        }
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ArgumentException ex){return BadRequest(new {code="INVALID_PHOTO",message=ex.Message});}
        catch(InvalidOperationException ex){return Conflict(new {code="PHOTO_CONFLICT",message=ex.Message});}
        catch(SiteReceptionException ex){return Conflict(new {code=ex.Code,message=ex.Message});}
        catch(DbUpdateException ex) when(ex.GetBaseException() is MySqlException {Number:1062 or 1205 or 1213})
        {return Conflict(new {code="PHOTO_CONFLICT",message="A concurrent upload changed this reception; retry the same request."});}
    }
    [HttpGet("evidence/files/{fileId:guid}")]
    public async Task<IActionResult> Download(int receptionId,Guid fileId,CancellationToken ct)
    {
        if(!await access.ReceptionAsync(receptionId,User,ct))return NotFound();
        var file=await db.Set<StoredEvidenceFile>().AsNoTracking().SingleOrDefaultAsync(f=>f.Id==fileId&&f.ReceptionId==receptionId,ct);
        if(file is null)return NotFound();
        Response.Headers.CacheControl="private, no-store";Response.Headers["X-Content-Type-Options"]="nosniff";
        return File(file.Content,file.ContentType,file.FileName);
    }
}

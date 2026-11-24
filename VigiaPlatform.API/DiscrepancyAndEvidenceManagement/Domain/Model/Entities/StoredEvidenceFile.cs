namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Domain.Model.Entities;

using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Entities;

public class StoredEvidenceFile
{
    public Guid Id { get; private set; }
    public int ReceptionId { get; private set; }
    public int EvidenceId { get; private set; }
    public ReceptionEvidence Evidence { get; private set; }=default!;
    public Guid RequestId { get; private set; }
    public string Fingerprint { get; private set; }="";
    public byte[] Content { get; private set; }=[];
    public string ContentType { get; private set; }="";
    public string FileName { get; private set; }="";
    public string Sha256 { get; private set; }="";
    public int UploadedByAccountId { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    private StoredEvidenceFile() { }
    public StoredEvidenceFile(Guid id,int receptionId,ReceptionEvidence evidence,Guid requestId,string fingerprint,
        byte[] content,string contentType,string fileName,string hash,int actor,DateTimeOffset at)
    {
        Id=id;ReceptionId=receptionId;Evidence=evidence;RequestId=requestId;Fingerprint=fingerprint;
        Content=content;ContentType=contentType;FileName=fileName;Sha256=hash;UploadedByAccountId=actor;UploadedAt=at;
    }
}

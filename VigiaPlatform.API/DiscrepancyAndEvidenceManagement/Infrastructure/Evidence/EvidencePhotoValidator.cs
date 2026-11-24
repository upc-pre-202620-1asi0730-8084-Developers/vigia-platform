namespace VigiaPlatform.API.DiscrepancyAndEvidenceManagement.Infrastructure.Evidence;
using System.Buffers.Binary;

public static class EvidencePhotoValidator
{
    public const int MaximumBytes=5*1024*1024;
    public static string Validate(byte[] bytes,string suppliedType)
    {
        if(bytes.Length==0 || bytes.Length>MaximumBytes) throw new ArgumentException("Select a JPEG or PNG photo of at most 5 MiB.");
        string type;
        if(bytes.Length>=45 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) &&
           bytes.AsSpan(12,4).SequenceEqual("IHDR"u8) && bytes.AsSpan(bytes.Length-8,4).SequenceEqual("IEND"u8))
        {
            var width=BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16,4));
            var height=BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20,4));
            if(width==0 || height==0 || (ulong)width*height>16_000_000) throw new ArgumentException("Photo dimensions exceed 16 megapixels.");
            type="image/png";
        }
        else if(bytes.Length>=12 && bytes[0]==255 && bytes[1]==216 && bytes[^2]==255 && bytes[^1]==217)
        {
            var found=false;
            for(var i=2;i<bytes.Length-4;)
            {
                if(bytes[i++]!=255) break;
                while(i<bytes.Length && bytes[i]==255)i++;
                if(i>=bytes.Length)break;
                var marker=bytes[i++];if(marker==218)break;
                if(marker is 216 or 217 || marker is >=208 and <=215)continue;
                if(i+2>bytes.Length)break;
                var length=(bytes[i]<<8)|bytes[i+1];if(length<2 || i+length>bytes.Length)break;
                if(marker is 192 or 193 or 194 && length>=8)
                {
                    var height=(bytes[i+3]<<8)|bytes[i+4];var width=(bytes[i+5]<<8)|bytes[i+6];
                    if(width==0 || height==0 || (long)width*height>16_000_000)throw new ArgumentException("Photo dimensions exceed 16 megapixels.");
                    found=true;break;
                }
                i+=length;
            }
            if(!found)throw new ArgumentException("The JPEG header is invalid or unsupported.");
            type="image/jpeg";
        }
        else throw new ArgumentException("The file must contain a JPEG or PNG photo.");
        if(!string.Equals(type,suppliedType,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Photo content and media type do not match.");
        return type;
    }
}

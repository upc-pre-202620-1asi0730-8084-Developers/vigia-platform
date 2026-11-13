namespace VigiaPlatform.API.Shared.Domain.Repositories;

public interface IChangeRecorder
{
    void CaptureChanges();
    void AppendChanges();
}

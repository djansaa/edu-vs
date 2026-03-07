using EduVS.Models;

namespace EduVS.Helpers
{
    public interface IManualQrResolutionDialogService
    {
        ManualQrResolutionResult? Show(ManualQrResolutionRequest request);
    }
}

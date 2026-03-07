using System.Windows.Media.Imaging;

namespace EduVS.Models
{
    public class ManualQrResolutionRequest
    {
        public int SourcePageNumber { get; init; }
        public BitmapSource OriginalPreview { get; init; } = null!;
        public BitmapSource RotatedPreview { get; init; } = null!;
        public int? SuggestedTestId { get; init; }
        public char? SuggestedGroupId { get; init; }
        public int? SuggestedPageNumber { get; init; }
        public string? TestSubject { get; init; }
        public string? TestName { get; init; }
    }
}

using System.Windows.Media.Imaging;

namespace EduVS.Models
{
    public class TestData
    {
        public int TestId { get; set; }
        public BitmapSource NameBoxBitmap { get; set; } = null!;
        public bool Assigned { get; set; } = false;
    }
}

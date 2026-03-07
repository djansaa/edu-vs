namespace EduVS.Models
{
    public class GenerateTestResultsStartProgressInfo
    {
        public int ProcessedPages { get; init; }
        public int TotalPages { get; init; }
        public string StatusText { get; init; } = string.Empty;
    }
}

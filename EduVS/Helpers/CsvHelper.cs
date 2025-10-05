using EduVS.Models;

namespace EduVS.Helpers
{
    internal class CsvHelper
    {
        public static bool TryParse(string? line, out StudentData? student)
        {
            student = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            // normalize
            line = line.Replace('\u00A0', ' ').Trim(); // non-breaking spaces -> space
            if (line.Length == 0) return false;

            // split by common CSV delimiters (yours uses ';')
            var parts = line.Split(new[] { ';', ',', '\t' }, StringSplitOptions.None)
                            .Select(p => p.Trim())
                            .ToArray();

            // nothing meaningful?
            if (parts.All(string.IsNullOrWhiteSpace)) return false;

            string? name = null;

            // Case 1: "index;Name;..."  -> take second column if first is an integer
            if (parts.Length >= 2 &&
                int.TryParse(parts[0], out _) &&
                !string.IsNullOrWhiteSpace(parts[1]))
            {
                name = parts[1];
            }
            // Case 2: single "Name" in first column (but not numeric/header/date)
            else if (!int.TryParse(parts[0], out _) && !string.IsNullOrWhiteSpace(parts[0]))
            {
                name = parts[0];
            }

            if (string.IsNullOrWhiteSpace(name)) return false;

            student = new StudentData
            {
                Name = name.Trim(),
                TestId = null // keep null as requested
            };
            return true;
        }
    }
}

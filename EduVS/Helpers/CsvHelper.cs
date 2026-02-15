using EduVS.Models;

namespace EduVS.Helpers
{
    internal class CsvHelper
    {
        public static bool TryParse(string? line, out StudentData? student)
        {
            student = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            // Normalize non-breaking spaces that often appear in spreadsheet exports.
            line = line.Replace('\u00A0', ' ').Trim();
            if (line.Length == 0) return false;

            // Legacy parser: split by common delimiters and derive a single name field.
            var parts = line.Split(new[] { ';', ',', '\t' }, StringSplitOptions.None)
                            .Select(p => p.Trim())
                            .ToArray();

            if (parts.All(string.IsNullOrWhiteSpace)) return false;

            string? name = null;

            if (parts.Length >= 2 &&
                int.TryParse(parts[0], out _) &&
                !string.IsNullOrWhiteSpace(parts[1]))
            {
                name = parts[1];
            }
            else if (!int.TryParse(parts[0], out _) && !string.IsNullOrWhiteSpace(parts[0]))
            {
                name = parts[0];
            }

            if (string.IsNullOrWhiteSpace(name)) return false;

            student = new StudentData
            {
                Name = name.Trim(),
                TestId = null
            };
            return true;
        }

        public static string GetValueAt(string[] row, int index)
        {
            if (index < 0 || index >= row.Length) return string.Empty;
            return row[index]?.Trim() ?? string.Empty;
        }

        public static char ResolveDelimiter(string separator)
        {
            // Allow explicit "\t" option from UI; otherwise take first char.
            if (string.IsNullOrEmpty(separator)) return ';';
            if (separator == "\\t") return '\t';
            return separator[0];
        }
    }
}

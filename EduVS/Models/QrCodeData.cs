using System.Diagnostics;
using System.Globalization;

namespace EduVS.Models
{
    internal class QrCodeData
    {
        // $"{testId}|{groupId}|{testSubject}|{testName}|{testDate:yyyy-MM-dd}|{page}";
        public int TestId { get; set; }
        public char GroupId { get; set; } // 'A' / 'B'
        public string TestSubject { get; set; } = "";
        public string TestName { get; set; } = "";
        public DateTime? TestDate { get; set; } // yyyy-MM-dd
        public int Page { get; set; } // 0-based

        public static QrCodeData Parse(string payload)
        {
            if (!TryParse(payload, out var data)) throw new FormatException("Invalid QR data.");
            return data!;
        }

        public static bool TryParse(string? payload, out QrCodeData? data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(payload)) return false;

            var compactData = TryParseCompact(payload);
            if (compactData is not null)
            {
                data = compactData;
                return true;
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = payload.Split('|');
            foreach (var part in parts)
            {
                var idx = part.IndexOf(':');
                if (idx <= 0) continue;
                var key = part[..idx].Trim();
                var val = part[(idx + 1)..].Trim();
                dict[key] = val;
            }

            // required fields
            if (!dict.TryGetValue("TESTID", out var testIdStr))
            {
                Debug.WriteLine("TESTID not found in QR code data.");
                return false;
            }
            if (!dict.TryGetValue("GROUPID", out var groupStr))
            {
                Debug.WriteLine("GROUPID not found in QR code data.");
                return false;
            }
            if (!dict.TryGetValue("TESTSUBJECT", out var subjectStr))
            {
                Debug.WriteLine("TESTSUBJECT not found in QR code data.");
                return false;
            }
            if (!dict.TryGetValue("TESTNAME", out var nameStr))
            {
                Debug.WriteLine("TESTNAME not found in QR code data.");
                return false;
            }
            if (!dict.TryGetValue("PAGE", out var pageStr))
            {
                Debug.WriteLine("PAGE not found in QR code data.");
                return false;
            }

            if (!int.TryParse(testIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var testId)) return false;
            if (!int.TryParse(pageStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var page)) return false;

            var group = string.IsNullOrEmpty(groupStr) ? '\0' : char.ToUpperInvariant(groupStr[0]);
            if (group != 'A' && group != 'B') return false;

            DateTime? date = null;
            if (dict.TryGetValue("TESTDATE", out var dateStr) && !string.IsNullOrWhiteSpace(dateStr))
            {
                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                    || DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d))
                {
                    date = d.Date;
                }
            }

            data = new QrCodeData
            {
                TestId = testId,
                GroupId = group,
                TestSubject = subjectStr,
                TestName = nameStr,
                TestDate = date,
                Page = page
            };
            return true;
        }

        private static QrCodeData? TryParseCompact(string payload)
        {
            var parts = payload.Split('|');
            if (parts.Length < 6) return null;

            if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var testId)) return null;
            if (!int.TryParse(parts[5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var page)) return null;

            var groupStr = parts[1].Trim();
            var group = string.IsNullOrEmpty(groupStr) ? '\0' : char.ToUpperInvariant(groupStr[0]);
            if (group != 'A' && group != 'B') return null;

            DateTime? date = null;
            var dateStr = parts[4].Trim();
            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                    || DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d))
                {
                    date = d.Date;
                }
            }

            return new QrCodeData
            {
                TestId = testId,
                GroupId = group,
                TestSubject = parts[2].Trim(),
                TestName = parts[3].Trim(),
                TestDate = date,
                Page = page
            };
        }
    }
}

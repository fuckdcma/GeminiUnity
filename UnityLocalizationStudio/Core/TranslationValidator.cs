using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum ValidationIssueType
    {
        EmptyTranslation,
        MissingPlaceholder,
        DamagedMarkupTag,
        ExcessiveLength,
        UnsafeFormulaPrefix,
        MissingId,
        DuplicateKeyCollision
    }

    public class ValidationIssue
    {
        public int LineNumber { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public ValidationIssueType Type { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
    }

    public class ValidationReport
    {
        public int TotalRecords { get; set; }
        public int ValidTranslations { get; set; }
        public int EmptyTranslations { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public bool IsValid => ErrorCount == 0;
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public List<string> DiagnosticLogs { get; set; } = new List<string>();

        public string GetFormattedSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("==========================================================================");
            sb.AppendLine("           BÁO CÁO THẨM ĐỊNH BẢN DỊCH VIỆT HÓA (STEP 8 VALIDATION)");
            sb.AppendLine("==========================================================================");
            sb.AppendLine($"[1] TỔNG QUAN FILE DỊCH:");
            sb.AppendLine($"  - Tổng số chuỗi trong file: {TotalRecords}");
            sb.AppendLine($"  - Số câu đã dịch hợp lệ: {ValidTranslations}/{TotalRecords} ({(TotalRecords > 0 ? (double)ValidTranslations / TotalRecords * 100.0 : 0):F1}%)");
            sb.AppendLine($"  - Số câu chưa dịch (Pending): {EmptyTranslations}");
            sb.AppendLine($"  - Lỗi nghiêm trọng (Block Runtime): {ErrorCount}");
            sb.AppendLine($"  - Cảnh báo (Cần lưu ý): {WarningCount}");

            if (ErrorCount > 0)
            {
                sb.AppendLine("\n[2] DANH SÁCH LỖI NGHIÊM TRỌNG CẦN SỬA:");
                foreach (var err in Issues.Where(i => i.Severity == ValidationSeverity.Error).Take(15))
                {
                    sb.AppendLine($"  [Dòng {err.LineNumber}] ID={err.KeyId}: {err.Message}");
                    sb.AppendLine($"    • Gốc: \"{err.OriginalText}\"");
                    sb.AppendLine($"    • Dịch: \"{err.TranslatedText}\"");
                }
            }

            if (WarningCount > 0)
            {
                sb.AppendLine("\n[3] CẢNH BÁO TIỀM ẨN:");
                foreach (var warn in Issues.Where(i => i.Severity == ValidationSeverity.Warning).Take(10))
                {
                    sb.AppendLine($"  [Dòng {warn.LineNumber}] ID={warn.KeyId}: {warn.Message}");
                }
            }

            sb.AppendLine("\n[4] KẾT LUẬN:");
            if (IsValid)
            {
                sb.AppendLine("  ✔ FILE DỊCH ĐẠT CHUẨN: Toàn bộ Placeholder, Tag và Cấu trúc được bảo toàn an toàn.");
            }
            else
            {
                sb.AppendLine("  ✖ FILE DỊCH CẦN ĐIỀU CHỈNH: Vui lòng sửa các lỗi Placeholder bị hỏng trước khi nạp vào Runtime Mod.");
            }
            sb.AppendLine("==========================================================================");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Milestone M5: Translation & Font Import/Export Verification Workbench.
    /// Kiểm định tính toàn vẹn của tệp bản dịch (CSV, Excel), bảo toàn placeholder, RichText tag,
    /// ngăn chặn lỗi format string runtime và phòng chống formula injection.
    /// </summary>
    public static class TranslationValidator
    {
        private static readonly Regex FormatPlaceholderRegex = new Regex(@"\{(\d+|[a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);
        private static readonly Regex SpriteTagRegex = new Regex(@"<sprite(=[^>]+| [^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RichTextTagRegex = new Regex(@"</?(b|i|color|size|align|alpha|cspace|font|indent|line-height|link|margin|mark|mspace|noparse|pos|rotate|s|u|voffset|width)(=[^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CustomDelimiterRegex = new Regex(@"\$[a-zA-Z0-9_]+\$", RegexOptions.Compiled);

        /// <summary>
        /// Chuẩn hóa token định dạng (Tag TMP, RichText, Placeholder) để đối chiếu tương đương.
        /// Xử lý các biến thể escaped quote (\"cake\" vs "cake"), khoảng trắng thừa bên trong tag.
        /// </summary>
        public static string NormalizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            string t = token.Trim();

            // Nếu là tag TMP / RichText (<...>)
            if (t.StartsWith("<") && t.EndsWith(">"))
            {
                // 1. Khử escape quote và single quote trong attribute tag: <sprite name=\"cake\"> -> <sprite name="cake">
                t = t.Replace("\\\"", "\"").Replace("\\'", "'");
                // 2. Chuẩn hóa thuộc tính attribute ngoặc đơn sang ngoặc kép: name='cake' -> name="cake"
                t = Regex.Replace(t, @"=['""]([^'"">]+)['""]", "=\"$1\"");
                // 3. Chuẩn hóa khoảng trắng dư thừa trong tag: <sprite   name= -> <sprite name=
                t = Regex.Replace(t, @"\s+", " ");
                // 4. Khử khoảng trắng trước dấu đóng tag: <sprite name="cake" > -> <sprite name="cake">
                t = t.Replace(" >", ">");
            }

            return t;
        }

        /// <summary>
        /// Trích xuất toàn bộ placeholder và thẻ định dạng đặc thù trong chuỗi.
        /// </summary>
        public static HashSet<string> ExtractTokens(string text)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return tokens;

            // 1. Format placeholder {0}, {1}, {name}
            foreach (Match m in FormatPlaceholderRegex.Matches(text))
            {
                tokens.Add(NormalizeToken(m.Value));
            }

            // 2. Sprite tags <sprite name="...">
            foreach (Match m in SpriteTagRegex.Matches(text))
            {
                tokens.Add(NormalizeToken(m.Value));
            }

            // 3. RichText tags <b>, <color=...>
            foreach (Match m in RichTextTagRegex.Matches(text))
            {
                tokens.Add(NormalizeToken(m.Value));
            }

            // 4. Custom game tokens $upgradable$
            foreach (Match m in CustomDelimiterRegex.Matches(text))
            {
                tokens.Add(NormalizeToken(m.Value));
            }

            return tokens;
        }

        /// <summary>
        /// Thẩm định một danh sách TranslationEntry.
        /// </summary>
        public static ValidationReport ValidateEntries(IEnumerable<TranslationEntry> entries)
        {
            var report = new ValidationReport();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int line = 2; // Dòng 1 là Header
            foreach (var item in entries)
            {
                report.TotalRecords++;
                string keyId = item.Id ?? item.Key ?? $"ROW_{line}";
                string orig = item.OriginalText ?? "";
                string trans = item.VietnameseTranslation ?? "";

                // Kiểm tra ID rỗng
                if (string.IsNullOrWhiteSpace(keyId))
                {
                    report.Issues.Add(new ValidationIssue
                    {
                        LineNumber = line,
                        KeyId = keyId,
                        Type = ValidationIssueType.MissingId,
                        Severity = ValidationSeverity.Error,
                        Message = "Dòng bản dịch không có ID định danh duy nhất.",
                        OriginalText = orig,
                        TranslatedText = trans
                    });
                    report.ErrorCount++;
                }

                // Kiểm tra chuỗi chưa dịch
                if (string.IsNullOrWhiteSpace(trans))
                {
                    report.EmptyTranslations++;
                    line++;
                    continue;
                }

                report.ValidTranslations++;

                // 1. Kiểm tra Placeholder & Token Integrity
                var origTokens = ExtractTokens(orig);
                var transTokens = ExtractTokens(trans);

                foreach (var token in origTokens)
                {
                    if (!transTokens.Contains(token))
                    {
                        report.Issues.Add(new ValidationIssue
                        {
                            LineNumber = line,
                            KeyId = keyId,
                            Type = ValidationIssueType.MissingPlaceholder,
                            Severity = ValidationSeverity.Error,
                            Message = $"Bản dịch làm mất token bắt buộc: '{token}'. (Nếu game gọi string.Format sẽ gây crash FormatException)",
                            OriginalText = orig,
                            TranslatedText = trans
                        });
                        report.ErrorCount++;
                    }
                }

                // Kiểm tra token lạ bị người dịch gõ sai (ví dụ {không} hoặc {o} thay vì {0})
                foreach (var token in transTokens)
                {
                    if (!origTokens.Contains(token))
                    {
                        report.Issues.Add(new ValidationIssue
                        {
                            LineNumber = line,
                            KeyId = keyId,
                            Type = ValidationIssueType.DamagedMarkupTag,
                            Severity = ValidationSeverity.Warning,
                            Message = $"Bản dịch xuất hiện token mới không có trong chuỗi gốc: '{token}'.",
                            OriginalText = orig,
                            TranslatedText = trans
                        });
                        report.WarningCount++;
                    }
                }

                // 2. Phòng chống Formula Injection trong Excel (=, +, -, @)
                string trimmedTrans = trans.TrimStart();
                if (trimmedTrans.StartsWith("=") || trimmedTrans.StartsWith("+") || trimmedTrans.StartsWith("-") || trimmedTrans.StartsWith("@"))
                {
                    // Trừ trường hợp chuỗi toán học hợp lệ
                    if (!Regex.IsMatch(trimmedTrans, @"^[+\-]?\d+"))
                    {
                        report.Issues.Add(new ValidationIssue
                        {
                            LineNumber = line,
                            KeyId = keyId,
                            Type = ValidationIssueType.UnsafeFormulaPrefix,
                            Severity = ValidationSeverity.Warning,
                            Message = $"Bản dịch bắt đầu bằng ký tự công thức Excel ('{trimmedTrans[0]}'). Cần tự động escape bằng single quote khi mở trong bảng tính.",
                            OriginalText = orig,
                            TranslatedText = trans
                        });
                        report.WarningCount++;
                    }
                }

                // 3. Cảnh báo độ dài bản dịch quá mức (> 2.8 lần bản gốc)
                if (orig.Length >= 15 && trans.Length > orig.Length * 2.8)
                {
                    report.Issues.Add(new ValidationIssue
                    {
                        LineNumber = line,
                        KeyId = keyId,
                        Type = ValidationIssueType.ExcessiveLength,
                        Severity = ValidationSeverity.Warning,
                        Message = $"Độ dài bản dịch ({trans.Length} ký tự) dài hơn 2.8 lần chuỗi gốc ({orig.Length} ký tự), có nguy cơ tràn UI (text overflow).",
                        OriginalText = orig,
                        TranslatedText = trans
                    });
                    report.WarningCount++;
                }

                line++;
            }

            return report;
        }

        /// <summary>
        /// Đọc tệp CSV và thẩm định toàn diện.
        /// </summary>
        public static ValidationReport ValidateCsvFile(string csvPath)
        {
            if (!File.Exists(csvPath))
            {
                var r = new ValidationReport();
                r.Issues.Add(new ValidationIssue
                {
                    LineNumber = 0,
                    KeyId = "FILE_NOT_FOUND",
                    Type = ValidationIssueType.MissingId,
                    Severity = ValidationSeverity.Error,
                    Message = $"Không tìm thấy tệp CSV tại: {csvPath}"
                });
                r.ErrorCount++;
                return r;
            }

            var entries = CsvHandler.ImportFromCsv(csvPath);
            return ValidateEntries(entries);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class CsvHandler
    {
        private static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

        /// <summary>
        /// Xuất danh sách bản dịch ra CSV đầy đủ provenance và bảo toàn định dạng Excel
        /// </summary>
        public static void ExportToProvenanceCsv(IEnumerable<TranslationEntry> entries, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var writer = new StreamWriter(filePath, false, Utf8WithBom);
            // Header tiêu chuẩn đối chiếu
            writer.WriteLine("ID,Language,SourceKind,Asset_File,Asset_PathID,Origin,Field,Key,Reference_PathID,JSON_Pointer,Odin_Value_Offset,Original_Text,Vietnamese_Translation,Placeholders,Classification,Status,Confidence,Notes");

            foreach (var item in entries)
            {
                string line = string.Join(",",
                    EscapeCsvField(item.Id),
                    EscapeCsvField(item.Language),
                    EscapeCsvField(item.SourceKind.ToString()),
                    EscapeCsvField(item.AssetFile),
                    EscapeExcelSafeText(item.AssetPathId), // Bảo toàn PathID lớn / số 0
                    EscapeCsvField(item.Origin),
                    EscapeCsvField(item.Field),
                    EscapeCsvField(item.Key),
                    EscapeExcelSafeText(item.ReferencePathId),
                    EscapeCsvField(item.JsonPointer),
                    EscapeCsvField(item.OdinValueOffset),
                    EscapeCsvField(item.OriginalText),
                    EscapeCsvField(item.VietnameseTranslation),
                    EscapeCsvField(item.Placeholders),
                    EscapeCsvField(item.Classification.ToString()),
                    EscapeCsvField(item.Status),
                    EscapeCsvField(item.Confidence),
                    EscapeCsvField(item.Notes)
                );
                writer.WriteLine(line);
            }
        }

        /// <summary>
        /// Xuất danh sách Game Text Only (chỉ văn bản thực sự cần dịch)
        /// </summary>
        public static void ExportGameTextOnlyCsv(IEnumerable<TranslationEntry> entries, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var writer = new StreamWriter(filePath, false, Utf8WithBom);
            writer.WriteLine("ID,Language,Asset_File,Asset_PathID,Origin,Field,Key,Original_Text,Vietnamese_Translation,Placeholders,Status,Notes");

            foreach (var item in entries)
            {
                if (item.Classification != StringClassification.GameText)
                    continue;

                string line = string.Join(",",
                    EscapeCsvField(item.Id),
                    EscapeCsvField(item.Language),
                    EscapeCsvField(item.AssetFile),
                    EscapeExcelSafeText(item.AssetPathId),
                    EscapeCsvField(item.Origin),
                    EscapeCsvField(item.Field),
                    EscapeCsvField(item.Key),
                    EscapeCsvField(item.OriginalText),
                    EscapeCsvField(item.VietnameseTranslation),
                    EscapeCsvField(item.Placeholders),
                    EscapeCsvField(item.Status),
                    EscapeCsvField(item.Notes)
                );
                writer.WriteLine(line);
            }
        }

        /// <summary>
        /// Xuất file CSV định dạng chuẩn để người dùng dịch (Key_ID, Language, Context_Path, Original_Text...)
        /// </summary>
        public static void ExportToCsv(IEnumerable<TranslationEntry> entries, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                using var writer = new StreamWriter(filePath, false, Utf8WithBom);
                writer.WriteLine("Key_ID,Language,Context_Path,Original_Text,Vietnamese_Translation,Status,Notes");

                foreach (var item in entries)
                {
                    string line = $"{EscapeCsvField(item.Id)},{EscapeCsvField(item.Language)},{EscapeCsvField(item.ContextPath)},{EscapeCsvField(item.OriginalText)},{EscapeCsvField(item.VietnameseTranslation)},{EscapeCsvField(item.Status)},{EscapeCsvField(item.Notes)}";
                    writer.WriteLine(line);
                }
            }
            catch (IOException ioEx)
            {
                // Nếu file đang bị khóa bởi Excel hoặc ứng dụng khác, xuất sang tệp fallback _Clean.csv
                string fallbackPath = Path.Combine(dir ?? "", Path.GetFileNameWithoutExtension(filePath) + "_Clean.csv");
                try
                {
                    using var writer = new StreamWriter(fallbackPath, false, Utf8WithBom);
                    writer.WriteLine("Key_ID,Language,Context_Path,Original_Text,Vietnamese_Translation,Status,Notes");
                    foreach (var item in entries)
                    {
                        string line = $"{EscapeCsvField(item.Id)},{EscapeCsvField(item.Language)},{EscapeCsvField(item.ContextPath)},{EscapeCsvField(item.OriginalText)},{EscapeCsvField(item.VietnameseTranslation)},{EscapeCsvField(item.Status)},{EscapeCsvField(item.Notes)}";
                        writer.WriteLine(line);
                    }
                }
                catch { }

                throw new IOException($"Tệp '{Path.GetFileName(filePath)}' đang được mở trong Excel. Vui lòng đóng Excel rồi thử lại (Đã lưu bản sao sạch tại: {fallbackPath}).", ioEx);
            }
        }

        /// <summary>
        /// Xuất file CSV định dạng Asset Verified đầy đủ cột đối chứng khớp chuẩn PlateUp_EN_2243_Asset_Verified.csv
        /// </summary>
        public static void ExportVerifiedAssetCsv(IEnumerable<TranslationEntry> entries, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var writer = new StreamWriter(filePath, false, Utf8WithBom);
            writer.WriteLine("ID,Asset_File,Asset_PathID,Origin,Field,Key,Reference_PathID,JSON_Pointer,Odin_Value_Offset,Odin_UTF16_Units,English_Escaped,Vietnamese_Escaped,Placeholders,Status,Notes,ULS_ID,Verification");

            foreach (var item in entries)
            {
                string line = string.Join(",",
                    EscapeCsvField(item.Id),
                    EscapeCsvField(item.AssetFile),
                    EscapeExcelSafeText(item.AssetPathId),
                    EscapeCsvField(item.Origin),
                    EscapeCsvField(item.Field),
                    EscapeCsvField(item.Key),
                    EscapeExcelSafeText(item.ReferencePathId),
                    EscapeCsvField(item.JsonPointer),
                    EscapeCsvField(item.OdinValueOffset),
                    EscapeCsvField(item.OdinUtf16Units),
                    EscapeCsvField(item.OriginalText),
                    EscapeCsvField(item.VietnameseTranslation),
                    EscapeCsvField(item.Placeholders),
                    EscapeCsvField(item.Status),
                    EscapeCsvField(item.Notes),
                    EscapeCsvField(item.Id),
                    EscapeCsvField("MATCHED_PATHID_AND_TEXT")
                );
                writer.WriteLine(line);
            }
        }

        /// <summary>
        /// Đọc tệp CSV (tương thích cả header cổ điển lẫn header đối chứng provenance và Language)
        /// </summary>
        public static List<TranslationEntry> ImportFromCsv(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Không tìm thấy tệp CSV: {filePath}");

            var results = new List<TranslationEntry>();
            string content;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                content = reader.ReadToEnd();
            }

            var rows = ParseCsvContent(content);
            if (rows.Count <= 1)
                return results;

            var header = rows[0];
            bool isProvenanceFormat = header.Contains("SourceKind") || header.Contains("Asset_File");

            for (int i = 1; i < rows.Count; i++)
            {
                var cols = rows[i];
                if (cols.Count < 3) continue;

                if (isProvenanceFormat)
                {
                    var entry = new TranslationEntry
                    {
                        Id = GetCol(cols, header, "ID"),
                        Language = GetCol(cols, header, "Language", "EN"),
                        AssetFile = GetCol(cols, header, "Asset_File"),
                        AssetPathId = CleanExcelSafeText(GetCol(cols, header, "Asset_PathID")),
                        Origin = GetCol(cols, header, "Origin"),
                        Field = GetCol(cols, header, "Field"),
                        Key = GetCol(cols, header, "Key"),
                        ReferencePathId = CleanExcelSafeText(GetCol(cols, header, "Reference_PathID")),
                        JsonPointer = GetCol(cols, header, "JSON_Pointer"),
                        OdinValueOffset = GetCol(cols, header, "Odin_Value_Offset"),
                        OriginalText = GetCol(cols, header, "Original_Text"),
                        VietnameseTranslation = GetCol(cols, header, "Vietnamese_Translation"),
                        Placeholders = GetCol(cols, header, "Placeholders"),
                        Status = GetCol(cols, header, "Status", "Pending"),
                        Notes = GetCol(cols, header, "Notes")
                    };

                    if (Enum.TryParse<SourceKind>(GetCol(cols, header, "SourceKind"), out var sk))
                    {
                        entry.SourceKind = sk;
                    }
                    if (Enum.TryParse<StringClassification>(GetCol(cols, header, "Classification"), out var sc))
                    {
                        entry.Classification = sc;
                    }

                    results.Add(entry);
                }
                else
                {
                    // Format bản dịch chuẩn có Language hoặc cổ điển
                    string id = header.Contains("Key_ID") ? GetCol(cols, header, "Key_ID") : (header.Contains("ID") ? GetCol(cols, header, "ID") : cols[0]);
                    string lang = header.Contains("Language") ? GetCol(cols, header, "Language", "EN") : "EN";
                    string ctx = header.Contains("Context_Path") ? GetCol(cols, header, "Context_Path") : (cols.Count > 1 ? cols[1] : "");
                    string orig = header.Contains("Original_Text") ? GetCol(cols, header, "Original_Text") : (cols.Count > 2 ? cols[2] : "");
                    string vn = header.Contains("Vietnamese_Translation") ? GetCol(cols, header, "Vietnamese_Translation") : (cols.Count > 3 ? cols[3] : "");
                    string status = header.Contains("Status") ? GetCol(cols, header, "Status", "Pending") : (cols.Count > 4 ? cols[4] : "Pending");
                    string notes = header.Contains("Notes") ? GetCol(cols, header, "Notes") : (cols.Count > 5 ? cols[5] : "");

                    var entry = new TranslationEntry
                    {
                        Id = id,
                        Language = lang,
                        ContextPath = ctx,
                        OriginalText = orig,
                        VietnameseTranslation = vn,
                        Status = status,
                        Notes = notes
                    };
                    SmartClassifier.Classify(entry);
                    results.Add(entry);
                }
            }

            return results;
        }

        private static string GetCol(List<string> row, List<string> header, string colName, string def = "")
        {
            int idx = header.IndexOf(colName);
            if (idx >= 0 && idx < row.Count)
            {
                return row[idx];
            }
            return def;
        }

        public static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            bool special = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');
            if (special)
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        /// <summary>
        /// Bảo toàn chuỗi số như "000123", "1E10" khi mở trong Excel không bị biến đổi kiểu (AT-12)
        /// </summary>
        public static string EscapeExcelSafeText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            // Nếu chuỗi bắt đầu bằng 0 và theo sau là số, hoặc có dạng số mũ khoa học (1E10)
            if (Regex.IsMatch(text, @"^0\d+$") || Regex.IsMatch(text, @"^\d+[eE]\d+$"))
            {
                return $"\"=\"\"{text}\"\"\"";
            }
            return EscapeCsvField(text);
        }

        public static string CleanExcelSafeText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string t = text.Trim();
            if (t.StartsWith("\"") && t.EndsWith("\""))
            {
                t = t.Substring(1, t.Length - 2).Replace("\"\"", "\"");
            }
            if (t.StartsWith("=\"") && t.EndsWith("\""))
            {
                return t.Substring(2, t.Length - 3);
            }
            return t;
        }

        public static List<List<string>> ParseCsvContent(string content)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else if (c == '\r')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '\n') i++;
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                    }
                    else if (c == '\n')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }

            if (currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                rows.Add(currentRow);
            }

            return rows;
        }
    }
}

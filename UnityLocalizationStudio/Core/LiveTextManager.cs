using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class LiveCapturedTextItem
    {
        public string KeyOrId { get; set; } = "";
        public string ContextPath { get; set; } = "";
        public string OriginalText { get; set; } = "";
        public string VietnameseTranslation { get; set; } = "";
        public string LanguageDetected { get; set; } = "English";
        public string ActiveFont { get; set; } = "";
        public string Status { get; set; } = "Captured_Live";
    }

    public static class LiveTextManager
    {
        public static List<LiveCapturedTextItem> ProcessLiveTelemetrySnapshot(
            string liveSnapshotJsonPath,
            string? inGameCapturedCsvPath = null,
            List<TranslationEntry>? rawEntries = null)
        {
            var results = new List<LiveCapturedTextItem>();
            var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Đọc từ Live Snapshot JSON nếu có
            if (File.Exists(liveSnapshotJsonPath))
            {
                var snapshot = LiveFontInspector.LoadSnapshot(liveSnapshotJsonPath);
                if (snapshot != null)
                {
                    foreach (var el in snapshot.Elements)
                    {
                        if (string.IsNullOrWhiteSpace(el.Text) || seenTexts.Contains(el.Text)) continue;
                        seenTexts.Add(el.Text);

                        string key = "";
                        string trans = "";

                        // Tìm trong raw entries để đối chiếu ID hoặc bản dịch có sẵn
                        if (rawEntries != null)
                        {
                            var match = rawEntries.FirstOrDefault(r => r.OriginalText.Equals(el.Text, StringComparison.OrdinalIgnoreCase) ||
                                                                     r.VietnameseTranslation.Equals(el.Text, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                key = match.Id ?? match.Key ?? "";
                                trans = match.VietnameseTranslation;
                            }
                        }

                        results.Add(new LiveCapturedTextItem
                        {
                            KeyOrId = string.IsNullOrEmpty(key) ? "0x" + Math.Abs(el.Text.GetHashCode()).ToString("X8") : key,
                            ContextPath = el.HierarchyPath,
                            OriginalText = el.Text,
                            VietnameseTranslation = trans,
                            LanguageDetected = DetectLanguage(el.Text),
                            ActiveFont = el.ActiveFont,
                            Status = "Active_On_Screen"
                        });
                    }
                }
            }

            // 2. Đọc bổ sung từ InGame_Live_Captured_Texts.csv nếu có
            if (!string.IsNullOrEmpty(inGameCapturedCsvPath) && File.Exists(inGameCapturedCsvPath))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(inGameCapturedCsvPath, Encoding.UTF8).Skip(1))
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 3)
                        {
                            string key = parts[0].Trim();
                            string ctx = parts[1].Trim();
                            string txt = parts[2].Trim().Trim('"');
                            if (!string.IsNullOrEmpty(txt) && !seenTexts.Contains(txt))
                            {
                                seenTexts.Add(txt);
                                results.Add(new LiveCapturedTextItem
                                {
                                    KeyOrId = key,
                                    ContextPath = ctx,
                                    OriginalText = txt,
                                    LanguageDetected = DetectLanguage(txt),
                                    Status = "Captured_InGame_Hook"
                                });
                            }
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        public static string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Unknown";

            // Kiểm tra dấu tiếng Việt đặc trưng
            string vietnameseChars = "áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđĐ";
            if (text.Any(c => vietnameseChars.Contains(c))) return "Tiếng Việt (vi-VN)";

            // Kiểm tra tiếng Nhật / Trung / Hàn
            if (text.Any(c => c >= 0x4E00 && c <= 0x9FFF)) return "Chinese/Japanese (CJK)";
            if (text.Any(c => c >= 0x3040 && c <= 0x30FF)) return "Japanese (Kana)";
            if (text.Any(c => c >= 0xAC00 && c <= 0xD7AF)) return "Korean (Hangul)";
            if (text.Any(c => c >= 0x0400 && c <= 0x04FF)) return "Russian (Cyrillic)";

            return "English (en-US)";
        }

        public static void ExportLiveMappingCsv(List<LiveCapturedTextItem> items, string outputPath)
        {
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine("Key_ID,Context_Path,Original_Text,Vietnamese_Translation,Language_Detected,Active_Font,Status");
            foreach (var it in items)
            {
                sb.AppendLine($"\"{EscapeCsv(it.KeyOrId)}\",\"{EscapeCsv(it.ContextPath)}\",\"{EscapeCsv(it.OriginalText)}\",\"{EscapeCsv(it.VietnameseTranslation)}\",\"{EscapeCsv(it.LanguageDetected)}\",\"{EscapeCsv(it.ActiveFont)}\",\"{EscapeCsv(it.Status)}\"");
            }
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\"\"");
        }
    }
}

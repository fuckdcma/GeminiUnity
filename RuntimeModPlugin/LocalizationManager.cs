using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VietnameseLocalizationMod
{
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _contextMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _fallbackMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _keyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _dictKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _mockMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _dumpedStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex ParameterRegex = new Regex(@"\{(\d+)\}|%[dsf]", RegexOptions.Compiled);

        public static bool IsEnabled { get; set; } = true;
        public static bool UseSampleMockText { get; set; } = false;
        public static bool DumpNewStringsEnabled { get; set; } = true;
        private static string _dumpFilePath = string.Empty;
        private static Action<string>? _logger;

        public static void Initialize(string datFilePath, string pluginDir, Action<string>? logger = null)
        {
            _logger = logger;
            _dumpFilePath = Path.Combine(pluginDir, "InGame_Live_Captured_Texts.csv");

            // 1. Nạp từ cơ sở dữ liệu mã hóa Vietnamese.dat (nếu có)
            if (File.Exists(datFilePath))
            {
                var (cMap, fMap, kMap, dMap) = MemoryDecrypter.LoadEncryptedDatabase(datFilePath);
                _contextMap = cMap;
                _fallbackMap = fMap;
                _keyMap = kMap;
                _dictKeyMap = dMap;
            }

            // 2. Nạp bổ sung từ các tệp CSV (Translated_Vietnamese.csv hoặc bất kỳ .csv bản dịch nào)
            LoadCsvTranslationsIfPresent(pluginDir);

            // 3. Nạp bổ sung dữ liệu Text mẫu thử nghiệm (Mock) nếu có để Live Test kiểm tra hook
            LoadMockTranslationsIfPresent(pluginDir);

            _logger?.Invoke($">>> [Vietnamese Localization Mod] Đã nạp {_contextMap.Count} Context, {_fallbackMap.Count} Fallback, {_keyMap.Count} Keys, {_dictKeyMap.Count} DictKeys vào RAM.");
        }

        private static void LoadCsvTranslationsIfPresent(string pluginDir)
        {
            if (string.IsNullOrEmpty(pluginDir) || !Directory.Exists(pluginDir)) return;

            string[] candidateCsvs = new[]
            {
                Path.Combine(pluginDir, "Translated_Vietnamese.csv"),
                Path.Combine(pluginDir, "Vietnamese.csv"),
                Path.Combine(pluginDir, "Sample_LiveTest_Texts.csv")
            };

            foreach (var csvPath in candidateCsvs)
            {
                if (!File.Exists(csvPath)) continue;
                try
                {
                    string content = File.ReadAllText(csvPath, Encoding.UTF8);
                    var rows = ParseCsvContent(content);
                    if (rows.Count <= 1) continue;

                    var header = rows[0];
                    int colKey = -1, colContext = -1, colOrig = -1, colTrans = -1, colNotes = -1;

                    for (int c = 0; c < header.Count; c++)
                    {
                        string h = header[c].Trim().ToLowerInvariant();
                        if (h.Contains("key") || h.Contains("id")) colKey = c;
                        if (h.Contains("context")) colContext = c;
                        if (h.Contains("original") || h.Contains("english") || h.Contains("tiếng anh")) colOrig = c;
                        if (h.Contains("translation") || h.Contains("vietnamese") || h.Contains("tiếng việt") || h.Contains("text_mẫu") || h.Contains("mock") || h.Contains("replacement")) colTrans = c;
                        if (h.Contains("notes") || h.Contains("ghi chú")) colNotes = c;
                    }

                    // Mặc định chuẩn định dạng ULS nếu không tìm thấy tên cột
                    if (colOrig == -1 && colTrans == -1 && header.Count >= 5)
                    {
                        colKey = 0; colContext = 2; colOrig = 3; colTrans = 4; colNotes = 6;
                    }
                    else if (colOrig == -1 && header.Count >= 2)
                    {
                        colKey = 0; colOrig = 1; colTrans = 1;
                    }

                    int loaded = 0;
                    for (int r = 1; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        if (row.Count == 0) continue;

                        string orig = colOrig >= 0 && colOrig < row.Count ? row[colOrig].Trim() : "";
                        string trans = colTrans >= 0 && colTrans < row.Count ? row[colTrans].Trim() : "";
                        string key = colKey >= 0 && colKey < row.Count ? row[colKey].Trim() : "";
                        string ctx = colContext >= 0 && colContext < row.Count ? row[colContext].Trim() : "";
                        string notes = colNotes >= 0 && colNotes < row.Count ? row[colNotes].Trim() : "";

                        if (string.IsNullOrEmpty(orig) && !string.IsNullOrEmpty(key)) orig = key;
                        if (string.IsNullOrEmpty(trans) && !string.IsNullOrEmpty(orig)) trans = orig;
                        if (string.IsNullOrEmpty(orig) || string.IsNullOrEmpty(trans)) continue;

                        // Lưu vào Fallback Map (cả chuỗi gốc và chuỗi đã sanitize)
                        _fallbackMap[orig] = trans;
                        string sanitized = Sanitize(orig);
                        if (!string.IsNullOrEmpty(sanitized))
                        {
                            _fallbackMap[sanitized] = trans;
                        }

                        // Lưu vào Context Map nếu có vị trí UI
                        if (!string.IsNullOrEmpty(ctx))
                        {
                            _contextMap[$"{ctx}::{sanitized}"] = trans;
                            _contextMap[$"{ctx}::{orig}"] = trans;
                        }

                        // Lưu vào Key Map
                        if (!string.IsNullOrEmpty(key))
                        {
                            _keyMap[key] = trans;
                        }

                        // Tách khóa Dictionary (ví dụ: [MAIN_MENU_OFFLINE_PLAY], [MENU_OPTIONS])
                        if (!string.IsNullOrEmpty(notes))
                        {
                            int bOpen = notes.LastIndexOf('[');
                            int bClose = notes.LastIndexOf(']');
                            if (bOpen >= 0 && bClose > bOpen)
                            {
                                string dictKey = notes.Substring(bOpen + 1, bClose - bOpen - 1).Trim();
                                if (!string.IsNullOrEmpty(dictKey))
                                {
                                    _dictKeyMap[dictKey] = trans;
                                }
                            }
                        }

                        loaded++;
                    }

                    _logger?.Invoke($">>> [Vietnamese Localization Mod] Đã nạp {loaded} chuỗi từ CSV: {Path.GetFileName(csvPath)}");
                }
                catch (Exception ex)
                {
                    _logger?.Invoke($">>> [Vietnamese Localization Mod] Lỗi nạp CSV {Path.GetFileName(csvPath)}: {ex.Message}");
                }
            }
        }

        private static List<List<string>> ParseCsvContent(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cur = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cur.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cur.Append(c);
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
                        row.Add(cur.ToString());
                        cur.Clear();
                    }
                    else if (c == '\r')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                        row.Add(cur.ToString());
                        cur.Clear();
                        rows.Add(row);
                        row = new List<string>();
                    }
                    else if (c == '\n')
                    {
                        row.Add(cur.ToString());
                        cur.Clear();
                        rows.Add(row);
                        row = new List<string>();
                    }
                    else
                    {
                        cur.Append(c);
                    }
                }
            }

            if (cur.Length > 0 || row.Count > 0)
            {
                row.Add(cur.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static void LoadMockTranslationsIfPresent(string pluginDir)
        {
            if (string.IsNullOrEmpty(pluginDir) || !Directory.Exists(pluginDir)) return;

            string[] candidateMocks = new[]
            {
                Path.Combine(pluginDir, "LiveTest_Mock_Translation.csv"),
                Path.Combine(pluginDir, "Sample_LiveTest_Texts.csv")
            };

            foreach (var mockPath in candidateMocks)
            {
                if (!File.Exists(mockPath)) continue;
                try
                {
                    int mockLoaded = 0;
                    var lines = File.ReadAllLines(mockPath, Encoding.UTF8);
                    foreach (var l in lines.Skip(1))
                    {
                        var p = l.Split(',');
                        if (p.Length >= 2)
                        {
                            string orig = "";
                            string mock = "";
                            if (p.Length >= 4)
                            {
                                orig = p[2].Trim().Trim('"');
                                mock = p[3].Trim().Trim('"');
                            }
                            else if (p.Length >= 2)
                            {
                                orig = p[0].Trim().Trim('"');
                                mock = p[1].Trim().Trim('"');
                            }

                            if (!string.IsNullOrEmpty(orig) && !string.IsNullOrEmpty(mock))
                            {
                                _mockMap[orig] = mock;
                                _mockMap[Sanitize(orig)] = mock;
                                _fallbackMap[orig] = mock;
                                _fallbackMap[Sanitize(orig)] = mock;
                                mockLoaded++;
                            }
                        }
                    }
                    _logger?.Invoke($">>> [Vietnamese Localization Mod] Đã nạp {mockLoaded} Text Mẫu Live Test từ {Path.GetFileName(mockPath)}.");
                    if (mockLoaded > 0)
                    {
                        // Ưu tiên cao nhất cho LiveTest_Mock_Translation.csv được trích xuất trực tiếp từ màn hình game Bước 5
                        break;
                    }
                }
                catch { }
            }
        }

        public static string GetGameObjectPath(Transform? transform)
        {
            if (transform == null) return "GLOBAL";
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        public static Dictionary<string, string> ContextMap => _contextMap;
        public static Dictionary<string, string> FallbackMap => _fallbackMap;
        public static Dictionary<string, string> KeyMap => _keyMap;
        public static Dictionary<string, string> DictKeyMap => _dictKeyMap;

        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return ParameterRegex.Replace(text, "{}");
        }

        public static string TranslateKey(string key)
        {
            if (string.IsNullOrEmpty(key) || !IsEnabled) return "";
            if (_dictKeyMap.TryGetValue(key, out string val) && !string.IsNullOrEmpty(val)) return val;
            if (_keyMap.TryGetValue(key, out string valK) && !string.IsNullOrEmpty(valK)) return valK;
            if (_fallbackMap.TryGetValue(key, out string valF) && !string.IsNullOrEmpty(valF)) return valF;
            return "";
        }

        public static string Translate(string originalText, Transform? transform)
        {
            if (string.IsNullOrEmpty(originalText))
                return originalText;

            string contextPath = GetGameObjectPath(transform);

            // Ghi nhận chuỗi THỰC TẾ XUẤT HIỆN trên màn hình Game UI
            if (DumpNewStringsEnabled && !string.IsNullOrWhiteSpace(originalText))
            {
                RecordInGameText(contextPath, originalText);
            }

            if (!IsEnabled)
                return originalText;

            string trimmed = originalText.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return originalText;

            string sanitized = Sanitize(trimmed);
            var paramMatches = ParameterRegex.Matches(trimmed);
            string contextKey = $"{contextPath}::{sanitized}";

            // 0. Nếu đang ở chế độ thử nghiệm Text mẫu (Step 6), ưu tiên mock template
            if (UseSampleMockText)
            {
                if (_mockMap.TryGetValue(trimmed, out string m1) && !string.IsNullOrEmpty(m1))
                {
                    return PreserveWhitespace(originalText, RestoreParameters(m1, paramMatches));
                }
                if (_mockMap.TryGetValue(sanitized, out string m2) && !string.IsNullOrEmpty(m2))
                {
                    return PreserveWhitespace(originalText, RestoreParameters(m2, paramMatches));
                }
            }

            // 1. Ưu tiên khớp chính xác theo Context Path (Context-Aware Disambiguation)
            if (_contextMap.TryGetValue(contextKey, out string viTemplate) && !string.IsNullOrEmpty(viTemplate))
            {
                return PreserveWhitespace(originalText, RestoreParameters(viTemplate, paramMatches));
            }

            // 2. Dự phòng tra cứu theo Fallback Generic (sanitized)
            if (_fallbackMap.TryGetValue(sanitized, out string fallbackTemplate) && !string.IsNullOrEmpty(fallbackTemplate))
            {
                return PreserveWhitespace(originalText, RestoreParameters(fallbackTemplate, paramMatches));
            }

            // 3. Dự phòng tra cứu theo Fallback Generic (chuỗi trimmed nguyên bản)
            if (_fallbackMap.TryGetValue(trimmed, out string directTemplate) && !string.IsNullOrEmpty(directTemplate))
            {
                return PreserveWhitespace(originalText, RestoreParameters(directTemplate, paramMatches));
            }

            // 4. Dự phòng tra cứu theo Dictionary Key (nếu đầu vào là tên khóa như MAIN_MENU_OFFLINE_PLAY)
            if (_dictKeyMap.TryGetValue(trimmed, out string dictTemplate) && !string.IsNullOrEmpty(dictTemplate))
            {
                return PreserveWhitespace(originalText, RestoreParameters(dictTemplate, paramMatches));
            }

            return originalText;
        }

        private static string PreserveWhitespace(string original, string translated)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(translated))
                return translated;

            int lead = 0;
            while (lead < original.Length && char.IsWhiteSpace(original[lead])) lead++;
            int trail = 0;
            while (trail < original.Length - lead && char.IsWhiteSpace(original[original.Length - 1 - trail])) trail++;

            string prefix = lead > 0 ? original.Substring(0, lead) : "";
            string suffix = trail > 0 ? original.Substring(original.Length - trail) : "";
            return prefix + translated + suffix;
        }

        private static string RestoreParameters(string template, MatchCollection paramMatches)
        {
            if (paramMatches.Count == 0 || string.IsNullOrEmpty(template))
                return template;

            string result = template;
            for (int i = 0; i < paramMatches.Count; i++)
            {
                string token = "{" + i + "}";
                if (result.Contains(token))
                {
                    result = result.Replace(token, paramMatches[i].Value);
                }
                else if (result.Contains("{}"))
                {
                    int idx = result.IndexOf("{}");
                    result = result.Remove(idx, 2).Insert(idx, paramMatches[i].Value);
                }
            }
            return result;
        }

        private static void RecordInGameText(string contextPath, string text)
        {
            string cleanText = text.Trim();
            if (cleanText.Length < 1) return;
            // Bỏ qua nếu chỉ là số đơn thuần (ví dụ số frame, số đếm không có chữ)
            if (long.TryParse(cleanText, out _)) return;

            string key = $"{contextPath}::{cleanText}";
            if (_dumpedStrings.Add(key))
            {
                try
                {
                    if (!File.Exists(_dumpFilePath))
                    {
                        File.WriteAllText(_dumpFilePath, "Key_ID,Context_Path,Original_Text,Vietnamese_Translation,Status,Notes\n", Encoding.UTF8);
                    }
                    string keyId = "0x" + Math.Abs(key.GetHashCode()).ToString("X8");
                    string escapedContext = contextPath.Contains(",") ? $"\"{contextPath}\"" : contextPath;
                    string escapedText = cleanText.Contains(",") || cleanText.Contains("\n") || cleanText.Contains("\"") ? $"\"{cleanText.Replace("\"", "\"\"")}\"" : cleanText;
                    File.AppendAllText(_dumpFilePath, $"{keyId},{escapedContext},{escapedText},,Captured_InGame,Thực tế xuất hiện trên UI Game\n", Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}

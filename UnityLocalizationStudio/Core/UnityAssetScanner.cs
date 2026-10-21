using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class UnityAssetScanner
    {
        private static readonly HashSet<string> TargetFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "Title", "Description", "FlavourText", "RangeDescription", "Text", "Subtitle", "Tooltip", "Label"
        };

        private static readonly byte[] OdinKText = new byte[] { 0x27, 0x01, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x6B, 0x00, 0x01 };
        private static readonly byte[] OdinVText = new byte[] { 0x27, 0x01, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x76, 0x00, 0x01 };
        private static readonly byte[] OdinKInt32 = new byte[] { 0x0B, 0x01, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x6B, 0x00 };
        private static readonly byte[] OdinKInt64 = new byte[] { 0x1D, 0x01, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x6B, 0x00 };
        private static readonly byte[] OdinFieldPrefix = new byte[] { 0x27, 0x01 };

        private static readonly Regex TokenRegex = new Regex(@"(\$[^$\r\n]+\$|\{[^{}\r\n]+\}|<[^>]*>|\\[nrt])", RegexOptions.Compiled);
        private static readonly Regex PureMarkupRegex = new Regex(@"^(?:\s|<[^>]+>|\$[^$]+\$|\{[^}]+\}|[+\-\d%]+)+$", RegexOptions.Compiled);

        public static readonly HashSet<string> LanguageNameTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "日本語", "한국어", "РУССКИЙ", "简体中文", "繁体中文"
        };

        public class VerifiedAssetMeta
        {
            public string Id { get; set; } = "";
            public string AssetFile { get; set; } = "";
            public string AssetPathId { get; set; } = "";
            public string Origin { get; set; } = "";
            public string Field { get; set; } = "";
            public string Key { get; set; } = "";
            public string ReferencePathId { get; set; } = "";
            public string JsonPointer { get; set; } = "";
            public string OdinValueOffset { get; set; } = "";
            public string OdinUtf16Units { get; set; } = "";
            public string EnglishEscaped { get; set; } = "";
            public string VietnameseEscaped { get; set; } = "";
            public string Placeholders { get; set; } = "";
            public string UlsId { get; set; } = "";
        }

        private static Dictionary<string, VerifiedAssetMeta>? _verifiedLookup;
        private static readonly object _lookupLock = new();

        public static Dictionary<string, VerifiedAssetMeta> GetVerifiedLookup()
        {
            if (_verifiedLookup != null) return _verifiedLookup;
            lock (_lookupLock)
            {
                if (_verifiedLookup != null) return _verifiedLookup;
                _verifiedLookup = LoadVerifiedLookup();
                return _verifiedLookup;
            }
        }

        private static string CleanEscapedQuotes(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\\"", "\"").Replace("\\'", "'");
        }

        private static Dictionary<string, VerifiedAssetMeta> LoadVerifiedLookup()
        {
            var dict = new Dictionary<string, VerifiedAssetMeta>(StringComparer.OrdinalIgnoreCase);
            string[] possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PlateUp_EN_2243_Asset_Verified.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "UnityLocalizationStudio", "Assets", "PlateUp_EN_2243_Asset_Verified.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "PlateUp_EN_2243_Asset_Verified.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "Output", "Audit_Verification", "PlateUp_EN_2243_Asset_Verified.csv")
            };

            string? foundPath = possiblePaths.FirstOrDefault(File.Exists);
            if (foundPath == null) return dict;

            var rows = CsvHandler.ParseCsvContent(File.ReadAllText(foundPath, Encoding.UTF8));
            if (rows.Count <= 1) return dict;

            var header = rows[0];
            int ulsIdIdx = header.FindIndex(h => h.Trim().Equals("ULS_ID", StringComparison.OrdinalIgnoreCase));
            int idIdx = header.FindIndex(h => h.Trim().Equals("ID", StringComparison.OrdinalIgnoreCase) || h.Trim().Equals("\ufeffID", StringComparison.OrdinalIgnoreCase));
            int fileIdx = header.FindIndex(h => h.Trim().Equals("Asset_File", StringComparison.OrdinalIgnoreCase));
            int pathIdIdx = header.FindIndex(h => h.Trim().Equals("Asset_PathID", StringComparison.OrdinalIgnoreCase));
            int originIdx = header.FindIndex(h => h.Trim().Equals("Origin", StringComparison.OrdinalIgnoreCase));
            int fieldIdx = header.FindIndex(h => h.Trim().Equals("Field", StringComparison.OrdinalIgnoreCase));
            int keyIdx = header.FindIndex(h => h.Trim().Equals("Key", StringComparison.OrdinalIgnoreCase));
            int refPathIdIdx = header.FindIndex(h => h.Trim().Equals("Reference_PathID", StringComparison.OrdinalIgnoreCase));
            int jsonPtrIdx = header.FindIndex(h => h.Trim().Equals("JSON_Pointer", StringComparison.OrdinalIgnoreCase));
            int offsetIdx = header.FindIndex(h => h.Trim().Equals("Odin_Value_Offset", StringComparison.OrdinalIgnoreCase));
            int unitsIdx = header.FindIndex(h => h.Trim().Equals("Odin_UTF16_Units", StringComparison.OrdinalIgnoreCase));
            int enIdx = header.FindIndex(h => h.Trim().Equals("English_Escaped", StringComparison.OrdinalIgnoreCase));
            int vnIdx = header.FindIndex(h => h.Trim().Equals("Vietnamese_Escaped", StringComparison.OrdinalIgnoreCase));
            int plIdx = header.FindIndex(h => h.Trim().Equals("Placeholders", StringComparison.OrdinalIgnoreCase));

            for (int i = 1; i < rows.Count; i++)
            {
                var r = rows[i];
                string ulsId = ulsIdIdx >= 0 && ulsIdIdx < r.Count ? r[ulsIdIdx].Trim() : "";
                string id = idIdx >= 0 && idIdx < r.Count ? r[idIdx].Trim() : "";
                var meta = new VerifiedAssetMeta
                {
                    Id = id,
                    AssetFile = fileIdx >= 0 && fileIdx < r.Count ? r[fileIdx].Trim() : "",
                    AssetPathId = pathIdIdx >= 0 && pathIdIdx < r.Count ? r[pathIdIdx].Trim() : "",
                    Origin = originIdx >= 0 && originIdx < r.Count ? r[originIdx].Trim() : "",
                    Field = fieldIdx >= 0 && fieldIdx < r.Count ? r[fieldIdx].Trim() : "",
                    Key = keyIdx >= 0 && keyIdx < r.Count ? r[keyIdx].Trim() : "",
                    ReferencePathId = refPathIdIdx >= 0 && refPathIdIdx < r.Count ? r[refPathIdIdx].Trim() : "",
                    JsonPointer = jsonPtrIdx >= 0 && jsonPtrIdx < r.Count ? r[jsonPtrIdx].Trim() : "",
                    OdinValueOffset = offsetIdx >= 0 && offsetIdx < r.Count ? r[offsetIdx].Trim() : "",
                    OdinUtf16Units = unitsIdx >= 0 && unitsIdx < r.Count ? r[unitsIdx].Trim() : "",
                    EnglishEscaped = enIdx >= 0 && enIdx < r.Count ? CleanEscapedQuotes(r[enIdx]) : "",
                    VietnameseEscaped = vnIdx >= 0 && vnIdx < r.Count ? CleanEscapedQuotes(r[vnIdx]) : "",
                    Placeholders = plIdx >= 0 && plIdx < r.Count ? CleanEscapedQuotes(r[plIdx]) : "",
                    UlsId = ulsId
                };

                if (!string.IsNullOrEmpty(ulsId)) dict[ulsId] = meta;
                if (!string.IsNullOrEmpty(id) && !dict.ContainsKey(id)) dict[id] = meta;
            }

            return dict;
        }

        public static List<TranslationEntry> GetVerifiedTranslationEntries()
        {
            var lookup = GetVerifiedLookup();
            var list = new List<TranslationEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in lookup)
            {
                var meta = kvp.Value;
                string uniqueKey = !string.IsNullOrEmpty(meta.UlsId) ? meta.UlsId : meta.Id;
                if (!seen.Add(uniqueKey)) continue;

                var entry = new TranslationEntry
                {
                    Id = uniqueKey,
                    Language = "EN",
                    SourceKind = SourceKind.UnityAsset,
                    AssetFile = meta.AssetFile,
                    AssetPathId = meta.AssetPathId,
                    Origin = meta.Origin,
                    Field = meta.Field,
                    Key = meta.Key,
                    ReferencePathId = meta.ReferencePathId,
                    JsonPointer = meta.JsonPointer,
                    OdinValueOffset = meta.OdinValueOffset,
                    OdinUtf16Units = meta.OdinUtf16Units,
                    ContextPath = !string.IsNullOrEmpty(meta.JsonPointer) ? $"{meta.AssetFile}:{meta.JsonPointer}" : $"{meta.Origin}:{meta.Field}",
                    OriginalText = meta.EnglishEscaped,
                    VietnameseTranslation = "", // Đảm bảo trống tuyệt đối cho quy trình dịch sạch của người dùng
                    Placeholders = meta.Placeholders,
                    Classification = StringClassification.GameText,
                    Status = "Translate",
                    Confidence = "High",
                    Notes = $"[ASSET VERIFIED] {meta.Origin} - {meta.Field}" + (!string.IsNullOrEmpty(meta.Key) ? $" [{meta.Key}]" : "")
                };
                list.Add(entry);
            }
            return list;
        }

        public static void EnrichWithVerifiedMetadata(List<TranslationEntry> entries)
        {
            var lookup = GetVerifiedLookup();
            if (lookup.Count == 0) return;

            foreach (var e in entries)
            {
                if (e.Language != "EN") continue;
                if (lookup.TryGetValue(e.Id, out var meta))
                {
                    if (!string.IsNullOrEmpty(meta.Field)) e.Field = meta.Field;
                    if (!string.IsNullOrEmpty(meta.Key)) e.Key = meta.Key;
                    if (!string.IsNullOrEmpty(meta.JsonPointer)) e.JsonPointer = meta.JsonPointer;
                    if (!string.IsNullOrEmpty(meta.ReferencePathId)) e.ReferencePathId = meta.ReferencePathId;
                    if (!string.IsNullOrEmpty(meta.Origin)) e.Origin = meta.Origin;
                    if (!string.IsNullOrEmpty(meta.AssetFile)) e.AssetFile = meta.AssetFile;
                    if (!string.IsNullOrEmpty(meta.AssetPathId)) e.AssetPathId = meta.AssetPathId;
                    if (!string.IsNullOrEmpty(meta.OdinValueOffset)) e.OdinValueOffset = meta.OdinValueOffset;
                    if (!string.IsNullOrEmpty(meta.OdinUtf16Units)) e.OdinUtf16Units = meta.OdinUtf16Units;
                    if (!string.IsNullOrEmpty(meta.Placeholders)) e.Placeholders = meta.Placeholders;
                    // TUYỆT ĐỐI KHÔNG GÁN BẢN DỊCH MẪU: VietnameseTranslation luôn để trống để người dùng/biên dịch viên tự dịch
                    e.Classification = StringClassification.GameText;
                    e.Status = "Translate";
                    e.Notes = $"[ASSET VERIFIED] {e.Origin} - {e.Field}" + (!string.IsNullOrEmpty(e.Key) ? $" [{e.Key}]" : "");
                }
            }
        }

        /// <summary>
        /// Quét TRỰC TIẾP từ các tệp nhị phân .assets trong thư mục game gốc (PlateUp_Data/*.assets)
        /// Hoàn toàn tự lực, không phụ thuộc vào bất kỳ tệp JSON dump hay CSV tham khảo nào.
        /// </summary>
        public static List<TranslationEntry> ScanDirectBinaryAssets(string dataDir, Action<string>? logCallback = null)
        {
            var entries = new List<TranslationEntry>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(dataDir)) return entries;

            // Ưu tiên các tệp sharedassets*.assets (chứa dữ liệu MonoBehaviour và Language Info của game)
            // Loại trừ tuyệt đối tệp streaming .resS, .resource, shader engine sharedassets0.assets và cấu hình globalgamemanagers.assets
            var assetFiles = Directory.GetFiles(dataDir, "sharedassets*.assets", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".resS", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".resource", StringComparison.OrdinalIgnoreCase) &&
                            !Path.GetFileName(f).Equals("sharedassets0.assets", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => new FileInfo(f).Length)
                .ToList();

            if (assetFiles.Count == 0)
            {
                assetFiles = Directory.GetFiles(dataDir, "*.assets", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith(".resS", StringComparison.OrdinalIgnoreCase) &&
                                !f.EndsWith(".resource", StringComparison.OrdinalIgnoreCase) &&
                                !f.EndsWith(".rar", StringComparison.OrdinalIgnoreCase) &&
                                !f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).Equals("sharedassets0.assets", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(f).Equals("globalgamemanagers.assets", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .ToList();
            }

            foreach (var assetPath in assetFiles)
            {
                string fileName = Path.GetFileName(assetPath);
                long fileLength = new FileInfo(assetPath).Length;
                logCallback?.Invoke($"Đang quét tệp nhị phân: {fileName} ({fileLength / 1024 / 1024} MB)...");

                try
                {
                    // Đọc tệp nhị phân để bóc tách các vùng chuỗi Odin & Text
                    byte[] bytes = File.ReadAllBytes(assetPath);
                    bool handled = ScanInfoContainersFromBytes(bytes, fileName, entries, seenKeys, logCallback);
                    if (!handled)
                    {
                        ScanOdinPayloadsFromBytes(bytes, fileName, entries, seenKeys);
                        if (fileName.StartsWith("sharedassets", StringComparison.OrdinalIgnoreCase) && !fileName.Equals("sharedassets0.assets", StringComparison.OrdinalIgnoreCase))
                        {
                            ScanStandardUnityStringsFromBytes(bytes, fileName, entries, seenKeys);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"  [Cảnh báo] Lỗi đọc tệp {fileName}: {ex.Message}");
                }
            }

            EnrichWithVerifiedMetadata(entries);
            logCallback?.Invoke($"Quét nhị phân hoàn tất: {entries.Count} chuỗi văn bản gameplay từ Assets.");
            return entries;
        }

        private static void ScanOdinPayloadsFromBytes(byte[] b, string assetFileName, List<TranslationEntry> list, HashSet<string> seenKeys)
        {
            var langMap = LanguageClassifier.BuildAssetLanguageMap(b);

            // 1. Quét Odin Dictionary Values (ODIN_V_TEXT)
            int offset = 0;
            while (offset <= b.Length - OdinVText.Length)
            {
                int idx = FindBytes(b, OdinVText, offset);
                if (idx < 0) break;

                var decoded = DecodeOdinString(b, idx + OdinVText.Length);
                if (decoded == null)
                {
                    offset = idx + 1;
                    continue;
                }

                var (val, valStart, end) = decoded.Value;
                offset = end;

                string cleanText = val.Trim();
                if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 2) continue;

                var (key, _, kind) = FindOdinKey(b, idx);
                string locationKey = $"{assetFileName}:OdinDict:{valStart}";
                string id = ComputeStableId(assetFileName, "OdinDictionary", valStart.ToString());

                if (seenKeys.Add(locationKey))
                {
                    string fallbackLang = LanguageClassifier.ResolveLanguageAtOffset(valStart, langMap);
                    string finalLang = LanguageClassifier.DetectLanguageFromText(cleanText, fallbackLang);

                    var entry = new TranslationEntry
                    {
                        Id = id,
                        Language = finalLang,
                        SourceKind = SourceKind.UnityAsset,
                        AssetFile = assetFileName,
                        Origin = "OdinDictionary",
                        Field = "Text",
                        Key = key,
                        OdinValueOffset = valStart.ToString(),
                        OdinUtf16Units = ((end - valStart) / 2).ToString(),
                        ContextPath = $"{assetFileName}:OdinDictionary" + (!string.IsNullOrEmpty(key) ? $"/[{key}]" : ""),
                        OriginalText = cleanText,
                        VietnameseTranslation = "", // ĐỂ TRỐNG ĐỂ NGƯỜI DÙNG TỰ DỊCH
                        Placeholders = ExtractPlaceholders(cleanText),
                        Status = "Translate",
                        Notes = $"[ASSET GỐC] {assetFileName} (Offset {valStart}, Lang={finalLang})" + (!string.IsNullOrEmpty(key) ? $" Key={key}" : "")
                    };
                    ClassifyAssetEntry(entry);
                    list.Add(entry);
                }
            }

            // 2. Quét Odin Struct Fields (Name, Title, Description, FlavourText...)
            offset = 0;
            while (offset <= b.Length - 12)
            {
                int idx = FindBytes(b, OdinFieldPrefix, offset);
                if (idx < 0) break;

                offset = idx + 2;
                var nameDec = DecodeOdinString(b, idx + 2);
                if (nameDec == null) continue;

                var (fieldName, _, nEnd) = nameDec.Value;
                if (!TargetFields.Contains(fieldName) || nEnd >= b.Length || b[nEnd] != 0x01)
                    continue;

                var valDec = DecodeOdinString(b, nEnd + 1);
                if (valDec == null) continue;

                var (val, valStart, vEnd) = valDec.Value;
                offset = vEnd;

                string cleanText = val.Trim();
                if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 2) continue;

                string locationKey = $"{assetFileName}:OdinStruct:{valStart}:{fieldName}";
                string id = ComputeStableId(assetFileName, "OdinStruct", $"{valStart}_{fieldName}");

                if (seenKeys.Add(locationKey))
                {
                    string finalLang = LanguageClassifier.DetectLanguageFromText(cleanText, "EN");

                    var entry = new TranslationEntry
                    {
                        Id = id,
                        Language = finalLang,
                        SourceKind = SourceKind.UnityAsset,
                        AssetFile = assetFileName,
                        Origin = "OdinStruct",
                        Field = fieldName,
                        Key = "",
                        OdinValueOffset = valStart.ToString(),
                        OdinUtf16Units = ((vEnd - valStart) / 2).ToString(),
                        ContextPath = $"{assetFileName}:OdinStruct/{fieldName}",
                        OriginalText = cleanText,
                        VietnameseTranslation = "", // ĐỂ TRỐNG ĐỂ NGƯỜI DÙNG TỰ DỊCH
                        Placeholders = ExtractPlaceholders(cleanText),
                        Status = "Translate",
                        Notes = $"[ASSET GỐC] Struct {fieldName} (Offset {valStart})"
                    };
                    ClassifyAssetEntry(entry);
                    list.Add(entry);
                }
            }
        }

        private static (string val, int start, int end)? DecodeOdinString(byte[] b, int startLen)
        {
            if (startLen < 0 || startLen + 4 > b.Length) return null;
            int units = BitConverter.ToInt32(b, startLen);
            if (units <= 0 || units > 100_000) return null;

            int payloadStart = startLen + 4;
            int payloadEnd = payloadStart + units * 2;
            if (payloadEnd > b.Length) return null;

            try
            {
                string s = Encoding.Unicode.GetString(b, payloadStart, units * 2);
                return (s, payloadStart, payloadEnd);
            }
            catch
            {
                return null;
            }
        }

        private static (string key, string refIndex, string kind) FindOdinKey(byte[] b, int valTokenStart)
        {
            int kStart = RFindBytes(b, OdinKText, Math.Max(0, valTokenStart - 200_020), valTokenStart);
            if (kStart >= 0)
            {
                var d = DecodeOdinString(b, kStart + OdinKText.Length);
                if (d != null && (d.Value.end == valTokenStart || d.Value.end == valTokenStart - 6))
                {
                    return (d.Value.val, "", "dictionary-string");
                }
            }

            int start32 = valTokenStart - OdinKInt32.Length - 4;
            if (start32 >= 0 && MatchBytes(b, OdinKInt32, start32))
            {
                int index = BitConverter.ToInt32(b, start32 + OdinKInt32.Length);
                return ($"#{index}", index.ToString(), "dictionary-unity-ref");
            }

            int kIntPos = RFindBytes(b, new byte[] { 0x24, 0x00, 0x6B, 0x00 }, Math.Max(0, valTokenStart - 40), valTokenStart);
            if (kIntPos >= 0 && kIntPos + 9 <= valTokenStart)
            {
                int index = BitConverter.ToInt32(b, kIntPos + 5);
                if (index >= 0 && index < 100000)
                {
                    return ($"#{index}", index.ToString(), "dictionary-unity-ref");
                }
            }

            int start64 = valTokenStart - OdinKInt64.Length - 8;
            if (start64 >= 0 && MatchBytes(b, OdinKInt64, start64))
            {
                long index = BitConverter.ToInt64(b, start64 + OdinKInt64.Length);
                return ($"enum:{index}", index.ToString(), "dictionary-enum");
            }

            return ("", "", "unmatched");
        }

        public static void ClassifyAssetEntry(TranslationEntry entry)
        {
            string s = entry.OriginalText;
            if (PureMarkupRegex.IsMatch(s))
            {
                entry.Classification = StringClassification.Protected;
                entry.Status = "Protected";
                entry.Notes = "Markup / substitution only — do not translate";
                entry.Confidence = "High";
                return;
            }

            if (entry.Field == "Icons" || entry.AssetFile.StartsWith("GameData", StringComparison.OrdinalIgnoreCase))
            {
                entry.Classification = StringClassification.Protected;
                entry.Status = "Protected";
                entry.Notes = "GameData/icon/substitution text";
                entry.Confidence = "High";
                return;
            }

            if (s.Trim().StartsWith("$") && Regex.IsMatch(s.Trim(), @"^\$[^$]+\$$"))
            {
                entry.Classification = StringClassification.Protected;
                entry.Status = "Protected";
                entry.Notes = "Substitution token";
                entry.Confidence = "High";
                return;
            }

            if (!s.Any(char.IsLetter))
            {
                entry.Classification = StringClassification.Protected;
                entry.Status = "Protected";
                entry.Notes = "No Latin language text";
                entry.Confidence = "High";
                return;
            }

            entry.Classification = StringClassification.GameText;
            entry.Status = "Pending";
            entry.Confidence = "High";
        }

        public static string ComputeStableId(string asset, string origin, string location)
        {
            byte[] input = Encoding.UTF8.GetBytes(asset + "\0" + origin + "\0" + location);
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(input);
            return Convert.ToHexString(hash, 0, 8).ToUpper();
        }

        public static string ExtractPlaceholders(string text)
        {
            var matches = TokenRegex.Matches(text);
            if (matches.Count == 0) return "";
            var tokens = new SortedSet<string>();
            foreach (Match m in matches)
            {
                tokens.Add(m.Value);
            }
            return string.Join("|", tokens);
        }

        private static int FindBytes(byte[] source, byte[] pattern, int start)
        {
            for (int i = start; i <= source.Length - pattern.Length; i++)
            {
                if (MatchBytes(source, pattern, i)) return i;
            }
            return -1;
        }

        private static int RFindBytes(byte[] source, byte[] pattern, int start, int end)
        {
            int max = Math.Min(source.Length - pattern.Length, end - pattern.Length);
            for (int i = max; i >= start; i--)
            {
                if (MatchBytes(source, pattern, i)) return i;
            }
            return -1;
        }

        private static bool MatchBytes(byte[] source, byte[] pattern, int at)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (source[at + i] != pattern[i]) return false;
            }
            return true;
        }

        private static readonly byte[] TagOdinDictValue = new byte[] { 0x24, 0x00, 0x76, 0x00, 0x01 };

        private static int FindBytes(byte[] source, byte[] pattern, int start, int end)
        {
            int max = Math.Min(source.Length, end) - pattern.Length;
            for (int i = start; i <= max; i++)
            {
                if (MatchBytes(source, pattern, i)) return i;
            }
            return -1;
        }

        private static bool ScanInfoContainersFromBytes(byte[] b, string assetFileName, List<TranslationEntry> list, HashSet<string> seenKeys, Action<string>? logCallback = null)
        {
            var objects = UnitySerializedFileReader.ReadObjects(b);
            if (objects.Count == 0) return false;

            bool hasInfoContainers = false;
            foreach (var o in objects)
            {
                if (o.ClassId == 114 && o.ByteSize >= 36 && o.ByteStart + o.ByteSize <= b.Length)
                {
                    int nameLen = BitConverter.ToInt32(b, (int)o.ByteStart + 28);
                    if (nameLen > 0 && nameLen < 64 && o.ByteStart + 32 + nameLen <= b.Length)
                    {
                        string name = Encoding.UTF8.GetString(b, (int)o.ByteStart + 32, nameLen);
                        if (Regex.IsMatch(name, @"^[A-Za-z]+\s*-\s*Info$"))
                        {
                            hasInfoContainers = true;
                            break;
                        }
                    }
                }
            }

            if (!hasInfoContainers) return false;

            logCallback?.Invoke($"Phát hiện kiến trúc Language Info Containers trong {assetFileName}. Đang bóc tách chuẩn xác...");

            foreach (var o in objects)
            {
                if (o.ClassId != 114 || o.ByteSize < 36 || o.ByteStart + o.ByteSize > b.Length)
                    continue;

                int start = (int)o.ByteStart;
                int size = (int)o.ByteSize;
                int end = start + size;

                int nameLen = BitConverter.ToInt32(b, start + 28);
                if (nameLen <= 0 || nameLen >= 64 || start + 32 + nameLen > end)
                    continue;

                string name = Encoding.UTF8.GetString(b, start + 32, nameLen);
                var match = Regex.Match(name, @"^([A-Za-z]+)\s*-\s*Info$");
                if (!match.Success)
                    continue;

                string rawLang = match.Groups[1].Value;
                if (rawLang.Equals("BlankText", StringComparison.OrdinalIgnoreCase))
                    continue;

                // CHỈ BÓC TÁCH CONTAINER NGÔN NGỮ NGUỒN CỦA GAME (English - Info)
                // Các container ngôn ngữ khác (Turkish, Russian, Japanese, German...) không thuộc tập ngôn ngữ nguồn dịch sang tiếng Việt
                if (!rawLang.Equals("English", StringComparison.OrdinalIgnoreCase))
                    continue;

                string langCode = LanguageClassifier.NormalizeLanguageCode(rawLang);
                bool isOdin = FindBytes(b, TagOdinDictValue, start, end) >= 0 || FindBytes(b, OdinFieldPrefix, start, end) >= 0;

                // 1. Standard Unity serialized strings (Chỉ quét khi KHÔNG PHẢI container Odin)
                if (!isOdin)
                {
                    int pos = start + 32 + nameLen;
                    pos = (pos + 3) & ~3;
                    while (pos <= end - 4)
                    {
                        int slen = BitConverter.ToInt32(b, pos);
                        if (slen >= 2 && slen <= Math.Min(2000, end - pos - 4))
                        {
                            try
                            {
                                string s = Encoding.UTF8.GetString(b, pos + 4, slen);
                                if (s.Any(char.IsLetter) && !s.Any(c => c < 32 && c != '\r' && c != '\n' && c != '\t'))
                                {
                                    string clean = s.Trim();
                                    if (clean.Length >= 2 && !clean.StartsWith("Assembly-") && !Regex.IsMatch(clean, @"^[a-fA-F0-9-]{32,}$"))
                                    {
                                        string locationKey = $"{assetFileName}:UnityString:{o.PathId}:{clean}";
                                        if (seenKeys.Add(locationKey))
                                        {
                                            var entry = new TranslationEntry
                                            {
                                                Id = ComputeStableId(assetFileName, "UnitySerializedString", $"{o.PathId}_{clean}"),
                                                Language = langCode,
                                                SourceKind = SourceKind.UnityAsset,
                                                AssetFile = assetFileName,
                                                AssetPathId = o.PathId.ToString(),
                                                Origin = "UnitySerializedString",
                                                Field = "Text",
                                                OdinValueOffset = pos.ToString(),
                                                ContextPath = $"{assetFileName}:UnityString/{TruncateKey(clean)}",
                                                OriginalText = clean,
                                                VietnameseTranslation = "",
                                                Placeholders = ExtractPlaceholders(clean),
                                                Status = "Translate",
                                                Notes = $"[ASSET GỐC] Container {name} (PathID {o.PathId})"
                                            };
                                            ClassifyAssetEntry(entry);
                                            list.Add(entry);
                                        }

                                        int aligned = (slen + 3) & ~3;
                                        pos += 4 + aligned;
                                        continue;
                                    }
                                }
                            }
                            catch { }
                        }
                        pos++;
                    }
                }

                // 2. Odin Dictionary strings ($v\x01)
                int odinPos = start;
                while (odinPos <= end - TagOdinDictValue.Length - 4)
                {
                    int idx = FindBytes(b, TagOdinDictValue, odinPos, end);
                    if (idx < 0) break;

                    int pLen = idx + TagOdinDictValue.Length;
                    if (pLen + 4 <= end)
                    {
                        int units = BitConverter.ToInt32(b, pLen);
                        if (units >= 1 && units <= 2000 && pLen + 4 + units * 2 <= end)
                        {
                            try
                            {
                                string s = Encoding.Unicode.GetString(b, pLen + 4, units * 2).Trim();
                                if (s.Length >= 2 && (s.Any(char.IsLetter) || LanguageNameTokens.Contains(s)))
                                {
                                    var (key, _, _) = FindOdinKey(b, idx);
                                    string locationKey = $"{assetFileName}:OdinDict:{o.PathId}:{idx}";
                                    if (seenKeys.Add(locationKey))
                                    {
                                        var entry = new TranslationEntry
                                        {
                                            Id = ComputeStableId(assetFileName, "OdinDictionary", $"{o.PathId}_{idx}"),
                                            Language = langCode,
                                            SourceKind = SourceKind.UnityAsset,
                                            AssetFile = assetFileName,
                                            AssetPathId = o.PathId.ToString(),
                                            Origin = "OdinDictionary",
                                            Field = "Text",
                                            Key = key,
                                            OdinValueOffset = (pLen + 4).ToString(),
                                            OdinUtf16Units = units.ToString(),
                                            ContextPath = $"{assetFileName}:OdinDictionary" + (!string.IsNullOrEmpty(key) ? $"/[{key}]" : ""),
                                            OriginalText = s,
                                            VietnameseTranslation = "",
                                            Placeholders = ExtractPlaceholders(s),
                                            Status = "Translate",
                                            Notes = $"[ASSET GỐC] Container {name} (PathID {o.PathId})" + (!string.IsNullOrEmpty(key) ? $" Key={key}" : "")
                                        };

                                        if (LanguageNameTokens.Contains(s))
                                        {
                                            entry.Classification = StringClassification.Protected;
                                            entry.Status = "Protected";
                                            entry.Confidence = "High";
                                            entry.Notes = "[MENU TÊN NGÔN NGỮ] Giữ nguyên tên ngôn ngữ hiển thị (KEEP_ORIGINAL)";
                                        }
                                        else
                                        {
                                            ClassifyAssetEntry(entry);
                                        }
                                        list.Add(entry);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    odinPos = idx + TagOdinDictValue.Length;
                }

                // 3. Odin Struct fields (27 01 <len_u16> <name_u16> \x01 <len_u16> <val_u16>)
                int fldPos = start;
                while (fldPos <= end - 12)
                {
                    int idx = FindBytes(b, OdinFieldPrefix, fldPos, end);
                    if (idx < 0) break;

                    int pFld = idx + 2;
                    if (pFld + 4 <= end)
                    {
                        int fldUnits = BitConverter.ToInt32(b, pFld);
                        if (fldUnits >= 1 && fldUnits <= 50 && pFld + 4 + fldUnits * 2 + 5 <= end)
                        {
                            try
                            {
                                string fldName = Encoding.Unicode.GetString(b, pFld + 4, fldUnits * 2);
                                if (TargetFields.Contains(fldName))
                                {
                                    int pFlag = pFld + 4 + fldUnits * 2;
                                    if (b[pFlag] == 0x01)
                                    {
                                        int valUnits = BitConverter.ToInt32(b, pFlag + 1);
                                        if (valUnits >= 1 && valUnits <= 2000 && pFlag + 5 + valUnits * 2 <= end)
                                        {
                                            string valS = Encoding.Unicode.GetString(b, pFlag + 5, valUnits * 2).Trim();
                                            if (valS.Length >= 2 && valS.Any(char.IsLetter))
                                            {
                                                string locationKey = $"{assetFileName}:OdinStruct:{o.PathId}:{idx}:{fldName}";
                                                if (seenKeys.Add(locationKey))
                                                {
                                                    var entry = new TranslationEntry
                                                    {
                                                        Id = ComputeStableId(assetFileName, "OdinStruct", $"{o.PathId}_{idx}_{fldName}"),
                                                        Language = langCode,
                                                        SourceKind = SourceKind.UnityAsset,
                                                        AssetFile = assetFileName,
                                                        AssetPathId = o.PathId.ToString(),
                                                        Origin = "OdinStruct",
                                                        Field = fldName,
                                                        OdinValueOffset = (pFlag + 5).ToString(),
                                                        OdinUtf16Units = valUnits.ToString(),
                                                        ContextPath = $"{assetFileName}:OdinStruct/{fldName}",
                                                        OriginalText = valS,
                                                        VietnameseTranslation = "",
                                                        Placeholders = ExtractPlaceholders(valS),
                                                        Status = "Translate",
                                                        Notes = $"[ASSET GỐC] Container {name} Struct {fldName} (PathID {o.PathId})"
                                                    };
                                                    ClassifyAssetEntry(entry);
                                                    list.Add(entry);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    fldPos = idx + 2;
                }
            }

            return true;
        }

        private static void ScanStandardUnityStringsFromBytes(byte[] b, string assetFileName, List<TranslationEntry> list, HashSet<string> seenKeys)
        {
            // Quét các chuỗi Unity serialized MonoBehaviour chuẩn (int32 length + UTF-8 payload + 4-byte alignment)
            // Trong sharedassets1.assets, vùng MonoBehaviour/ScriptableObject của game bắt đầu từ offset ~100MB
            int offset = assetFileName.Equals("sharedassets1.assets", StringComparison.OrdinalIgnoreCase) ? 100_000_000 : 0;
            while (offset <= b.Length - 8)
            {
                int len = BitConverter.ToInt32(b, offset);
                if (len >= 3 && len <= 2000 && offset + 4 + len <= b.Length)
                {
                    byte first = b[offset + 4];
                    byte last = b[offset + 4 + len - 1];

                    if (first >= 32 && first <= 126 && last >= 32 && last <= 126)
                    {
                        bool valid = true;
                        int letterCount = 0;

                        for (int i = 0; i < len; i++)
                        {
                            byte c = b[offset + 4 + i];
                            if (c < 32 && c != '\n' && c != '\r' && c != '\t')
                            {
                                valid = false;
                                break;
                            }
                            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) letterCount++;
                        }

                        if (valid && letterCount >= 2)
                        {
                            string s = Encoding.UTF8.GetString(b, offset + 4, len).Trim();

                            if (IsValidUnityAssetText(s))
                            {
                                string locationKey = $"{assetFileName}:UnityString:{s}";
                                if (seenKeys.Add(locationKey))
                                {
                                    string id = ComputeStableId(assetFileName, "UnityString", s);
                                    string lang = LanguageClassifier.DetectLanguageFromText(s, "EN");

                                    var entry = new TranslationEntry
                                    {
                                        Id = id,
                                        Language = lang,
                                        SourceKind = SourceKind.UnityAsset,
                                        AssetFile = assetFileName,
                                        Origin = "UnitySerializedString",
                                        Field = "Text",
                                        OdinValueOffset = offset.ToString(),
                                        ContextPath = $"{assetFileName}:UnityString/{TruncateKey(s)}",
                                        OriginalText = s,
                                        VietnameseTranslation = "",
                                        Placeholders = ExtractPlaceholders(s),
                                        Status = "Translate",
                                        Notes = $"[ASSET GỐC] Unity serialized string (Offset {offset})"
                                    };
                                    ClassifyAssetEntry(entry);
                                    list.Add(entry);
                                }

                                int alignedLen = (len + 3) & ~3;
                                offset += 4 + alignedLen;
                                continue;
                            }
                        }
                    }
                }
                offset += 4;
            }
        }

        private static bool IsValidUnityAssetText(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length < 2) return false;
            if (FileExtensionRegex.IsMatch(s) || GuidRegex.IsMatch(s) || HexColorRegex.IsMatch(s)) return false;
            if (s.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("Hidden/", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("ProjectSettings/", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                return false;

            if (s.StartsWith("m_") || s.StartsWith("_") || s.StartsWith("k__")) return false;
            if (s.StartsWith("UnityEngine.") || s.StartsWith("System.") || s.StartsWith("Kitchen.")) return false;
            if (!s.Any(char.IsLetter)) return false;

            return true;
        }

        private static string TruncateKey(string s)
        {
            if (s.Length <= 30) return s.Replace('/', '_').Replace('\\', '_');
            return s.Substring(0, 30).Replace('/', '_').Replace('\\', '_');
        }

        private static readonly Regex FileExtensionRegex = new Regex(@"\.(png|jpg|jpeg|wav|mp3|ogg|asset|prefab|mat|shader|unity|dll|json|txt|xml|bytes|hlsl|cginc)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex GuidRegex = new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        private static readonly Regex HexColorRegex = new Regex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);
    }
}

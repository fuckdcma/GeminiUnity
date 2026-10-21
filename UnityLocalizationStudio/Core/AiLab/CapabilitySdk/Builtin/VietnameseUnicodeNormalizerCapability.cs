using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin
{
    public class VietnameseUnicodeNormalizerCapability : ICapability
    {
        public string Id => "CAP_VIETNAMESE_UNICODE_NORMALIZER";
        public string Name => "Vietnamese Unicode Normalizer & Tone Rectifier";
        public string SemanticVersion => "1.2.0";

        private readonly CapabilityManifest _manifest;

        public CapabilityManifest Manifest => _manifest;

        public VietnameseUnicodeNormalizerCapability()
        {
            _manifest = new CapabilityManifest
            {
                Id = Id,
                Name = Name,
                SemanticVersion = SemanticVersion,
                Description = "Chuẩn hóa bảng mã Unicode tiếng Việt (NFC/NFD), xử lý quy tắc dấu thanh, phát hiện ký tự hỏng font và so sánh chuỗi.",
                Author = "Antigravity AI Lab",
                Category = "Linguistic_Typography",
                InputContractJson = @"{
                    ""action"": ""NORMALIZE|REMOVE_DIACRITICS|FIX_DIACRITICS|COMPARE|ANALYZE_ANOMALIES|ALL"",
                    ""text"": ""string"",
                    ""textA"": ""string"",
                    ""textB"": ""string"",
                    ""targetForm"": ""NFC|NFD"",
                    ""modernStyle"": true,
                    ""ignoreDiacritics"": false,
                    ""ignoreCase"": true
                }",
                OutputContractJson = @"{
                    ""normalizedText"": ""string"",
                    ""unaccentedText"": ""string"",
                    ""hasAnomalies"": false,
                    ""anomalyCount"": 0,
                    ""anomalies"": [""string""],
                    ""comparisonResult"": 0
                }",
                Tags = new List<string> { "Vietnamese", "Unicode", "NFC", "NFD", "ToneMarks", "Localization" },
                Dependencies = new List<string>(),
                ContentHash = "SHA256:VUN_CAP_20261017_V1",
                CreatedAt = DateTime.UtcNow,
                IsVerified = true
            };
        }

        public CapabilityExecutionResult Execute(CapabilityExecutionContext context)
        {
            var sw = Stopwatch.StartNew();
            string action = context.GetParameter<string>("action", "ALL")?.ToUpperInvariant() ?? "ALL";
            string text = context.GetParameter<string>("text", string.Empty) ?? string.Empty;

            var outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                switch (action)
                {
                    case "NORMALIZE":
                    {
                        string formStr = context.GetParameter<string>("targetForm", "NFC")?.ToUpperInvariant() ?? "NFC";
                        NormalizationForm form = formStr == "NFD" ? NormalizationForm.FormD : NormalizationForm.FormC;
                        string normalized = NormalizeUnicode(text, form);
                        outputs["normalizedText"] = normalized;
                        outputs["originalLength"] = text.Length;
                        outputs["normalizedLength"] = normalized.Length;
                        break;
                    }
                    case "REMOVE_DIACRITICS":
                    {
                        string unaccented = RemoveDiacritics(text);
                        outputs["unaccentedText"] = unaccented;
                        break;
                    }
                    case "FIX_DIACRITICS":
                    {
                        bool modernStyle = context.GetParameter<bool>("modernStyle", true);
                        string fixedText = FixDiacriticPlacement(text, modernStyle);
                        outputs["fixedText"] = fixedText;
                        break;
                    }
                    case "COMPARE":
                    {
                        string textA = context.GetParameter<string>("textA", text) ?? string.Empty;
                        string textB = context.GetParameter<string>("textB", string.Empty) ?? string.Empty;
                        bool ignoreDiacritics = context.GetParameter<bool>("ignoreDiacritics", false);
                        bool ignoreCase = context.GetParameter<bool>("ignoreCase", true);

                        int cmp = CompareVietnamese(textA, textB, ignoreDiacritics, ignoreCase);
                        outputs["comparisonResult"] = cmp;
                        outputs["isEqual"] = (cmp == 0);
                        break;
                    }
                    case "ANALYZE_ANOMALIES":
                    {
                        var anomalies = DetectAnomalies(text);
                        outputs["hasAnomalies"] = anomalies.Count > 0;
                        outputs["anomalyCount"] = anomalies.Count;
                        outputs["anomalies"] = anomalies;
                        break;
                    }
                    case "ALL":
                    default:
                    {
                        string nfc = NormalizeUnicode(text, NormalizationForm.FormC);
                        string nfd = NormalizeUnicode(text, NormalizationForm.FormD);
                        string unaccented = RemoveDiacritics(text);
                        string fixedPlacement = FixDiacriticPlacement(nfc, true);
                        var anomalies = DetectAnomalies(text);

                        outputs["nfc"] = nfc;
                        outputs["nfd"] = nfd;
                        outputs["normalizedText"] = fixedPlacement;
                        outputs["unaccentedText"] = unaccented;
                        outputs["hasAnomalies"] = anomalies.Count > 0;
                        outputs["anomalyCount"] = anomalies.Count;
                        outputs["anomalies"] = anomalies;
                        break;
                    }
                }

                sw.Stop();
                string hash = ComputeHash(outputs.TryGetValue("normalizedText", out var nt) ? nt?.ToString() ?? text : text);
                return CapabilityExecutionResult.Ok(outputs, hash, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return CapabilityExecutionResult.Fail($"Lỗi thực thi VietnameseUnicodeNormalizerCapability: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        public Task<CapabilityExecutionResult> ExecuteAsync(CapabilityExecutionContext context, CancellationToken ct = default)
        {
            return Task.FromResult(Execute(context));
        }

        public static string NormalizeUnicode(string input, NormalizationForm form = NormalizationForm.FormC)
        {
            if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
            return input.Normalize(form);
        }

        public static string RemoveDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Chuyển sang NFD để tách các dấu thanh rời rạc
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    // Xử lý riêng ký tự Đ/đ trong tiếng Việt không bị NonSpacingMark tách
                    if (c == 'Đ') stringBuilder.Append('D');
                    else if (c == 'đ') stringBuilder.Append('d');
                    else stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string FixDiacriticPlacement(string input, bool modernStyle = true)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string text = input.Normalize(NormalizationForm.FormC);

            // Bảng chuẩn hóa các cụm nguyên âm kép/ba hay bị lỗi đặt dấu giữa lối cũ và mới
            // Lối mới (modern): hòa, hóa, thủy, thúy, quỳnh, loan, hoàn
            // Lối cũ (traditional): hoà, hoá, thuỷ, thuý, quỳnh, loàn, hoàn
            if (modernStyle)
            {
                text = Regex.Replace(text, @"\b(h|k|l|m|n|p|r|s|t|v|x|ch|th|tr|nh|kh|ph|gh|ngh|d|gi|b|c|g)?(oà|oá|oả|oã|oạ)\b", m =>
                {
                    string p = m.Groups[1].Value;
                    string v = m.Groups[2].Value;
                    return p + v.Replace("oà", "òa").Replace("oá", "óa").Replace("oả", "ỏa").Replace("oã", "õa").Replace("oạ", "ọa");
                }, RegexOptions.IgnoreCase);

                text = Regex.Replace(text, @"\b(th|h|t|s|l|m|n|ch|kh|ph)?(uỷ|uý|uỳ|uỹ|uỵ)\b", m =>
                {
                    string p = m.Groups[1].Value;
                    string v = m.Groups[2].Value;
                    return p + v.Replace("uỷ", "ủy").Replace("uý", "úy").Replace("uỳ", "ùy").Replace("uỹ", "ũy").Replace("uỵ", "ụy");
                }, RegexOptions.IgnoreCase);
            }

            return text;
        }

        public static int CompareVietnamese(string a, string b, bool ignoreDiacritics = false, bool ignoreCase = true)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (ignoreDiacritics)
            {
                a = RemoveDiacritics(a);
                b = RemoveDiacritics(b);
            }

            CompareOptions options = CompareOptions.None;
            if (ignoreCase) options |= CompareOptions.IgnoreCase;

            var viCulture = CultureInfo.GetCultureInfo("vi-VN");
            return viCulture.CompareInfo.Compare(a, b, options);
        }

        public static List<string> DetectAnomalies(string input)
        {
            var anomalies = new List<string>();
            if (string.IsNullOrEmpty(input)) return anomalies;

            // 1. Kiểm tra ký tự thay thế (Replacement character U+FFFD)
            if (input.Contains('\uFFFD'))
            {
                anomalies.Add("Chứa ký tự U+FFFD (Replacement Character / Tofu / Font missing)");
            }

            // 2. Kiểm tra Private Use Area (E000-F8FF)
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c >= '\uE000' && c <= '\uF8FF')
                {
                    anomalies.Add($"Chứa ký tự Private Use Area (PUA) tại vị trí {i}: \\u{(int)c:X4}");
                }
            }

            // 3. Kiểm tra dấu thanh đứng độc lập không có nguyên âm đứng trước
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark)
                {
                    if (i == 0 || char.IsWhiteSpace(input[i - 1]))
                    {
                        anomalies.Add($"Dấu thanh Unicode tổ hợp lơ lửng không có ký tự gốc tại vị trí {i} (\\u{(int)c:X4})");
                    }
                }
            }

            // 4. Kiểm tra các ký tự điều khiển lạ (ngoài CR, LF, Tab)
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                {
                    anomalies.Add($"Ký tự điều khiển bất thường tại vị trí {i}: \\u{(int)c:X4}");
                }
            }

            return anomalies;
        }

        private static string ComputeHash(string text)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}

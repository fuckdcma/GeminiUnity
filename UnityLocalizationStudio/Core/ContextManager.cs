using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityLocalizationStudio.Core
{
    public class ContextManager
    {
        // Regex nhận diện các tham số động: {0}, {1}, %d, %s, %f
        private static readonly Regex ParameterRegex = new Regex(@"\{(\d+)\}|%[dsf]", RegexOptions.Compiled);
        
        // Regex nhận diện RichText tags của Unity: <color=...>, <b>, </b>, <size=...>, etc.
        private static readonly Regex UnityTagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

        /// <summary>
        /// Tạo mã định danh duy nhất (Hash Key) dựa trên Context Path và Chuỗi nguyên bản đã làm sạch
        /// </summary>
        public static string ComputeKeyId(string contextPath, string originalText)
        {
            string sanitized = SanitizeText(originalText);
            string input = $"{contextPath?.Trim()}::{sanitized?.Trim()}";
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return "0x" + Convert.ToHexString(hash, 0, 4); // 8 ký tự Hex
        }

        /// <summary>
        /// Tạo Global Fallback Key chỉ dựa trên chuỗi nguyên bản
        /// </summary>
        public static string ComputeFallbackKeyId(string originalText)
        {
            return ComputeKeyId("GLOBAL_FALLBACK", originalText);
        }

        /// <summary>
        /// Chuẩn hóa chuỗi bằng cách thay thế tham số runtime bằng placeholder cố định {}
        /// để tránh sinh ra nhiều key trùng lặp khi số lượng thay đổi
        /// </summary>
        public static string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Thay thế tham số {0}, {1} thành {}
            return ParameterRegex.Replace(text, "{}");
        }

        /// <summary>
        /// Trích xuất danh sách giá trị tham số trong chuỗi gốc
        /// </summary>
        public static MatchCollection ExtractParameters(string text)
        {
            return ParameterRegex.Matches(text ?? string.Empty);
        }

        /// <summary>
        /// Khôi phục các giá trị tham số vào bản dịch tiếng Việt tương ứng
        /// </summary>
        public static string RestoreParameters(string translatedTemplate, MatchCollection originalParams)
        {
            if (string.IsNullOrEmpty(translatedTemplate) || originalParams == null || originalParams.Count == 0)
                return translatedTemplate;

            string result = translatedTemplate;
            for (int i = 0; i < originalParams.Count; i++)
            {
                string token = "{" + i + "}";
                if (result.Contains(token))
                {
                    result = result.Replace(token, originalParams[i].Value);
                }
                else if (result.Contains("{}"))
                {
                    // Trường hợp người dịch để {} thay vì {0}
                    int idx = result.IndexOf("{}");
                    result = result.Remove(idx, 2).Insert(idx, originalParams[i].Value);
                }
            }
            return result;
        }
    }
}

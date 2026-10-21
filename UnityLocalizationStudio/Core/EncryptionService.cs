using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class EncryptionService
    {
        // 32 Bytes Key (AES-256) chuẩn hóa bí mật cho dự án Việt hóa
        private static readonly byte[] DefaultAesKey = new byte[32]
        {
            0x41, 0x6E, 0x74, 0x69, 0x67, 0x72, 0x61, 0x76,
            0x69, 0x74, 0x79, 0x55, 0x6E, 0x69, 0x74, 0x79,
            0x56, 0x69, 0x65, 0x74, 0x48, 0x6F, 0x61, 0x32,
            0x30, 0x32, 0x36, 0x4B, 0x65, 0x79, 0x21, 0x23
        };

        private static readonly byte[] MagicHeader = Encoding.ASCII.GetBytes("VMOD"); // 4 Bytes Magic
        private const int FormatVersion = 1;

        /// <summary>
        /// Mã hóa danh sách bản dịch thành tệp nhị phân bảo mật Vietnamese.dat
        /// Không ai có thể mở bằng Notepad để xem hay chỉnh sửa trái phép
        /// </summary>
        public static void EncryptEntriesToFile(IEnumerable<TranslationEntry> entries, string outputPath, byte[]? customKey = null)
        {
            byte[] key = customKey ?? DefaultAesKey;
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Tạo chuỗi cấu trúc dữ liệu bản dịch: mỗi dòng là [Key_Context]::[Key_Fallback]=[Translation]
            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.VietnameseTranslation))
                    continue;

                string sanitized = ContextManager.SanitizeText(entry.OriginalText);
                
                // 1. Lưu Exact Context Key: "ContextPath::SanitizedText"
                if (!string.IsNullOrEmpty(entry.ContextPath))
                {
                    string contextFullKey = $"{entry.ContextPath}::{sanitized}";
                    sb.AppendLine($"C:{contextFullKey}={entry.VietnameseTranslation}");
                }

                // 2. Lưu Fallback Generic Key (nếu context không khớp lúc runtime)
                sb.AppendLine($"F:{sanitized}={entry.VietnameseTranslation}");

                // 3. Lưu KeyId
                if (!string.IsNullOrEmpty(entry.KeyId))
                {
                    sb.AppendLine($"K:{entry.KeyId}={entry.VietnameseTranslation}");
                }

                // 4. Lưu Dictionary Key (ví dụ: VIEW_CARDS, START_DAY_READY)
                string dictKey = entry.Key;
                if (string.IsNullOrEmpty(dictKey) && !string.IsNullOrEmpty(entry.ContextPath))
                {
                    int startIdx = entry.ContextPath.IndexOf('[');
                    int endIdx = entry.ContextPath.IndexOf(']');
                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        dictKey = entry.ContextPath.Substring(startIdx + 1, endIdx - startIdx - 1);
                    }
                }
                if (string.IsNullOrEmpty(dictKey) && !string.IsNullOrEmpty(entry.Notes))
                {
                    int startIdx = entry.Notes.IndexOf('[');
                    int endIdx = entry.Notes.IndexOf(']');
                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        dictKey = entry.Notes.Substring(startIdx + 1, endIdx - startIdx - 1);
                    }
                }
                if (!string.IsNullOrEmpty(dictKey))
                {
                    sb.AppendLine($"D:{dictKey}={entry.VietnameseTranslation}");
                }
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(sb.ToString());

            // Tạo IV ngẫu nhiên 16 bytes
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(outStream);

            // Ghi Header
            writer.Write(MagicHeader);
            writer.Write(FormatVersion);
            writer.Write(iv);

            // Mã hóa dữ liệu bằng AES-256-CBC
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
        }

        /// <summary>
        /// Giải mã tệp Vietnamese.dat trực tiếp trên luồng bộ nhớ RAM (Zero Disk Footprint)
        /// Trả về 2 bảng: Context Table và Fallback Table
        /// </summary>
        public static (Dictionary<string, string> ContextMap, Dictionary<string, string> FallbackMap) DecryptFromStream(Stream inputStream, byte[]? customKey = null)
        {
            byte[] key = customKey ?? DefaultAesKey;
            var contextMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fallbackMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var reader = new BinaryReader(inputStream, Encoding.UTF8, leaveOpen: true);
            byte[] magic = reader.ReadBytes(4);
            if (Encoding.ASCII.GetString(magic) != "VMOD")
            {
                throw new InvalidDataException("Tệp dữ liệu không hợp lệ hoặc không phải định dạng VMOD.");
            }

            int version = reader.ReadInt32();
            byte[] iv = reader.ReadBytes(16);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
            using var textReader = new StreamReader(cryptoStream, Encoding.UTF8);

            string? line;
            while ((line = textReader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                int eqIdx = line.IndexOf('=');
                if (eqIdx <= 2) continue;

                string prefix = line.Substring(0, 2);
                string keyName = line.Substring(2, eqIdx - 2);
                string value = line.Substring(eqIdx + 1);

                if (prefix == "C:")
                {
                    contextMap[keyName] = value;
                }
                else if (prefix == "F:")
                {
                    fallbackMap[keyName] = value;
                }
            }

            return (contextMap, fallbackMap);
        }

        /// <summary>
        /// Giải mã tệp từ đĩa vào RAM Dictionary
        /// </summary>
        public static (Dictionary<string, string> ContextMap, Dictionary<string, string> FallbackMap) DecryptFromFile(string filePath, byte[]? customKey = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Không tìm thấy tệp mã hóa: {filePath}");

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return DecryptFromStream(fs, customKey);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VietnameseLocalizationMod
{
    public static class MemoryDecrypter
    {
        private static readonly byte[] DefaultAesKey = new byte[32]
        {
            0x41, 0x6E, 0x74, 0x69, 0x67, 0x72, 0x61, 0x76,
            0x69, 0x74, 0x79, 0x55, 0x6E, 0x69, 0x74, 0x79,
            0x56, 0x69, 0x65, 0x74, 0x48, 0x6F, 0x61, 0x32,
            0x30, 0x32, 0x36, 0x4B, 0x65, 0x79, 0x21, 0x23
        };

        /// <summary>
        /// Giải mã Vietnamese.dat trực tiếp trên luồng RAM, không để lại bất kỳ byte plaintext nào trên ổ đĩa
        /// </summary>
        public static (Dictionary<string, string> ContextMap, Dictionary<string, string> FallbackMap, Dictionary<string, string> KeyMap, Dictionary<string, string> DictKeyMap) LoadEncryptedDatabase(string datFilePath)
        {
            var contextMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fallbackMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var keyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dictKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(datFilePath))
                return (contextMap, fallbackMap, keyMap, dictKeyMap);

            try
            {
                using (var fs = new FileStream(datFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    byte[] magic = reader.ReadBytes(4);
                    if (Encoding.ASCII.GetString(magic) != "VMOD")
                        return (contextMap, fallbackMap, keyMap, dictKeyMap);

                    int version = reader.ReadInt32();
                    byte[] iv = reader.ReadBytes(16);

                    using (var aes = Aes.Create())
                    {
                        aes.Key = DefaultAesKey;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var decryptor = aes.CreateDecryptor())
                        using (var cryptoStream = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                        using (var textReader = new StreamReader(cryptoStream, Encoding.UTF8))
                        {
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
                                else if (prefix == "K:")
                                {
                                    keyMap[keyName] = value;
                                }
                                else if (prefix == "D:")
                                {
                                    dictKeyMap[keyName] = value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Xử lý an toàn nếu file bị hỏng
            }

            return (contextMap, fallbackMap, keyMap, dictKeyMap);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UnityLocalizationStudio.Core.Security
{
    public static class WindowsSecretStore
    {
        private static string GetSecretFilePath(string keyName)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "UnityLocalizationStudio", "Secrets");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, $"{keyName}.dat");
        }

        public static bool SaveSecret(string keyName, string secretValue)
        {
            if (string.IsNullOrEmpty(keyName)) return false;
            try
            {
                string path = GetSecretFilePath(keyName);
                if (string.IsNullOrEmpty(secretValue))
                {
                    if (File.Exists(path)) File.Delete(path);
                    return true;
                }

                byte[] plainBytes = Encoding.UTF8.GetBytes(secretValue);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, encryptedBytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string? LoadSecret(string keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return null;
            try
            {
                string path = GetSecretFilePath(keyName);
                if (!File.Exists(path)) return null;

                byte[] encryptedBytes = File.ReadAllBytes(path);
                if (encryptedBytes.Length == 0) return null;

                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }

        public static bool DeleteSecret(string keyName)
        {
            try
            {
                string path = GetSecretFilePath(keyName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasSecret(string keyName)
        {
            try
            {
                string path = GetSecretFilePath(keyName);
                return File.Exists(path) && new FileInfo(path).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

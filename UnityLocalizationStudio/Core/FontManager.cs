using System;
using System.IO;

namespace UnityLocalizationStudio.Core
{
    public class FontManager
    {
        public static readonly string[] SystemVietnameseFonts = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "tahoma.ttf")
        };

        /// <summary>
        /// Tìm tệp Font TrueType trên hệ thống Windows có hỗ trợ đầy đủ ký tự tiếng Việt Unicode
        /// </summary>
        public static string? FindAvailableSystemVietnameseFont()
        {
            foreach (var fontPath in SystemVietnameseFonts)
            {
                if (File.Exists(fontPath))
                {
                    return fontPath;
                }
            }
            return null;
        }

        /// <summary>
        /// Sao chép tệp Font tiếng Việt chuẩn vào thư mục mục tiêu để đóng gói mod
        /// </summary>
        public static string PrepareModFont(string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string targetFontFile = Path.Combine(targetDir, "vietnamese_fallback.ttf");
            if (File.Exists(targetFontFile))
                return targetFontFile;

            string? sysFont = FindAvailableSystemVietnameseFont();
            if (sysFont != null && File.Exists(sysFont))
            {
                File.Copy(sysFont, targetFontFile, true);
                return targetFontFile;
            }

            return string.Empty;
        }
    }
}

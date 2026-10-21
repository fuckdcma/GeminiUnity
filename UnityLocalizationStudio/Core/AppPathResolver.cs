using System;
using System.IO;

namespace UnityLocalizationStudio.Core
{
    public static class AppPathResolver
    {
        private static string? _cachedWorkspaceRoot;

        public static string ResolveWorkspaceRoot()
        {
            if (!string.IsNullOrEmpty(_cachedWorkspaceRoot) && Directory.Exists(_cachedWorkspaceRoot))
            {
                return _cachedWorkspaceRoot;
            }

            // 1. Kiểm tra CurrentDirectory
            string current = Directory.GetCurrentDirectory();
            if (IsWorkspaceRoot(current))
            {
                _cachedWorkspaceRoot = current;
                return current;
            }

            // 2. Kiểm tra các thư mục cha của AppDomain BaseDirectory
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (IsWorkspaceRoot(dir))
                {
                    _cachedWorkspaceRoot = dir;
                    return dir;
                }
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }

            // 3. Fallback
            _cachedWorkspaceRoot = AppDomain.CurrentDomain.BaseDirectory;
            return _cachedWorkspaceRoot;
        }

        private static bool IsWorkspaceRoot(string path)
        {
            bool hasStudio = Directory.Exists(Path.Combine(path, "UnityLocalizationStudio"));
            bool hasMod = Directory.Exists(Path.Combine(path, "RuntimeModPlugin"));
            bool hasOutput = Directory.Exists(Path.Combine(path, "Output", "Studio"));
            return (hasStudio && hasMod) || (hasStudio && hasOutput);
        }

        public static string ResolveBepInExZip()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "BepInEx", "BepInEx_x64_5.4.22.0.zip"),
                Path.Combine(baseDir, "Assets", "BepInEx_x64_5.4.22.0.zip"),
                Path.Combine(baseDir, "BepInEx_x64_5.4.22.0.zip"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "BepInEx", "BepInEx_x64_5.4.22.0.zip"),
                Path.Combine(ws, "Output", "Studio", "Assets", "BepInEx", "BepInEx_x64_5.4.22.0.zip"),
                Path.Combine(ws, "Assets", "BepInEx", "BepInEx_x64_5.4.22.0.zip"),
                Path.Combine(ws, "Assets", "BepInEx_x64_5.4.22.0.zip")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }

            return candidates[0];
        }

        public static string ResolveModDll()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(ws, "RuntimeModPlugin", "bin", "Release", "netstandard2.0", "VietnameseLocalizationMod.dll"),
                Path.Combine(baseDir, "VietnameseLocalizationMod.dll"),
                Path.Combine(baseDir, "Assets", "VietnameseLocalizationMod.dll"),
                Path.Combine(ws, "Output", "Studio", "VietnameseLocalizationMod.dll"),
                Path.Combine(ws, "RuntimeModPlugin", "bin", "Debug", "netstandard2.0", "VietnameseLocalizationMod.dll"),
                Path.Combine(baseDir, "RuntimeModPlugin", "bin", "Release", "netstandard2.0", "VietnameseLocalizationMod.dll"),
                Path.Combine(Directory.GetCurrentDirectory(), "RuntimeModPlugin", "bin", "Release", "netstandard2.0", "VietnameseLocalizationMod.dll")
            };

            var existing = candidates.Where(File.Exists).ToList();
            if (existing.Count > 0)
            {
                return existing.OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc).First();
            }

            return candidates[0];
        }

        public static string ResolveDefaultFontBundle()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "Fonts", "vietnamese_font.bundle"),
                Path.Combine(baseDir, "vietnamese_font.bundle"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "Fonts", "vietnamese_font.bundle"),
                Path.Combine(ws, "Output", "Studio", "Assets", "Fonts", "vietnamese_font.bundle"),
                Path.Combine(ws, "Assets", "Fonts", "vietnamese_font.bundle")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }

            return "";
        }

        public static string ResolveVietnameseFontBundle() => ResolveDefaultFontBundle();

        public static string ResolveDefaultFontMappingConfig()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "Fonts", "Font_Mapping_Config.json"),
                Path.Combine(baseDir, "Font_Mapping_Config.json"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "Fonts", "Font_Mapping_Config.json"),
                Path.Combine(ws, "Output", "Studio", "Assets", "Fonts", "Font_Mapping_Config.json"),
                Path.Combine(ws, "Assets", "Fonts", "Font_Mapping_Config.json")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }

            return "";
        }
        public static string ResolveSampleTextCsv()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "SampleData", "Sample_LiveTest_Texts.csv"),
                Path.Combine(baseDir, "Sample_LiveTest_Texts.csv"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "SampleData", "Sample_LiveTest_Texts.csv"),
                Path.Combine(ws, "Output", "Studio", "Assets", "SampleData", "Sample_LiveTest_Texts.csv"),
                Path.Combine(ws, "Assets", "SampleData", "Sample_LiveTest_Texts.csv")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }

            return candidates[0];
        }

        public static string ResolveSampleFontMappingJson()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "SampleData", "Sample_Font_Mapping.json"),
                Path.Combine(baseDir, "Sample_Font_Mapping.json"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "SampleData", "Sample_Font_Mapping.json"),
                Path.Combine(ws, "Output", "Studio", "Assets", "SampleData", "Sample_Font_Mapping.json"),
                Path.Combine(ws, "Assets", "SampleData", "Sample_Font_Mapping.json"),
                Path.Combine(baseDir, "Assets", "Fonts", "Font_Mapping_Config.json"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "Fonts", "Font_Mapping_Config.json")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }

            return candidates[0];
        }

        public static string ResolveSampleFontBundle()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ws = ResolveWorkspaceRoot();

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "Fonts", "Sample_vietnamese_font.bundle"),
                Path.Combine(baseDir, "Sample_vietnamese_font.bundle"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "Fonts", "Sample_vietnamese_font.bundle"),
                Path.Combine(ws, "Output", "Studio", "Assets", "Fonts", "Sample_vietnamese_font.bundle"),
                Path.Combine(ws, "Assets", "Fonts", "Sample_vietnamese_font.bundle"),
                Path.Combine(baseDir, "Assets", "Fonts", "vietnamese_font.bundle"),
                Path.Combine(baseDir, "vietnamese_font.bundle"),
                Path.Combine(ws, "UnityLocalizationStudio", "Assets", "Fonts", "vietnamese_font.bundle"),
                Path.Combine(ws, "Output", "Studio", "Assets", "Fonts", "vietnamese_font.bundle"),
                Path.Combine(ws, "Assets", "Fonts", "vietnamese_font.bundle")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return Path.GetFullPath(c);
            }

            return ResolveDefaultFontBundle();
        }
    }
}

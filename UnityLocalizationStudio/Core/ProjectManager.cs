using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public static class ProjectManager
    {
        public static string GetProjectsRootDirectory()
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
            return root;
        }

        public static string GetProjectDirectory(string projectName)
        {
            string safeName = SanitizeFileName(projectName);
            string dir = Path.Combine(GetProjectsRootDirectory(), safeName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        public static LocalizationProject CreateProject(string gameDirectory, string? projectName = null)
        {
            var gameInfo = GameDetector.DetectGame(gameDirectory);
            string name = projectName ?? Path.GetFileNameWithoutExtension(gameInfo.ExeName);
            if (string.IsNullOrWhiteSpace(name)) name = "UnityGame_Localization";

            var project = new LocalizationProject
            {
                ProjectName = name,
                GameDirectory = gameDirectory,
                GameExeName = gameInfo.ExeName,
                UnityVersion = gameInfo.UnityVersion,
                Architecture = gameInfo.Architecture.ToString(),
                ModProfile = gameInfo.Architecture == GameArchitecture.Mono ? "BepInEx 5 (Mono x64)" : "BepInEx 6 (IL2CPP x64)"
            };

            string projDir = GetProjectDirectory(name);
            project.RawTextCsvPath = Path.Combine(projDir, "Raw_Game_Texts.csv");
            project.RawFontsJsonPath = Path.Combine(projDir, "Raw_Game_Fonts.json");
            project.LiveTextMappingCsvPath = Path.Combine(projDir, "Live_Captured_Text_Mapping.csv");
            project.FontMappingConfigPath = Path.Combine(projDir, "Font_Mapping_Config.json");
            project.TranslatedCsvPath = Path.Combine(projDir, "Translated_Vietnamese.csv");
            project.EncryptedDatPath = Path.Combine(projDir, "Vietnamese.dat");
            project.PrereleaseZipPath = Path.Combine(projDir, $"{name}_VietHoa_Prerelease.zip");
            project.PrereleaseExePath = Path.Combine(projDir, $"Setup_VietHoa_{name}_Prerelease.exe");
            project.LastPrereleaseZipPath = Path.Combine(projDir, $"{name}_VietHoa_LastPrerelease.zip");
            project.LastPrereleaseExePath = Path.Combine(projDir, $"Setup_VietHoa_{name}_LastPrerelease.exe");
            project.ReleaseZipPath = Path.Combine(projDir, $"{name}_VietHoa_Release.zip");
            project.ReleaseExePath = Path.Combine(projDir, $"Setup_VietHoa_{name}.exe");

            project.SetStepStatus(1, StepStatus.Passed, $"Đã tạo dự án thành công cho game {name}.");

            SaveProject(project);
            return project;
        }

        public static void SaveProject(LocalizationProject project)
        {
            string projDir = GetProjectDirectory(project.ProjectName);
            string filePath = Path.Combine(projDir, $"{SanitizeFileName(project.ProjectName)}.ulsproj");

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"ProjectId\": \"{EscapeJson(project.ProjectId)}\",");
            sb.AppendLine($"  \"ProjectName\": \"{EscapeJson(project.ProjectName)}\",");
            sb.AppendLine($"  \"GameDirectory\": \"{EscapeJson(project.GameDirectory)}\",");
            sb.AppendLine($"  \"GameExeName\": \"{EscapeJson(project.GameExeName)}\",");
            sb.AppendLine($"  \"UnityVersion\": \"{EscapeJson(project.UnityVersion)}\",");
            sb.AppendLine($"  \"Architecture\": \"{EscapeJson(project.Architecture)}\",");
            sb.AppendLine($"  \"ModProfile\": \"{EscapeJson(project.ModProfile)}\",");
            sb.AppendLine($"  \"BaseLanguage\": \"{EscapeJson(project.BaseLanguage)}\",");
            sb.AppendLine($"  \"TargetLanguage\": \"{EscapeJson(project.TargetLanguage)}\",");
            sb.AppendLine($"  \"CreatedAt\": \"{project.CreatedAt:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"LastModified\": \"{project.LastModified:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"CurrentStep\": {project.CurrentStep},");

            sb.AppendLine($"  \"RawTextCsvPath\": \"{EscapeJson(project.RawTextCsvPath)}\",");
            sb.AppendLine($"  \"RawFontsJsonPath\": \"{EscapeJson(project.RawFontsJsonPath)}\",");
            sb.AppendLine($"  \"LiveTextMappingCsvPath\": \"{EscapeJson(project.LiveTextMappingCsvPath)}\",");
            sb.AppendLine($"  \"FontMappingConfigPath\": \"{EscapeJson(project.FontMappingConfigPath)}\",");
            sb.AppendLine($"  \"TranslatedCsvPath\": \"{EscapeJson(project.TranslatedCsvPath)}\",");
            sb.AppendLine($"  \"CustomFontBundlePath\": \"{EscapeJson(project.CustomFontBundlePath)}\",");
            sb.AppendLine($"  \"EncryptedDatPath\": \"{EscapeJson(project.EncryptedDatPath)}\",");
            sb.AppendLine($"  \"PrereleaseZipPath\": \"{EscapeJson(project.PrereleaseZipPath)}\",");
            sb.AppendLine($"  \"PrereleaseExePath\": \"{EscapeJson(project.PrereleaseExePath)}\",");
            sb.AppendLine($"  \"LastPrereleaseZipPath\": \"{EscapeJson(project.LastPrereleaseZipPath)}\",");
            sb.AppendLine($"  \"LastPrereleaseExePath\": \"{EscapeJson(project.LastPrereleaseExePath)}\",");
            sb.AppendLine($"  \"ReleaseZipPath\": \"{EscapeJson(project.ReleaseZipPath)}\",");
            sb.AppendLine($"  \"ReleaseExePath\": \"{EscapeJson(project.ReleaseExePath)}\",");

            sb.AppendLine("  \"Steps\": [");
            for (int i = 0; i < project.Steps.Count; i++)
            {
                var s = project.Steps[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"StepNumber\": {s.StepNumber},");
                sb.AppendLine($"      \"Title\": \"{EscapeJson(s.Title)}\",");
                sb.AppendLine($"      \"Description\": \"{EscapeJson(s.Description)}\",");
                sb.AppendLine($"      \"Status\": \"{s.Status}\",");
                sb.AppendLine($"      \"Summary\": \"{EscapeJson(s.Summary)}\",");
                sb.AppendLine($"      \"CompletedAt\": \"{(s.CompletedAt.HasValue ? s.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}\"");
                sb.Append("    }");
                if (i < project.Steps.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static LocalizationProject? LoadProject(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                var project = new LocalizationProject();

                project.ProjectId = ExtractString(content, "ProjectId") ?? project.ProjectId;
                project.ProjectName = ExtractString(content, "ProjectName") ?? "Dự Án Việt Hóa";
                project.GameDirectory = ExtractString(content, "GameDirectory") ?? "";
                project.GameExeName = ExtractString(content, "GameExeName") ?? "";
                if (!string.IsNullOrEmpty(project.GameDirectory) && Directory.Exists(project.GameDirectory))
                {
                    try
                    {
                        var det = GameDetector.DetectGame(project.GameDirectory);
                        project.GameDirectory = det.GameRootPath;
                        if (!string.IsNullOrEmpty(det.ExeName)) project.GameExeName = det.ExeName;
                        if (!string.IsNullOrEmpty(det.UnityVersion)) project.UnityVersion = det.UnityVersion;
                    }
                    catch { }
                }
                project.UnityVersion = ExtractString(content, "UnityVersion") ?? "";
                project.Architecture = ExtractString(content, "Architecture") ?? "Mono x64";
                project.ModProfile = ExtractString(content, "ModProfile") ?? "BepInEx 5 (Mono)";
                project.BaseLanguage = ExtractString(content, "BaseLanguage") ?? "English";
                project.TargetLanguage = ExtractString(content, "TargetLanguage") ?? "Tiếng Việt";
                project.CurrentStep = ExtractInt(content, "CurrentStep", 1);

                project.RawTextCsvPath = ExtractString(content, "RawTextCsvPath") ?? "";
                project.RawFontsJsonPath = ExtractString(content, "RawFontsJsonPath") ?? "";
                project.LiveTextMappingCsvPath = ExtractString(content, "LiveTextMappingCsvPath") ?? "";
                project.FontMappingConfigPath = ExtractString(content, "FontMappingConfigPath") ?? "";
                project.TranslatedCsvPath = ExtractString(content, "TranslatedCsvPath") ?? "";
                project.CustomFontBundlePath = ExtractString(content, "CustomFontBundlePath") ?? "";
                project.EncryptedDatPath = ExtractString(content, "EncryptedDatPath") ?? "";
                project.PrereleaseZipPath = ExtractString(content, "PrereleaseZipPath") ?? "";
                project.PrereleaseExePath = ExtractString(content, "PrereleaseExePath") ?? "";
                project.LastPrereleaseZipPath = ExtractString(content, "LastPrereleaseZipPath") ?? "";
                project.LastPrereleaseExePath = ExtractString(content, "LastPrereleaseExePath") ?? "";
                project.ReleaseZipPath = ExtractString(content, "ReleaseZipPath") ?? "";
                project.ReleaseExePath = ExtractString(content, "ReleaseExePath") ?? "";

                // Parse Steps
                var stepBlocks = Regex.Matches(content, @"\{[^{}]*""StepNumber""[^{}]*\}", RegexOptions.Singleline);
                if (stepBlocks.Count > 0)
                {
                    project.Steps.Clear();
                    foreach (Match m in stepBlocks)
                    {
                        string b = m.Value;
                        var step = new ProjectStepInfo
                        {
                            StepNumber = ExtractInt(b, "StepNumber", 1),
                            Title = ExtractString(b, "Title") ?? "",
                            Description = ExtractString(b, "Description") ?? "",
                            Summary = ExtractString(b, "Summary") ?? ""
                        };
                        string? statusStr = ExtractString(b, "Status");
                        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<StepStatus>(statusStr, true, out var st))
                        {
                            step.Status = st;
                        }
                        string? dateStr = ExtractString(b, "CompletedAt");
                        if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var dt))
                        {
                            step.CompletedAt = dt;
                        }
                        project.Steps.Add(step);
                    }

                    // Đồng bộ hóa tiêu đề và mô tả chuẩn của 13 bước quy trình theo đặc tả chính thức
                    var defaultProj = new LocalizationProject();
                    foreach (var s in project.Steps)
                    {
                        var def = defaultProj.Steps.Find(d => d.StepNumber == s.StepNumber);
                        if (def != null)
                        {
                            s.Title = def.Title;
                            if (string.IsNullOrWhiteSpace(s.Description))
                            {
                                s.Description = def.Description;
                            }
                        }
                    }
                }

                return project;
            }
            catch
            {
                return null;
            }
        }

        public static bool DeleteProject(LocalizationProject project)
        {
            if (project == null) return false;
            try
            {
                string projDir = GetProjectDirectory(project.ProjectName);
                if (Directory.Exists(projDir))
                {
                    Directory.Delete(projDir, true);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static List<LocalizationProject> ListAllProjects()
        {
            var list = new List<LocalizationProject>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void ScanDir(string dir)
            {
                if (!Directory.Exists(dir)) return;
                try
                {
                    foreach (var f in Directory.GetFiles(dir, "*.ulsproj", SearchOption.AllDirectories))
                    {
                        var p = LoadProject(f);
                        if (p != null && seenIds.Add(p.ProjectId))
                        {
                            list.Add(p);
                        }
                    }
                }
                catch { }
            }

            ScanDir(GetProjectsRootDirectory());

            string ws = AppPathResolver.ResolveWorkspaceRoot();
            if (!string.IsNullOrEmpty(ws))
            {
                ScanDir(Path.Combine(ws, "Projects"));
                ScanDir(Path.Combine(ws, "Output", "Studio", "Projects"));
            }

            return list;
        }

        private static string EscapeJson(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        }

        private static string? ExtractString(string text, string key)
        {
            var m = Regex.Match(text, $@"""{key}""\s*:\s*""([^""]*)""");
            return m.Success ? m.Groups[1].Value.Replace("\\\\", "\\").Replace("\\\"", "\"").Replace("\\n", "\n") : null;
        }

        private static int ExtractInt(string text, string key, int defaultValue = 0)
        {
            var m = Regex.Match(text, $@"""{key}""\s*:\s*(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : defaultValue;
        }
    }
}

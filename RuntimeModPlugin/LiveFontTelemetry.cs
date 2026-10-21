using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VietnameseLocalizationMod
{
    [Serializable]
    public class LiveFontElement
    {
        public string GameObject = "";
        public string HierarchyPath = "";
        public string ComponentType = "";
        public string Text = "";
        public string OriginalFont = "";
        public string ActiveFont = "";
        public string ExpectedFont = "";
        public string Material = "";
        public string Shader = "";
        public string Status = ""; // REPLACED_1TO1 | FALLBACK_ATTACHED | ORIGINAL_UNTOUCHED | PROTECTED
        public List<string> Fallbacks = new List<string>();
    }

    [Serializable]
    public class LiveFontSnapshot
    {
        public string Game = "Unity Game";
        public string Timestamp = "";
        public string ActiveScene = "";
        public int TotalTexts = 0;
        public int Replaced1to1Count = 0;
        public int FallbackCount = 0;
        public int UntouchedCount = 0;
        public int ProtectedCount = 0;
        public List<LiveFontElement> Elements = new List<LiveFontElement>();
    }

    public static class LiveFontTelemetry
    {
        private static string _dumpJsonPath = "";
        private static string _dumpCsvPath = "";

        public static void Initialize(string pluginDir)
        {
            if (string.IsNullOrEmpty(pluginDir)) return;
            _dumpJsonPath = Path.Combine(pluginDir, "Live_Screen_Fonts.json");
            _dumpCsvPath = Path.Combine(pluginDir, "Live_Screen_Fonts.csv");
        }

        public static string GetHierarchyPath(Transform? t)
        {
            if (t == null) return "";
            var parts = new List<string>();
            var cur = t;
            while (cur != null)
            {
                parts.Insert(0, cur.name);
                cur = cur.parent;
            }
            return string.Join("/", parts);
        }

        public static LiveFontSnapshot CaptureCurrentScreenSnapshot(string gameName = "Unity Game")
        {
            var snapshot = new LiveFontSnapshot
            {
                Game = gameName,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ActiveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };

            var processedTransforms = new HashSet<Transform>();

            // Đảm bảo font hook được áp dụng trước khi snapshot telemetry
            if (FontFallbackPatcher.IsEnabled)
            {
                try
                {
                    var allActive = Resources.FindObjectsOfTypeAll<TMP_Text>();
                    if (allActive != null)
                    {
                        foreach (var t in allActive)
                        {
                            if (t != null && t.gameObject.activeInHierarchy)
                            {
                                FontFallbackPatcher.AttachFallbackFont(t);
                            }
                        }
                    }
                }
                catch { }
            }

            // 1. Quét toàn bộ TextMeshPro (TMP_Text bao gồm cả UGUI và World 3D)
            try
            {
                var allTMP = Resources.FindObjectsOfTypeAll<TMP_Text>();
                if (allTMP != null)
                {
                    foreach (var tmp in allTMP)
                    {
                        if (tmp == null || tmp.gameObject == null || !tmp.gameObject.activeInHierarchy) continue;
                        processedTransforms.Add(tmp.transform);

                        var font = tmp.font;
                        string fontName = font != null ? font.name : "(null)";

                        // Tra cứu font gốc nguyên bản từ tracker
                        string origName = fontName;
                        if (FontFallbackPatcher.OriginalFontTracker.TryGetValue(tmp, out string trackedOrig) && !string.IsNullOrEmpty(trackedOrig))
                        {
                            origName = trackedOrig;
                        }

                        // Nếu origName vẫn bị dính tên font đã hook (ARIAL SDF / TIMES SDF), truy vết từ Material hoặc cây phân cấp
                        if (FontFallbackPatcher.IsCustomFontPublic(origName))
                        {
                            if (tmp.fontSharedMaterial != null && tmp.fontSharedMaterial.name.Contains("_Preserved"))
                            {
                                string matName = tmp.fontSharedMaterial.name.Replace("_Preserved", "");
                                int uIdx = matName.IndexOf('_');
                                if (uIdx >= 0)
                                {
                                    string part = matName.Substring(uIdx + 1).Replace(" Atlas Material", " SDF").Replace(" Material", " SDF").Trim();
                                    if (!string.IsNullOrEmpty(part) && !part.Equals("Universal", StringComparison.OrdinalIgnoreCase))
                                    {
                                        origName = part;
                                    }
                                }
                            }

                            if (FontFallbackPatcher.IsCustomFontPublic(origName))
                            {
                                string hPath = GetHierarchyPath(tmp.transform);
                                if (hPath.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0 || tmp.gameObject.name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    origName = "Chewy SDF";
                                }
                                else if (hPath.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    origName = "Grandstander-clean SDF";
                                }
                                else
                                {
                                    origName = "Capriola-Regular SDF";
                                }
                            }
                        }

                        string expected = FontFallbackPatcher.ResolveTargetFontName(origName) 
                            ?? FontFallbackPatcher.ResolveTargetFontName(fontName) 
                            ?? "(none)";

                        var elem = new LiveFontElement
                        {
                            GameObject = tmp.gameObject.name,
                            HierarchyPath = GetHierarchyPath(tmp.transform),
                            ComponentType = tmp.GetType().Name,
                            Text = CleanTextPreview(tmp.text),
                            OriginalFont = origName,
                            ActiveFont = fontName,
                            ExpectedFont = expected,
                            Material = tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial.name : (font?.material?.name ?? "(none)"),
                            Shader = tmp.fontSharedMaterial?.shader != null ? tmp.fontSharedMaterial.shader.name : (font?.material?.shader?.name ?? "(none)")
                        };

                        if (font != null && font.fallbackFontAssetTable != null)
                        {
                            foreach (var fb in font.fallbackFontAssetTable)
                            {
                                if (fb != null) elem.Fallbacks.Add(fb.name);
                            }
                        }

                        // Phân loại trạng thái
                        if (FontFallbackPatcher.IsProtectedFontPublic(fontName) || FontFallbackPatcher.IsProtectedFontPublic(origName))
                        {
                            elem.Status = "PROTECTED";
                            snapshot.ProtectedCount++;
                        }
                        else if ((expected != "(none)" && fontName.Equals(expected, StringComparison.OrdinalIgnoreCase)) ||
                                 (font != null && FontFallbackPatcher.LoadedCustomFonts.Values.Contains(font)) ||
                                 fontName.IndexOf("ARIAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 fontName.IndexOf("TIMES", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 fontName.IndexOf("IMES", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            elem.Status = "REPLACED_1TO1";
                            snapshot.Replaced1to1Count++;
                        }
                        else if (elem.Fallbacks.Any(f => FontFallbackPatcher.LoadedCustomFonts.ContainsKey(f)))
                        {
                            elem.Status = "FALLBACK_ATTACHED";
                            snapshot.FallbackCount++;
                        }
                        else
                        {
                            elem.Status = "ORIGINAL_UNTOUCHED";
                            snapshot.UntouchedCount++;
                        }

                        snapshot.Elements.Add(elem);
                    }
                }
            }
            catch { }

            // 2. Quét Legacy UnityEngine.UI.Text (hỗ trợ cho game Unity cổ điển)
            try
            {
                var allLegacyText = Resources.FindObjectsOfTypeAll<Text>();
                if (allLegacyText != null)
                {
                    foreach (var txt in allLegacyText)
                    {
                        if (txt == null || txt.gameObject == null || !txt.gameObject.activeInHierarchy) continue;
                        if (processedTransforms.Contains(txt.transform)) continue;

                        string fontName = txt.font != null ? txt.font.name : "(null)";
                        var elem = new LiveFontElement
                        {
                            GameObject = txt.gameObject.name,
                            HierarchyPath = GetHierarchyPath(txt.transform),
                            ComponentType = "Legacy.Text",
                            Text = CleanTextPreview(txt.text),
                            ActiveFont = fontName,
                            ExpectedFont = FontFallbackPatcher.ResolveTargetFontName(fontName) ?? "(none)",
                            Material = txt.material != null ? txt.material.name : "(none)",
                            Shader = txt.material?.shader != null ? txt.material.shader.name : "(none)",
                            Status = "LEGACY_UI"
                        };
                        snapshot.Elements.Add(elem);
                    }
                }
            }
            catch { }

            snapshot.TotalTexts = snapshot.Elements.Count;
            return snapshot;
        }

        private static string CleanTextPreview(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string clean = raw!.Replace("\r", " ").Replace("\n", " ").Trim();
            if (clean.Length > 60) clean = clean.Substring(0, 57) + "...";
            return clean;
        }

        public static void DumpSnapshotToFile(LiveFontSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(_dumpJsonPath)) return;

            try
            {
                // Xuất JSON tự lực (tránh phụ thuộc JSON.NET)
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"Game\": \"{EscapeJson(snapshot.Game)}\",");
                sb.AppendLine($"  \"Timestamp\": \"{EscapeJson(snapshot.Timestamp)}\",");
                sb.AppendLine($"  \"ActiveScene\": \"{EscapeJson(snapshot.ActiveScene)}\",");
                sb.AppendLine($"  \"TotalTexts\": {snapshot.TotalTexts},");
                sb.AppendLine($"  \"Replaced1to1Count\": {snapshot.Replaced1to1Count},");
                sb.AppendLine($"  \"FallbackCount\": {snapshot.FallbackCount},");
                sb.AppendLine($"  \"UntouchedCount\": {snapshot.UntouchedCount},");
                sb.AppendLine($"  \"ProtectedCount\": {snapshot.ProtectedCount},");
                sb.AppendLine("  \"Elements\": [");

                for (int i = 0; i < snapshot.Elements.Count; i++)
                {
                    var el = snapshot.Elements[i];
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"GameObject\": \"{EscapeJson(el.GameObject)}\",");
                    sb.AppendLine($"      \"HierarchyPath\": \"{EscapeJson(el.HierarchyPath)}\",");
                    sb.AppendLine($"      \"ComponentType\": \"{EscapeJson(el.ComponentType)}\",");
                    sb.AppendLine($"      \"Text\": \"{EscapeJson(el.Text)}\",");
                    sb.AppendLine($"      \"OriginalFont\": \"{EscapeJson(el.OriginalFont)}\",");
                    sb.AppendLine($"      \"ActiveFont\": \"{EscapeJson(el.ActiveFont)}\",");
                    sb.AppendLine($"      \"ExpectedFont\": \"{EscapeJson(el.ExpectedFont)}\",");
                    sb.AppendLine($"      \"Material\": \"{EscapeJson(el.Material)}\",");
                    sb.AppendLine($"      \"Shader\": \"{EscapeJson(el.Shader)}\",");
                    sb.AppendLine($"      \"Status\": \"{EscapeJson(el.Status)}\",");
                    sb.AppendLine($"      \"Fallbacks\": [{string.Join(", ", el.Fallbacks.Select(f => $"\"{EscapeJson(f)}\""))}]");
                    sb.Append("    }");
                    if (i < snapshot.Elements.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }

                sb.AppendLine("  ]");
                sb.AppendLine("}");

                File.WriteAllText(_dumpJsonPath, sb.ToString(), Encoding.UTF8);

                // Xuất cả CSV để tiện mở bằng Excel/Notepad
                var csvSb = new StringBuilder();
                csvSb.AppendLine("No,GameObject,Component,ActiveFont,ExpectedFont,Status,Material,Shader,Text,HierarchyPath");
                for (int i = 0; i < snapshot.Elements.Count; i++)
                {
                    var el = snapshot.Elements[i];
                    csvSb.AppendLine($"{i + 1},\"{EscapeCsv(el.GameObject)}\",\"{EscapeCsv(el.ComponentType)}\",\"{EscapeCsv(el.ActiveFont)}\",\"{EscapeCsv(el.ExpectedFont)}\",\"{EscapeCsv(el.Status)}\",\"{EscapeCsv(el.Material)}\",\"{EscapeCsv(el.Shader)}\",\"{EscapeCsv(el.Text)}\",\"{EscapeCsv(el.HierarchyPath)}\"");
                }
                File.WriteAllText(_dumpCsvPath, csvSb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                VietnamesePlugin.ModLog($"[LiveFontTelemetry] Lỗi ghi file snapshot: {ex.Message}");
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", " ")
                    .Replace("\n", "\\n")
                    .Replace("\t", " ");
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\"\"");
        }
    }
}

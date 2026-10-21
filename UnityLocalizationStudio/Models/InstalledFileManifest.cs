using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityLocalizationStudio.Models
{
    public class InstalledFileManifest
    {
        public string ModName { get; set; } = "Vietnamese Localization";
        public string GameName { get; set; } = "";
        public string InstallDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string Version { get; set; } = "1.0.0";
        public bool IsPrototype { get; set; } = false;
        public bool BepInExWasPreExisting { get; set; } = false;
        public List<string> InstalledRelativeFiles { get; set; } = new List<string>();
        public List<string> CreatedRelativeDirectories { get; set; } = new List<string>();

        public void SaveToFile(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# MANIFEST CÁC TỆP ĐÃ ĐƯỢC CÀI ĐẶT BỞI BẢN DỊCH TIẾNG VIỆT");
            sb.AppendLine($"# Mod: {ModName} | Game: {GameName} | Ngày cài: {InstallDate} | Prototype: {IsPrototype} | BepInExPreExisting: {BepInExWasPreExisting}");
            sb.AppendLine("[METADATA]");
            sb.AppendLine($"BepInExPreExisting={BepInExWasPreExisting}");
            sb.AppendLine("[FILES]");
            foreach (var f in InstalledRelativeFiles)
            {
                sb.AppendLine(f);
            }
            sb.AppendLine("[DIRECTORIES]");
            foreach (var d in CreatedRelativeDirectories)
            {
                sb.AppendLine(d);
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static InstalledFileManifest LoadFromFile(string filePath)
        {
            var manifest = new InstalledFileManifest();
            if (!File.Exists(filePath)) return manifest;

            string currentSection = "";
            foreach (var line in File.ReadAllLines(filePath, Encoding.UTF8))
            {
                string tr = line.Trim();
                if (string.IsNullOrEmpty(tr) || tr.StartsWith("#")) continue;

                if (tr == "[METADATA]") { currentSection = "METADATA"; continue; }
                if (tr == "[FILES]") { currentSection = "FILES"; continue; }
                if (tr == "[DIRECTORIES]") { currentSection = "DIRECTORIES"; continue; }

                if (currentSection == "METADATA")
                {
                    if (tr.StartsWith("BepInExPreExisting=", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = tr.Substring("BepInExPreExisting=".Length).Trim();
                        manifest.BepInExWasPreExisting = string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                    }
                }
                else if (currentSection == "FILES")
                {
                    manifest.InstalledRelativeFiles.Add(tr);
                }
                else if (currentSection == "DIRECTORIES")
                {
                    manifest.CreatedRelativeDirectories.Add(tr);
                }
            }
            return manifest;
        }
    }
}

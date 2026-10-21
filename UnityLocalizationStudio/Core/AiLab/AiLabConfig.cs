using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class AiLabConfig
    {
        public bool IsEnabled { get; set; } = false;
        public string ProviderId { get; set; } = "ollama_local";
        public string ModelId { get; set; } = "qwen2.5-coder:7b";
        public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
        public bool RedactLogs { get; set; } = true;
        public bool RedactMetadata { get; set; } = true;
        public string ConnectionStatus { get; set; } = "OFFLINE_NO_AI";
        public DateTime? LastTestedUtc { get; set; }

        private static string GetConfigFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "UnityLocalizationStudio");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "ai_settings.json");
        }

        public static AiLabConfig Load()
        {
            try
            {
                string path = GetConfigFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<AiLabConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (cfg != null) return cfg;
                }
            }
            catch
            {
                // Fallback to default config on error
            }

            return new AiLabConfig();
        }

        public bool Save()
        {
            try
            {
                string path = GetConfigFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityLocalizationStudio.Core.Security;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class AiLabService
    {
        private static AiLabService? _instance;
        public static AiLabService Instance => _instance ??= new AiLabService();

        private readonly Dictionary<string, IAiProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

        public AiLabConfig Config { get; set; }

        public AiLabService()
        {
            RegisterProvider(new OllamaLocalProvider());
            RegisterProvider(new GeminiApiProvider());
            RegisterProvider(new OpenAiApiProvider());
            Config = AiLabConfig.Load();
        }

        public void RegisterProvider(IAiProvider provider)
        {
            _providers[provider.ProviderId] = provider;
        }

        public IEnumerable<IAiProvider> GetAllProviders() => _providers.Values;

        public IAiProvider? GetProvider(string? providerId = null)
        {
            string id = providerId ?? Config.ProviderId;
            if (_providers.TryGetValue(id, out var provider))
            {
                return provider;
            }
            return null;
        }

        public bool HasApiKey(string? providerId = null)
        {
            string id = providerId ?? Config.ProviderId;
            if (id.Equals("ollama_local", StringComparison.OrdinalIgnoreCase)) return true;
            return WindowsSecretStore.HasSecret($"ai_key_{id}");
        }

        public string? GetApiKey(string? providerId = null)
        {
            string id = providerId ?? Config.ProviderId;
            return WindowsSecretStore.LoadSecret($"ai_key_{id}");
        }

        public bool SaveApiKey(string providerId, string key)
        {
            return WindowsSecretStore.SaveSecret($"ai_key_{providerId}", key);
        }

        public bool DeleteApiKey(string providerId)
        {
            return WindowsSecretStore.DeleteSecret($"ai_key_{providerId}");
        }

        public string Redact(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            string text = input;

            // Redact OS User Name
            try
            {
                string userName = Environment.UserName;
                if (!string.IsNullOrEmpty(userName) && userName.Length > 1)
                {
                    text = Regex.Replace(text, Regex.Escape(userName), "[REDACTED_USER]", RegexOptions.IgnoreCase);
                }

                string machineName = Environment.MachineName;
                if (!string.IsNullOrEmpty(machineName) && machineName.Length > 1)
                {
                    text = Regex.Replace(text, Regex.Escape(machineName), "[REDACTED_HOST]", RegexOptions.IgnoreCase);
                }
            }
            catch
            {
                // Ignore environment read failure
            }

            // Redact Windows User Paths e.g. C:\Users\<name>\
            text = Regex.Replace(text, @"([A-Za-z]:\\Users\\)[^\\]+(\\)", "$1[REDACTED_USER]$2", RegexOptions.IgnoreCase);

            // Redact standard API keys / tokens
            text = Regex.Replace(text, @"AIza[0-9A-Za-z_-]{30,45}", "[REDACTED_GEMINI_KEY]");
            text = Regex.Replace(text, @"sk-[a-zA-Z0-9_-]{20,}", "[REDACTED_OPENAI_KEY]");

            return text;
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(string? explicitKey = null, CancellationToken ct = default)
        {
            var provider = GetProvider();
            if (provider == null)
            {
                Config.ConnectionStatus = "UNCONFIGURED";
                Config.Save();
                return (false, $"Không tìm thấy nhà cung cấp AI: '{Config.ProviderId}'.");
            }

            bool isLocal = provider.ProviderId.Equals("ollama_local", StringComparison.OrdinalIgnoreCase);
            string? apiKey = !string.IsNullOrEmpty(explicitKey) ? explicitKey : GetApiKey();
            if (!isLocal && string.IsNullOrWhiteSpace(apiKey))
            {
                Config.ConnectionStatus = "KEY_ERROR";
                Config.Save();
                return (false, "Chưa cài đặt API Key trong Windows Secret Store.");
            }
            apiKey ??= string.Empty;

            string baseUrl = !string.IsNullOrWhiteSpace(Config.BaseUrl) ? Config.BaseUrl : provider.DefaultBaseUrl;
            string modelId = !string.IsNullOrWhiteSpace(Config.ModelId) ? Config.ModelId : provider.DefaultModelId;

            var (success, msg) = await provider.TestConnectionAsync(apiKey, baseUrl, modelId, ct);
            Config.LastTestedUtc = DateTime.UtcNow;
            Config.ConnectionStatus = success ? "CONNECTED" : (msg.Contains("401") || msg.Contains("403") ? "KEY_ERROR" : "ERROR");
            Config.Save();

            return (success, msg);
        }

        public async Task<(bool Success, string Response, string Message)> AnalyzeLogAsync(string logText, string promptContext = "", CancellationToken ct = default)
        {
            if (!Config.IsEnabled)
            {
                return (false, string.Empty, "AI API Lab đang tắt (Studio hoạt động ở chế độ hoàn toàn Offline).");
            }

            var provider = GetProvider();
            if (provider == null)
            {
                return (false, string.Empty, $"Không tìm thấy nhà cung cấp AI: '{Config.ProviderId}'.");
            }

            bool isLocal = provider.ProviderId.Equals("ollama_local", StringComparison.OrdinalIgnoreCase);
            string? apiKey = GetApiKey();
            if (!isLocal && string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, string.Empty, "Chưa cài đặt API Key trong Windows Secret Store (DPAPI).");
            }
            apiKey ??= string.Empty;

            string payload = logText;
            if (Config.RedactLogs)
            {
                payload = Redact(payload);
            }

            string fullPrompt = $"Bạn là trợ lý AI chuyên phân tích log và chẩn đoán game Unity trong UnityLocalizationStudio R&D Lab.\n" +
                                $"Bối cảnh: {promptContext}\n\n" +
                                $"Dưới đây là log runtime cần phân tích (đã lọc bỏ thông tin nhạy cảm):\n" +
                                $"```\n{payload}\n```\n\n" +
                                $"Hãy chỉ ra ngắn gọn:\n1. Nguyên nhân lỗi nếu có.\n2. Font hoặc chuỗi bị ảnh hưởng.\n3. Gợi ý phương án xử lý hook/material TMP.";

            string baseUrl = !string.IsNullOrWhiteSpace(Config.BaseUrl) ? Config.BaseUrl : provider.DefaultBaseUrl;
            string modelId = !string.IsNullOrWhiteSpace(Config.ModelId) ? Config.ModelId : provider.DefaultModelId;

            return await provider.AnalyzeAsync(fullPrompt, apiKey, baseUrl, modelId, ct);
        }
    }
}

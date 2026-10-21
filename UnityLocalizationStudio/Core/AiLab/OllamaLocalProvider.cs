using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class OllamaLocalProvider : IAiProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        public string ProviderId => "ollama_local";
        public string DisplayName => "Ollama Local (Offline / No Key)";
        public string DefaultBaseUrl => "http://127.0.0.1:11434";
        public string DefaultModelId => "qwen2.5-coder:7b";

        public async Task<(bool Success, string Message)> TestConnectionAsync(string apiKey, string baseUrl, string modelId, CancellationToken ct = default)
        {
            string rootUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            string model = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId.Trim();
            string tagsUrl = $"{rootUrl}/api/tags";

            try
            {
                using var response = await _httpClient.GetAsync(tagsUrl, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Lỗi kết nối Ollama: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                string json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                bool modelFound = false;

                if (doc.RootElement.TryGetProperty("models", out var modelsElem) && modelsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in modelsElem.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var nameProp))
                        {
                            string? mName = nameProp.GetString();
                            if (!string.IsNullOrEmpty(mName) && mName.Contains(model, StringComparison.OrdinalIgnoreCase))
                            {
                                modelFound = true;
                                break;
                            }
                        }
                    }
                }

                if (modelFound)
                {
                    return (true, $"Kết nối Ollama Local thành công! Model '{model}' đã sẵn sàng trên GPU.");
                }

                return (true, $"Kết nối Ollama Local thành công tại {rootUrl} (Cảnh báo: Model '{model}' chưa được pull, vui lòng kiểm tra).");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Không thể kết nối đến Ollama tại {rootUrl}. Hãy chắc chắn tiến trình Ollama đang chạy ({ex.Message}).");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kiểm tra Ollama Local: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Response, string Message)> AnalyzeAsync(string prompt, string apiKey, string baseUrl, string modelId, CancellationToken ct = default)
        {
            string rootUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            string model = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId.Trim();
            string generateUrl = $"{rootUrl}/api/generate";

            var payload = new
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    num_ctx = 8192,
                    temperature = 0.2
                }
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(payload);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync(generateUrl, content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    string errText = await response.Content.ReadAsStringAsync(ct);
                    return (false, string.Empty, $"Ollama HTTP {(int)response.StatusCode}: {errText}");
                }

                string resJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(resJson);
                if (doc.RootElement.TryGetProperty("response", out var respProp))
                {
                    string? generatedText = respProp.GetString();
                    return (true, generatedText ?? string.Empty, "Ollama xử lý hoàn tất thành công.");
                }

                return (false, string.Empty, "Phản hồi từ Ollama không chứa trường 'response'.");
            }
            catch (OperationCanceledException)
            {
                return (false, string.Empty, "Tác vụ gọi Ollama đã bị hủy do timeout.");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Lỗi giao tiếp Ollama Local: {ex.Message}");
            }
        }

        public static async Task<(bool Running, string ModelName, long VramBytes, int ContextLength)> GetResidentTelemetryAsync(string? baseUrl = null, CancellationToken ct = default)
        {
            string rootUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://127.0.0.1:11434" : baseUrl.TrimEnd('/');
            string psUrl = $"{rootUrl}/api/ps";

            try
            {
                using var response = await _httpClient.GetAsync(psUrl, ct);
                if (!response.IsSuccessStatusCode) return (false, "N/A", 0, 0);

                string json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in models.EnumerateArray())
                    {
                        string name = m.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                        long vram = m.TryGetProperty("size_vram", out var vp) ? vp.GetInt64() : 0;
                        int ctx = m.TryGetProperty("context_length", out var cp) ? cp.GetInt32() : 0;
                        return (true, name, vram, ctx);
                    }
                }
            }
            catch { }

            return (false, "N/A", 0, 0);
        }
    }
}

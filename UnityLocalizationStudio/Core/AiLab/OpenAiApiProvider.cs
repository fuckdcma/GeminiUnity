using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class OpenAiApiProvider : IAiProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        public string ProviderId => "openai_standard";
        public string DisplayName => "OpenAI API / Custom Compatible";
        public string DefaultBaseUrl => "https://api.openai.com/v1";
        public string DefaultModelId => "gpt-4o-mini";

        public async Task<(bool Success, string Message)> TestConnectionAsync(string apiKey, string baseUrl, string modelId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "Lỗi: API Key đang để trống. Vui lòng nhập API Key.");
            }

            string model = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId.Trim();
            string rootUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            string requestUrl = $"{rootUrl}/chat/completions";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = "Ping test connection. Respond with 'PONG'." }
                },
                max_tokens = 5
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"Kết nối OpenAI / Custom API thành công! (Model: {model})");
                }

                int statusCode = (int)response.StatusCode;
                string errorBody = await response.Content.ReadAsStringAsync(ct);

                return statusCode switch
                {
                    401 => (false, "Lỗi xác thực (401): API Key không hợp lệ hoặc đã bị thu hồi."),
                    404 => (false, $"Lỗi 404: Endpoint hoặc Model '{model}' không tồn tại tại {rootUrl}."),
                    429 => (false, "Lỗi 429: Hết hạn mức gọi API hoặc bị giới hạn tốc độ (Rate Limit / Insufficient Quota)."),
                    _ => (false, $"Lỗi HTTP {statusCode}: {errorBody}")
                };
            }
            catch (OperationCanceledException)
            {
                return (false, "Yêu cầu kiểm tra kết nối đã bị hủy.");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Lỗi mạng hoặc kết nối endpoint không thành công: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kiểm tra kết nối: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Response, string Message)> AnalyzeAsync(string prompt, string apiKey, string baseUrl, string modelId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "", "Chưa có API key.");
            }

            string model = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId.Trim();
            string rootUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            string requestUrl = $"{rootUrl}/chat/completions";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, "", $"Lỗi API (HTTP {(int)response.StatusCode}): {response.ReasonPhrase}");
                }

                string responseJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseJson);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var text = choices[0].GetProperty("message").GetProperty("content").GetString();
                    return (true, text ?? "", "Phân tích thành công.");
                }

                return (false, "", "Không nhận được phản hồi hợp lệ.");
            }
            catch (Exception ex)
            {
                return (false, "", $"Lỗi phân tích: {ex.Message}");
            }
        }
    }
}

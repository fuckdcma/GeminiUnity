using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class GeminiApiProvider : IAiProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        public string ProviderId => "google_gemini";
        public string DisplayName => "Google Gemini API";
        public string DefaultBaseUrl => "https://generativelanguage.googleapis.com";
        public string DefaultModelId => "gemini-1.5-flash";

        public async Task<(bool Success, string Message)> TestConnectionAsync(string apiKey, string baseUrl, string modelId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "Lỗi: API Key đang để trống. Vui lòng nhập API Key.");
            }

            string model = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId.Trim();
            string rootUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            string requestUrl = $"{rootUrl}/v1beta/models/{model}:generateContent?key={apiKey.Trim()}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Ping connection test for Unity Localization Studio. Reply 'PONG'." }
                        }
                    }
                }
            };

            try
            {
                string jsonBody = JsonSerializer.Serialize(payload);
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(requestUrl, content, ct);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"Kết nối Gemini API thành công! (Model: {model})");
                }

                int statusCode = (int)response.StatusCode;
                string errorBody = await response.Content.ReadAsStringAsync(ct);

                return statusCode switch
                {
                    400 => (false, $"Lỗi 400 (Yêu cầu không hợp lệ hoặc sai tên model '{model}'): {errorBody}"),
                    401 or 403 => (false, "Lỗi xác thực: API Key không chính xác hoặc không có quyền truy cập Google Gemini API."),
                    404 => (false, $"Lỗi 404: Không tìm thấy model '{model}' tại endpoint {rootUrl}."),
                    429 => (false, "Lỗi 429: Hạn mức gọi API đã bị vượt quá (Rate Limit / Quota Exceeded)."),
                    500 or 503 => (false, $"Lỗi máy chủ Google ({statusCode}): Dịch vụ tạm thời bận."),
                    _ => (false, $"Lỗi HTTP {statusCode}: {errorBody}")
                };
            }
            catch (OperationCanceledException)
            {
                return (false, "Yêu cầu kiểm tra kết nối đã bị hủy bởi người dùng.");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Lỗi mạng hoặc kết nối internet không khả dụng: {ex.Message}");
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
            string requestUrl = $"{rootUrl}/v1beta/models/{model}:generateContent?key={apiKey.Trim()}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            try
            {
                string jsonBody = JsonSerializer.Serialize(payload);
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(requestUrl, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "", $"Lỗi API (HTTP {(int)response.StatusCode}): {response.ReasonPhrase}");
                }

                string responseJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    return (true, text ?? "", "Phân tích thành công.");
                }

                return (false, "", "Không nhận được nội dung phản hồi từ model.");
            }
            catch (Exception ex)
            {
                return (false, "", $"Lỗi thực thi phân tích: {ex.Message}");
            }
        }
    }
}

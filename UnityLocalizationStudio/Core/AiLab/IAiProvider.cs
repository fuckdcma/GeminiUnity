using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab
{
    public interface IAiProvider
    {
        string ProviderId { get; }
        string DisplayName { get; }
        string DefaultBaseUrl { get; }
        string DefaultModelId { get; }

        Task<(bool Success, string Message)> TestConnectionAsync(string apiKey, string baseUrl, string modelId, CancellationToken ct = default);
        Task<(bool Success, string Response, string Message)> AnalyzeAsync(string prompt, string apiKey, string baseUrl, string modelId, CancellationToken ct = default);
    }
}

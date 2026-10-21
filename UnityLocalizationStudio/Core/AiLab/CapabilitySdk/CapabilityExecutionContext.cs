using System;
using System.Collections.Generic;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk
{
    public class CapabilityExecutionContext
    {
        public Dictionary<string, object> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Action<string>? Logger { get; set; }
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString("N");
        public string? WorkspacePath { get; set; }

        public T? GetParameter<T>(string key, T? defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var val))
            {
                if (val is T typedVal) return typedVal;
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void Log(string message) => Logger?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    public class CapabilityExecutionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Outputs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public long DurationMs { get; set; }
        public string? ContentHash { get; set; }

        public static CapabilityExecutionResult Ok(Dictionary<string, object>? outputs = null, string? hash = null, long durationMs = 0)
        {
            return new CapabilityExecutionResult
            {
                Success = true,
                Outputs = outputs ?? new(StringComparer.OrdinalIgnoreCase),
                ContentHash = hash,
                DurationMs = durationMs
            };
        }

        public static CapabilityExecutionResult Fail(string error, long durationMs = 0)
        {
            return new CapabilityExecutionResult
            {
                Success = false,
                ErrorMessage = error,
                DurationMs = durationMs
            };
        }

        public T? GetOutput<T>(string key, T? defaultValue = default)
        {
            if (Outputs.TryGetValue(key, out var val))
            {
                if (val is T typedVal) return typedVal;
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}

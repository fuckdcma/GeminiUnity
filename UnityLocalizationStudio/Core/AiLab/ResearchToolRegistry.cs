using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class ToolExecutionResult
    {
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
    }

    public interface IResearchTool
    {
        string Name { get; }
        string Description { get; }
        int TimeoutMs { get; }
        Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken ct = default);
    }

    public class ResearchToolRegistry
    {
        private static readonly Lazy<ResearchToolRegistry> _instance = new(() => new ResearchToolRegistry());
        public static ResearchToolRegistry Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, IResearchTool> _tools = new(StringComparer.OrdinalIgnoreCase);

        public ResearchToolRegistry()
        {
            RegisterDefaultTools();
        }

        public void Register(IResearchTool tool)
        {
            if (tool != null) _tools[tool.Name] = tool;
        }

        public IResearchTool? GetTool(string name)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }

        public IReadOnlyCollection<IResearchTool> GetAllTools() => _tools.Values.ToList().AsReadOnly();

        private void RegisterDefaultTools()
        {
            Register(new KnowledgeSearchTool());
            Register(new SandboxCompileTool());
            Register(new SyntheticTestTool());
            Register(new ShaderInspectTool());
        }
    }

    public class KnowledgeSearchTool : IResearchTool
    {
        public string Name => "KnowledgeSearchTool";
        public string Description => "Tìm kiếm tri thức đã kiểm định trong SQLite Knowledge Base (FTS5).";
        public int TimeoutMs => 5000;

        public Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string query = parameters.TryGetValue("query", out var q) ? q?.ToString() ?? "" : "";
            try
            {
                var db = new KnowledgeDatabase();
                var results = db.SearchKnowledgeFts(query);
                sw.Stop();
                return Task.FromResult(new ToolExecutionResult
                {
                    Success = true,
                    Output = $"Tìm thấy {results.Count} mục tri thức phù hợp với từ khóa '{query}'.",
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Task.FromResult(new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
        }
    }

    public class SandboxCompileTool : IResearchTool
    {
        public string Name => "SandboxCompileTool";
        public string Description => "Biên dịch mã C# trong môi trường sandbox cô lập ngoài tiến trình.";
        public int TimeoutMs => 30000;

        public async Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string expId = parameters.TryGetValue("expId", out var id) ? id?.ToString() ?? "EXP" : "EXP";
            string moduleCode = parameters.TryGetValue("moduleCode", out var mc) ? mc?.ToString() ?? "" : "";
            string testCode = parameters.TryGetValue("testCode", out var tc) ? tc?.ToString() ?? "" : "";

            var res = await LocalResearchHost.RunExperimentIsolatedAsync(expId, moduleCode, testCode, TimeoutMs, 10000, ct);
            sw.Stop();

            return new ToolExecutionResult
            {
                Success = res.CompilePassed,
                Output = res.CompileOutput,
                ErrorMessage = res.CompilePassed ? null : res.FailureReason,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    public class SyntheticTestTool : IResearchTool
    {
        public string Name => "SyntheticTestTool";
        public string Description => "Thực thi test harness trong sandbox với giám sát Watchdog Timeout.";
        public int TimeoutMs => 20000;

        public async Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string expId = parameters.TryGetValue("expId", out var id) ? id?.ToString() ?? "EXP" : "EXP";
            string moduleCode = parameters.TryGetValue("moduleCode", out var mc) ? mc?.ToString() ?? "" : "";
            string testCode = parameters.TryGetValue("testCode", out var tc) ? tc?.ToString() ?? "" : "";

            var res = await LocalResearchHost.RunExperimentIsolatedAsync(expId, moduleCode, testCode, 20000, TimeoutMs, ct);
            sw.Stop();

            return new ToolExecutionResult
            {
                Success = res.Success,
                Output = res.TestOutput,
                ErrorMessage = res.Success ? null : res.FailureReason,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    public class ShaderInspectTool : IResearchTool
    {
        public string Name => "ShaderInspectTool";
        public string Description => "Kiểm tra và chuẩn hóa cấu trúc TextMeshPro Distance Field Material.";
        public int TimeoutMs => 5000;

        public Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string matJson = parameters.TryGetValue("materialJson", out var m) ? m?.ToString() ?? "" : "";

            var cap = new CapabilitySdk.Builtin.TMPDistanceFieldShaderPatchCapability();
            var ctx = new CapabilitySdk.CapabilityExecutionContext();
            ctx.Parameters["action"] = "ANALYZE_MATERIAL";
            ctx.Parameters["materialJson"] = matJson;

            var res = cap.Execute(ctx);
            sw.Stop();

            return Task.FromResult(new ToolExecutionResult
            {
                Success = res.Success,
                Output = res.Success ? "Phân tích Shader Material thành công." : null,
                ErrorMessage = res.ErrorMessage,
                DurationMs = sw.ElapsedMilliseconds
            });
        }
    }
}

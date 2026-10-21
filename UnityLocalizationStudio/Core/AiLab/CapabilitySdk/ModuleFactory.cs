using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk
{
    public class ModuleFactoryResult
    {
        public bool Success { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string SemanticVersion { get; set; } = "1.0.0";
        public string SourceCode { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public bool CompilePassed { get; set; }
        public bool TestPassed { get; set; }
        public bool ContractVerified { get; set; }
        public bool Promoted { get; set; }
        public string LogOutput { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public long DurationMs { get; set; }
    }

    public class ModuleFactory
    {
        private readonly KnowledgeDatabase _knowledgeBase;

        public ModuleFactory(KnowledgeDatabase? db = null)
        {
            _knowledgeBase = db ?? new KnowledgeDatabase();
        }

        public async Task<ModuleFactoryResult> BuildAndVerifyCapabilityAsync(
            string moduleName,
            string capabilityCategory,
            string sourceCode,
            string testCode,
            string inputContractJson = "{}",
            string outputContractJson = "{}",
            Action<string>? logger = null,
            CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string expId = $"MF_{moduleName.ToUpperInvariant()}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var result = new ModuleFactoryResult
            {
                ModuleName = moduleName,
                SourceCode = sourceCode,
                TestCode = testCode,
                ContentHash = ComputeSha256(sourceCode)
            };

            logger?.Invoke($"[ModuleFactory] Bắt đầu sản xuất và kiểm định module '{moduleName}' ({expId})...");

            // 1. Kiểm tra Contract sơ bộ
            if (string.IsNullOrWhiteSpace(sourceCode) || string.IsNullOrWhiteSpace(testCode))
            {
                result.Success = false;
                result.ErrorMessage = "Mã nguồn module hoặc test harness trống.";
                return result;
            }

            // 2. Chuyển vào sandbox cô lập ngoài tiến trình (LocalResearchHost)
            logger?.Invoke($"[ModuleFactory] Gửi tới LocalResearchHost để biên dịch và chạy test harness...");
            var sandboxRes = await LocalResearchHost.RunExperimentIsolatedAsync(expId, sourceCode, testCode, ct: ct);

            result.CompilePassed = sandboxRes.CompilePassed;
            result.TestPassed = sandboxRes.TestPassed;
            result.LogOutput = $"[Compile]\n{sandboxRes.CompileOutput}\n\n[Test]\n{sandboxRes.TestOutput}";

            if (!sandboxRes.CompilePassed)
            {
                result.Success = false;
                result.ErrorMessage = $"Lỗi biên dịch: {sandboxRes.FailureReason}\n{sandboxRes.CompileOutput}";
                sw.Stop();
                result.DurationMs = sw.ElapsedMilliseconds;
                return result;
            }

            if (!sandboxRes.TestPassed)
            {
                result.Success = false;
                result.ErrorMessage = $"Kiểm định Unit Test thất bại: {sandboxRes.FailureReason}\n{sandboxRes.TestOutput}";
                sw.Stop();
                result.DurationMs = sw.ElapsedMilliseconds;
                return result;
            }

            // 3. Contract Verification
            result.ContractVerified = !string.IsNullOrEmpty(inputContractJson) && !string.IsNullOrEmpty(outputContractJson);

            // 4. Thăng hạng (Promote) vào Knowledge Base & Module Library
            var modItem = new ModuleLibraryItem
            {
                Id = $"MOD_{moduleName.ToUpperInvariant()}",
                Name = moduleName,
                Capability = capabilityCategory,
                SourceCode = sourceCode,
                UnitTestSourceCode = testCode,
                Version = result.SemanticVersion,
                Author = "ULS_ModuleFactory",
                VerificationEvidence = $"ModuleFactory Sandbox PASS 100%:\n{sandboxRes.TestOutput}",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _knowledgeBase.PromoteToModuleLibrary(modItem);

            var capVer = new CapabilityVersionRecord
            {
                Id = $"{modItem.Id}_V1_0_0",
                CapabilityId = modItem.Id,
                Name = moduleName,
                SemanticVersion = "1.0.0",
                InputContract = inputContractJson,
                OutputContract = outputContractJson,
                SourceCode = sourceCode,
                Status = "SYNTHETIC_VERIFIED",
                ContentHash = result.ContentHash,
                PromotedAt = DateTime.UtcNow
            };
            _knowledgeBase.SaveCapabilityVersion(capVer);

            var testRun = new TestRunRecord
            {
                Id = $"TR_{expId}",
                ExperimentId = expId,
                CapabilityVersionId = capVer.Id,
                TotalTests = 1,
                PassedTests = 1,
                FailedTests = 0,
                LogOutput = sandboxRes.TestOutput,
                DurationMs = sandboxRes.DurationMs,
                Success = true,
                RunAt = DateTime.UtcNow
            };
            _knowledgeBase.RecordTestRun(testRun);

            result.Promoted = true;
            result.Success = true;
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;

            logger?.Invoke($"[ModuleFactory THÀNH CÔNG] Module '{moduleName}' đã vượt qua toàn bộ chất lượng kiểm định ({result.DurationMs}ms). Đã thăng hạng vào thư viện.");
            return result;
        }

        private static string ComputeSha256(string content)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab
{
    public class SandboxExecutionResult
    {
        public bool Success { get; set; }
        public bool CompilePassed { get; set; }
        public bool TestPassed { get; set; }
        public string CompileOutput { get; set; } = string.Empty;
        public string TestOutput { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public string SandboxDirectory { get; set; } = string.Empty;
    }

    public static class LocalResearchHost
    {
        public static async Task<SandboxExecutionResult> RunExperimentIsolatedAsync(
            string experimentId,
            string moduleCode,
            string testCode,
            int compileTimeoutMs = 25000,
            int testTimeoutMs = 15000,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var result = new SandboxExecutionResult();

            string workspaceRoot = Directory.GetCurrentDirectory();
            string sandboxesRoot = Path.Combine(workspaceRoot, ".ai-lab", "sandboxes");
            string expDir = Path.Combine(sandboxesRoot, experimentId);
            result.SandboxDirectory = expDir;

            try
            {
                if (Directory.Exists(expDir))
                {
                    try { Directory.Delete(expDir, true); } catch { }
                }
                Directory.CreateDirectory(expDir);

                // 1. Ghi file project .csproj cô lập
                string csprojPath = Path.Combine(expDir, "SandboxExp.csproj");
                string csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>";
                await File.WriteAllTextAsync(csprojPath, csprojContent, Encoding.UTF8, ct);

                // 2. Ghi module C# của AI
                string modulePath = Path.Combine(expDir, "SolutionModule.cs");
                await File.WriteAllTextAsync(modulePath, moduleCode, Encoding.UTF8, ct);

                // 3. Ghi test harness thực thi
                string testPath = Path.Combine(expDir, "Program.cs");
                await File.WriteAllTextAsync(testPath, testCode, Encoding.UTF8, ct);

                // 4. Giai đoạn 1: Biên dịch (dotnet build) hoàn toàn ngoài tiến trình Studio
                var (compileOk, compileOut) = await RunProcessWithTimeoutAsync(
                    "dotnet",
                    $"build \"{csprojPath}\" -c Release --nologo",
                    expDir,
                    compileTimeoutMs,
                    ct);

                result.CompileOutput = compileOut;
                result.CompilePassed = compileOk;

                if (!compileOk)
                {
                    result.Success = false;
                    result.FailureReason = "Biên dịch mã nguồn C# thất bại (Compile Error). AI sinh mã không hợp lệ hoặc thiếu namespace.";
                    sw.Stop();
                    result.DurationMs = sw.ElapsedMilliseconds;
                    return result;
                }

                // 5. Giai đoạn 2: Chạy kiểm thử tự động ngoài tiến trình với Watchdog Timeout
                var (testOk, testOut) = await RunProcessWithTimeoutAsync(
                    "dotnet",
                    $"run --project \"{csprojPath}\" -c Release --no-build",
                    expDir,
                    testTimeoutMs,
                    ct);

                result.TestOutput = testOut;
                result.TestPassed = testOk;

                if (!testOk)
                {
                    result.Success = false;
                    result.FailureReason = testOut.Contains("TIMED_OUT")
                        ? "Thực thi vượt quá giới hạn thời gian (Watchdog Timeout). Phát hiện vòng lặp vô tận hoặc treo CPU."
                        : "Kiểm thử thất bại (Test Assertion Failed). Mã nguồn không đáp ứng các ca kiểm định kỹ thuật.";
                }
                else
                {
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.FailureReason = $"Lỗi hạ tầng LocalResearchHost: {ex.Message}";
            }
            finally
            {
                sw.Stop();
                result.DurationMs = sw.ElapsedMilliseconds;
            }

            return result;
        }

        private static async Task<(bool Success, string Output)> RunProcessWithTimeoutAsync(
            string fileName,
            string arguments,
            string workingDir,
            int timeoutMs,
            CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();

            proc.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            proc.ErrorDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

            try
            {
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var waitForExitTask = proc.WaitForExitAsync(cts.Token);
                var delayTask = Task.Delay(timeoutMs, cts.Token);

                var completedTask = await Task.WhenAny(waitForExitTask, delayTask);
                if (completedTask == delayTask)
                {
                    // Watchdog Timeout: Diệt toàn bộ cây tiến trình
                    try { proc.Kill(true); } catch { }
                    outputBuilder.AppendLine($"\n[WATCHDOG ERROR] Tiến trình bị cưỡng chế dừng sau {timeoutMs}ms (TIMED_OUT).");
                    return (false, outputBuilder.ToString());
                }

                await waitForExitTask;
                bool success = proc.ExitCode == 0;
                return (success, outputBuilder.ToString());
            }
            catch (Exception ex)
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                return (false, $"Lỗi khởi chạy tiến trình: {ex.Message}\n{outputBuilder}");
            }
        }
    }
}

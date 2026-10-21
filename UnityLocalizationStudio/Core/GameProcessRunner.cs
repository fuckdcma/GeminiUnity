using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core
{
    public static class GameProcessRunner
    {
        private static Process? _activeGameProcess;
        private static int? _lastKnownPid;
        private static bool _wasRunning = false;
        private static bool _justExitedPending = false;
        private static string _targetProcessName = "PlateUp";

        public static Process? ActiveGameProcess => _activeGameProcess;
        public static bool IsGameRunning => CheckIsRunning();
        public static int? CurrentGamePid => IsGameRunning ? _lastKnownPid : null;

        public static event Action<int>? OnGameStarted;
        public static event Action? OnGameExited;

        public static bool CheckIsRunning(string? exeName = null)
        {
            if (!string.IsNullOrEmpty(exeName))
            {
                _targetProcessName = Path.GetFileNameWithoutExtension(exeName);
            }

            // 1. Kiểm tra _activeGameProcess nếu có
            if (_activeGameProcess != null)
            {
                try
                {
                    _activeGameProcess.Refresh();
                    if (!_activeGameProcess.HasExited)
                    {
                        _lastKnownPid = _activeGameProcess.Id;
                        return true;
                    }
                }
                catch
                {
                    // Tiến trình đã thoát hoặc không thể truy vấn
                }
                _activeGameProcess = null;
            }

            // 2. Tìm kiếm trong danh sách process của hệ điều hành
            try
            {
                var procs = Process.GetProcessesByName(_targetProcessName);
                foreach (var p in procs)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            _activeGameProcess = p;
                            _lastKnownPid = p.Id;
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }

        public static Process? FindRunningGameProcess(string? gameDirectory = null, string? exeName = null)
        {
            if (CheckIsRunning(exeName))
            {
                return _activeGameProcess;
            }
            return null;
        }

        public static (bool IsRunning, int? Pid, bool JustExited) CheckGameStatus(string? gameDirectory = null, string? exeName = null)
        {
            bool isRunning = CheckIsRunning(exeName);
            bool justExited = false;

            if (_justExitedPending || (_wasRunning && !isRunning))
            {
                justExited = true;
                _justExitedPending = false;
                _wasRunning = false;
                _activeGameProcess = null;
                try { OnGameExited?.Invoke(); } catch { }
            }
            else if (isRunning)
            {
                _wasRunning = true;
            }

            int? pid = isRunning ? _lastKnownPid : (_lastKnownPid ?? null);
            return (isRunning, pid, justExited);
        }

        public static void SetModMode(string gameDirectory, string mode, Action<string>? logger = null)
        {
            try
            {
                string pluginDir = Path.Combine(gameDirectory, "BepInEx", "plugins", "VietnameseLocalization");
                if (!Directory.Exists(pluginDir))
                {
                    Directory.CreateDirectory(pluginDir);
                }
                string modeFile = Path.Combine(pluginDir, "mod_mode.json");
                string json = "{\n  \"Mode\": \"" + mode + "\",\n  \"Timestamp\": \"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"\n}\n";
                File.WriteAllText(modeFile, json, System.Text.Encoding.UTF8);
                logger?.Invoke($"[GameRunner] Đã thiết lập chế độ Mod: {mode} ({Path.GetFileName(modeFile)})");
            }
            catch (Exception ex)
            {
                logger?.Invoke($"[GameRunner] Lỗi ghi mod_mode.json: {ex.Message}");
            }
        }

        public static (bool Success, string Message, Process? Process) LaunchGame(
            string gameDirectory,
            string exeName,
            Action<string>? logger = null)
        {
            if (!Directory.Exists(gameDirectory))
            {
                return (false, $"Thư mục game không tồn tại: {gameDirectory}", null);
            }

            string exePath = Path.Combine(gameDirectory, exeName);
            if (!File.Exists(exePath))
            {
                // 1. Kiểm tra các thư mục con (trường hợp chọn thư mục Steam cha chứa subfolder cài đặt game)
                try
                {
                    string[] subMatches = Directory.GetFiles(gameDirectory, exeName, SearchOption.AllDirectories);
                    var validMatch = Array.Find(subMatches, e => !e.Contains("UnityCrashHandler") && !e.Contains("unins"));
                    if (validMatch != null)
                    {
                        exePath = validMatch;
                        gameDirectory = Path.GetDirectoryName(validMatch) ?? gameDirectory;
                    }
                }
                catch { }

                // 2. Nếu vẫn không thấy, tìm tệp .exe bất kỳ trong thư mục hoặc thư mục con
                if (!File.Exists(exePath))
                {
                    try
                    {
                        string[] exes = Directory.GetFiles(gameDirectory, "*.exe", SearchOption.AllDirectories);
                        var candidate = Array.Find(exes, e => !e.Contains("UnityCrashHandler") && !e.Contains("unins"));
                        if (candidate != null)
                        {
                            exePath = candidate;
                            gameDirectory = Path.GetDirectoryName(candidate) ?? gameDirectory;
                        }
                        else
                        {
                            return (false, $"Không tìm thấy tệp thực thi {exeName} tại: {gameDirectory}", null);
                        }
                    }
                    catch
                    {
                        return (false, $"Không tìm thấy tệp thực thi {exeName} tại: {gameDirectory}", null);
                    }
                }
            }

            if (!string.IsNullOrEmpty(exeName))
            {
                _targetProcessName = Path.GetFileNameWithoutExtension(exeName);
            }

            try
            {
                string workingDir = Path.GetDirectoryName(exePath) ?? gameDirectory;
                logger?.Invoke($"[GameRunner] Đang khởi chạy: {Path.GetFileName(exePath)} (Thư mục: {workingDir})...");
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = workingDir,
                    UseShellExecute = true
                };

                _activeGameProcess = Process.Start(psi);
                if (_activeGameProcess != null)
                {
                    _lastKnownPid = _activeGameProcess.Id;
                    _wasRunning = true;
                    _justExitedPending = false;
                    try
                    {
                        _activeGameProcess.EnableRaisingEvents = true;
                        _activeGameProcess.Exited += (s, e) =>
                        {
                            _justExitedPending = true;
                            try { OnGameExited?.Invoke(); } catch { }
                        };
                    }
                    catch { }

                    OnGameStarted?.Invoke(_activeGameProcess.Id);
                    logger?.Invoke($"[GameRunner] Game đã khởi động thành công (PID: {_activeGameProcess.Id}). Đang lắng nghe Telemetry...");
                    return (true, $"Đã bật game (PID: {_activeGameProcess.Id}).", _activeGameProcess);
                }
                return (false, "Không thể bắt đầu tiến trình game.", null);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi bật game: {ex.Message}", null);
            }
        }

        public static void StopGame(Action<string>? logger = null)
        {
            try
            {
                if (_activeGameProcess != null)
                {
                    try
                    {
                        if (!_activeGameProcess.HasExited)
                        {
                            logger?.Invoke($"[GameRunner] Đang dừng tiến trình game PID {_activeGameProcess.Id}...");
                            _activeGameProcess.Kill();
                            _activeGameProcess.WaitForExit(3000);
                        }
                    }
                    catch { }
                }

                var procs = Process.GetProcessesByName(_targetProcessName);
                foreach (var p in procs)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                _activeGameProcess = null;
                _wasRunning = false;
                _justExitedPending = true;
                logger?.Invoke("[GameRunner] Game đã tắt.");
                try { OnGameExited?.Invoke(); } catch { }
            }
        }

        public static async Task<bool> WaitForTelemetryAsync(string telemetryJsonPath, int timeoutSeconds = 30, Action<string>? logger = null)
        {
            logger?.Invoke($"[GameRunner] Đang chờ tín hiệu Live Telemetry tại: {Path.GetFileName(telemetryJsonPath)}...");
            int waited = 0;
            while (waited < timeoutSeconds)
            {
                if (File.Exists(telemetryJsonPath))
                {
                    try
                    {
                        var info = new FileInfo(telemetryJsonPath);
                        if (info.Length > 20)
                        {
                            logger?.Invoke($"[GameRunner] Đã nhận tín hiệu Telemetry hợp lệ ({info.Length} bytes)!");
                            return true;
                        }
                    }
                    catch { }
                }
                await Task.Delay(1000);
                waited++;
            }
            logger?.Invoke("[GameRunner] Hết thời gian chờ Telemetry (Timeout). Game tự động capture khi mở màn hình.");
            return false;
        }
    }
}

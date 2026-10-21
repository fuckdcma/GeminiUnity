using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class GameDetector
    {
        public static GameInfo DetectGame(string gameRootPath)
        {
            if (string.IsNullOrWhiteSpace(gameRootPath) || !Directory.Exists(gameRootPath))
            {
                throw new DirectoryNotFoundException($"Không tìm thấy thư mục game: {gameRootPath}");
            }

            var info = new GameInfo
            {
                GameRootPath = Path.GetFullPath(gameRootPath)
            };

            // 1. Tìm thư mục [GameName]_Data
            var dataDirs = Directory.GetDirectories(info.GameRootPath, "*_Data", SearchOption.TopDirectoryOnly);
            if (dataDirs.Length == 0)
            {
                // Quét tìm trong thư mục con cấp 1 (ví dụ chọn thư mục Steam cha chứa subfolder cài đặt game)
                try
                {
                    foreach (var sub in Directory.GetDirectories(info.GameRootPath))
                    {
                        var candidate = Directory.GetDirectories(sub, "*_Data", SearchOption.TopDirectoryOnly);
                        if (candidate.Length > 0)
                        {
                            info.GameRootPath = Path.GetFullPath(sub);
                            dataDirs = candidate;
                            break;
                        }
                    }
                }
                catch { }
            }

            if (dataDirs.Length == 0)
            {
                throw new FileNotFoundException($"Không tìm thấy thư mục dữ liệu Unity (*_Data) trong '{gameRootPath}'. Hãy đảm bảo bạn chọn đúng thư mục chứa tệp .exe và thư mục dữ liệu (*_Data) của game.");
            }

            info.DataDirectory = dataDirs[0];
            string dataDirName = Path.GetFileName(info.DataDirectory);
            string baseGameName = dataDirName.Substring(0, dataDirName.Length - 5); // Bỏ '_Data'

            // 2. Tìm tệp thực thi chính (.exe)
            string candidateExe = Path.Combine(info.GameRootPath, baseGameName + ".exe");
            if (File.Exists(candidateExe))
            {
                info.ExePath = candidateExe;
            }
            else
            {
                var exeFiles = Directory.GetFiles(info.GameRootPath, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).StartsWith("UnityCrashHandler", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (exeFiles.Count > 0)
                {
                    info.ExePath = exeFiles[0];
                }
                else
                {
                    throw new FileNotFoundException("Không tìm thấy tệp .exe thực thi của game trong thư mục.");
                }
            }

            info.ExeName = Path.GetFileName(info.ExePath);

            // 3. Phân tích phiên bản Unity từ ProductVersion
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(info.ExePath);
                string? prodVersion = versionInfo.ProductVersion;
                if (!string.IsNullOrEmpty(prodVersion))
                {
                    var match = Regex.Match(prodVersion, @"\b(20\d{2}\.\d+\.\d+[a-z0-9]*)|\b(5\.\d+\.\d+)|\b(6000\.\d+\.\d+)");
                    if (match.Success)
                    {
                        info.UnityVersion = match.Value;
                    }
                    else
                    {
                        info.UnityVersion = prodVersion;
                    }
                }
            }
            catch
            {
                // Fallback nếu không đọc được từ Exe
            }

            // Nếu chưa tìm được phiên bản, kiểm tra UnityPlayer.dll
            if (string.IsNullOrEmpty(info.UnityVersion))
            {
                string unityPlayerDll = Path.Combine(info.GameRootPath, "UnityPlayer.dll");
                if (File.Exists(unityPlayerDll))
                {
                    var vi = FileVersionInfo.GetVersionInfo(unityPlayerDll);
                    info.UnityVersion = vi.ProductVersion ?? vi.FileVersion ?? "Unknown";
                }
            }

            // 4. Xác định kiến trúc Mono vs IL2CPP
            string managedDir = Path.Combine(info.DataDirectory, "Managed");
            string monoBleedingEdge = Path.Combine(info.GameRootPath, "MonoBleedingEdge");
            string gameAssembly = Path.Combine(info.GameRootPath, "GameAssembly.dll");
            string il2cppMeta = Path.Combine(info.DataDirectory, "il2cpp_data", "Metadata", "global-metadata.dat");

            if (File.Exists(gameAssembly) || File.Exists(il2cppMeta))
            {
                info.Architecture = GameArchitecture.IL2CPP;
            }
            else if (Directory.Exists(managedDir) || Directory.Exists(monoBleedingEdge))
            {
                info.Architecture = GameArchitecture.Mono;
            }
            else
            {
                info.Architecture = GameArchitecture.Unknown;
            }

            // 5. Kiểm tra Text Engine và danh sách Managed Assemblies
            if (Directory.Exists(managedDir))
            {
                var dllFiles = Directory.GetFiles(managedDir, "*.dll", SearchOption.TopDirectoryOnly);
                info.ManagedAssemblies = dllFiles.Select(Path.GetFileName).Where(f => !string.IsNullOrEmpty(f)).Select(f => f!).ToList();

                info.HasTextMeshPro = dllFiles.Any(f => Path.GetFileName(f).Equals("Unity.TextMeshPro.dll", StringComparison.OrdinalIgnoreCase));
                info.HasLegacyText = dllFiles.Any(f => Path.GetFileName(f).Equals("UnityEngine.UI.dll", StringComparison.OrdinalIgnoreCase));
            }

            // 6. Quét các file assets và resources
            var assetFiles = Directory.GetFiles(info.DataDirectory, "*.assets", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(info.DataDirectory, "resources.*", SearchOption.TopDirectoryOnly));
            info.AssetFiles = assetFiles.Select(Path.GetFileName).Where(f => !string.IsNullOrEmpty(f)).Select(f => f!).ToList();

            return info;
        }
    }
}

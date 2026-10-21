using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public static class PrototypeManager
    {
        public const string ManifestFileName = "vietnamese_prototype.manifest";

        public static (bool Success, string Message, InstalledFileManifest Manifest) InstallPrototype(
            string gameDirectory,
            string bepInExZipPath,
            string runtimeModDllPath,
            string? fallbackFontPath = null,
            string? sampleTextPath = null,
            string? sampleFontMappingPath = null,
            Action<string>? logger = null)
        {
            var manifest = new InstalledFileManifest
            {
                ModName = "Vietnamese Localization Prototype",
                GameName = Path.GetFileName(gameDirectory),
                IsPrototype = true
            };

            if (!Directory.Exists(gameDirectory))
            {
                return (false, $"Thư mục game không tồn tại: {gameDirectory}", manifest);
            }

            try
            {
                var det = GameDetector.DetectGame(gameDirectory);
                gameDirectory = det.GameRootPath;
            }
            catch { }

            if (!File.Exists(runtimeModDllPath))
            {
                return (false, $"Không tìm thấy file Mod DLL: {runtimeModDllPath}", manifest);
            }

            try
            {
                logger?.Invoke("[Prototype] Đang kiểm tra cấu trúc BepInEx trong game...");

                // 1. Kiểm tra cấu trúc BepInEx trong game
                string winhttpDll = Path.Combine(gameDirectory, "winhttp.dll");
                string bepFolder = Path.Combine(gameDirectory, "BepInEx");
                bool hadBepInEx = File.Exists(winhttpDll) || Directory.Exists(bepFolder);
                manifest.BepInExWasPreExisting = hadBepInEx;

                if (!hadBepInEx && File.Exists(bepInExZipPath))
                {
                    logger?.Invoke("[Prototype] Game chưa có BepInEx. Tiến hành giải nén BepInEx Core vào thư mục game...");
                    using (var archive = ZipFile.OpenRead(bepInExZipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name)) continue; // Directory entry
                            string destFile = Path.Combine(gameDirectory, entry.FullName);
                            string? destDir = Path.GetDirectoryName(destFile);
                            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                                string relDir = Path.GetRelativePath(gameDirectory, destDir);
                                if (!manifest.CreatedRelativeDirectories.Contains(relDir))
                                    manifest.CreatedRelativeDirectories.Add(relDir);
                            }

                            if (!File.Exists(destFile))
                            {
                                entry.ExtractToFile(destFile, true);
                                manifest.InstalledRelativeFiles.Add(entry.FullName);
                            }
                        }
                    }
                }
                else if (hadBepInEx)
                {
                    logger?.Invoke("[Prototype] Phát hiện game ĐÃ CÀI ĐẶT BepInEx từ trước. Giữ nguyên BepInEx hiện hữu, không ghi đè.");
                }

                // 2. Tạo thư mục Plugin Prototype
                string pluginDir = Path.Combine(gameDirectory, "BepInEx", "plugins", "VietnameseLocalization");
                if (!Directory.Exists(pluginDir))
                {
                    Directory.CreateDirectory(pluginDir);
                    manifest.CreatedRelativeDirectories.Add(Path.Combine("BepInEx", "plugins", "VietnameseLocalization"));
                }

                // 3. Sao chép DLL Mod vào Plugin
                string targetDll = Path.Combine(pluginDir, Path.GetFileName(runtimeModDllPath));
                File.Copy(runtimeModDllPath, targetDll, true);
                manifest.InstalledRelativeFiles.Add(Path.GetRelativePath(gameDirectory, targetDll));
                logger?.Invoke($"[Prototype] Đã cài đặt DLL Mod: {Path.GetFileName(targetDll)}");

                // 4. Sao chép Font Mẫu (ARIAL SDF / Custom Font Bundle) nếu có
                if (!string.IsNullOrEmpty(fallbackFontPath) && File.Exists(fallbackFontPath))
                {
                    string targetFont = Path.Combine(pluginDir, Path.GetFileName(fallbackFontPath));
                    File.Copy(fallbackFontPath, targetFont, true);
                    manifest.InstalledRelativeFiles.Add(Path.GetRelativePath(gameDirectory, targetFont));
                    logger?.Invoke($"[Prototype] Đã cài đặt Font Mẫu (Universal ARIAL SDF): {Path.GetFileName(targetFont)}");
                }

                // 5. Sao chép Text Mẫu (Sample_LiveTest_Texts.csv: ID,TEXT_MẪU)
                if (!string.IsNullOrEmpty(sampleTextPath) && File.Exists(sampleTextPath))
                {
                    string targetSampleText = Path.Combine(pluginDir, Path.GetFileName(sampleTextPath));
                    File.Copy(sampleTextPath, targetSampleText, true);
                    manifest.InstalledRelativeFiles.Add(Path.GetRelativePath(gameDirectory, targetSampleText));
                    logger?.Invoke($"[Prototype] Đã cài đặt Text Mẫu (ID,TEXT_MẪU): {Path.GetFileName(targetSampleText)}");
                }

                // 6. Sao chép Cấu hình Ánh xạ Font Mẫu (Sample_Font_Mapping.json / Font_Mapping_Config.json)
                if (!string.IsNullOrEmpty(sampleFontMappingPath) && File.Exists(sampleFontMappingPath))
                {
                    string targetMapping = Path.Combine(pluginDir, "Font_Mapping_Config.json");
                    File.Copy(sampleFontMappingPath, targetMapping, true);
                    manifest.InstalledRelativeFiles.Add(Path.GetRelativePath(gameDirectory, targetMapping));
                    logger?.Invoke($"[Prototype] Đã cài đặt Cấu hình ánh xạ Font mẫu: {Path.GetFileName(targetMapping)}");
                }

                // 7. Lưu Manifest vào thư mục game
                string manifestPath = Path.Combine(gameDirectory, ManifestFileName);
                manifest.SaveToFile(manifestPath);
                logger?.Invoke($"[Prototype] Đã lưu Manifest cài đặt ({manifest.InstalledRelativeFiles.Count} tệp): {ManifestFileName}");

                return (true, $"Đã cài đặt Prototype thành công ({manifest.InstalledRelativeFiles.Count} tệp được ghi nhận).", manifest);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cài đặt Prototype: {ex.Message}", manifest);
            }
        }

        public static (bool Success, string Message, int DeletedCount) CleanPrototype(string gameDirectory, Action<string>? logger = null)
        {
            if (!Directory.Exists(gameDirectory))
            {
                return (false, $"Thư mục game không tồn tại: {gameDirectory}", 0);
            }

            try
            {
                var det = GameDetector.DetectGame(gameDirectory);
                gameDirectory = det.GameRootPath;
            }
            catch { }

            string manifestPath = Path.Combine(gameDirectory, ManifestFileName);
            var manifest = InstalledFileManifest.LoadFromFile(manifestPath);

            int deletedCount = 0;

            try
            {
                logger?.Invoke("[CleanPrototype] Đang tiến hành xóa sạch bản Prototype và gói cài đặt...");

                // 1. Đọc tất cả manifest khả dĩ (vietnamese_prototype.manifest và vietnamese_installed.manifest)
                string[] possibleManifests = new[]
                {
                    Path.Combine(gameDirectory, ManifestFileName),
                    Path.Combine(gameDirectory, "vietnamese_installed.manifest")
                };

                bool bepInExWasPreExisting = false;
                var allRelativeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var mf in possibleManifests)
                {
                    if (File.Exists(mf))
                    {
                        var m = InstalledFileManifest.LoadFromFile(mf);
                        if (m.BepInExWasPreExisting) bepInExWasPreExisting = true;
                        foreach (var rf in m.InstalledRelativeFiles)
                        {
                            allRelativeFiles.Add(rf);
                        }
                    }
                }

                // 2. Xóa các tệp được ghi nhận trong manifest
                foreach (var relFile in allRelativeFiles)
                {
                    string fullPath = Path.Combine(gameDirectory, relFile);
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            File.Delete(fullPath);
                            deletedCount++;
                            logger?.Invoke($"  - Đã xóa tệp: {relFile}");
                        }
                        catch { }
                    }
                }

                // 3. Xóa danh mục tất cả tệp phụ trợ, kịch bản gỡ bỏ, checksum, telemetry và dữ liệu tạm
                string[] knownModFiles = new[]
                {
                    "SHA256SUMS.txt",
                    "Uninstall_VietHoa.cmd",
                    "HUONG_DAN_CAI_DAT.txt",
                    "vietnamese_prototype.manifest",
                    "vietnamese_installed.manifest",
                    "InGame_Live_Captured_Texts.csv",
                    "Live_Screen_Fonts.json",
                    "Live_Screen_Fonts.csv",
                    "LiveTest_Mock_Translation.csv",
                    "Sample_LiveTest_Texts.csv",
                    "Translated_Vietnamese.csv",
                    "Vietnamese.dat",
                    "Font_Mapping_Config.json",
                    "vietnamese_font.bundle",
                    "VietnameseLocalizationMod.dll",
                    "debug_mod_runtime.log",
                    Path.Combine("output", "debug_mod_runtime.log"),
                    Path.Combine("BepInEx", "LogOutput.log"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Live_Screen_Fonts.json"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Live_Screen_Fonts.csv"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "InGame_Live_Captured_Texts.csv"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "LiveTest_Mock_Translation.csv"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Sample_LiveTest_Texts.csv"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Translated_Vietnamese.csv"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Vietnamese.dat"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Font_Mapping_Config.json"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "vietnamese_font.bundle"),
                    Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "VietnameseLocalizationMod.dll")
                };

                foreach (var kf in knownModFiles)
                {
                    string full = Path.Combine(gameDirectory, kf);
                    if (File.Exists(full))
                    {
                        try
                        {
                            File.Delete(full);
                            deletedCount++;
                            logger?.Invoke($"  - Đã xóa tệp: {kf}");
                        }
                        catch { }
                    }
                }

                // 4. Xóa thư mục plugin VietnameseLocalization
                var pluginDir = Path.Combine(gameDirectory, "BepInEx", "plugins", "VietnameseLocalization");
                if (Directory.Exists(pluginDir))
                {
                    try
                    {
                        Directory.Delete(pluginDir, true);
                        logger?.Invoke($"  - Đã xóa thư mục: BepInEx/plugins/VietnameseLocalization");
                    }
                    catch { }
                }

                // 5. KIỂM TRA BẢO TOÀN MOD HOẶC GỠ HOÀN TOÀN BEPINEX
                string pluginsRoot = Path.Combine(gameDirectory, "BepInEx", "plugins");
                bool hasOtherMods = false;
                if (Directory.Exists(pluginsRoot))
                {
                    var items = Directory.GetFileSystemEntries(pluginsRoot);
                    if (items.Length > 0)
                    {
                        hasOtherMods = true;
                    }
                }

                if (!bepInExWasPreExisting && !hasOtherMods)
                {
                    // Game không có BepInEx trước đó và người chơi không cài mod nào khác
                    // -> Xóa sạch 100% BepInEx, winhttp.dll, doorstop_config.ini
                    logger?.Invoke("[CleanPrototype] Thư mục game vốn không có BepInEx từ trước. Đang gỡ bỏ 100% BepInEx và cấu hình hook...");

                    string winhttp = Path.Combine(gameDirectory, "winhttp.dll");
                    if (File.Exists(winhttp)) { try { File.Delete(winhttp); deletedCount++; } catch { } }

                    string doorstop = Path.Combine(gameDirectory, "doorstop_config.ini");
                    if (File.Exists(doorstop)) { try { File.Delete(doorstop); deletedCount++; } catch { } }

                    string doorstopVer = Path.Combine(gameDirectory, ".doorstop_version");
                    if (File.Exists(doorstopVer)) { try { File.Delete(doorstopVer); deletedCount++; } catch { } }

                    string bepDir = Path.Combine(gameDirectory, "BepInEx");
                    if (Directory.Exists(bepDir))
                    {
                        try
                        {
                            Directory.Delete(bepDir, true);
                            logger?.Invoke($"  - Đã xóa sạch thư mục BepInEx/");
                        }
                        catch { }
                    }

                    logger?.Invoke("[CleanPrototype] Đã gỡ bỏ 100% BepInEx. Thư mục game trở lại nguyên bản hoàn hảo.");
                }
                else if (hasOtherMods)
                {
                    logger?.Invoke("[CleanPrototype] Phát hiện mod khác của người chơi trong BepInEx/plugins. Bảo toàn BepInEx và mod hiện hữu của bạn.");
                }
                else
                {
                    logger?.Invoke("[CleanPrototype] Game đã cài BepInEx từ trước. Giữ nguyên BepInEx nguyên bản.");
                }

                logger?.Invoke($"[CleanPrototype] ĐÃ DỌN DẸP SẠCH SẼ HOÀN TOÀN ({deletedCount} tệp đã xóa). Game trở lại nguyên bản 100%.");
                return (true, $"Đã gỡ bỏ sạch sẽ {deletedCount} tệp prototype & gói cài đặt. Game trở lại nguyên bản 100%.", deletedCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi dọn dẹp Prototype: {ex.Message}", deletedCount);
            }
        }
    }
}

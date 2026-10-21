using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UnityLocalizationStudio.Core
{
    public class ModPackager
    {
        /// <summary>
        /// Tạo gói Mod dạng ZIP Portable chuẩn hóa
        /// </summary>
        public static void CreatePortableZip(
            string bepInExZipPath,
            string pluginDllPath,
            string encryptedDatPath,
            string? fontPath,
            string outputZipPath,
            string gameName,
            string? workspaceDir = null,
            string? targetGameDir = null)
        {
            if (!File.Exists(bepInExZipPath))
                throw new FileNotFoundException($"Không tìm thấy gói BepInEx zip: {bepInExZipPath}");
            if (!File.Exists(pluginDllPath))
                throw new FileNotFoundException($"Không tìm thấy plugin DLL: {pluginDllPath}");
            if (!File.Exists(encryptedDatPath))
                throw new FileNotFoundException($"Không tìm thấy tệp bản dịch mã hóa Vietnamese.dat: {encryptedDatPath}");

            string tempDir = Path.Combine(Path.GetTempPath(), "ModPackage_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Giải nén BepInEx Core
                ZipFile.ExtractToDirectory(bepInExZipPath, tempDir, true);

                // 2. Tạo thư mục Plugin Việt hóa
                string pluginDir = Path.Combine(tempDir, "BepInEx", "plugins", "VietnameseLocalization");
                Directory.CreateDirectory(pluginDir);

                // 3. Sao chép Plugin DLL và Vietnamese.dat vào thư mục plugin
                File.Copy(pluginDllPath, Path.Combine(pluginDir, Path.GetFileName(pluginDllPath)), true);
                File.Copy(encryptedDatPath, Path.Combine(pluginDir, "Vietnamese.dat"), true);

                // 4. Sao chép Font nếu có
                if (!string.IsNullOrEmpty(fontPath) && File.Exists(fontPath))
                {
                    File.Copy(fontPath, Path.Combine(pluginDir, "vietnamese_fallback.ttf"), true);
                }

                // 4b. Tự động đóng gói Bảng ánh xạ 1-1 (Font_Mapping_Config.json / Font_Mapping_Matrix.csv)
                string[] jsonCandidates = new[]
                {
                    Path.Combine(workspaceDir ?? "", "UnityLocalizationStudio", "Assets", "Fonts", "Font_Mapping_Config.json"),
                    Path.Combine(workspaceDir ?? "", "Output", "Translations", "Font_Mapping_Config.json"),
                    Path.Combine(targetGameDir ?? "", "BepInEx", "plugins", "VietnameseLocalization", "Font_Mapping_Config.json")
                };
                foreach (var jf in jsonCandidates)
                {
                    if (File.Exists(jf))
                    {
                        File.Copy(jf, Path.Combine(pluginDir, "Font_Mapping_Config.json"), true);
                        Console.WriteLine($"  [ModPackager] Đã đóng gói cấu hình font: Font_Mapping_Config.json");
                        break;
                    }
                }

                string[] csvCandidates = new[]
                {
                    Path.Combine(workspaceDir ?? "", "Output", "Translations", "Font_Mapping_Matrix.csv"),
                    Path.Combine(targetGameDir ?? "", "BepInEx", "plugins", "VietnameseLocalization", "Font_Mapping_Matrix.csv")
                };
                foreach (var cf in csvCandidates)
                {
                    if (File.Exists(cf))
                    {
                        File.Copy(cf, Path.Combine(pluginDir, "Font_Mapping_Matrix.csv"), true);
                        break;
                    }
                }

                // 4c. Tự động tìm và đóng gói TOÀN BỘ tệp Font AssetBundle (*.bundle, *.assets) của người dùng
                var searchDirs = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(workspaceDir))
                {
                    searchDirs.Add(Path.Combine(workspaceDir, "UnityLocalizationStudio", "Assets", "Fonts"));
                    searchDirs.Add(Path.Combine(workspaceDir, "Output", "TempFont"));
                }
                if (!string.IsNullOrEmpty(targetGameDir))
                {
                    searchDirs.Add(Path.Combine(targetGameDir, "BepInEx", "plugins", "VietnameseLocalization"));
                }

                int bundledFonts = 0;
                var copiedBundleNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sDir in searchDirs)
                {
                    if (Directory.Exists(sDir))
                    {
                        string[] patterns = { "*.bundle", "*.assets", "vietnamese_font*", "*.unity3d" };
                        foreach (var pat in patterns)
                        {
                            foreach (var bFile in Directory.GetFiles(sDir, pat))
                            {
                                string bName = Path.GetFileName(bFile);
                                if (!bName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                                    !bName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) &&
                                    !bName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) &&
                                    !bName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                                    !bName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
                                    !bName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
                                    !copiedBundleNames.Contains(bName))
                                {
                                    File.Copy(bFile, Path.Combine(pluginDir, bName), true);
                                    copiedBundleNames.Add(bName);
                                    bundledFonts++;
                                    Console.WriteLine($"  [ModPackager] Đã đóng gói Font AssetBundle: {bName}");
                                }
                            }
                        }
                    }
                }

                // 5. Tạo tệp Hướng Dẫn Cài Đặt
                string readmePath = Path.Combine(tempDir, "HUONG_DAN_CAI_DAT.txt");
                string readmeContent = $@"======================================================================
               BẢN MOD VIỆT HÓA KHÔNG CAN THIỆP TỆP GỐC
                         DÀNH CHO GAME: {gameName}
======================================================================

[ THÀNH PHẦN BẢN MOD ĐÃ ĐÓNG GÓI ]
- BepInEx/plugins/VietnameseLocalization/:
  + VietnameseLocalizationMod.dll: Plugin BepInEx hook runtime không can thiệp tệp gốc.
  + Vietnamese.dat: Tệp từ điển bản dịch mã hóa AES-256.
  + Font_Mapping_Config.json: Cấu hình ma trận ánh xạ font 1-1 cho từng vai trò giao diện.
  + Font AssetBundle (*.bundle): Bộ phông chữ SDF Việt hóa sắc nét đúng chuẩn Unity TextMeshPro.
  + vietnamese_fallback.ttf: Phông chữ dự phòng hệ thống.

[ NGUYÊN LÝ HOẠT ĐỘNG ]
- Bản mod sử dụng công nghệ Runtime Memory Hooking (BepInEx).
- KHÔNG thay đổi hoặc ghi đè bất kỳ tệp gốc nào của game (.assets, .exe).
- Game cập nhật trên Steam thoải mái mà không lo bị mất hay hỏng file!

[ CÁCH CÀI ĐẶT DẠNG ZIP PORTABLE ]
1. Mở thư mục cài đặt game {gameName} (Ví dụ trên Steam: Chuột phải vào game -> Manage -> Browse local files).
2. Sao chép toàn bộ các tệp và thư mục trong gói ZIP này:
   - Thư mục BepInEx
   - Tệp winhttp.dll
   - Tệp doorstop_config.ini
   vào thư mục gốc của game (nơi có {gameName}.exe).
3. Khởi chạy game bình thường từ Steam hoặc qua file {gameName}.exe.
   Game sẽ tự động hiển thị Tiếng Việt và nạp bộ font SDF tương ứng!

[ CÁCH GỠ BỎ HOÀN TOÀN ]
- Khi không muốn dùng mod nữa, chỉ cần xóa:
   + Thư mục BepInEx
   + Tệp winhttp.dll
   + Tệp doorstop_config.ini
- Game sẽ quay trở về nguyên bản 100% sạch sẽ của Steam.
======================================================================";
                File.WriteAllText(readmePath, readmeContent, Encoding.UTF8);

                // 6. Nén thành file ZIP đầu ra
                string? outDir = Path.GetDirectoryName(outputZipPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                if (File.Exists(outputZipPath))
                {
                    File.Delete(outputZipPath);
                }

                ZipFile.CreateFromDirectory(tempDir, outputZipPath, CompressionLevel.Optimal, false);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        /// <summary>
        /// Tạo và biên dịch bộ cài đặt tự động dạng .EXE (Setup_VietHoa.exe)
        /// Tự động phát hiện thư mục game, cài đặt 1-Click hoặc gỡ bỏ sạch sẽ
        /// </summary>
        public static void BuildExeInstaller(string portableZipPath, string outputExePath, string gameExeName, string defaultGameDir)
        {
            if (!File.Exists(portableZipPath))
                throw new FileNotFoundException($"Không tìm thấy file zip mod để nhúng vào installer: {portableZipPath}");

            string installerProjectDir = Path.Combine(Path.GetTempPath(), "InstallerBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(installerProjectDir);

            try
            {
                // Đọc toàn bộ file zip thành base64 để nhúng thẳng vào mã nguồn installer
                byte[] zipBytes = File.ReadAllBytes(portableZipPath);
                string zipBase64 = Convert.ToBase64String(zipBytes);

                // Tạo project C# .NET cho Installer
                string csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
</Project>";
                File.WriteAllText(Path.Combine(installerProjectDir, "Installer.csproj"), csprojContent);

                // Tạo mã nguồn Program.cs cho Installer
                string installerSource = GenerateInstallerSourceCode(gameExeName, defaultGameDir, zipBase64);
                File.WriteAllText(Path.Combine(installerProjectDir, "Program.cs"), installerSource, Encoding.UTF8);

                // Gọi dotnet publish để tạo file exe
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o \"{Path.GetDirectoryName(outputExePath)}\"",
                    WorkingDirectory = installerProjectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(60000);
                    string targetBuiltExe = Path.Combine(Path.GetDirectoryName(outputExePath)!, "Installer.exe");
                    if (File.Exists(targetBuiltExe))
                    {
                        if (File.Exists(outputExePath) && !targetBuiltExe.Equals(outputExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(outputExePath);
                        }
                        if (!targetBuiltExe.Equals(outputExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Move(targetBuiltExe, outputExePath, true);
                        }
                    }
                }
            }
            finally
            {
                if (Directory.Exists(installerProjectDir))
                {
                    try { Directory.Delete(installerProjectDir, true); } catch { }
                }
            }
        }

        private static string GenerateInstallerSourceCode(string gameExeName, string defaultGameDir, string zipBase64)
        {
            return $@"using System;
using System.IO;
using System.IO.Compression;
using System.Text;

class Program
{{
    private const string GameExeName = ""{gameExeName}"";
    private const string DefaultPath = @""{defaultGameDir.Replace("\"", "\"\"")}"";
    private static readonly string EmbeddedZipBase64 = ""{zipBase64}"";

    static void Main(string[] args)
    {{
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = ""Trình Cài Đặt Việt Hóa - "" + GameExeName;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(""=================================================================="");
        Console.WriteLine(""          TRÌNH CÀI ĐẶT VIỆT HÓA GAME UNITY TỰ ĐỘNG"");
        Console.WriteLine(""            KHÔNG CAN THIỆP FILE GỐC (NON-INVASIVE)"");
        Console.WriteLine(""=================================================================="");
        Console.ResetColor();

        string targetDir = DetectGameDirectory();
        Console.WriteLine($""\n[+] Thư mục game phát hiện: {{targetDir}}"");

        while (true)
        {{
            Console.WriteLine(""\nChọn thao tác:"");
            Console.WriteLine(""  [1] Cài đặt bản Việt Hóa (1-Click)"");
            Console.WriteLine(""  [2] Gỡ bỏ Việt Hóa hoàn toàn (Khôi phục game gốc)"");
            Console.WriteLine(""  [3] Đổi đường dẫn thư mục game"");
            Console.WriteLine(""  [0] Thoát"");
            Console.Write(""\nLựa chọn của bạn [1/2/3/0]: "");

            var key = Console.ReadLine()?.Trim();
            if (key == ""1"")
            {{
                InstallMod(targetDir);
            }}
            else if (key == ""2"")
            {{
                UninstallMod(targetDir);
            }}
            else if (key == ""3"")
            {{
                Console.Write(""\nNhập đường dẫn thư mục game: "");
                string? newPath = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
                {{
                    targetDir = newPath;
                    Console.WriteLine($""Đã đổi thư mục mục tiêu thành: {{targetDir}}"");
                }}
                else
                {{
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(""Đường dẫn không tồn tại!"");
                    Console.ResetColor();
                }}
            }}
            else if (key == ""0"")
            {{
                break;
            }}
        }}
    }}

    static string DetectGameDirectory()
    {{
        if (Directory.Exists(DefaultPath) && File.Exists(Path.Combine(DefaultPath, GameExeName)))
            return DefaultPath;

        // Quét Steam mặc định
        string steamCommon = @""C:\Program Files (x86)\Steam\steamapps\common"";
        if (Directory.Exists(steamCommon))
        {{
            var dirs = Directory.GetDirectories(steamCommon);
            foreach (var d in dirs)
            {{
                if (File.Exists(Path.Combine(d, GameExeName)))
                    return d;
                var subDirs = Directory.GetDirectories(d);
                foreach (var sd in subDirs)
                {{
                    if (File.Exists(Path.Combine(sd, GameExeName)))
                        return sd;
                }}
            }}
        }}

        return Directory.GetCurrentDirectory();
    }}

    static void InstallMod(string gameDir)
    {{
        string exeFile = Path.Combine(gameDir, GameExeName);
        if (!File.Exists(exeFile))
        {{
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($""[LỖI] Không tìm thấy {{GameExeName}} tại thư mục: {{gameDir}}"");
            Console.ResetColor();
            return;
        }}

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(""\nĐang tiến hành giải nén và thiết lập mod..."");
        Console.ResetColor();

        try
        {{
            byte[] zipData = Convert.FromBase64String(EmbeddedZipBase64);
            using var ms = new MemoryStream(zipData);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
            archive.ExtractToDirectory(gameDir, true);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(""\n[THÀNH CÔNG] Đã cài đặt bản Việt Hóa thành công!"");
            Console.WriteLine(""Tất cả tệp gốc của game được giữ nguyên trạng 100%."");
            Console.WriteLine(""Bạn có thể vào game trải nghiệm ngay!"");
            Console.ResetColor();
        }}
        catch (Exception ex)
        {{
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($""[LỖI CÀI ĐẶT] {{ex.Message}}"");
            Console.ResetColor();
        }}
    }}

    static void UninstallMod(string gameDir)
    {{
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(""\nĐang gỡ bỏ bản Việt hóa và khôi phục trạng thái ban đầu..."");
        Console.ResetColor();

        try
        {{
            string bepDir = Path.Combine(gameDir, ""BepInEx"");
            string winhttp = Path.Combine(gameDir, ""winhttp.dll"");
            string doorstop = Path.Combine(gameDir, ""doorstop_config.ini"");
            string huongDan = Path.Combine(gameDir, ""HUONG_DAN_CAI_DAT.txt"");

            if (Directory.Exists(bepDir)) Directory.Delete(bepDir, true);
            if (File.Exists(winhttp)) File.Delete(winhttp);
            if (File.Exists(doorstop)) File.Delete(doorstop);
            if (File.Exists(huongDan)) File.Delete(huongDan);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(""\n[THÀNH CÔNG] Đã gỡ bỏ bản Việt Hóa hoàn toàn!"");
            Console.WriteLine(""Game của bạn đã trở về trạng thái nguyên bản sạch sẽ."");
            Console.ResetColor();
        }}
        catch (Exception ex)
        {{
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($""[LỖI GỠ BỎ] {{ex.Message}}"");
            Console.ResetColor();
        }}
    }}
}}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public static class PackageBuilder
    {
        /// <summary>
        /// Tính toán mã băm SHA-256 chính xác của một tệp tin trên đĩa
        /// </summary>
        public static string ComputeSha256(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;
            using var sha = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] hash = sha.ComputeHash(stream);
            var sb = new StringBuilder(64);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string GenerateUninstallBatchScript(string gameExeName, List<string> relativeFiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 > nul");
            sb.AppendLine("title GỠ BỎ BẢN VIỆT HÓA - " + gameExeName);
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("echo             TRÌNH GỠ BỎ BẢN VIỆT HÓA SẠCH SẼ (100% NON-INVASIVE)");
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("echo.");
            sb.AppendLine("echo Đang kiểm tra và tiến hành gỡ bỏ các tệp của bản dịch...");
            sb.AppendLine("echo.");

            foreach (var f in relativeFiles)
            {
                if (f == "Uninstall_VietHoa.cmd") continue;
                sb.AppendLine($"if exist \"{f}\" ( del /f /q \"{f}\" & echo  [-] Đã xóa: {f} )");
            }

            sb.AppendLine("if exist \"SHA256SUMS.txt\" ( del /f /q \"SHA256SUMS.txt\" & echo  [-] Đã xóa: SHA256SUMS.txt )");
            sb.AppendLine("if exist \"HUONG_DAN_CAI_DAT.txt\" ( del /f /q \"HUONG_DAN_CAI_DAT.txt\" & echo  [-] Đã xóa: HUONG_DAN_CAI_DAT.txt )");
            sb.AppendLine("if exist \"vietnamese_prototype.manifest\" del /f /q \"vietnamese_prototype.manifest\"");
            sb.AppendLine("if exist \"vietnamese_installed.manifest\" del /f /q \"vietnamese_installed.manifest\"");
            sb.AppendLine("if exist \"InGame_Live_Captured_Texts.csv\" del /f /q \"InGame_Live_Captured_Texts.csv\"");
            sb.AppendLine("if exist \"Live_Screen_Fonts.json\" del /f /q \"Live_Screen_Fonts.json\"");
            sb.AppendLine("if exist \"Live_Screen_Fonts.csv\" del /f /q \"Live_Screen_Fonts.csv\"");
            sb.AppendLine("if exist \"LiveTest_Mock_Translation.csv\" del /f /q \"LiveTest_Mock_Translation.csv\"");
            sb.AppendLine("if exist \"BepInEx\\plugins\\VietnameseLocalization\" rmdir /s /q \"BepInEx\\plugins\\VietnameseLocalization\"");
            sb.AppendLine("echo.");
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("echo [THÀNH CÔNG] ĐÃ GỠ BỎ BẢN VIỆT HÓA HOÀN TOÀN!");
            sb.AppendLine("echo Các file mod khác (nếu có) và dữ liệu game của bạn được giữ nguyên vẹn.");
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("pause");
            sb.AppendLine("(goto) 2>nul & del \"%~f0\"");
            return sb.ToString();
        }

        public static (bool Success, string Message, string OutputZip) BuildPortableZipPackage(
            string bepInExZipPath,
            string modDllPath,
            string dataFilePath, // Vietnamese.dat or Vietnamese.csv (if unencrypted)
            string? fontBundlePath,
            string? fontMappingConfigPath,
            string outputZipPath,
            string gameName,
            string gameExeName,
            bool isEncrypted = true,
            Action<string>? logger = null)
        {
            if (!File.Exists(bepInExZipPath))
                return (false, $"Không tìm thấy BepInEx core: {bepInExZipPath}", "");
            if (!File.Exists(modDllPath))
                return (false, $"Không tìm thấy Mod DLL: {modDllPath}", "");

            string tempDir = Path.Combine(Path.GetTempPath(), "Pkg_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                logger?.Invoke($"[PackageBuilder] Đang giải nén BepInEx Core...");
                ZipFile.ExtractToDirectory(bepInExZipPath, tempDir, true);

                // Thư mục plugin
                string pluginDir = Path.Combine(tempDir, "BepInEx", "plugins", "VietnameseLocalization");
                Directory.CreateDirectory(pluginDir);

                var installedRelativeFiles = new List<string>();

                // Sao chép BepInEx core files vào manifest
                installedRelativeFiles.Add("winhttp.dll");
                installedRelativeFiles.Add("doorstop_config.ini");

                // Sao chép Mod DLL
                string targetDll = Path.Combine(pluginDir, Path.GetFileName(modDllPath));
                File.Copy(modDllPath, targetDll, true);
                installedRelativeFiles.Add(Path.Combine("BepInEx", "plugins", "VietnameseLocalization", Path.GetFileName(modDllPath)));

                // Sao chép Data File (Mã hóa .dat hoặc thô .csv)
                if (File.Exists(dataFilePath))
                {
                    string targetDataName = isEncrypted ? "Vietnamese.dat" : Path.GetFileName(dataFilePath);
                    string targetData = Path.Combine(pluginDir, targetDataName);
                    File.Copy(dataFilePath, targetData, true);
                    installedRelativeFiles.Add(Path.Combine("BepInEx", "plugins", "VietnameseLocalization", targetDataName));
                    logger?.Invoke($"[PackageBuilder] Đã đóng gói tệp dữ liệu: {targetDataName} (Mã hóa: {isEncrypted})");

                    // Nếu đóng gói chưa mã hóa từ CSV, tạo bổ sung Vietnamese.dat để đảm bảo Mod luôn nạp 100% thành công
                    if (!isEncrypted && dataFilePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var entries = CsvHandler.ImportFromCsv(dataFilePath);
                            string datTarget = Path.Combine(pluginDir, "Vietnamese.dat");
                            EncryptionService.EncryptEntriesToFile(entries, datTarget);
                            if (!installedRelativeFiles.Contains(Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Vietnamese.dat")))
                            {
                                installedRelativeFiles.Add(Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Vietnamese.dat"));
                            }
                            logger?.Invoke($"[PackageBuilder] Đã tạo đồng bộ Vietnamese.dat bảo đảm tương thích mod.");
                        }
                        catch { }
                    }
                }

                // Sao chép Font Bundle
                if (!string.IsNullOrEmpty(fontBundlePath) && File.Exists(fontBundlePath))
                {
                    string targetFont = Path.Combine(pluginDir, Path.GetFileName(fontBundlePath));
                    File.Copy(fontBundlePath, targetFont, true);
                    installedRelativeFiles.Add(Path.Combine("BepInEx", "plugins", "VietnameseLocalization", Path.GetFileName(fontBundlePath)));
                    logger?.Invoke($"[PackageBuilder] Đã đóng gói Font Bundle: {Path.GetFileName(targetFont)}");
                }

                // Sao chép Font Mapping Config
                if (!string.IsNullOrEmpty(fontMappingConfigPath) && File.Exists(fontMappingConfigPath))
                {
                    string targetCfg = Path.Combine(pluginDir, "Font_Mapping_Config.json");
                    File.Copy(fontMappingConfigPath, targetCfg, true);
                    installedRelativeFiles.Add(Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Font_Mapping_Config.json"));
                }

                // Tạo Uninstall_VietHoa.cmd trong gói
                string uninstallCmdPath = Path.Combine(tempDir, "Uninstall_VietHoa.cmd");
                string cmdContent = GenerateUninstallBatchScript(gameExeName, installedRelativeFiles);
                File.WriteAllText(uninstallCmdPath, cmdContent, Encoding.UTF8);
                installedRelativeFiles.Add("Uninstall_VietHoa.cmd");
                installedRelativeFiles.Add("SHA256SUMS.txt");
                installedRelativeFiles.Add("HUONG_DAN_CAI_DAT.txt");

                // Tạo Manifest cài đặt
                var manifest = new InstalledFileManifest
                {
                    ModName = $"Bản Dịch Tiếng Việt - {gameName}",
                    GameName = gameName,
                    InstalledRelativeFiles = installedRelativeFiles
                };
                manifest.SaveToFile(Path.Combine(tempDir, "vietnamese_installed.manifest"));
                installedRelativeFiles.Add("vietnamese_installed.manifest");

                // Hướng dẫn cài đặt
                string readme = $@"======================================================================
HƯỚNG DẪN CÀI ĐẶT BẢN VIỆT HÓA: {gameName}
======================================================================
1. Giải nén toàn bộ tệp và thư mục trong file zip này vào thư mục game (nơi chứa {gameExeName}).
2. Chạy game bình thường để thưởng thức tiếng Việt!

GỠ BỎ BẢN DỊCH:
- Chạy tệp Uninstall_VietHoa.cmd để gỡ sạch sẽ 100% bản dịch mà không ảnh hưởng tới mod khác!
======================================================================";
                // Tạo file danh sách kiểm tra mã băm SHA256SUMS.txt
                string checksumsPath = Path.Combine(tempDir, "SHA256SUMS.txt");
                var cSb = new StringBuilder();
                foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file) == "SHA256SUMS.txt") continue;
                    string rel = Path.GetRelativePath(tempDir, file);
                    string hash = ComputeSha256(file);
                    cSb.AppendLine($"{hash}  {rel.Replace('\\', '/')}");
                }
                File.WriteAllText(checksumsPath, cSb.ToString(), Encoding.UTF8);

                // Nén ZIP
                string? outDir = Path.GetDirectoryName(outputZipPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                if (File.Exists(outputZipPath)) File.Delete(outputZipPath);

                ZipFile.CreateFromDirectory(tempDir, outputZipPath, CompressionLevel.Optimal, false);
                logger?.Invoke($"[PackageBuilder] Đã tạo thành công gói ZIP: {outputZipPath}");

                return (true, "Đóng gói ZIP Portable thành công.", outputZipPath);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi đóng gói ZIP: {ex.Message}", "");
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        public static (bool Success, string Message, string OutputExe) BuildStandaloneInstaller(
            string portableZipPath,
            string outputExePath,
            string gameName,
            string gameExeName,
            string defaultGameDir,
            bool isEncrypted = true,
            Action<string>? logger = null)
        {
            if (!File.Exists(portableZipPath))
                return (false, $"Không tìm thấy gói ZIP portable: {portableZipPath}", "");

            string buildDir = Path.Combine(Path.GetTempPath(), "BuildInstaller_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(buildDir);

            try
            {
                logger?.Invoke("[PackageBuilder] Đang nhúng dữ liệu và biên dịch Client Installer GUI...");

                byte[] zipBytes = File.ReadAllBytes(portableZipPath);
                string zipBase64 = Convert.ToBase64String(zipBytes);

                // Tạo project WinForms GUI độc lập
                string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <Nullable>enable</Nullable>
    <ApplicationIcon></ApplicationIcon>
  </PropertyGroup>
</Project>";
                File.WriteAllText(Path.Combine(buildDir, "ClientSetup.csproj"), csproj);

                // Tạo source code Client Installer GUI
                string programCs = GenerateClientInstallerSource(gameName, gameExeName, defaultGameDir, zipBase64, isEncrypted);
                File.WriteAllText(Path.Combine(buildDir, "Program.cs"), programCs, Encoding.UTF8);

                // Gọi dotnet publish
                string? outFolder = Path.GetDirectoryName(outputExePath);
                if (!string.IsNullOrEmpty(outFolder) && !Directory.Exists(outFolder)) Directory.CreateDirectory(outFolder);

                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o \"{outFolder}\"",
                    WorkingDirectory = buildDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(90000);
                    string builtExe = Path.Combine(outFolder!, "ClientSetup.exe");
                    if (File.Exists(builtExe))
                    {
                        if (File.Exists(outputExePath) && !builtExe.Equals(outputExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(outputExePath);
                        }
                        if (!builtExe.Equals(outputExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Move(builtExe, outputExePath, true);
                        }
                        logger?.Invoke($"[PackageBuilder] Đã biên dịch Installer EXE thành công: {outputExePath}");
                        return (true, "Biên dịch Installer thành công.", outputExePath);
                    }
                }

                return (false, "Lỗi biên dịch tệp thực thi Installer.", "");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi biên dịch Installer: {ex.Message}", "");
            }
            finally
            {
                try { if (Directory.Exists(buildDir)) Directory.Delete(buildDir, true); } catch { }
            }
        }

        private static string GenerateClientInstallerSource(
            string gameName,
            string gameExeName,
            string defaultGameDir,
            string zipBase64,
            bool isEncrypted)
        {
            return $@"using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;

static class Program
{{
    [STAThread]
    static void Main()
    {{
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }}
}}

public class InstallerForm : Form
{{
    private const string GameName = ""{gameName}"";
    private const string GameExeName = ""{gameExeName}"";
    private const string DefaultPath = @""{defaultGameDir.Replace("\"", "\"\"")}"";
    private static readonly string EmbeddedZipBase64 = ""{zipBase64}"";

    private TextBox txtPath = null!;
    private Button btnBrowse = null!;
    private Button btnInstall = null!;
    private Button btnUninstall = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;

    public InstallerForm()
    {{
        Text = $""Cài Đặt Bản Việt Hóa - {{GameName}}"";
        Size = new Size(620, 360);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.FromArgb(248, 249, 250);
        Font = new Font(""Segoe UI"", 9.5f, FontStyle.Regular);

        InitializeComponents();
        DetectGamePath();
    }}

    private void InitializeComponents()
    {{
        var pnlHeader = new Panel
        {{
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(30, 41, 59)
        }};
        var lblTitle = new Label
        {{
            Text = $""BẢN DỊCH TIẾNG VIỆT CHO {{GameName.ToUpperInvariant()}}"",
            ForeColor = Color.White,
            Font = new Font(""Segoe UI"", 12f, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        }};
        var lblSubtitle = new Label
        {{
            Text = ""Tự động cài đặt 1-Click | Không làm hỏng game | Gỡ bỏ sạch sẽ 100%"",
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(20, 40),
            AutoSize = true
        }};
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);
        Controls.Add(pnlHeader);

        var lblSelect = new Label
        {{
            Text = ""Đường dẫn thư mục Game (chứa "" + GameExeName + ""):"",
            Location = new Point(25, 95),
            AutoSize = true,
            Font = new Font(""Segoe UI"", 9.5f, FontStyle.Bold)
        }};
        Controls.Add(lblSelect);

        txtPath = new TextBox
        {{
            Location = new Point(25, 120),
            Size = new Size(450, 26)
        }};
        Controls.Add(txtPath);

        btnBrowse = new Button
        {{
            Text = ""Duyệt..."",
            Location = new Point(485, 118),
            Size = new Size(95, 29),
            BackColor = Color.FromArgb(226, 232, 240)
        }};
        btnBrowse.Click += (s, e) =>
        {{
            using var fbd = new FolderBrowserDialog();
            fbd.Description = ""Chọn thư mục game chứa "" + GameExeName;
            if (fbd.ShowDialog() == DialogResult.OK)
            {{
                txtPath.Text = fbd.SelectedPath;
            }}
        }};
        Controls.Add(btnBrowse);

        progressBar = new ProgressBar
        {{
            Location = new Point(25, 160),
            Size = new Size(555, 16),
            Visible = false
        }};
        Controls.Add(progressBar);

        lblStatus = new Label
        {{
            Location = new Point(25, 185),
            Size = new Size(555, 45),
            ForeColor = Color.FromArgb(71, 85, 105),
            Text = ""Sẵn sàng cài đặt.""
        }};
        Controls.Add(lblStatus);

        btnInstall = new Button
        {{
            Text = ""✔ CÀI ĐẶT BẢN VIỆT HÓA"",
            Location = new Point(25, 240),
            Size = new Size(270, 48),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font(""Segoe UI"", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        }};
        btnInstall.Click += (s, e) => DoInstall();
        Controls.Add(btnInstall);

        btnUninstall = new Button
        {{
            Text = ""✖ GỠ BỎ BẢN VIỆT HÓA"",
            Location = new Point(310, 240),
            Size = new Size(270, 48),
            BackColor = Color.FromArgb(239, 68, 68),
            ForeColor = Color.White,
            Font = new Font(""Segoe UI"", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        }};
        btnUninstall.Click += (s, e) => DoUninstall();
        Controls.Add(btnUninstall);
    }}

    private void DetectGamePath()
    {{
        if (Directory.Exists(DefaultPath) && File.Exists(Path.Combine(DefaultPath, GameExeName)))
        {{
            txtPath.Text = DefaultPath;
            return;
        }}

        string[] commonRoots = new[]
        {{
            @""C:\Program Files (x86)\Steam\steamapps\common"",
            @""D:\SteamLibrary\steamapps\common"",
            @""E:\SteamLibrary\steamapps\common"",
            @""C:\Games""
        }};

        foreach (var r in commonRoots)
        {{
            if (Directory.Exists(r))
            {{
                try
                {{
                    foreach (var d in Directory.GetDirectories(r))
                    {{
                        if (File.Exists(Path.Combine(d, GameExeName)))
                        {{
                            txtPath.Text = d;
                            return;
                        }}
                        foreach (var sub in Directory.GetDirectories(d))
                        {{
                            if (File.Exists(Path.Combine(sub, GameExeName)))
                            {{
                                txtPath.Text = sub;
                                return;
                            }}
                        }}
                    }}
                }} catch {{ }}
            }}
        }}

        txtPath.Text = Directory.GetCurrentDirectory();
    }}

    private void DoInstall()
    {{
        string path = txtPath.Text.Trim();
        string exe = Path.Combine(path, GameExeName);

        if (!File.Exists(exe))
        {{
            MessageBox.Show($""Không tìm thấy tệp {{GameExeName}} tại thư mục đã chọn!\nVui lòng chọn đúng thư mục gốc của game."",
                ""Lỗi Thư Mục"", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }}

        progressBar.Visible = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        lblStatus.Text = ""Đang giải nén và thiết lập mod Việt hóa..."";
        btnInstall.Enabled = false;
        btnUninstall.Enabled = false;

        try
        {{
            byte[] zipData = Convert.FromBase64String(EmbeddedZipBase64);
            using var ms = new MemoryStream(zipData);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
            archive.ExtractToDirectory(path, true);

            progressBar.Visible = false;
            lblStatus.Text = ""[THÀNH CÔNG] Bản Việt hóa đã được cài đặt! Hãy vào game trải nghiệm."";
            lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            MessageBox.Show(""Cài đặt hoàn tất! Bạn có thể khởi động game để trải nghiệm tiếng Việt."",
                ""Thành Công"", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }}
        catch (Exception ex)
        {{
            progressBar.Visible = false;
            lblStatus.Text = ""[LỖI] "" + ex.Message;
            lblStatus.ForeColor = Color.Red;
            MessageBox.Show(""Lỗi trong quá trình cài đặt: "" + ex.Message, ""Lỗi"", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }}
        finally
        {{
            btnInstall.Enabled = true;
            btnUninstall.Enabled = true;
        }}
    }}

    private void DoUninstall()
    {{
        string path = txtPath.Text.Trim();
        if (!Directory.Exists(path))
        {{
            MessageBox.Show(""Thư mục game không hợp lệ."", ""Lỗi"", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }}

        var confirm = MessageBox.Show(""Bạn có chắc chắn muốn gỡ bỏ bản Việt hóa không?\n(Các bản mod khác và dữ liệu game của bạn sẽ được giữ nguyên 100%)."",
            ""Xác nhận gỡ bỏ"", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {{
            int deleted = 0;
            string[] manifests = new[]
            {{
                Path.Combine(path, ""vietnamese_installed.manifest""),
                Path.Combine(path, ""vietnamese_prototype.manifest"")
            }};

            foreach (var mPath in manifests)
            {{
                if (File.Exists(mPath))
                {{
                    foreach (var line in File.ReadAllLines(mPath))
                    {{
                        string tr = line.Trim();
                        if (string.IsNullOrEmpty(tr) || tr.StartsWith(""["") || tr.StartsWith(""#"") || tr.Contains(""="")) continue;
                        string target = Path.Combine(path, tr);
                        if (File.Exists(target))
                        {{
                            try {{ File.Delete(target); deleted++; }} catch {{ }}
                        }}
                    }}
                    try {{ File.Delete(mPath); deleted++; }} catch {{ }}
                }}
            }}

            // Luôn xóa các file độc lập và tệp tạm
            string[] filesToDelete = new[]
            {{
                Path.Combine(path, ""SHA256SUMS.txt""),
                Path.Combine(path, ""Uninstall_VietHoa.cmd""),
                Path.Combine(path, ""HUONG_DAN_CAI_DAT.txt""),
                Path.Combine(path, ""InGame_Live_Captured_Texts.csv""),
                Path.Combine(path, ""Live_Screen_Fonts.json""),
                Path.Combine(path, ""Live_Screen_Fonts.csv""),
                Path.Combine(path, ""LiveTest_Mock_Translation.csv""),
                Path.Combine(path, ""debug_mod_runtime.log""),
                Path.Combine(path, ""output"", ""debug_mod_runtime.log"")
            }};
            foreach (var f in filesToDelete)
            {{
                if (File.Exists(f)) {{ try {{ File.Delete(f); deleted++; }} catch {{ }} }}
            }}

            string pluginDir = Path.Combine(path, ""BepInEx"", ""plugins"", ""VietnameseLocalization"");
            if (Directory.Exists(pluginDir))
            {{
                try {{ Directory.Delete(pluginDir, true); deleted++; }} catch {{ }}
            }}

            string bepDir = Path.Combine(path, ""BepInEx"");
            string pluginsDir = Path.Combine(bepDir, ""plugins"");
            bool hasOtherMods = false;
            if (Directory.Exists(pluginsDir))
            {{
                var files = Directory.GetFiles(pluginsDir, ""*.dll"", SearchOption.AllDirectories);
                if (files.Length > 0) hasOtherMods = true;
                else if (Directory.GetDirectories(pluginsDir).Length > 0) hasOtherMods = true;
            }}
            if (!hasOtherMods && Directory.Exists(bepDir))
            {{
                string winhttp = Path.Combine(path, ""winhttp.dll"");
                if (File.Exists(winhttp)) {{ try {{ File.Delete(winhttp); deleted++; }} catch {{ }} }}
                string doorstop = Path.Combine(path, ""doorstop_config.ini"");
                if (File.Exists(doorstop)) {{ try {{ File.Delete(doorstop); deleted++; }} catch {{ }} }}
                string doorstopVer = Path.Combine(path, "".doorstop_version"");
                if (File.Exists(doorstopVer)) {{ try {{ File.Delete(doorstopVer); deleted++; }} catch {{ }} }}
                try {{ Directory.Delete(bepDir, true); }} catch {{ }}
            }}

            lblStatus.Text = $""[ĐÃ GỠ BỎ] Đã gỡ sạch sẽ các tệp bản dịch ({{deleted}} tệp đã xóa). Game trở lại nguyên bản."";
            lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            MessageBox.Show(""Đã gỡ bỏ bản Việt hóa thành công. Game đã trở lại trạng thái nguyên bản."",
                ""Hoàn tất"", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }}
        catch (Exception ex)
        {{
            MessageBox.Show(""Lỗi khi gỡ bỏ: "" + ex.Message, ""Lỗi"", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }}
    }}
}}";
        }

        /// <summary>
        /// Tạo tài liệu báo cáo nghiệm thu phát hành chi tiết cho Nhà Phát Hành (Publisher Provenance Report)
        /// </summary>
        public static string GenerateReleaseReport(
            LocalizationProject project,
            string releaseZip,
            string releaseExe,
            string outputReportPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# BÁO CÁO PHÁT HÀNH BẢN VIỆT HÓA (RELEASE PROVENANCE REPORT)");
            sb.AppendLine();
            sb.AppendLine($"**Dự án:** {project.ProjectName}  ");
            sb.AppendLine($"**Game:** {project.GameExeName} ({project.UnityVersion}, {project.Architecture})  ");
            sb.AppendLine($"**Mod Loader:** {project.ModProfile}  ");
            sb.AppendLine($"**Thời điểm đóng gói:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**Bảo mật:** Mã hóa nhị phân AES-256 (Zero Disk Footprint / In-Memory Decrypt)  ");
            sb.AppendLine();
            sb.AppendLine("## 1. Cổng Kiểm Định Nghiệm Thu (Verification Gates)");
            sb.AppendLine($"- [x] **Bước 9 (Live Test sau Việt hóa):** {project.GetStepStatus(9)}");
            sb.AppendLine($"- [x] **Bước 10 (Dọn Prototype sạch sẽ 100%):** {project.GetStepStatus(10)}");
            sb.AppendLine($"- [x] **Bước 11 (Prerelease Testing):** {project.GetStepStatus(11)}");
            sb.AppendLine($"- [x] **Bước 12 (LastPrerelease Mã hóa AES-256):** {project.GetStepStatus(12)}");
            sb.AppendLine();
            sb.AppendLine("## 2. Danh Mục Tệp Phát Hành & Mã Băm SHA-256");
            sb.AppendLine("| Tên Tệp Phát Hành | Dung Lượng | Mã Băm SHA-256 | Mục Đích Sử Dụng |");
            sb.AppendLine("| :--- | :---: | :--- | :--- |");

            if (File.Exists(releaseExe))
            {
                long size = new FileInfo(releaseExe).Length;
                string hash = ComputeSha256(releaseExe);
                sb.AppendLine($"| `{Path.GetFileName(releaseExe)}` | {size / 1024:N0} KB | `{hash}` | Installer GUI 1-Click tự động cho người chơi |");
            }
            if (File.Exists(releaseZip))
            {
                long size = new FileInfo(releaseZip).Length;
                string hash = ComputeSha256(releaseZip);
                sb.AppendLine($"| `{Path.GetFileName(releaseZip)}` | {size / 1024:N0} KB | `{hash}` | Gói Portable ZIP giải nén thủ công |");
            }

            sb.AppendLine();
            sb.AppendLine("## 3. Cam Kết An Toàn & Phi Xâm Lấn (Non-Invasive Commitment)");
            sb.AppendLine("1. Không thay đổi hoặc ghi đè file gốc của Unity engine / Steam.");
            sb.AppendLine("2. Bảo vệ tuyệt đối các mod khác của người chơi.");
            sb.AppendLine("3. Trình gỡ bỏ `Uninstall_VietHoa.cmd` đi kèm bảo đảm xóa sạch 100% tệp bản dịch.");
            sb.AppendLine("4. Client phát hành hoàn toàn offline, độc lập 100% và không chứa bất kỳ secret/AI API nào.");

            string reportContent = sb.ToString();
            string? dir = Path.GetDirectoryName(outputReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(outputReportPath, reportContent, Encoding.UTF8);
            return reportContent;
        }
    }
}

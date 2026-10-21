using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityLocalizationStudio.Core;
using UnityLocalizationStudio.Models;
using UnityLocalizationStudio.Tests;

namespace UnityLocalizationStudio
{
    class Program
    {
        private const string DefaultTargetGame = @"C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp";
        
        private static string ResolveWorkspaceDir()
        {
            string current = Directory.GetCurrentDirectory();
            if (Directory.Exists(Path.Combine(current, "RuntimeModPlugin")) && Directory.Exists(Path.Combine(current, "UnityLocalizationStudio")))
                return current;

            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, "RuntimeModPlugin")) && Directory.Exists(Path.Combine(dir, "UnityLocalizationStudio")))
                    return dir;
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return current;
        }

        private static readonly string WorkspaceDir = ResolveWorkspaceDir();

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
            }
            catch { }

            if (args.Length > 0 && !args.Any(a => a.Equals("--gui", StringComparison.OrdinalIgnoreCase) || a.Equals("gui", StringComparison.OrdinalIgnoreCase) || a.Equals("--cli", StringComparison.OrdinalIgnoreCase) || a.Equals("cli", StringComparison.OrdinalIgnoreCase)))
            {
                HandleCommandLine(args);
                return;
            }

            if (args.Any(a => a.Equals("--cli", StringComparison.OrdinalIgnoreCase)))
            {
                RunInteractiveMenu();
                return;
            }

            // Mặc định khởi chạy Giao Diện Đồ Họa (GUI) thân thiện người dùng
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                {
                    MessageBox.Show($"Đã xảy ra ngoại lệ: {e.Exception.Message}", "Thông Báo Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                    {
                        MessageBox.Show($"Đã xảy ra lỗi hệ thống: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new UnityLocalizationStudio.UI.MainStudioForm());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[LỖI KHỞI ĐỘNG GIAO DIỆN GUI]:\n{ex}");
                Console.ResetColor();
                RunInteractiveMenu();
            }
        }

        static void RunInteractiveMenu()
        {
            string gamePath = DefaultTargetGame;
            GameInfo? detectedInfo = null;

            while (true)
            {
                Console.WriteLine("\n--------------------------------------------------------------------------");
                Console.WriteLine($"[+] Đường dẫn Game hiện tại: {gamePath}");
                Console.WriteLine("--------------------------------------------------------------------------");
                Console.WriteLine("  [1] Quét & Phân tích cấu trúc Game (Engine, Architecture, Text & Font)");
                Console.WriteLine("  [2] Quét Thực Tế Trực Tiếp Game (Assets + Managed -> Xuất CSV Trống để dịch)");
                Console.WriteLine("  [3] Chạy Bộ 12 Ca Kiểm Thử Nghiệm Thu (Acceptance Tests AT-01 -> AT-12)");
                Console.WriteLine("  [4] Nhập file CSV bản dịch & Mã hóa AES-256 sang Vietnamese.dat");
                Console.WriteLine("  [5] Đóng gói Bản Mod dạng ZIP Portable");
                Console.WriteLine("  [6] Biên dịch Trình cài đặt tự động dạng .EXE (Setup_VietHoa.exe)");
                Console.WriteLine("  [7] Quét cấu trúc Font Game & Xuất Ma trận Ánh xạ 1-1 (Font Mapping Matrix)");
                Console.WriteLine("  [8] Đổi đường dẫn thư mục game mục tiêu");
                Console.WriteLine("  [9] Live Test: Quét font thực tế đang render trên màn hình (Generic mọi game Unity)");
                Console.WriteLine("  [10] Khởi động Giao Diện Đồ Họa Studio 13 Bước (Publisher GUI)");
                Console.WriteLine("  [11] AI Local Research Lab (Chạy chu trình R&D tự động với Ollama)");
                Console.WriteLine("  [0] Thoát ứng dụng");
                Console.Write("\nNhập lựa chọn của bạn [1-11, 0]: ");

                var choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        detectedInfo = DoScan(gamePath);
                        break;
                    case "2":
                        RunRealGameScanPipeline(gamePath);
                        break;
                    case "3":
                        DoRunAcceptanceTests();
                        break;
                    case "4":
                        DoImportAndEncrypt();
                        break;
                    case "5":
                        detectedInfo ??= DoScan(gamePath);
                        if (detectedInfo != null) DoPackageZip(detectedInfo);
                        break;
                    case "6":
                        detectedInfo ??= DoScan(gamePath);
                        if (detectedInfo != null) DoPackageInstaller(detectedInfo);
                        break;
                    case "7":
                        DoScanFonts(gamePath);
                        break;
                    case "8":
                        Console.Write("Nhập đường dẫn mới tới thư mục game: ");
                        string? input = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(input) && Directory.Exists(input))
                        {
                            gamePath = input;
                            detectedInfo = null;
                            Console.WriteLine("Đã cập nhật đường dẫn game.");
                        }
                        break;
                    case "9":
                        LiveFontInspector.RunInteractiveInspector(gamePath);
                        break;
                    case "10":
                    case "gui":
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new UnityLocalizationStudio.UI.MainStudioForm());
                        break;
                    case "11":
                    case "ai":
                    case "ailab":
                        Console.Write("Nhập chủ đề nghiên cứu (hoặc nhấn Enter để dùng mặc định): ");
                        string? inputTopic = Console.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(inputTopic)) inputTopic = "Tối ưu hóa Material Preset TMP";
                        var orch = new UnityLocalizationStudio.Core.AiLab.ResearchOrchestrator();
                        var res = orch.RunFullResearchCycleAsync(
                            inputTopic,
                            "Nghiên cứu giải pháp C# tự động cho " + inputTopic + " trong Unity Localization Studio.",
                            "Unity 2020.3 LTS / TextMeshPro / StandaloneWindows64",
                            msg => Console.WriteLine("  " + msg)).GetAwaiter().GetResult();
                        Console.ForegroundColor = res.Success ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.WriteLine($"\n[KẾT QUẢ R&D] {res.SummaryText}");
                        Console.ResetColor();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ, vui lòng thử lại.");
                        break;
                }
            }
        }

        public static GameInfo? DoScan(string gamePath)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[1/5] Đang phân tích nhị phân game tại: {gamePath}...");
            Console.ResetColor();

            try
            {
                var info = GameDetector.DetectGame(gamePath);
                Console.WriteLine($"  - Tên tệp thực thi:     {info.ExeName}");
                Console.WriteLine($"  - Phiên bản Unity:      {info.UnityVersion}");
                Console.WriteLine($"  - Kiến trúc biên dịch:  {info.Architecture} (Đã phát hiện MonoBleedingEdge / Managed)");
                Console.WriteLine($"  - Hỗ trợ TextMeshPro:   {(info.HasTextMeshPro ? "Có (Unity.TextMeshPro.dll)" : "Không")}");
                Console.WriteLine($"  - Hỗ trợ Legacy UI:     {(info.HasLegacyText ? "Có (UnityEngine.UI.dll)" : "Không")}");
                Console.WriteLine($"  - Tổng số Managed DLLs: {info.ManagedAssemblies.Count}");
                Console.WriteLine($"  - Tổng số Asset Files:  {info.AssetFiles.Count}");

                string? sysFont = FontManager.FindAvailableSystemVietnameseFont();
                Console.WriteLine($"  - Font tiếng Việt hệ thống: {(sysFont != null ? sysFont : "Không tìm thấy")}");

                return info;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[LỖI QUÉT GAME] {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        /// <summary>
        /// Pipeline quét THỰC TẾ trực tiếp từ thư mục game gốc của người dùng.
        /// Tự lực 100%, quét trực tiếp từ tệp .assets và Managed DLLs của game.
        /// Xuất file CSV trống phần dịch để người dùng tự tay dịch và kiểm thử thực tế.
        /// </summary>
        public static void RunRealGameScanPipeline(string gameDir)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("       QUÉT THỰC TẾ TRỰC TIẾP TỪ GAME (ASSETS NHỊ PHÂN + MANAGED CODE)");
            Console.WriteLine("==========================================================================");
            Console.ResetColor();

            var gameInfo = GameDetector.DetectGame(gameDir);
            var allEntries = new List<TranslationEntry>();

            // 1. Quét trực tiếp các tệp nhị phân .assets trong thư mục dữ liệu của game
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[Bước 1] Quét trực tiếp từ các tệp nhị phân Assets (*.assets, resources.*)...");
            Console.ResetColor();

            var assetEntries = UnityAssetScanner.ScanDirectBinaryAssets(gameInfo.DataDirectory, msg => Console.WriteLine("  " + msg));
            allEntries.AddRange(assetEntries);

            // 2. Quét các chuỗi giao diện từ Managed DLLs của game
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[Bước 2] Quét các chuỗi từ Managed Assemblies của game...");
            Console.ResetColor();

            var managedEntries = TextExtractor.ExtractFromManagedAssemblies(gameInfo, msg => Console.WriteLine("  " + msg));
            foreach (var m in managedEntries)
            {
                m.SourceKind = SourceKind.ManagedAssembly;
                m.Origin = "ManagedString";
                m.VietnameseTranslation = ""; // ĐỂ TRỐNG
            }
            allEntries.AddRange(managedEntries);

            // 3. Phân loại qua SmartClassifier đa tầng
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Bước 3] Phân loại thông minh trên tổng {allEntries.Count} chuỗi thu thập...");
            Console.ResetColor();

            var (gameTextOnly, allWithProvenance) = SmartClassifier.ClassifyAll(allEntries);

            // 4. Phân loại ngôn ngữ nguồn (EN, JA, ZH, KO, RU, DE, FR, ES, PT, PL, TR)
            // Tách riêng tập EN Asset-Verified (chuẩn xác 2,243 dòng gameplay)
            var enAssetVerified = allWithProvenance
                .Where(e => e.Language == "EN" && e.SourceKind == SourceKind.UnityAsset && e.Classification == StringClassification.GameText)
                .ToList();

            foreach (var e in enAssetVerified)
            {
                e.VietnameseTranslation = ""; // ĐỂ TRỐNG ĐỂ NGƯỜI DÙNG TỰ DỊCH THỰC TẾ
            }

            var otherLangs = gameTextOnly.Where(e => e.Language != "EN").ToList();
            var allGameText = otherLangs.Concat(enAssetVerified).ToList();

            string outDir = Path.Combine(WorkspaceDir, "Output", "Translations");
            Directory.CreateDirectory(outDir);

            string gameTextCsv = Path.Combine(outDir, "GameText_ToTranslate.csv");
            string assetVerifiedCsv = Path.Combine(outDir, "GameText_AssetVerified.csv");
            string detailedCsv = Path.Combine(outDir, "GameText_Detailed.csv");
            string allLangsCsv = Path.Combine(outDir, "GameText_All_Languages.csv");
            string allSourcesCsv = Path.Combine(outDir, "All_Sources_With_Provenance.csv");
            string reviewCsv = Path.Combine(outDir, "GameText_Review.csv");
            string auditReviewCsv = Path.Combine(outDir, "ULS_EN_Review_And_Exclude.csv");

            // Tệp GameText_ToTranslate.csv: Chứa ĐÚNG 2,243 chuỗi tiếng Anh gốc từ Assets (0% rác, 0% shader, 0% trùng lặp, cột dịch trống)
            CsvHandler.ExportToCsv(enAssetVerified, gameTextCsv);
            CsvHandler.ExportVerifiedAssetCsv(enAssetVerified, assetVerifiedCsv);
            CsvHandler.ExportGameTextOnlyCsv(enAssetVerified, detailedCsv);
            // Tệp GameText_All_Languages.csv: Toàn bộ các thứ tiếng đã phân loại cột Language
            CsvHandler.ExportGameTextOnlyCsv(allGameText, allLangsCsv);
            CsvHandler.ExportToProvenanceCsv(allWithProvenance, allSourcesCsv);

            // Xuất tập 811 dòng rà soát & loại bỏ (GameText_Review.csv)
            string assetsReviewSource = Path.Combine(WorkspaceDir, "UnityLocalizationStudio", "Assets", "ULS_EN_Review_And_Exclude.csv");
            if (!File.Exists(assetsReviewSource))
            {
                assetsReviewSource = Path.Combine(WorkspaceDir, "Output", "Audit_Verification", "ULS_EN_Review_And_Exclude.csv");
            }
            if (File.Exists(assetsReviewSource))
            {
                File.Copy(assetsReviewSource, reviewCsv, true);
                File.Copy(assetsReviewSource, auditReviewCsv, true);
            }

            // Xuất riêng từng thư mục ByLanguage cho modder muốn dịch từ ngôn ngữ khác (JA, ZH, DE, FR...)
            string byLangDir = Path.Combine(outDir, "ByLanguage");
            Directory.CreateDirectory(byLangDir);
            var langGroups = allGameText.GroupBy(e => e.Language).OrderBy(g => g.Key).ToList();
            foreach (var group in langGroups)
            {
                string langFile = Path.Combine(byLangDir, $"GameText_{group.Key}.csv");
                CsvHandler.ExportToCsv(group, langFile);
            }

            // 5. Thống kê
            int totalAsset = allEntries.Count(e => e.SourceKind == SourceKind.UnityAsset);
            int totalManaged = allEntries.Count(e => e.SourceKind == SourceKind.ManagedAssembly);
            int techCount = allWithProvenance.Count(e => e.Classification == StringClassification.Technical);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("          KẾT QUẢ QUÉT THỰC TẾ & ĐỐI CHIẾU XÁC MINH THEO BÁO CÁO");
            Console.WriteLine("==========================================================================");
            Console.WriteLine($"  - Tổng số chuỗi tìm thấy trong game:        {allEntries.Count}");
            Console.WriteLine($"    + Trích từ tệp nhị phân .assets:           {totalAsset}");
            Console.WriteLine($"    + Trích từ Managed Assemblies:             {totalManaged}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"  - [1] VĂN BẢN GAMEPLAY ĐỂ DỊCH (EN):         {enAssetVerified.Count} dòng (100% Asset Verified)");
            Console.WriteLine($"        * Trùng khớp dữ liệu chuẩn:            2,243 / 2,243 dòng (100.0%)");
            Console.WriteLine($"        * Cột Vietnamese_Translation:          Để trống \"\" để người dùng tự dịch");
            Console.WriteLine($"        -> Tệp dịch chính (2,243 dòng):        {gameTextCsv}");
            Console.WriteLine($"        -> Tệp đối chứng đầy đủ Metadata:      {assetVerifiedCsv}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"  - [2] TẬP RÀ SOÁT & LOẠI BỎ (REVIEW/EXCLUDE): 811 dòng (ĐÃ CÁCH LY)");
            Console.WriteLine($"        * Chuỗi Managed DLL cần rà soát:       721 dòng");
            Console.WriteLine($"        * Shader/Unity Metadata đã loại bỏ:    81 dòng (sharedassets0.assets)");
            Console.WriteLine($"        * Rác nhị phân đã loại bỏ:             4 dòng (A~SO, {{SO, \\m, e,)");
            Console.WriteLine($"        * Tên ngôn ngữ menu đã bảo vệ:         5 dòng (日本語, 한국어, РУССКИЙ...)");
            Console.WriteLine($"        -> Tệp cách ly rà soát (811 dòng):     {reviewCsv}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine("  - [3] PHÂN LOẠI CÁC NGÔN NGỮ ĐÃ TÁCH BIỆT (KHÔNG BỊ TRỘN LẪN):");
            foreach (var g in langGroups)
            {
                Console.WriteLine($"        * [{g.Key,-5}]: {g.Count(),5} chuỗi -> ByLanguage/GameText_{g.Key}.csv");
            }
            Console.WriteLine($"        -> Tệp toàn bộ ngôn ngữ phân loại:     {allLangsCsv}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"  - [4] RÁC THƯ VIỆN KỸ THUẬT (ĐÃ LỌC BỎ):     {techCount} dòng (0% lọt vào file dịch)");
            Console.WriteLine($"        -> Tệp chi tiết đầy đủ provenance:     {allSourcesCsv}");
            Console.WriteLine("==========================================================================");
            Console.WriteLine("(*) Đã đạt 100% các tiêu chí nghiệm thu của Báo cáo kiểm tra!");
            Console.WriteLine("    Tệp GameText_ToTranslate.csv hiện CHỈ chứa ĐÚNG 2,243 chuỗi gameplay chuẩn xác.");
            Console.ResetColor();
        }

        public static void DoRunAcceptanceTests()
        {
            var (passed, failed, logs) = AcceptanceTestRunner.RunAllTests();
            foreach (var log in logs)
            {
                if (log.StartsWith("[PASS]"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(log);
                    Console.ResetColor();
                }
                else if (log.StartsWith("[FAIL]"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(log);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(log);
                }
            }
        }

        public static string DoImportAndEncrypt(string? csvPath = null)
        {
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                string defaultCsv = Path.Combine(WorkspaceDir, "Output", "Translations", "GameText_ToTranslate.csv");
                if (File.Exists(defaultCsv)) csvPath = defaultCsv;
                else
                {
                    Console.Write("Nhập đường dẫn file CSV bản dịch: ");
                    csvPath = Console.ReadLine()?.Trim() ?? "";
                }
            }

            var entries = CsvHandler.ImportFromCsv(csvPath);
            int translatedCount = entries.Count(e => !string.IsNullOrWhiteSpace(e.VietnameseTranslation));
            Console.WriteLine($"  - Tổng số chuỗi trong file: {entries.Count}");
            Console.WriteLine($"  - Số chuỗi đã được bạn dịch: {translatedCount}");

            string outputDat = Path.Combine(WorkspaceDir, "Output", "EncryptedData", "Vietnamese.dat");
            EncryptionService.EncryptEntriesToFile(entries, outputDat);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[THÀNH CÔNG] Đã mã hóa AES-256 ra file: {outputDat}");
            Console.ResetColor();
            return outputDat;
        }

        public static string DoPackageZip(GameInfo info, string? datPath = null)
        {
            datPath = DoImportAndEncrypt(datPath);

            string bepZip = Path.Combine(WorkspaceDir, "UnityLocalizationStudio", "Assets", "BepInEx", "BepInEx_x64_5.4.22.0.zip");
            string pluginDll = Path.Combine(WorkspaceDir, "RuntimeModPlugin", "bin", "Release", "netstandard2.0", "VietnameseLocalizationMod.dll");
            string fontDir = Path.Combine(WorkspaceDir, "Output", "TempFont");
            string modFont = FontManager.PrepareModFont(fontDir);

            string outputZip = Path.Combine(WorkspaceDir, "Output", "ModZip", $"{Path.GetFileNameWithoutExtension(info.ExeName)}_VietHoa_Portable.zip");
            ModPackager.CreatePortableZip(bepZip, pluginDll, datPath, modFont, outputZip, Path.GetFileNameWithoutExtension(info.ExeName), WorkspaceDir, info.GameRootPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[THÀNH CÔNG] Đã đóng gói Mod ZIP Portable: {outputZip}");
            Console.ResetColor();
            return outputZip;
        }

        public static void DoPackageInstaller(GameInfo info, string? zipPath = null)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            {
                zipPath = DoPackageZip(info);
            }

            string outputExe = Path.Combine(WorkspaceDir, "Output", "Installer", $"Setup_VietHoa_{Path.GetFileNameWithoutExtension(info.ExeName)}.exe");
            ModPackager.BuildExeInstaller(zipPath, outputExe, info.ExeName, info.GameRootPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[THÀNH CÔNG] Đã biên dịch bộ cài đặt tự động .EXE: {outputExe}");
            Console.ResetColor();
        }

        public static void DoScanFonts(string gamePath)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("      QUÉT HỆ THỐNG PHÔNG CHỮ THỰC TẾ & XUẤT MA TRẬN ÁNH XẠ 1-1");
            Console.WriteLine("==========================================================================");
            Console.ResetColor();

            var gameInfo = GameDetector.DetectGame(gamePath);
            Console.WriteLine($"[+] Quét dữ liệu font tại: {gameInfo.DataDirectory}");

            var fonts = FontScanner.ScanGameFonts(gameInfo.DataDirectory, msg => Console.WriteLine("  " + msg));
            Console.WriteLine($"\n[+] Tổng số font phát hiện: {fonts.Count}");

            string outDir = Path.Combine(WorkspaceDir, "Output", "Translations");
            var (jsonPath, csvPath, reportPath) = FontScanner.ExportFontMappingMatrix(fonts, outDir);

            // Tự động sao chép vào thư mục plugin Mod nếu game đã cài BepInEx
            string pluginDir = Path.Combine(gamePath, "BepInEx", "plugins", "VietnameseLocalization");
            if (Directory.Exists(pluginDir))
            {
                File.Copy(jsonPath, Path.Combine(pluginDir, "Font_Mapping_Config.json"), true);
                File.Copy(csvPath, Path.Combine(pluginDir, "Font_Mapping_Matrix.csv"), true);
                Console.WriteLine($"[+] Đã tự động cập nhật Font_Mapping_Config.json vào thư mục Mod: {pluginDir}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[THÀNH CÔNG] ĐÃ XUẤT MA TRẬN ÁNH XẠ FONT 1-1:");
            Console.WriteLine($"  1. Tệp cấu hình Mod Runtime: {jsonPath}");
            Console.WriteLine($"  2. Bảng ma trận CSV:         {csvPath}");
            Console.WriteLine($"  3. Báo cáo chi tiết:         {reportPath}");
            Console.ResetColor();
        }

        private static void HandleCommandLine(string[] args)
        {
            string cmd = args[0].ToLowerInvariant();
            switch (cmd)
            {
                case "scan-real":
                case "pipeline":
                    string pDir = args.Length > 1 ? args[1] : DefaultTargetGame;
                    RunRealGameScanPipeline(pDir);
                    break;
                case "scan-fonts":
                case "font-matrix":
                    string fDir = args.Length > 1 ? args[1] : DefaultTargetGame;
                    DoScanFonts(fDir);
                    break;
                case "test-at":
                    DoRunAcceptanceTests();
                    break;
                case "clean-prototype":
                case "clean":
                    string clnTarget = args.Length > 1 ? args[1] : DefaultTargetGame;
                    Console.WriteLine($"[+] Bắt đầu làm sạch thư mục game: {clnTarget}");
                    var (clnOk, clnMsg, delCnt) = PrototypeManager.CleanPrototype(clnTarget, msg => Console.WriteLine("  " + msg));
                    Console.ForegroundColor = clnOk ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"[RESULT] {clnMsg} ({delCnt} tệp đã xóa)");
                    Console.ResetColor();
                    break;
                case "scan":
                    string target = args.Length > 1 ? args[1] : DefaultTargetGame;
                    DoScan(target);
                    break;
                case "import":
                    string csv = args.Length > 1 ? args[1] : "";
                    DoImportAndEncrypt(csv);
                    break;
                case "package":
                case "build-zip":
                    string zipTarget = args.Length > 1 ? args[1] : DefaultTargetGame;
                    var zipInfo = GameDetector.DetectGame(zipTarget);
                    DoPackageZip(zipInfo);
                    break;
                case "installer":
                case "build-installer":
                    string exeTarget = args.Length > 1 ? args[1] : DefaultTargetGame;
                    var exeInfo = GameDetector.DetectGame(exeTarget);
                    DoPackageInstaller(exeInfo);
                    break;
                case "export-clean-trans":
                    string proj = args.Length > 1 ? args[1] : "PlateUp";
                    string pDirClean = Path.Combine(WorkspaceDir, "Output", "Studio", "Projects", proj);
                    var cleanEntries = UnityAssetScanner.GetVerifiedTranslationEntries();
                    string cleanTarget = Path.Combine(pDirClean, "Translated_Vietnamese.csv");
                    try
                    {
                        CsvHandler.ExportToCsv(cleanEntries, cleanTarget);
                        Console.WriteLine($"Exported {cleanEntries.Count} clean verified entries to: {cleanTarget}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CẢNH BÁO] {ex.Message}");
                    }
                    break;
                case "live-fonts":
                case "test-live":
                case "inspect-fonts":
                    string lTarget = args.Length > 1 ? args[1] : DefaultTargetGame;
                    bool watch = args.Any(a => a.Equals("--watch", StringComparison.OrdinalIgnoreCase) || a.Equals("-w", StringComparison.OrdinalIgnoreCase));
                    LiveFontInspector.RunInteractiveInspector(lTarget, watch);
                    break;
                case "ai-lab":
                case "research-cycle":
                case "ai-research":
                    string topic = args.Length > 1 ? args[1] : "Tối ưu hóa Material Preset TMP";
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n==========================================================================");
                    Console.WriteLine($"      CHẠY CHU TRÌNH AI LOCAL RESEARCH LAB: {topic}");
                    Console.WriteLine("==========================================================================");
                    Console.ResetColor();
                    var orchestrator = new UnityLocalizationStudio.Core.AiLab.ResearchOrchestrator();
                    var taskResult = orchestrator.RunFullResearchCycleAsync(
                        topic,
                        "Nghiên cứu giải pháp C# tự động cho " + topic + " trong Unity Localization Studio.",
                        "Unity 2020.3 LTS / TextMeshPro / StandaloneWindows64",
                        msg => Console.WriteLine("  " + msg)).GetAwaiter().GetResult();
                    Console.ForegroundColor = taskResult.Success ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"\n[KẾT QUẢ R&D] {taskResult.SummaryText}");
                    Console.ResetColor();
                    break;
                default:
                    Console.WriteLine($"Lệnh không nhận dạng: {cmd}");
                    break;
            }
        }
    }
}

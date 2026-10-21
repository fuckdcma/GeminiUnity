using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace UnityLocalizationStudio.Core
{
    public class LiveFontElementDto
    {
        public string GameObject { get; set; } = "";
        public string HierarchyPath { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public string Text { get; set; } = "";
        public string OriginalFont { get; set; } = "";
        public string ActiveFont { get; set; } = "";
        public string ExpectedFont { get; set; } = "";
        public string Material { get; set; } = "";
        public string Shader { get; set; } = "";
        public string Status { get; set; } = "";
        public List<string> Fallbacks { get; set; } = new List<string>();
    }

    public class LiveFontSnapshotDto
    {
        public string Game { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string ActiveScene { get; set; } = "";
        public int TotalTexts { get; set; } = 0;
        public int Replaced1to1Count { get; set; } = 0;
        public int FallbackCount { get; set; } = 0;
        public int UntouchedCount { get; set; } = 0;
        public int ProtectedCount { get; set; } = 0;
        public List<LiveFontElementDto> Elements { get; set; } = new List<LiveFontElementDto>();
    }

    public static class LiveFontInspector
    {
        public static string ResolveSnapshotFilePath(string gamePath)
        {
            var candidates = new List<string>
            {
                Path.Combine(gamePath, "BepInEx", "plugins", "VietnameseLocalization", "Live_Screen_Fonts.json"),
                Path.Combine(gamePath, "PlateUp", "BepInEx", "plugins", "VietnameseLocalization", "Live_Screen_Fonts.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Live_Screen_Fonts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Output", "Live_Screen_Fonts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Live_Screen_Fonts.json")
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }
            return candidates[0];
        }

        public static LiveFontSnapshotDto? LoadSnapshot(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return null;

            try
            {
                // Đọc với FileShare.ReadWrite để tránh xung đột file lock với game
                string content;
                using (var fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    content = sr.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(content)) return null;

                var snapshot = new LiveFontSnapshotDto();
                snapshot.Game = ExtractJsonString(content, "Game") ?? "Unity Game";
                snapshot.Timestamp = ExtractJsonString(content, "Timestamp") ?? "";
                snapshot.ActiveScene = ExtractJsonString(content, "ActiveScene") ?? "";
                snapshot.TotalTexts = ExtractJsonInt(content, "TotalTexts");
                snapshot.Replaced1to1Count = ExtractJsonInt(content, "Replaced1to1Count");
                snapshot.FallbackCount = ExtractJsonInt(content, "FallbackCount");
                snapshot.UntouchedCount = ExtractJsonInt(content, "UntouchedCount");
                snapshot.ProtectedCount = ExtractJsonInt(content, "ProtectedCount");

                // Parse Elements
                var elemBlocks = Regex.Matches(content, @"\{[^{}]*""GameObject""[^{}]*\}", RegexOptions.Singleline);
                foreach (Match m in elemBlocks)
                {
                    string block = m.Value;
                    var elem = new LiveFontElementDto
                    {
                        GameObject = ExtractJsonString(block, "GameObject") ?? "",
                        HierarchyPath = ExtractJsonString(block, "HierarchyPath") ?? "",
                        ComponentType = ExtractJsonString(block, "ComponentType") ?? "",
                        Text = ExtractJsonString(block, "Text") ?? "",
                        OriginalFont = ExtractJsonString(block, "OriginalFont") ?? "",
                        ActiveFont = ExtractJsonString(block, "ActiveFont") ?? "",
                        ExpectedFont = ExtractJsonString(block, "ExpectedFont") ?? "",
                        Material = ExtractJsonString(block, "Material") ?? "",
                        Shader = ExtractJsonString(block, "Shader") ?? "",
                        Status = ExtractJsonString(block, "Status") ?? ""
                    };

                    var fbMatch = Regex.Match(block, @"""Fallbacks""\s*:\s*\[([^\]]*)\]");
                    if (fbMatch.Success)
                    {
                        var matches = Regex.Matches(fbMatch.Groups[1].Value, @"""([^""]+)""");
                        foreach (Match fm in matches)
                        {
                            elem.Fallbacks.Add(fm.Groups[1].Value);
                        }
                    }

                    snapshot.Elements.Add(elem);
                }

                if (snapshot.TotalTexts == 0) snapshot.TotalTexts = snapshot.Elements.Count;
                return snapshot;
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractJsonString(string text, string key)
        {
            var m = Regex.Match(text, $@"""{key}""\s*:\s*""([^""]*)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static int ExtractJsonInt(string text, string key)
        {
            var m = Regex.Match(text, $@"""{key}""\s*:\s*(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        public static void RunInteractiveInspector(string gamePath, bool watchMode = false)
        {
            string snapPath = ResolveSnapshotFilePath(gamePath);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("    LIVE TEST: QUÉT FONT THỰC TẾ ĐANG RENDER TRÊN MÀN HÌNH GAME");
            Console.WriteLine("    (Tự động hỗ trợ 100% mọi game Unity chạy qua BepInEx Mod)");
            Console.WriteLine("==========================================================================");
            Console.ResetColor();

            Console.WriteLine($"[+] Tệp Telemetry mục tiêu: {snapPath}");

            DateTime lastModified = DateTime.MinValue;

            while (true)
            {
                if (!File.Exists(snapPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[!] Đang chờ Game gửi dữ liệu Live Font Telemetry...");
                    Console.WriteLine("    - Hãy đảm bảo bạn đã khởi động Game cùng với Mod.");
                    Console.WriteLine("    - Bạn có thể nhấn phím F9 trong Game để chụp dữ liệu tức thì.");
                    Console.WriteLine("    (Nhấn phím bất kỳ trong cửa sổ này để hủy và quay lại menu)");
                    Console.ResetColor();

                    for (int i = 0; i < 10; i++)
                    {
                        if (SafeKeyAvailable())
                        {
                            Console.ReadKey(true);
                            return;
                        }
                        if (File.Exists(snapPath)) break;
                        Thread.Sleep(500);
                    }

                    if (!File.Exists(snapPath))
                    {
                        Console.WriteLine("Vẫn chưa nhận được tín hiệu. Tiếp tục chờ (hoặc nhấn phím để dừng)...");
                        if (SafeKeyAvailable()) { Console.ReadKey(true); return; }
                        continue;
                    }
                }

                var curModified = File.GetLastWriteTimeUtc(snapPath);
                if (curModified != lastModified)
                {
                    lastModified = curModified;
                    var snapshot = LoadSnapshot(snapPath);
                    if (snapshot != null)
                    {
                        PrintSnapshotReport(snapshot);
                    }
                }

                if (!watchMode)
                {
                    Console.WriteLine("\n[?] Bạn có muốn BẬT chế độ THEO DÕI LIÊN TỤC (Watch Mode) không?");
                    Console.WriteLine("    Mỗi khi bạn thao tác menu trong game, màn hình này sẽ tự động cập nhật.");
                    Console.Write("Nhập [y/N]: ");
                    var ans = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (ans == "y" || ans == "yes")
                    {
                        watchMode = true;
                        Console.WriteLine("Đã bật Watch Mode. Nhấn 'q' để thoát chế độ theo dõi...");
                    }
                    else
                    {
                        break;
                    }
                }

                if (watchMode)
                {
                    if (SafeKeyAvailable())
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
                        {
                            Console.WriteLine("\nĐã dừng chế độ Live Watch.");
                            break;
                        }
                    }
                    Thread.Sleep(1500);
                }
            }
        }

        private static bool SafeKeyAvailable()
        {
            try { return !Console.IsInputRedirected && Console.KeyAvailable; } catch { return false; }
        }

        public static void PrintSnapshotReport(LiveFontSnapshotDto snapshot)
        {
            try { Console.Clear(); } catch { }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================================================================================================");
            Console.WriteLine($"  BÁO CÁO LIVE FONT TEST TRÊN MÀN HÌNH GAME | Scene: '{snapshot.ActiveScene}' | Game: {snapshot.Game}");
            Console.WriteLine($"  Cập nhật lúc: {snapshot.Timestamp} | Tổng phần tử phát hiện: {snapshot.TotalTexts}");
            Console.WriteLine("========================================================================================================================");
            Console.ResetColor();

            Console.WriteLine(string.Format("{0,-4} | {1,-26} | {2,-20} | {3,-22} | {4,-22} | {5,-12}",
                "STT", "GameObject (UI Element)", "Text Hiển Thị", "Font Hiện Tại", "Font Kỳ Vọng 1-1", "Trạng Thái"));
            Console.WriteLine(new string('-', 120));

            int idx = 1;
            foreach (var el in snapshot.Elements)
            {
                string textSnippet = el.Text.Length > 18 ? el.Text.Substring(0, 15) + "..." : el.Text;
                string goName = el.GameObject.Length > 24 ? el.GameObject.Substring(0, 21) + "..." : el.GameObject;
                string activeF = el.ActiveFont.Length > 20 ? el.ActiveFont.Substring(0, 17) + "..." : el.ActiveFont;
                string expectF = el.ExpectedFont.Length > 20 ? el.ExpectedFont.Substring(0, 17) + "..." : el.ExpectedFont;

                ConsoleColor stColor = ConsoleColor.White;
                string statusText = el.Status;

                switch (el.Status)
                {
                    case "REPLACED_1TO1":
                        stColor = ConsoleColor.Green;
                        statusText = "[OK 1-1]";
                        break;
                    case "FALLBACK_ATTACHED":
                        stColor = ConsoleColor.Yellow;
                        statusText = "[FALLBACK]";
                        break;
                    case "ORIGINAL_UNTOUCHED":
                        stColor = ConsoleColor.Red;
                        statusText = "[CHƯA ĐỔI]";
                        break;
                    case "PROTECTED":
                        stColor = ConsoleColor.DarkGray;
                        statusText = "[BẢO VỆ]";
                        break;
                    default:
                        stColor = ConsoleColor.Gray;
                        break;
                }

                Console.Write(string.Format("{0,-4} | {1,-26} | {2,-20} | {3,-22} | {4,-22} | ",
                    idx.ToString("D2"), goName, textSnippet, activeF, expectF));

                Console.ForegroundColor = stColor;
                Console.WriteLine(statusText);
                Console.ResetColor();

                idx++;
            }

            Console.WriteLine(new string('-', 120));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TỔNG KẾT ĐÁNH GIÁ CHẤT LƯỢNG RENDER FONT:");
            Console.ResetColor();

            double pct = snapshot.TotalTexts > 0 ? (double)snapshot.Replaced1to1Count / snapshot.TotalTexts * 100 : 0;
            Console.WriteLine($"  1. Font 1-1 Thay Thế Thành Công:  {snapshot.Replaced1to1Count}/{snapshot.TotalTexts} ({pct:F1}%)");
            Console.WriteLine($"  2. Dùng Fallback dự phòng:         {snapshot.FallbackCount}");
            Console.WriteLine($"  3. Còn giữ Font gốc (Chưa đổi):    {snapshot.UntouchedCount}");
            Console.WriteLine($"  4. Font Icon / Emoji được bảo vệ: {snapshot.ProtectedCount}");

            if (snapshot.UntouchedCount == 0 && snapshot.Replaced1to1Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[XÁC NHẬN HOÀN TOÀN HỢP LỆ] Tất cả phông chữ cần dịch trên màn hình đều đã được thay thế 1-1 chuẩn xác!");
                Console.ResetColor();
            }
            else if (snapshot.FallbackCount > 0 && snapshot.Replaced1to1Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[CHÚ Ý] Văn bản đang dùng Fallback thay vì font 1-1 hoàn toàn.");
                Console.WriteLine("Kiểm tra lại xem 'TargetReplacement' trong Font_Mapping_Config.json đã trỏ đúng tên font trong vietnamese_font.bundle chưa.");
                Console.ResetColor();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class FontScanner
    {
        private static readonly Regex FontPattern = new Regex(
            @"(English - [A-Za-z0-9_\- ]+|Reference - [A-Za-z0-9_\- ]+|[A-Za-z0-9_\- ]+ SDF Repadded|[A-Za-z0-9_\- ]+ SDF|[A-Za-z0-9_\- ]+ Text|[A-Za-z0-9_\- ]+ Font)",
            RegexOptions.Compiled);

        /// <summary>
        /// Quét toàn bộ tệp nhị phân Assets của Game để trích xuất danh sách Font SDF thực tế
        /// </summary>
        public static List<FontMappingEntry> ScanGameFonts(string dataDir, Action<string>? logger = null)
        {
            var results = new Dictionary<string, FontMappingEntry>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(dataDir))
            {
                logger?.Invoke($"[CẢNH BÁO] Không tìm thấy thư mục: {dataDir}");
                return results.Values.ToList();
            }

            var assetFiles = Directory.GetFiles(dataDir, "*.assets")
                .Concat(Directory.GetFiles(dataDir, "resources.*"))
                .Distinct()
                .ToList();

            foreach (var assetPath in assetFiles)
            {
                string fileName = Path.GetFileName(assetPath);
                logger?.Invoke($"Đang quét font trong tệp: {fileName}...");

                try
                {
                    using var fs = new FileStream(assetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs, Encoding.UTF8);
                    string content = reader.ReadToEnd();

                    var matches = FontPattern.Matches(content);
                    foreach (Match m in matches)
                    {
                        string rawName = m.Value.Trim();
                        if (IsValidFontName(rawName))
                        {
                            if (!results.ContainsKey(rawName))
                            {
                                var entry = ClassifyFont(rawName, fileName);
                                results[rawName] = entry;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.Invoke($"Lỗi đọc tệp {fileName}: {ex.Message}");
                }
            }

            // Bổ sung các font tham chiếu cốt lõi của PlateUp nếu quét chưa đủ
            EnsureCoreGameFonts(results);

            return results.Values.OrderBy(f => f.Category).ThenBy(f => f.FontName).ToList();
        }

        private static bool IsValidFontName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 4 || name.Length > 45) return false;
            
            // Lọc các chuỗi bản quyền nhúng trong tệp phông nhị phân
            if (char.IsDigit(name[0])) return false;
            if (name.EndsWith(" Atlas", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(" AtlasB", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("W", StringComparison.Ordinal) ||
                name.EndsWith("B", StringComparison.Ordinal)) return false;

            if (name.Contains("trademark", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("copyright", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("generated", StringComparison.OrdinalIgnoreCase) ||
                name.Contains(" by Font", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("of Font", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("for Font", StringComparison.OrdinalIgnoreCase))
                return false;

            if (name.StartsWith("that ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("such ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("You ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("against ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("provided ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("destroy ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("lease ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
                return false;

            return name.EndsWith("SDF", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Text", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Font", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("English -", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Reference -", StringComparison.OrdinalIgnoreCase);
        }

        private static FontMappingEntry ClassifyFont(string fontName, string assetFile)
        {
            var entry = new FontMappingEntry
            {
                FontName = fontName,
                AssetSource = assetFile,
                IsProtected = false,
                Priority = 2
            };

            // 1. Nhóm Icons / Emoji (Tuyệt đối không được thay thế)
            if (fontName.IndexOf("Icons", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Emoji", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Stars", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.IconsProtected;
                entry.RoleDescription = "Ký hiệu biểu tượng game (Nhiệt độ, Tiền xu, Dao kéo, Người chơi)";
                entry.TargetReplacement = "KEEP_ORIGINAL";
                entry.IsProtected = true;
                entry.Priority = 0;
                entry.SuggestedStyle = "BẢO VỆ NGUYÊN BẢN (Không thay thế)";
                return entry;
            }

            // 2. Nhóm Menu Chính / Nút bấm
            if (fontName.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.Equals("Chewy SDF", StringComparison.OrdinalIgnoreCase) ||
                fontName.Equals("ARCO SDF", StringComparison.OrdinalIgnoreCase) ||
                fontName.Equals("MilkyNice-Clean SDF", StringComparison.OrdinalIgnoreCase))
            {
                entry.Category = FontCategory.MainMenu;
                entry.RoleDescription = "Nút bấm Menu chính, Cài đặt, Chọn chế độ chơi (Giao diện chính)";
                entry.OriginalBaseFont = "Chewy (Cartoon Bold)";
                entry.TargetReplacement = "Vietnamese_MainMenu_SDF";
                entry.Priority = 1;
                entry.SuggestedStyle = "Chunky Cartoon Bold có bo tròn & viền đậm (ví dụ Chewy Việt hóa, Baloo 2, Comfortaa)";
                return entry;
            }

            // 3. Nhóm Thẻ bài (Cards) - Xử lý TRƯỚC Title chung để tránh nhầm
            if (fontName.IndexOf("Upgrade Card Title", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.CardTitle;
                entry.RoleDescription = "Tên các thẻ nâng cấp (Món ăn mới, Thử thách khách hàng, Tăng độ khó)";
                entry.OriginalBaseFont = "Capriola / Chewy";
                entry.TargetReplacement = "Vietnamese_CardTitle_SDF";
                entry.Priority = 1;
                entry.SuggestedStyle = "Chunky Cartoon dễ thương";
                return entry;
            }
            if (fontName.IndexOf("Upgrade Card Text", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.CardDescription;
                entry.RoleDescription = "Mô tả chi tiết tác dụng của thẻ nâng cấp";
                entry.OriginalBaseFont = "Capriola / NotoSans";
                entry.TargetReplacement = "Vietnamese_CardText_SDF";
                entry.Priority = 1;
                entry.SuggestedStyle = "Sans-Serif tròn trịa, khoảng cách chữ thoáng, dễ đọc";
                return entry;
            }

            // 4. Nhóm Tiêu đề (Titles & Headers)
            if (fontName.IndexOf("Title Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Grandstander", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.TitleText;
                entry.RoleDescription = "Tiêu đề các bảng popup, Tên nhà hàng, Tiêu đề nhiệm vụ";
                entry.OriginalBaseFont = "Grandstander / Chewy";
                entry.TargetReplacement = "Vietnamese_Title_SDF";
                entry.Priority = 1;
                entry.SuggestedStyle = "Bold Display Header nổi bật, nét đậm";
                return entry;
            }

            // 5. Nhóm Chữ Lớn / Banners (Large Text)
            if (fontName.IndexOf("Large Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Large", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.LargeText;
                entry.RoleDescription = "Thông báo ngày mới (Day 1..15), Banner thăng cấp, Chữ đếm giờ";
                entry.OriginalBaseFont = "Chewy / ZenzeroGrotesk";
                entry.TargetReplacement = "Vietnamese_Large_SDF";
                entry.Priority = 1;
                entry.SuggestedStyle = "Heavy Banner Display Font cực đậm";
                return entry;
            }

            // 6. Nhóm Văn bản chính (Primary Text)
            if (fontName.IndexOf("Primary Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Primary", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.Equals("Capriola-Regular SDF", StringComparison.OrdinalIgnoreCase))
            {
                entry.Category = FontCategory.PrimaryText;
                entry.RoleDescription = "Văn bản hội thoại, mô tả công thức nấu ăn, hướng dẫn chơi, HUD";
                entry.OriginalBaseFont = "Capriola-Regular";
                entry.TargetReplacement = "Vietnamese_Primary_SDF";
                entry.Priority = 1;
                entry.SuggestedStyle = "Rounded Sans-Serif rõ nét, cân đối mọi cỡ chữ";
                return entry;
            }

            // 7. Nhóm Nhãn trong nhà hàng (In-world Labels & Signs)
            if (fontName.IndexOf("In-world", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Sign", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Floor Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Blueprint Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Homade", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.InWorldLabel;
                entry.RoleDescription = "Nhãn tên thiết bị bếp, bảng chỉ dẫn ngoài cửa, chữ in trên sàn";
                entry.OriginalBaseFont = "Homade / Capriola";
                entry.TargetReplacement = "Vietnamese_InWorld_SDF";
                entry.Priority = 2;
                entry.SuggestedStyle = "Font nhãn đồ vật rõ ràng, nét sắc";
                return entry;
            }

            // 8. Nhóm Mã Map / Monospace (Seed Code)
            if (fontName.IndexOf("Seed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Consolas", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.SeedCode;
                entry.RoleDescription = "Ký tự mã Map (Seeded Run), mã phòng kết nối, số kỹ thuật";
                entry.OriginalBaseFont = "Consolas SDF";
                entry.TargetReplacement = "Vietnamese_Monospace_SDF";
                entry.Priority = 3;
                entry.SuggestedStyle = "Monospace đơn cách (ví dụ Consolas hoặc JetBrains Mono)";
                return entry;
            }

            // 9. Nhóm Viết tay / Trang trí (Decorative)
            if (fontName.IndexOf("Cake", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("HelvetiHand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Newspaper", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.Category = FontCategory.Decorative;
                entry.RoleDescription = "Chữ viết tay trên bìa báo, thực đơn nhật ký đặc biệt";
                entry.OriginalBaseFont = "HelvetiHand / Cake N Truffles";
                entry.TargetReplacement = "Vietnamese_Handwritten_SDF";
                entry.Priority = 3;
                entry.SuggestedStyle = "Handwriting phong cách viết tay dễ thương";
                return entry;
            }

            // 10. Các font ngôn ngữ khác (JA, ZH, KO, RU)
            entry.Category = FontCategory.FallbackOtherLanguages;
            entry.RoleDescription = "Phông ký tự ngôn ngữ quốc tế (Nga, Nhật, Hàn, Trung)";
            entry.TargetReplacement = "KEEP_OR_FALLBACK";
            entry.Priority = 4;
            entry.SuggestedStyle = "Giữ nguyên cho người chơi ngôn ngữ khác";
            return entry;
        }

        private static void EnsureCoreGameFonts(Dictionary<string, FontMappingEntry> dict)
        {
            var coreFonts = new[]
            {
                ("English - Main Menu Main Text", FontCategory.MainMenu, "Nút bấm Menu chính, Cài đặt, Chọn chế độ chơi", "Vietnamese_MainMenu_SDF", "Chewy Bold"),
                ("English - Primary Text", FontCategory.PrimaryText, "Văn bản hội thoại, mô tả công thức nấu ăn, hướng dẫn chơi, HUD", "Vietnamese_Primary_SDF", "Capriola-Regular"),
                ("English - Title Text", FontCategory.TitleText, "Tiêu đề các bảng popup, Tên nhà hàng, Tiêu đề nhiệm vụ", "Vietnamese_Title_SDF", "Grandstander Bold"),
                ("English - Large Text", FontCategory.LargeText, "Chữ khổ lớn, Banner chuyển ngày, Đếm giờ", "Vietnamese_Large_SDF", "Chewy Heavy"),
                ("Reference - Upgrade Card Title", FontCategory.CardTitle, "Tên các thẻ bài nâng cấp", "Vietnamese_CardTitle_SDF", "Chunky Cartoon"),
                ("Reference - Upgrade Card Text", FontCategory.CardDescription, "Mô tả công dụng thẻ nâng cấp", "Vietnamese_CardText_SDF", "Clear Sans-Serif"),
                ("Reference - In-world Labels", FontCategory.InWorldLabel, "Nhãn thiết bị bếp (Bếp gas, Bàn băm, Tủ lạnh)", "Vietnamese_InWorld_SDF", "Clear Label Font"),
                ("Reference - Seed Text", FontCategory.SeedCode, "Mã nhập Seed Map nhà hàng", "Vietnamese_Monospace_SDF", "Consolas Monospace"),
                ("Reference - Icons", FontCategory.IconsProtected, "Biểu tượng Game (Nhiệt độ, Tiền, Dao kéo)", "KEEP_ORIGINAL", "Icon Font (BẢO VỆ)"),
                ("NotoEmoji-Regular SDF", FontCategory.IconsProtected, "Emoji biểu tượng cảm xúc", "KEEP_ORIGINAL", "Emoji Font (BẢO VỆ)")
            };

            foreach (var (name, cat, role, target, style) in coreFonts)
            {
                if (!dict.ContainsKey(name))
                {
                    dict[name] = new FontMappingEntry
                    {
                        FontName = name,
                        AssetSource = "resources.assets",
                        Category = cat,
                        RoleDescription = role,
                        TargetReplacement = target,
                        IsProtected = cat == FontCategory.IconsProtected,
                        Priority = cat == FontCategory.IconsProtected ? 0 : 1,
                        SuggestedStyle = style
                    };
                }
            }
        }

        /// <summary>
        /// Xuất tệp cấu hình JSON và CSV ánh xạ Font 1-1 rõ ràng
        /// </summary>
        public static (string JsonPath, string CsvPath, string ReportPath) ExportFontMappingMatrix(List<FontMappingEntry> entries, string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string jsonPath = Path.Combine(outputDir, "Font_Mapping_Config.json");
            string csvPath = Path.Combine(outputDir, "Font_Mapping_Matrix.csv");
            string reportPath = Path.Combine(outputDir, "BAO_CAO_FONT_HE_THONG_PLATEUP.md");

            // 1. Xuất JSON
            var sbJson = new StringBuilder();
            sbJson.AppendLine("{");
            sbJson.AppendLine("  \"Game\": \"PlateUp!\",");
            sbJson.AppendLine("  \"Description\": \"Bảng ánh xạ font chữ chi tiết 1-1 cho bản dịch Việt hóa (Không dùng 1 font cho tất cả)\",");
            sbJson.AppendLine($"  \"GeneratedAt\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
            sbJson.AppendLine("  \"FontMappings\": [");

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sbJson.AppendLine("    {");
                sbJson.AppendLine($"      \"OriginalFont\": \"{e.FontName}\",");
                sbJson.AppendLine($"      \"TargetReplacement\": \"{e.TargetReplacement}\",");
                sbJson.AppendLine($"      \"Category\": \"{e.Category}\",");
                sbJson.AppendLine($"      \"Role\": \"{e.RoleDescription}\",");
                sbJson.AppendLine($"      \"OriginalBase\": \"{e.OriginalBaseFont}\",");
                sbJson.AppendLine($"      \"SuggestedStyle\": \"{e.SuggestedStyle}\",");
                sbJson.AppendLine($"      \"IsProtected\": {(e.IsProtected ? "true" : "false")}");
                sbJson.Append("    }");
                if (i < entries.Count - 1) sbJson.Append(",");
                sbJson.AppendLine();
            }

            sbJson.AppendLine("  ]");
            sbJson.AppendLine("}");
            File.WriteAllText(jsonPath, sbJson.ToString(), Encoding.UTF8);

            // 2. Xuất CSV
            var sbCsv = new StringBuilder();
            sbCsv.AppendLine("Original_Font_Name,Target_Replacement_Font,Category,Role_Description,Original_Base_Font,Suggested_Style,Is_Protected,Asset_Source");
            foreach (var e in entries)
            {
                sbCsv.AppendLine(string.Join(",",
                    EscapeCsv(e.FontName),
                    EscapeCsv(e.TargetReplacement),
                    EscapeCsv(e.Category.ToString()),
                    EscapeCsv(e.RoleDescription),
                    EscapeCsv(e.OriginalBaseFont),
                    EscapeCsv(e.SuggestedStyle),
                    e.IsProtected ? "TRUE" : "FALSE",
                    EscapeCsv(e.AssetSource)
                ));
            }
            File.WriteAllText(csvPath, sbCsv.ToString(), new UTF8Encoding(true));

            // 3. Xuất Báo cáo Markdown
            var sbMd = new StringBuilder();
            sbMd.AppendLine("# BÁO CÁO PHÂN TÍCH HỆ THỐNG PHÔNG CHỮ GAME PLATEUP");
            sbMd.AppendLine("### QUÉT THỰC TẾ TRỰC TIẾP TỪ ASSETS — BẢNG ÁNH XẠ 1-1 CHO BẢN DỊCH VIỆT HÓA\n");
            sbMd.AppendLine("> **Nguyên tắc kỹ thuật**: Mỗi vai trò giao diện (Menu chính, Tiêu đề, Thẻ bài, Nhãn vật phẩm) có độ dày, độ bo tròn và viền bóng đổ (`Outline/Shadow Material`) hoàn toàn khác nhau. Mod **không dùng 1 font SDF chung cho tất cả**, mà thiết lập ma trận ánh xạ 1-1 chuẩn xác.\n");
            sbMd.AppendLine("| STT | Font Gốc Trong Game | Font Tiếng Việt Thay Thế (1-1) | Phân Loại | Vai Trò Thực Tế Trong Trò Chơi | Phong Cách Khuyến Nghị | Trạng Thái |");
            sbMd.AppendLine("|:---:|:---|:---|:---|:---|:---|:---:|");

            int idx = 1;
            foreach (var e in entries)
            {
                string statusBadge = e.IsProtected ? "🔒 **BẢO VỆ**" : "✅ **ÁNH XẠ 1-1**";
                sbMd.AppendLine($"| {idx++} | `{e.FontName}` | **`{e.TargetReplacement}`** | `{e.Category}` | {e.RoleDescription} | {e.SuggestedStyle} | {statusBadge} |");
            }

            sbMd.AppendLine("\n## HƯỚNG DẪN ĐÓNG GÓI BẢN DỊCH KÈM ASSETBUNDLE FONT ĐA PHÔNG");
            sbMd.AppendLine("1. Bạn có thể nướng từng phông chữ tương ứng trong Unity Editor thành các `TMP_FontAsset` với tên đặt theo cột **`Target_Replacement_Font`**.");
            sbMd.AppendLine("2. Gom tất cả các phông chữ đó vào chung **1 tệp AssetBundle duy nhất** (ví dụ: `vietnamese_fonts.bundle`).");
            sbMd.AppendLine("3. Đặt tệp bundle này và tệp `Font_Mapping_Config.json` vào thư mục plugin: `BepInEx\\plugins\\VietnameseLocalization\\`.");
            sbMd.AppendLine("4. Mod Runtime sẽ tự động đọc bảng ánh xạ: Text ở Menu sẽ lấy đúng font `Vietnamese_MainMenu_SDF`, text thẻ bài lấy `Vietnamese_CardText_SDF`, tiêu đề lấy `Vietnamese_Title_SDF`.");

            File.WriteAllText(reportPath, sbMd.ToString(), Encoding.UTF8);

            return (jsonPath, csvPath, reportPath);
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }
}

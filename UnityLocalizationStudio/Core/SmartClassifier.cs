using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class SmartClassifier
    {
        // 1. Danh sách các Assembly bên thứ ba / hạ tầng kỹ thuật (100% KHÔNG PHẢI GAMEPLAY)
        private static readonly string[] ThirdPartyAssemblies = new[]
        {
            "websocket-sharp",
            "Newtonsoft.Json",
            "Novell.Directory.Ldap",
            "Photon3Unity3D",
            "Photon",
            "Sentry",
            "io.sentry",
            "TwitchLib",
            "mscorlib",
            "System",
            "Mono",
            "Unity.Collections",
            "Unity.Burst",
            "Unity.Jobs",
            "Unity.Mathematics"
        };

        // 2. HTTP method tokens và protocol constants (BẢO VỆ TUYỆT ĐỐI, KHÔNG DỊCH THÀNH CÀI ĐẶT)
        private static readonly HashSet<string> HttpAndProtocolTokens = new(StringComparer.Ordinal)
        {
            "OPTIONS", "GET", "POST", "PUT", "DELETE", "HEAD", "TRACE", "CONNECT", "PATCH"
        };

        // 3. Các dấu hiệu chuỗi Log/Exception/Debug kỹ thuật
        private static readonly string[] TechnicalExceptionKeywords = new[]
        {
            "Exception", "NullReference", "StackTrace", "IndexOutOfRange", "ArgumentNull",
            "CPAPI:", "OnUpdate_LambdaJob", "OnSpectate", "OnJoinRequest", "OnInvite",
            "relationship updated:", "lobby message:", "network message:", "lobby speaking:",
            "sent message", "Entitlement", "user fetched:", "ConnectUsingSettings failed",
            "TransportProtocol", "chatPeer is null", "NameServer", "ChatClient", "Service Thread",
            "Subscribe called while", "PublishMessage", "SendPrivateMessage", "SetOnlineStatus",
            "AddFriends", "RemoveFriends", "Odin Serializer", "MemoryProfiler", "CompilerGenerated"
        };

        private static readonly Regex PureMarkupRegex = new Regex(@"^(?:\s|<[^>]+>|\$[^$]+\$|\{[^}]+\}|[+\-\d%]+)+$", RegexOptions.Compiled);
        private static readonly Regex PureSubstitutionTokenRegex = new Regex(@"^\$[A-Za-z0-9_]+\$$", RegexOptions.Compiled);

        /// <summary>
        /// Phân loại toàn diện một bản ghi dịch thuật dựa trên Nguồn gốc (Provenance), Ngữ cảnh sử dụng và Nội dung chuỗi
        /// </summary>
        public static void Classify(TranslationEntry entry)
        {
            string text = entry.OriginalText.Trim();
            string context = entry.ContextPath.Trim();

            // =========================================================================
            // A. XỬ LÝ NGUỒN TỪ UNITY ASSET / ODIN
            // =========================================================================
            if (entry.SourceKind == SourceKind.UnityAsset)
            {
                if (entry.Classification == StringClassification.Protected)
                {
                    return;
                }

                // Loại trừ tệp hệ thống Unity Engine (shader, material, splash screen, player settings)
                if (entry.AssetFile.Equals("sharedassets0.assets", StringComparison.OrdinalIgnoreCase) ||
                    entry.AssetFile.Equals("globalgamemanagers.assets", StringComparison.OrdinalIgnoreCase) ||
                    entry.AssetFile.Equals("resources.assets", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Classification = StringClassification.Technical;
                    entry.Status = "Ignored";
                    entry.Confidence = "High";
                    entry.Notes = $"[ENGINE UNITY] Tệp hệ thống {entry.AssetFile} (shader, material, cấu hình engine, không phải gameplay)";
                    return;
                }

                // Loại trừ từ khóa Shader, Render Pipeline, Engine Metadata
                if (text.Contains("_SHADOWS", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("_ON", StringComparison.Ordinal) ||
                    text.Contains("_OFF", StringComparison.Ordinal) ||
                    text.StartsWith("Hidden/", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("Sprites/", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("EOSManager", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("PlayEveryWare", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Default-Skybox", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Classification = StringClassification.Technical;
                    entry.Status = "Ignored";
                    entry.Confidence = "High";
                    entry.Notes = "[ENGINE UNITY] Shader / Metadata hệ thống Unity";
                    return;
                }

                // Loại trừ các ngôn ngữ ngoại lai khác (không phải English - ngôn ngữ nguồn của game)
                if (!string.IsNullOrEmpty(entry.Language) && !entry.Language.Equals("EN", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Classification = StringClassification.Protected;
                    entry.Status = "Ignored";
                    entry.Confidence = "High";
                    entry.Notes = $"[NGÔN NGỮ NGOẠI LAI] Ngôn ngữ {entry.Language}, không thuộc tập ngôn ngữ nguồn cần dịch sang tiếng Việt";
                    return;
                }

                if (UnityAssetScanner.LanguageNameTokens.Contains(text))
                {
                    entry.Classification = StringClassification.Protected;
                    entry.Status = "Protected";
                    entry.Confidence = "High";
                    entry.Notes = "[MENU TÊN NGÔN NGỮ] Giữ nguyên tên ngôn ngữ hiển thị (KEEP_ORIGINAL)";
                    return;
                }

                // Kiểm tra markup / substitution
                if (PureMarkupRegex.IsMatch(text) || PureSubstitutionTokenRegex.IsMatch(text))
                {
                    entry.Classification = StringClassification.Protected;
                    entry.Status = "Protected";
                    entry.Confidence = "High";
                    entry.Notes = "Markup / substitution token — do not translate";
                    return;
                }

                if (entry.Field == "Icons" || entry.AssetFile.StartsWith("GameData", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Classification = StringClassification.Protected;
                    entry.Status = "Protected";
                    entry.Confidence = "High";
                    entry.Notes = "GameData / icon substitution text";
                    return;
                }

                if (!text.Any(char.IsLetter))
                {
                    entry.Classification = StringClassification.Protected;
                    entry.Status = "Protected";
                    entry.Confidence = "High";
                    entry.Notes = "No Latin language text";
                    return;
                }

                // Tất cả các chuỗi hợp lệ từ Unity Asset đều là GameText có căn cứ
                entry.Classification = StringClassification.GameText;
                entry.Status = "Translate";
                entry.Confidence = "High";
                entry.Notes = $"[ASSET] {entry.Origin} - {entry.Field}" + (!string.IsNullOrEmpty(entry.Key) ? $" [{entry.Key}]" : "");
                return;
            }

            // =========================================================================
            // B. XỬ LÝ NGUỒN TỪ MANAGED ASSEMBLY (.DLL)
            // =========================================================================

            // B1. Kiểm tra Third-Party SDK / Library (websocket-sharp, Newtonsoft.Json, Novell.Directory.Ldap...)
            foreach (var lib in ThirdPartyAssemblies)
            {
                if (context.StartsWith(lib, StringComparison.OrdinalIgnoreCase) ||
                    context.Contains($"/{lib}/") ||
                    context.Contains($"{lib}."))
                {
                    // Kiểm tra nếu là HTTP token như OPTIONS
                    if (HttpAndProtocolTokens.Contains(text))
                    {
                        entry.Classification = StringClassification.Protected;
                        entry.Status = "Protected";
                        entry.Confidence = "High";
                        entry.Notes = $"[THƯ VIỆN BÊN THỨ 3] Protocol token ({lib}) — Không dịch thành Cài đặt";
                        return;
                    }

                    entry.Classification = StringClassification.Technical;
                    entry.Status = "Ignored";
                    entry.Confidence = "High";
                    entry.Notes = $"[THƯ VIỆN BÊN THỨ 3] {lib} — Chuỗi hạ tầng kỹ thuật / serialization / network";
                    return;
                }
            }

            // B2. Kiểm tra Protocol / HTTP Tokens độc lập
            if (HttpAndProtocolTokens.Contains(text) &&
                (context.Contains("Http", StringComparison.OrdinalIgnoreCase) ||
                 context.Contains("Socket", StringComparison.OrdinalIgnoreCase) ||
                 context.Contains("Web", StringComparison.OrdinalIgnoreCase)))
            {
                entry.Classification = StringClassification.Protected;
                entry.Status = "Protected";
                entry.Confidence = "High";
                entry.Notes = "HTTP Method token — Không dịch thành Cài đặt";
                return;
            }

            // B3. Kiểm tra chuỗi mạng hạ tầng trong Assembly-CSharp (ví dụ: ChatClient)
            if (context.StartsWith("Assembly-CSharp/ChatClient", StringComparison.OrdinalIgnoreCase) ||
                context.Contains("ChatClient") ||
                context.Contains("PhotonStream"))
            {
                entry.Classification = StringClassification.Technical;
                entry.Status = "Ignored";
                entry.Confidence = "High";
                entry.Notes = "[KỸ THUẬT] Mã kết nối mạng / chat server hạ tầng";
                return;
            }

            // B4. Kiểm tra chuỗi Log / Debug / Exception
            if (TechnicalExceptionKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                                                    context.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                entry.Classification = StringClassification.Technical;
                entry.Status = "Ignored";
                entry.Confidence = "High";
                entry.Notes = "[KỸ THUẬT] Chuỗi Exception / Debug / Network log";
                return;
            }

            // B5. BẢO TỒN VĂN BẢN GAMEPLAY HỢP LỆ TỪ MANAGED CODE (Kitchen.*, Game UI Views)
            bool isGameBusinessAssembly = context.StartsWith("Kitchen.", StringComparison.OrdinalIgnoreCase) ||
                                           context.StartsWith("KitchenMode", StringComparison.OrdinalIgnoreCase);

            bool isGameUiOrView = context.Contains("View") ||
                                  context.Contains("Menu") ||
                                  context.Contains("Profile") ||
                                  context.Contains("Recipe") ||
                                  context.Contains("Tutorial") ||
                                  context.Contains("Setting");

            if (isGameBusinessAssembly && isGameUiOrView)
            {
                entry.Classification = StringClassification.GameText;
                entry.Status = "Translate";
                entry.Confidence = "High";
                entry.Notes = "[GAME CODE] Giao diện / View / Menu người chơi nhìn thấy";
                return;
            }

            if (isGameBusinessAssembly && text.Length >= 3 && char.IsLetter(text[0]) && text.Contains(" ") &&
                !text.Contains("_") && !text.Contains(".") && !text.Contains("=") && !text.Contains("/") && !text.Contains("\\") &&
                !text.All(c => !char.IsLetter(c) || char.IsUpper(c)))
            {
                entry.Classification = StringClassification.GameText;
                entry.Status = "Translate";
                entry.Confidence = "Medium";
                entry.Notes = "[GAME CODE] Thông báo / nội dung trò chơi";
                return;
            }

            // Mặc định các chuỗi còn lại cần xem xét kỹ
            entry.Classification = StringClassification.NeedsReview;
            entry.Status = "Review";
            entry.Confidence = "Low";
            entry.Notes = "[CẦN ĐÁNH GIÁ] Chuỗi chưa đủ căn cứ xác nhận là văn bản người chơi";
        }

        /// <summary>
        /// Phân loại danh sách và tách thành 2 tập: GameText (đủ điều kiện dịch) và Danh sách tổng hợp
        /// </summary>
        public static (List<TranslationEntry> GameTextOnly, List<TranslationEntry> AllWithProvenance) ClassifyAll(IEnumerable<TranslationEntry> entries)
        {
            var gameTextOnly = new List<TranslationEntry>();
            var all = new List<TranslationEntry>();

            foreach (var e in entries)
            {
                Classify(e);
                all.Add(e);
                if (e.Classification == StringClassification.GameText)
                {
                    gameTextOnly.Add(e);
                }
            }

            return (gameTextOnly, all);
        }
    }
}

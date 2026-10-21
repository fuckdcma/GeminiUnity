using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public static class LanguageClassifier
    {
        // Nhận diện script Unicode
        private static readonly Regex JapaneseRegex = new Regex(@"[\u3040-\u309F\u30A0-\u30FF]", RegexOptions.Compiled); // Hiragana & Katakana
        private static readonly Regex KoreanRegex = new Regex(@"[\uAC00-\uD7AF\u1100-\u11FF]", RegexOptions.Compiled);   // Hangul
        private static readonly Regex CyrillicRegex = new Regex(@"[\u0400-\u04FF]", RegexOptions.Compiled);             // Russian
        private static readonly Regex CjkRegex = new Regex(@"[\u4E00-\u9FFF]", RegexOptions.Compiled);                  // Chinese

        // Ký tự đặc trưng từng ngôn ngữ châu Âu
        private static readonly Regex TurkishChars = new Regex(@"[ıİğĞşŞ]", RegexOptions.Compiled);
        private static readonly Regex PolishChars = new Regex(@"[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]", RegexOptions.Compiled);
        private static readonly Regex GermanChars = new Regex(@"[äöüßÄÖÜ]", RegexOptions.Compiled);
        private static readonly Regex FrenchChars = new Regex(@"[éèêëàâùûôîïçœÉÈÊËÀÂÙÛÔÎÏÇŒ]", RegexOptions.Compiled);
        private static readonly Regex SpanishChars = new Regex(@"[ñ¿¡Ñ]", RegexOptions.Compiled);
        private static readonly Regex PortugueseChars = new Regex(@"[ãõÃÕ]", RegexOptions.Compiled);

        /// <summary>
        /// Chuẩn hóa mã ngôn ngữ quốc tế sang mã ngắn gọn chuẩn (EN, JA, ZH_CN, ZH_TW, KO, RU, DE, FR, ES, PT_BR, PL, TR)
        /// </summary>
        public static string NormalizeLanguageCode(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "EN";

            string s = rawName.Trim().ToLowerInvariant();
            if (s.Contains("english") || s == "en" || s == "en-us" || s == "en-gb") return "EN";
            if (s.Contains("japanese") || s == "ja" || s == "jp" || s == "ja-jp") return "JA";
            if (s.Contains("chinesesimplified") || s.Contains("simplified") || s == "zh_cn" || s == "zh-cn" || s == "zh-hans") return "ZH_CN";
            if (s.Contains("chinesetraditional") || s.Contains("traditional") || s == "zh_tw" || s == "zh-tw" || s == "zh-hant") return "ZH_TW";
            if (s.Contains("chinese") || s == "zh") return "ZH_CN";
            if (s.Contains("korean") || s == "ko" || s == "kr" || s == "ko-kr") return "KO";
            if (s.Contains("russian") || s == "ru" || s == "ru-ru") return "RU";
            if (s.Contains("german") || s == "de" || s == "de-de") return "DE";
            if (s.Contains("french") || s == "fr" || s == "fr-fr") return "FR";
            if (s.Contains("spanish") || s == "es" || s == "es-es") return "ES";
            if (s.Contains("portuguese") || s == "pt" || s == "pt_br" || s == "pt-br") return "PT_BR";
            if (s.Contains("polish") || s == "pl" || s == "pl-pl") return "PL";
            if (s.Contains("turkish") || s == "tr" || s == "tr-tr") return "TR";

            return "EN";
        }

        /// <summary>
        /// Nhận diện ngôn ngữ dựa trên phân tích chữ viết Unicode và từ vựng
        /// </summary>
        public static string DetectLanguageFromText(string text, string fallback = "EN")
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;

            if (JapaneseRegex.IsMatch(text)) return "JA";
            if (KoreanRegex.IsMatch(text)) return "KO";
            if (CyrillicRegex.IsMatch(text)) return "RU";
            if (CjkRegex.IsMatch(text)) return fallback.StartsWith("ZH") ? fallback : "ZH_CN";

            if (TurkishChars.IsMatch(text)) return "TR";
            if (PolishChars.IsMatch(text)) return "PL";

            // Nếu có gợi ý từ fallback, ưu tiên kiểm tra
            if (fallback != "EN")
            {
                if (fallback == "DE" && (GermanChars.IsMatch(text) || MatchesGerman(text))) return "DE";
                if (fallback == "FR" && (FrenchChars.IsMatch(text) || MatchesFrench(text))) return "FR";
                if (fallback == "ES" && (SpanishChars.IsMatch(text) || MatchesSpanish(text))) return "ES";
                if (fallback == "PT_BR" && (PortugueseChars.IsMatch(text) || MatchesPortuguese(text))) return "PT_BR";
                return fallback;
            }

            if (GermanChars.IsMatch(text) || MatchesGerman(text)) return "DE";
            if (FrenchChars.IsMatch(text) || MatchesFrench(text)) return "FR";
            if (SpanishChars.IsMatch(text) || MatchesSpanish(text)) return "ES";
            if (PortugueseChars.IsMatch(text) || MatchesPortuguese(text)) return "PT_BR";

            return "EN";
        }

        private static bool MatchesGerman(string text)
        {
            return Regex.IsMatch(text, @"\b(Spiel|verlassen|zurückkehren|Speichern|Beenden|Einstellungen|Schließen|Neues|Löschen|HQ|Nicht|oder)\b", RegexOptions.IgnoreCase);
        }

        private static bool MatchesFrench(string text)
        {
            return Regex.IsMatch(text, @"\b(Quitter|Sauvegarder|Retour|Partie|Joueur|Options|Nouveau|Supprimer|Fermer|le|la|les|du|jeu)\b", RegexOptions.IgnoreCase);
        }

        private static bool MatchesSpanish(string text)
        {
            return Regex.IsMatch(text, @"\b(Salir|Juego|Guardar|Volver|Jugador|Nuevo|Eliminar|del|la|base)\b", RegexOptions.IgnoreCase);
        }

        private static bool MatchesPortuguese(string text)
        {
            return Regex.IsMatch(text, @"\b(Sair|Salvar|Retornar|Jogador|Opções|Novo|Excluir|do|ao|QG)\b", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Quét và xây dựng bản đồ vị trí các phân vùng ngôn ngữ trong tệp nhị phân .assets
        /// </summary>
        public static List<(int offset, string lang)> BuildAssetLanguageMap(byte[] b)
        {
            var map = new List<(int offset, string lang)>();

            string[] knownLangMarkers = new string[]
            {
                "English", "French", "German", "Spanish", "Turkish", "Polish", "Russian",
                "Japanese", "ChineseSimplified", "ChineseTraditional", "Korean", "PortugueseBrazil"
            };

            foreach (var lang in knownLangMarkers)
            {
                byte[] pat = Encoding.ASCII.GetBytes(lang + " - Info");
                for (int i = 0; i <= b.Length - pat.Length; i++)
                {
                    bool m = true;
                    for (int j = 0; j < pat.Length; j++)
                    {
                        if (b[i + j] != pat[j]) { m = false; break; }
                    }
                    if (m)
                    {
                        map.Add((i, NormalizeLanguageCode(lang)));
                    }
                }
            }

            return map.OrderBy(m => m.offset).ToList();
        }

        /// <summary>
        /// Tìm ngôn ngữ của một offset trong asset file dựa vào marker gần nhất phía trước
        /// </summary>
        public static string ResolveLanguageAtOffset(int offset, List<(int offset, string lang)> langMap)
        {
            if (langMap == null || langMap.Count == 0) return "EN";

            for (int i = langMap.Count - 1; i >= 0; i--)
            {
                if (langMap[i].offset <= offset)
                {
                    return langMap[i].lang;
                }
            }

            return "EN";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace VietnameseLocalizationMod
{
    public static class FontFallbackPatcher
    {
        private static readonly Dictionary<string, TMP_FontAsset> _loadedCustomFonts = new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _fontMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _protectedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<AssetBundle> _openAssetBundles = new List<AssetBundle>();
        
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TMP_Text, string> _originalFontTracker = new System.Runtime.CompilerServices.ConditionalWeakTable<TMP_Text, string>();
        public static System.Runtime.CompilerServices.ConditionalWeakTable<TMP_Text, string> OriginalFontTracker => _originalFontTracker;

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TMP_Text, Material> _originalMaterialTracker = new System.Runtime.CompilerServices.ConditionalWeakTable<TMP_Text, Material>();
        public static System.Runtime.CompilerServices.ConditionalWeakTable<TMP_Text, Material> OriginalMaterialTracker => _originalMaterialTracker;

        private static readonly Dictionary<string, Material> _clonedMaterialCache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static Material? _cachedBestGameTemplateMaterial;

        private static TMP_FontAsset? _cachedFallbackFontAsset;
        private static bool _initialized = false;
        private static string _cachedPluginDir = "";
        [ThreadStatic] private static bool _isAttachingFont;

        public static bool IsEnabled { get; set; } = true;
        public static IReadOnlyDictionary<string, TMP_FontAsset> LoadedCustomFonts => _loadedCustomFonts;
        public static TMP_FontAsset? CachedFallbackFontAsset => _cachedFallbackFontAsset;
        public static TMP_FontAsset? CustomSdfFontAsset => _cachedFallbackFontAsset;
        public static bool IsProtectedFontPublic(string fontName) => IsProtectedFont(fontName);

        private static readonly HashSet<int> _injectedFontInstanceIds = new HashSet<int>();

        /// <summary>
        /// Thay thế Font Asset trực tiếp trong bộ nhớ (In-Place Injection) tương tự phương pháp CourierPrime thay the.json:
        /// 1. Giữ nguyên 100% đối tượng TMP_FontAsset gốc, tên phông, và Material gốc (cùng mọi Preset, shader, keywords, outline, drop shadow).
        /// 2. Chỉ hoán đổi Atlas Texture và sao chép FaceInfo, GlyphTable, CharacterTable từ font Việt hóa.
        /// 3. Không tạo Material mới, không đoán hay ghi đè các tham số nghệ thuật của game.
        /// </summary>
        public static bool InjectCustomFontIntoOriginal(TMP_FontAsset origFont, TMP_FontAsset customFont)
        {
            if (origFont == null || customFont == null || origFont == customFont) return false;
            if (_injectedFontInstanceIds.Contains(origFont.GetInstanceID())) return true;

            try
            {
                // 1. Cập nhật mảng Atlas Texture
                if (customFont.atlasTexture != null)
                {
                    origFont.atlasTextures = new Texture2D[] { customFont.atlasTexture };
                    try
                    {
                        var fld = typeof(TMP_FontAsset).GetField("m_AtlasTexture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (fld != null) fld.SetValue(origFont, customFont.atlasTexture);
                    }
                    catch { }
                }

                // 2. Cập nhật Texture trên Material mặc định của font gốc (BẢO TOÀN 100% MỌI THUỘC TÍNH VÀ KEYWORDS)
                if (origFont.material != null && customFont.atlasTexture != null)
                {
                    origFont.material.mainTexture = customFont.atlasTexture;
                    origFont.material.SetTexture(ShaderUtilities.ID_MainTex, customFont.atlasTexture);
                }

                // 3. Sao chép FaceInfo từ font mới
                origFont.faceInfo = customFont.faceInfo;

                // 4. Sao chép GlyphTable và CharacterTable
                if (origFont.glyphTable != null && customFont.glyphTable != null)
                {
                    origFont.glyphTable.Clear();
                    origFont.glyphTable.AddRange(customFont.glyphTable);
                }
                if (origFont.characterTable != null && customFont.characterTable != null)
                {
                    origFont.characterTable.Clear();
                    origFont.characterTable.AddRange(customFont.characterTable);
                }

                // 5. Tái xây dựng bảng tra cứu ký tự
                origFont.ReadFontAssetDefinition();

                _injectedFontInstanceIds.Add(origFont.GetInstanceID());
                VietnamesePlugin.ModLog($"[InjectCustomFontIntoOriginal] THAY FONT NGUYÊN BẢN THÀNH CÔNG: '{origFont.name}' <- '{customFont.name}' (Giữ nguyên 100% Material gốc và mọi Preset!)");
                return true;
            }
            catch (Exception ex)
            {
                VietnamesePlugin.ModLog($"[InjectCustomFontIntoOriginal] Lỗi khi inject '{origFont.name}': {ex.Message}");
                return false;
            }
        }

        public static bool AreCustomFontsValid()
        {
            if (_loadedCustomFonts.Count == 0) return false;
            foreach (var kv in _loadedCustomFonts)
            {
                if (kv.Value != null) return true;
            }
            return false;
        }

        public static void EnsureFontsLoaded(string? pluginDir = null)
        {
            string dir = !string.IsNullOrEmpty(pluginDir) ? pluginDir : _cachedPluginDir;
            if (!AreCustomFontsValid() && !string.IsNullOrEmpty(dir))
            {
                _initialized = false;
                foreach (var b in _openAssetBundles)
                {
                    try { if (b != null) b.Unload(true); } catch { }
                }
                _openAssetBundles.Clear();
                _loadedCustomFonts.Clear();
                _clonedMaterialCache.Clear();
                Initialize(dir, null);
            }
        }

        public static bool IsCustomFont(TMP_FontAsset? font)
        {
            if (font == null) return false;
            string fn = font.name ?? "";
            return IsCustomFont(fn);
        }

        public static bool IsCustomFont(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName)) return false;
            string clean = fontName.Replace("(Instance)", "").Trim();
            if (_loadedCustomFonts.ContainsKey(clean)) return true;
            if (clean.IndexOf("ARIAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                clean.IndexOf("TIMES", StringComparison.OrdinalIgnoreCase) >= 0 ||
                clean.IndexOf("IMES", StringComparison.OrdinalIgnoreCase) >= 0 ||
                clean.StartsWith("Vietnamese_", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        public static bool IsCustomFontPublic(string? fontName) => IsCustomFont(fontName);

        public static TMP_FontAsset? GetAnyCustomFont()
        {
            if (_loadedCustomFonts.TryGetValue("Vietnamese_Mohr_Rounded_Black_SDF", out var mohr) && mohr != null) return mohr;
            if (_loadedCustomFonts.TryGetValue("ARIAL SDF", out var arial) && arial != null) return arial;
            if (_loadedCustomFonts.TryGetValue("TIMES SDF", out var times) && times != null) return times;
            if (_cachedFallbackFontAsset != null) return _cachedFallbackFontAsset;
            return _loadedCustomFonts.Values.FirstOrDefault(f => f != null);
        }

        public static void Initialize(string pluginDir, Action<string>? logger = null)
        {
            if (!string.IsNullOrEmpty(pluginDir)) _cachedPluginDir = pluginDir;
            if (string.IsNullOrEmpty(pluginDir)) pluginDir = _cachedPluginDir;
            if (_initialized && AreCustomFontsValid()) return;

            try
            {
                // 1. Tải trước danh sách các phông chữ được bảo vệ (Icon, Emoji)
                RegisterDefaultProtectedFonts();

                // 2. Nạp TẤT CẢ các Custom SDF Font từ các AssetBundle trong thư mục plugin
                LoadCustomFontsFromBundles(pluginDir, logger);

                // 3. Tải bảng ánh xạ 1-1 từ cấu hình (Font_Mapping_Config.json / Font_Mapping_Matrix.csv)
                LoadFontMappingConfiguration(pluginDir, logger);

                // 4. Nếu có Custom Font từ AssetBundle, chọn font Primary hoặc font đầu tiên làm fallback mặc định
                if (_loadedCustomFonts.TryGetValue("Vietnamese_Primary_SDF", out var primaryFont))
                {
                    _cachedFallbackFontAsset = primaryFont;
                }
                else if (_loadedCustomFonts.Count > 0)
                {
                    _cachedFallbackFontAsset = _loadedCustomFonts.Values.FirstOrDefault();
                }

                // 5. Nếu chưa có fallback từ AssetBundle, tìm Dynamic / Builtin / Ram font
                if (_cachedFallbackFontAsset == null)
                {
                    InitializeFallbackFromSystemOrGame(logger);
                }

                if (_cachedFallbackFontAsset != null)
                {
                    RegisterGlobalFallback(_cachedFallbackFontAsset);
                }

                _initialized = true;
                VietnamesePlugin.ModLog($">>> [FontFallbackPatcher] Khởi tạo hoàn tất. Đã nạp {_loadedCustomFonts.Count} font tùy chỉnh từ AssetBundle, {_fontMappings.Count} quy tắc ánh xạ 1-1.");
            }
            catch (Exception ex)
            {
                logger?.Invoke($">>> [Vietnamese Localization Mod] Lỗi khởi tạo Font: {ex.Message}");
                VietnamesePlugin.ModLog($">>> [FontFallbackPatcher] Lỗi: {ex}");
            }
        }

        private static void RegisterDefaultProtectedFonts()
        {
            _protectedFonts.Add("Reference - Icons");
            _protectedFonts.Add("NotoEmoji-Regular SDF");
            _protectedFonts.Add("Icons");
            _protectedFonts.Add("Emoji");
            _protectedFonts.Add("Sprites");
            _protectedFonts.Add("Stars");
        }

        private static void LoadFontMappingConfiguration(string pluginDir, Action<string>? logger)
        {
            // Các ánh xạ cốt lõi chuẩn 1-1 mặc định của PlateUp
            _fontMappings["Reference - Main Menu Main Text"] = "Vietnamese_MainMenu_SDF";
            _fontMappings["English - Main Menu Main Text"] = "Vietnamese_MainMenu_SDF";
            _fontMappings["Chewy SDF"] = "Vietnamese_MainMenu_SDF";
            _fontMappings["ARCO SDF"] = "Vietnamese_MainMenu_SDF";
            _fontMappings["MilkyNice-Clean SDF"] = "Vietnamese_MainMenu_SDF";

            _fontMappings["Reference - Primary Text"] = "Vietnamese_Primary_SDF";
            _fontMappings["English - Primary Text"] = "Vietnamese_Primary_SDF";
            _fontMappings["Capriola-Regular SDF"] = "Vietnamese_Primary_SDF";

            _fontMappings["Reference - Title Text"] = "Vietnamese_Title_SDF";
            _fontMappings["English - Title Text"] = "Vietnamese_Title_SDF";
            _fontMappings["Grandstander-clean SDF"] = "Vietnamese_Title_SDF";

            _fontMappings["Reference - Large Text"] = "Vietnamese_Large_SDF";
            _fontMappings["English - Large Text"] = "Vietnamese_Large_SDF";

            _fontMappings["Reference - Upgrade Card Title"] = "Vietnamese_CardTitle_SDF";
            _fontMappings["Reference - Upgrade Card Text"] = "Vietnamese_CardText_SDF";

            _fontMappings["Reference - In-world Labels"] = "Vietnamese_InWorld_SDF";
            _fontMappings["Reference - Front Door Sign"] = "Vietnamese_InWorld_SDF";
            _fontMappings["Reference - Blueprint Text"] = "Vietnamese_InWorld_SDF";

            _fontMappings["Reference - Seed Text"] = "Vietnamese_Monospace_SDF";
            _fontMappings["Consolas SDF"] = "Vietnamese_Monospace_SDF";

            _fontMappings["Cake N Truffles SDF"] = "Vietnamese_Handwritten_SDF";
            _fontMappings["HelvetiHand SDF"] = "Vietnamese_Handwritten_SDF";

            // Nếu có tệp Font_Mapping_Config.json trong pluginDir thì đọc bổ sung/ghi đè
            if (string.IsNullOrEmpty(pluginDir) || !Directory.Exists(pluginDir)) return;

            string jsonPath = Path.Combine(pluginDir, "Font_Mapping_Config.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    string content = File.ReadAllText(jsonPath);
                    // Match từng block { ... } độc lập với thứ tự các khóa
                    var blockMatches = Regex.Matches(content, @"\{([^{}]+)\}", RegexOptions.Singleline);
                    int loadedCount = 0;
                    foreach (Match b in blockMatches)
                    {
                        string blockText = b.Groups[1].Value;
                        var mOrig = Regex.Match(blockText, @"""OriginalFont""\s*:\s*""([^""]+)""");
                        var mTarget = Regex.Match(blockText, @"""TargetReplacement""\s*:\s*""([^""]+)""");
                        bool isProtected = blockText.IndexOf("\"IsProtected\": true", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (mOrig.Success)
                        {
                            string orig = mOrig.Groups[1].Value.Trim();
                            string target = mTarget.Success ? mTarget.Groups[1].Value.Trim() : "";

                            if (isProtected || target == "KEEP_ORIGINAL")
                            {
                                _protectedFonts.Add(orig);
                            }
                            else if (!string.IsNullOrEmpty(target) && target != "KEEP_OR_FALLBACK")
                            {
                                _fontMappings[orig] = target;
                                loadedCount++;
                            }
                        }
                    }
                    VietnamesePlugin.ModLog($"[FontFallbackPatcher] Đã nạp thành công {loadedCount} quy tắc ánh xạ từ JSON: {jsonPath}");
                }
                catch (Exception ex)
                {
                    VietnamesePlugin.ModLog($"[FontFallbackPatcher] Không thể đọc {jsonPath}: {ex.Message}");
                }
            }
        }

        private static void LoadCustomFontsFromBundles(string pluginDir, Action<string>? logger)
        {
            if (string.IsNullOrEmpty(pluginDir) || !Directory.Exists(pluginDir)) return;

            try
            {
                string[] patterns = { "*.bundle", "*.assets", "vietnamese_font*", "*.unity3d", "*font*" };
                var candidateFiles = new List<string>();
                foreach (var pat in patterns)
                {
                    foreach (var f in Directory.GetFiles(pluginDir, pat, SearchOption.AllDirectories))
                    {
                        if (!candidateFiles.Contains(f) &&
                            !f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        {
                            candidateFiles.Add(f);
                        }
                    }
                }

                foreach (var file in candidateFiles)
                {
                    try
                    {
                        var bundle = AssetBundle.LoadFromFile(file);
                        if (bundle != null)
                        {
                            _openAssetBundles.Add(bundle);
                            var fonts = bundle.LoadAllAssets<TMP_FontAsset>();
                            if (fonts != null && fonts.Length > 0)
                            {
                                foreach (var f in fonts)
                                {
                                    if (f != null && !string.IsNullOrEmpty(f.name))
                                    {
                                        try
                                        {
                                            f.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                                            if (f.material != null)
                                            {
                                                f.material.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                                                if (f.material.mainTexture != null)
                                                    f.material.mainTexture.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                                            }
                                            if (f.atlasTexture != null)
                                                f.atlasTexture.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                                            if (f.atlasTextures != null)
                                            {
                                                foreach (var at in f.atlasTextures)
                                                    if (at != null) at.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                                            }
                                        }
                                        catch { }

                                        string fName = f.name.Trim();
                                        _loadedCustomFonts[f.name] = f;
                                        _loadedCustomFonts[fName] = f;

                                        // Đăng ký bí danh thông minh để tương thích cả 2 chuẩn đặt tên
                                        if (fName.IndexOf("Chewy", StringComparison.OrdinalIgnoreCase) >= 0 || fName.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["Vietnamese_MainMenu_SDF"] = f;
                                            _loadedCustomFonts["Vietnamese_Chewy_SDF"] = f;
                                            _loadedCustomFonts["Chewy SDF"] = f;
                                            _loadedCustomFonts["Chewy"] = f;
                                        }
                                        if (fName.IndexOf("Capriola", StringComparison.OrdinalIgnoreCase) >= 0 || fName.IndexOf("Primary", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["Vietnamese_Primary_SDF"] = f;
                                            _loadedCustomFonts["Vietnamese_Capriola_SDF"] = f;
                                            _loadedCustomFonts["Capriola-Regular SDF"] = f;
                                            _loadedCustomFonts["Capriola"] = f;
                                        }
                                        if (fName.IndexOf("Grandstander", StringComparison.OrdinalIgnoreCase) >= 0 || fName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["Vietnamese_Title_SDF"] = f;
                                            _loadedCustomFonts["Vietnamese_Grandstander-clean_SDF"] = f;
                                            _loadedCustomFonts["Grandstander-clean SDF"] = f;
                                        }
                                        if (fName.IndexOf("Arial", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["ARIAL SDF"] = f;
                                            _loadedCustomFonts["Arial SDF"] = f;
                                            _loadedCustomFonts["Arial"] = f;
                                            _loadedCustomFonts["ARIAL"] = f;
                                        }
                                        if (fName.IndexOf("Times", StringComparison.OrdinalIgnoreCase) >= 0 || fName.IndexOf("IMES", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["TIMES SDF"] = f;
                                            _loadedCustomFonts["Times SDF"] = f;
                                            _loadedCustomFonts["IMES SDF"] = f;
                                            _loadedCustomFonts["Times"] = f;
                                            _loadedCustomFonts["times"] = f;
                                            _loadedCustomFonts["Times New Roman"] = f;
                                        }
                                        if (fName.IndexOf("ARCO", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["Vietnamese_ARCO_SDF"] = f;
                                            _loadedCustomFonts["ARCO SDF"] = f;
                                        }
                                        if (fName.IndexOf("MilkyNice", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["Vietnamese_MilkyNice_SDF"] = f;
                                            _loadedCustomFonts["MilkyNice-Clean SDF"] = f;
                                        }
                                        if (fName.IndexOf("Mohr", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _loadedCustomFonts["Vietnamese_Mohr_Rounded_Black_SDF"] = f;
                                            _loadedCustomFonts["Mohr_Rounded_Black_SDF"] = f;
                                            _loadedCustomFonts["Mohr_Rounded"] = f;
                                            _loadedCustomFonts["Mohr"] = f;
                                        }

                                        EnsureEssentialGlyphsCovered(f);
                                        ConfigureFontMaterialProperties(f.material, f);
                                        VietnamesePlugin.ModLog($">>> [FontFallbackPatcher] Đã nạp SDF Font từ AssetBundle: '{f.name}' (File: {Path.GetFileName(file)})");
                                        logger?.Invoke($">>> [Vietnamese Localization Mod] Đã nạp SDF Font: {f.name}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception bEx)
                    {
                        VietnamesePlugin.ModLog($"[FontFallbackPatcher] Bỏ qua bundle {file}: {bEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                VietnamesePlugin.ModLog($"[FontFallbackPatcher] Lỗi quét bundle: {ex.Message}");
            }
        }

        public static void EnsureEssentialGlyphsCovered(TMP_FontAsset font)
        {
            if (font == null) return;
            try
            {
                if (font.characterLookupTable == null) return;

                // 1. Phục hồi chữ 'W' hoa từ chữ 'w' thường nếu thiếu (Khắc phục triệt để lỗi tách font Wiki -> W vs iki)
                if (!font.characterLookupTable.ContainsKey(87) && font.characterLookupTable.TryGetValue(119, out var wLower) && wLower != null && wLower.glyph != null)
                {
                    var wUpper = new TMP_Character(87, wLower.glyph);
                    wUpper.scale = 1.25f;
                    font.characterTable?.Add(wUpper);
                    font.characterLookupTable[87] = wUpper;
                    VietnamesePlugin.ModLog($"[EnsureEssentialGlyphsCovered] Font '{font.name}': Tự động phục hồi glyph 'W' (Unicode 87) từ 'w'.");
                }
            }
            catch (Exception ex)
            {
                VietnamesePlugin.ModLog($"[EnsureEssentialGlyphsCovered] Lỗi font {font.name}: {ex.Message}");
            }
        }

        private static void InitializeFallbackFromSystemOrGame(Action<string>? logger)
        {
            // 1. Thử tạo Dynamic Font từ font hệ thống
            string[] fontNames = { "Arial", "Segoe UI", "Tahoma", "Times New Roman" };
            foreach (var fn in fontNames)
            {
                try
                {
                    var testFont = Font.CreateDynamicFontFromOSFont(fn, 16);
                    if (testFont != null)
                    {
                        var asset = TMP_FontAsset.CreateFontAsset(testFont);
                        if (asset != null)
                        {
                            _cachedFallbackFontAsset = asset;
                            _cachedFallbackFontAsset.name = "Vietnamese_OS_Fallback_" + fn;
                            break;
                        }
                    }
                }
                catch { }
            }

            // 2. Thử Builtin Arial.ttf
            if (_cachedFallbackFontAsset == null)
            {
                try
                {
                    var builtinArial = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (builtinArial != null)
                    {
                        var asset = TMP_FontAsset.CreateFontAsset(builtinArial);
                        if (asset != null)
                        {
                            _cachedFallbackFontAsset = asset;
                            _cachedFallbackFontAsset.name = "Vietnamese_Builtin_Arial";
                        }
                    }
                }
                catch { }
            }

            // 3. Nếu vẫn null, tìm font có sẵn trong memory của game
            if (_cachedFallbackFontAsset == null)
            {
                try
                {
                    var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (allFonts != null && allFonts.Length > 0)
                    {
                        var fallbackCandidate = allFonts.FirstOrDefault(f => 
                            f != null && (f.name.IndexOf("noto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          f.name.IndexOf("sans", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          f.name.IndexOf("cyrillic", StringComparison.OrdinalIgnoreCase) >= 0))
                            ?? allFonts.FirstOrDefault(f => f != null);

                        if (fallbackCandidate != null)
                        {
                            _cachedFallbackFontAsset = fallbackCandidate;
                            VietnamesePlugin.ModLog($">>> [FontFallbackPatcher] Dùng font nội tại game làm fallback: {fallbackCandidate.name}");
                        }
                    }
                }
                catch { }
            }
        }

        private static void RegisterGlobalFallback(TMP_FontAsset fontAsset)
        {
            try
            {
                if (TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(fontAsset))
                {
                    TMP_Settings.fallbackFontAssets.Add(fontAsset);
                }
            }
            catch { }
        }

        /// <summary>
        /// Tìm TMP_FontAsset thay thế cụ thể từ AssetBundle theo tên phông gốc
        /// </summary>
        public static TMP_FontAsset? GetReplacementFontFor(string originalFontName)
        {
            if (!_initialized)
            {
                Initialize("", null);
            }

            if (IsProtectedFont(originalFontName)) return null;

            string? targetName = ResolveTargetFontName(originalFontName);
            if (!string.IsNullOrEmpty(targetName) && _loadedCustomFonts.TryGetValue(targetName!, out var font) && font != null)
            {
                return font;
            }

            if (!string.IsNullOrEmpty(targetName))
            {
                foreach (var kvp in _loadedCustomFonts)
                {
                    if (kvp.Key.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        targetName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return kvp.Value;
                    }
                }
            }

            return GetAnyCustomFont();
        }

        /// <summary>
        /// Gán hoặc thay thế Font theo ma trận 1-1 cho TMP_Text:
        /// 1. Kiểm tra Font được bảo vệ (Icon, Emoji) -> KHÔNG BAO GIỜ THAY THẾ
        /// 2. Nếu đã là Font tùy chỉnh của bản dịch -> đảm bảo fontSharedMaterial chuẩn
        /// 3. Thay thế 1-1 chuẩn xác từ AssetBundle (cập nhật font, fontSharedMaterial, Dirty, Mesh)
        /// 4. Nếu không có Font 1-1 trong Bundle -> dùng Fallback Font kèm đồng bộ Material
        /// </summary>
        public static void AttachFallbackFont(TMP_Text textComponent)
        {
            if (!IsEnabled) return;
            if (textComponent == null) return;
            if (_isAttachingFont) return;

            _isAttachingFont = true;
            try
            {
                EnsureFontsLoaded();
                if (!_initialized)
                {
                    Initialize(_cachedPluginDir, null);
                }

                var currentFont = textComponent.font;
                if (currentFont == null) return;

                string rawFontName = currentFont.name ?? "";
                string cleanFontName = rawFontName.Replace("(Instance)", "").Trim();

                // A. BẢO VỆ TUYỆT ĐỐI CÁC PHÔNG ICON / EMOJI
                if (IsProtectedFont(cleanFontName))
                {
                    return;
                }

                // Ghi nhớ font gốc nguyên bản của UI element này từ game (trước khi bị mod thay thế)
                string originalFontName;
                if (!_originalFontTracker.TryGetValue(textComponent, out originalFontName))
                {
                    if (!IsCustomFont(currentFont))
                    {
                        originalFontName = cleanFontName;
                        _originalFontTracker.Add(textComponent, originalFontName);
                    }
                    else
                    {
                        originalFontName = cleanFontName;
                    }
                }

                // Ghi nhớ material gốc nguyên bản của UI element này từ game (để bảo toàn 100% outline, shadow, glow)
                if (!_originalMaterialTracker.TryGetValue(textComponent, out var origMat))
                {
                    if (textComponent.fontSharedMaterial != null && !IsCustomFont(currentFont))
                    {
                        origMat = textComponent.fontSharedMaterial;
                        _originalMaterialTracker.Add(textComponent, origMat);
                    }
                }

                if (origMat != null && _cachedBestGameTemplateMaterial == null && HasVisualEffects(origMat))
                {
                    _cachedBestGameTemplateMaterial = origMat;
                }

                // B. TÌM FONT THAY THẾ (1-1 hoặc Universal Custom Font)
                TMP_FontAsset? customFont = GetReplacementFontFor(originalFontName) 
                    ?? GetReplacementFontFor(cleanFontName) 
                    ?? (IsCustomFont(currentFont) ? currentFont : null);

                if (customFont != null)
                {
                    // Đăng ký font gốc làm fallback cho custom font để không mất các icon/ký hiệu đặc biệt
                    if (!IsCustomFont(currentFont))
                    {
                        if (customFont.fallbackFontAssetTable == null)
                        {
                            customFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                        }
                        if (!customFont.fallbackFontAssetTable.Contains(currentFont))
                        {
                            customFont.fallbackFontAssetTable.Add(currentFont);
                        }
                    }

                    // Lấy hoặc tạo Material Preserved với hiệu ứng đổ bóng mềm nguyên bản của game
                    Material targetMat = GetOrCreatePreservedMaterial(customFont, origMat ?? _cachedBestGameTemplateMaterial);

                    bool fontChanged = textComponent.font != customFont;
                    bool matChanged = textComponent.fontSharedMaterial != targetMat;
                    bool paddingNeeded = !textComponent.extraPadding;

                    if (fontChanged || matChanged || paddingNeeded)
                    {
                        var oldName = textComponent.font != null ? textComponent.font.name : "(null)";

                        // 1. Gán phông chữ tùy chỉnh từ bundle (loại bỏ hoàn toàn lỗi UV và vệt rách lặp ký tự)
                        textComponent.font = customFont;

                        // 2. Gán Material bảo toàn hiệu ứng gốc
                        if (targetMat != null)
                        {
                            textComponent.fontSharedMaterial = targetMat;
                            try { textComponent.fontMaterial = targetMat; } catch { }
                            try { textComponent.fontSharedMaterials = new Material[] { targetMat }; } catch { }
                            try { textComponent.material = targetMat; } catch { }
                            try
                            {
                                var mr = textComponent.GetComponent<MeshRenderer>();
                                if (mr != null) mr.sharedMaterial = targetMat;
                            }
                            catch { }
                            try
                            {
                                if (textComponent is TextMeshProUGUI ugui)
                                {
                                    ugui.material = targetMat;
                                    var prop = ugui.GetType().GetProperty("canvasRenderer");
                                    var cr = prop?.GetValue(ugui, null);
                                    if (cr != null)
                                    {
                                        var setMat = cr.GetType().GetMethod("SetMaterial", new[] { typeof(Material), typeof(int) });
                                        setMat?.Invoke(cr, new object[] { targetMat, 0 });
                                    }
                                }
                            }
                            catch { }
                        }

                        // 3. Bật padding mở rộng để bóng đổ mềm không bị cắt xén biên quad
                        textComponent.extraPadding = true;
                        try { textComponent.UpdateMeshPadding(); } catch { }
                        textComponent.SetAllDirty();
                        try { textComponent.SetMaterialDirty(); } catch { }
                        try { textComponent.SetVerticesDirty(); } catch { }
                        SynchronizeSubMeshMaterials(textComponent, targetMat);

                        if (fontChanged || matChanged)
                        {
                            VietnamesePlugin.ModLog($"[AttachFallbackFont] ĐÃ BẢO TOÀN NGUYÊN BẢN HIỆU ỨNG GỐC: '{oldName}' (Gốc: '{originalFontName}') -> '{customFont.name}', Material='{targetMat?.name}' (Comp: {textComponent.GetType().Name}, Obj: {textComponent.gameObject.name})");
                        }
                    }
                    return;
                }

                // D. NẾU CHƯA CÓ FONT 1-1 TƯƠNG ỨNG TRONG BUNDLE -> DÙNG FALLBACK FONT NỘI TẠI KÈM ĐỒNG BỘ MATERIAL
                if (_cachedFallbackFontAsset == null || currentFont == _cachedFallbackFontAsset) return;

                try
                {
                    if (currentFont.fallbackFontAssetTable == null)
                    {
                        currentFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    }

                    if (!currentFont.fallbackFontAssetTable.Contains(_cachedFallbackFontAsset))
                    {
                        currentFont.fallbackFontAssetTable.Add(_cachedFallbackFontAsset);
                    }

                    var parentMat = textComponent.fontSharedMaterial ?? currentFont.material;
                    if (parentMat != null && _cachedFallbackFontAsset.material != null)
                    {
                        SyncMaterialProperties(parentMat, _cachedFallbackFontAsset.material);
                    }

                    SynchronizeSubMeshMaterials(textComponent, parentMat);
                }
                catch { }
            }
            finally
            {
                _isAttachingFont = false;
            }
        }

        private static bool IsProtectedFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return false;
            if (_protectedFonts.Contains(fontName)) return true;
            if (fontName.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (fontName.IndexOf("Emoji", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (fontName.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (fontName.IndexOf("Star", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static string? ResolveTargetFontName(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return null;

            // 1. Khớp chính xác tên font trong bảng mapping (ưu tiên cao nhất từ Font_Mapping_Config.json)
            if (_fontMappings.TryGetValue(fontName, out var target) && !string.IsNullOrEmpty(target))
            {
                return target;
            }

            // 2. Thử khớp tên sau khi bỏ "SDF" hoặc "(Instance)"
            string cleanName = fontName.Replace("(Instance)", "").Replace(" SDF", "").Trim();
            if (_fontMappings.TryGetValue(cleanName, out target) && !string.IsNullOrEmpty(target))
            {
                return target;
            }
            if (_fontMappings.TryGetValue(cleanName + " SDF", out target) && !string.IsNullOrEmpty(target))
            {
                return target;
            }

            // 3. Khớp theo vai trò giao diện (Fuzzy match)
            if (fontName.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Chewy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("ARCO", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Main Menu Main Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("English - Main Menu Main Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("Chewy SDF", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("ARCO SDF", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_MainMenu_SDF")) return "Vietnamese_MainMenu_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Mohr_Rounded_Black_SDF")) return "Vietnamese_Mohr_Rounded_Black_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Chewy_SDF")) return "Vietnamese_Chewy_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
            }

            if (fontName.IndexOf("Title Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Grandstander", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Title Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("English - Title Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Title_SDF")) return "Vietnamese_Title_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Grandstander-clean_SDF")) return "Vietnamese_Grandstander-clean_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Mohr_Rounded_Black_SDF")) return "Vietnamese_Mohr_Rounded_Black_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
            }

            if (fontName.IndexOf("Primary Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Capriola", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Primary Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("English - Primary Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("Capriola-Regular SDF", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Primary_SDF")) return "Vietnamese_Primary_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Capriola_SDF")) return "Vietnamese_Capriola_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Mohr_Rounded_Black_SDF")) return "Vietnamese_Mohr_Rounded_Black_SDF";
            }

            if (fontName.IndexOf("Large Text", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Large Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_fontMappings.TryGetValue("English - Large Text", out target) && !string.IsNullOrEmpty(target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Large_SDF")) return "Vietnamese_Large_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Mohr_Rounded_Black_SDF")) return "Vietnamese_Mohr_Rounded_Black_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
            }

            if (fontName.IndexOf("Upgrade Card Title", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Upgrade Card Title", out target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_CardTitle_SDF")) return "Vietnamese_CardTitle_SDF";
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Mohr_Rounded_Black_SDF")) return "Vietnamese_Mohr_Rounded_Black_SDF";
            }

            if (fontName.IndexOf("Upgrade Card Text", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Upgrade Card Text", out target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_CardText_SDF")) return "Vietnamese_CardText_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
            }

            if (fontName.IndexOf("In-world", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Sign", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Blueprint", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Homade", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - In-world Labels", out target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_InWorld_SDF")) return "Vietnamese_InWorld_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
            }

            if (fontName.IndexOf("Seed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontName.IndexOf("Consolas", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (_fontMappings.TryGetValue("Reference - Seed Text", out target)) return target;
                if (_loadedCustomFonts.ContainsKey("Vietnamese_Monospace_SDF")) return "Vietnamese_Monospace_SDF";
                if (_loadedCustomFonts.ContainsKey("ARIAL SDF")) return "ARIAL SDF";
            }

            // 4. Khớp wildcard toàn cầu nếu có cấu hình "*" (khi không có khớp riêng)
            if (_fontMappings.TryGetValue("*", out var wildcardTarget) && !string.IsNullOrEmpty(wildcardTarget) && wildcardTarget != "KEEP_ORIGINAL")
            {
                return wildcardTarget;
            }

            return null;
        }

        public static bool HasVisualEffects(Material? mat)
        {
            if (mat == null) return false;
            try
            {
                bool hasOutline = (mat.HasProperty(ShaderUtilities.ID_OutlineWidth) && mat.GetFloat(ShaderUtilities.ID_OutlineWidth) > 0.01f) ||
                                  (mat.HasProperty("_OutlineWidth") && mat.GetFloat("_OutlineWidth") > 0.01f);
                bool hasUnderlay = mat.IsKeywordEnabled("UNDERLAY_ON") ||
                                   mat.IsKeywordEnabled("UNDERLAY_INNER") ||
                                   (mat.HasProperty("_UnderlaySoftness") && mat.GetFloat("_UnderlaySoftness") > 0.01f) ||
                                   (mat.HasProperty("_UnderlayDilate") && mat.GetFloat("_UnderlayDilate") > 0.01f);
                return hasOutline || hasUnderlay;
            }
            catch { return false; }
        }

        public static void FindAndCacheBestGameTemplateMaterial()
        {
            if (_cachedBestGameTemplateMaterial != null) return;
            try
            {
                var allMats = Resources.FindObjectsOfTypeAll<Material>();
                if (allMats != null && allMats.Length > 0)
                {
                    _cachedBestGameTemplateMaterial = allMats.FirstOrDefault(m => m != null && m.name.IndexOf("Alphakind", StringComparison.OrdinalIgnoreCase) >= 0 && m.IsKeywordEnabled("UNDERLAY_ON"))
                        ?? allMats.FirstOrDefault(m => m != null && m.name.IndexOf("Capriola", StringComparison.OrdinalIgnoreCase) >= 0 && m.IsKeywordEnabled("UNDERLAY_ON"))
                        ?? allMats.FirstOrDefault(m => m != null && HasVisualEffects(m));

                    if (_cachedBestGameTemplateMaterial != null)
                    {
                        VietnamesePlugin.ModLog($"[FindAndCacheBestGameTemplateMaterial] Đã lưu mẫu Material chuẩn PlateUp: '{_cachedBestGameTemplateMaterial.name}', Shader='{_cachedBestGameTemplateMaterial.shader?.name}'");
                    }
                }
            }
            catch { }
        }

        public static void ConfigureFontMaterialProperties(Material? mat, TMP_FontAsset? font = null)
        {
            if (mat == null) return;
            try
            {
                FindAndCacheBestGameTemplateMaterial();
                var nativeShader = _cachedBestGameTemplateMaterial?.shader 
                    ?? Shader.Find("TextMeshPro/Distance Field") 
                    ?? Shader.Find("TextMeshPro/Mobile/Distance Field");
                if (nativeShader != null && (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    mat.shader = nativeShader;
                }

                if (font != null && font.atlasTexture != null)
                {
                    mat.mainTexture = font.atlasTexture;
                    mat.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
                    mat.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasTexture.width);
                    mat.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasTexture.height);
                    mat.SetFloat("_TextureWidth", font.atlasTexture.width);
                    mat.SetFloat("_TextureHeight", font.atlasTexture.height);
                }

                // Cấu hình viền (Outline) đậm nét và bóng đổ (Underlay) mềm mại cho Material gốc của phông
                mat.EnableKeyword("OUTLINE_ON");
                mat.EnableKeyword("UNDERLAY_ON");
                mat.shaderKeywords = new string[] { "OUTLINE_ON", "UNDERLAY_ON" };

                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.30f);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.02f, 0.02f, 0.02f, 1.0f));
                mat.SetFloat("_OutlineWidth", 0.30f);
                mat.SetColor("_OutlineColor", new Color(0.02f, 0.02f, 0.02f, 1.0f));

                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.55f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.80f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.30f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.40f);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.02f, 0.01f, 0.01f, 0.85f));

                mat.SetFloat("_UnderlayOffsetX", 0.55f);
                mat.SetFloat("_UnderlayOffsetY", -0.80f);
                mat.SetFloat("_UnderlayDilate", 0.30f);
                mat.SetFloat("_UnderlaySoftness", 0.40f);
                mat.SetColor("_UnderlayColor", new Color(0.02f, 0.01f, 0.01f, 0.85f));

                try { ShaderUtilities.UpdateShaderRatios(mat); } catch { }
            }
            catch (Exception ex)
            {
                VietnamesePlugin.ModLog($"[ConfigureFontMaterialProperties] Lỗi cấu hình font material: {ex.Message}");
            }
        }

        public static Material GetOrCreatePreservedMaterial(TMP_FontAsset targetFont, Material? sourceMat)
        {
            if (targetFont == null || targetFont.material == null) return null!;
            FindAndCacheBestGameTemplateMaterial();
            if (sourceMat == null) sourceMat = _cachedBestGameTemplateMaterial;

            string key = sourceMat != null
                ? $"{targetFont.name}_{sourceMat.name}_{sourceMat.GetInstanceID()}"
                : $"{targetFont.name}_DefaultPreserved";

            if (_clonedMaterialCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var created = CreatePreservedMaterialPreset(targetFont, sourceMat);
            _clonedMaterialCache[key] = created;
            return created;
        }

        public static Material CreatePreservedMaterialPreset(TMP_FontAsset targetFont, Material? sourceMat)
        {
            if (targetFont == null || targetFont.material == null) return null!;
            FindAndCacheBestGameTemplateMaterial();

            // Nếu sourceMat không có hiệu ứng (ví dụ PathID 109 phẳng) thì kế thừa từ template chuẩn của game
            if (sourceMat == null || !HasVisualEffects(sourceMat))
            {
                if (_cachedBestGameTemplateMaterial != null && HasVisualEffects(_cachedBestGameTemplateMaterial))
                {
                    sourceMat = _cachedBestGameTemplateMaterial;
                }
            }
            if (sourceMat == null) sourceMat = targetFont.material;

            // 1. Tạo Material nhân bản từ sourceMat
            Material clonedMat = new Material(sourceMat);

            // Đảm bảo shader hợp lệ
            var nativeShader = sourceMat?.shader 
                ?? _cachedBestGameTemplateMaterial?.shader
                ?? Shader.Find("TextMeshPro/Distance Field") 
                ?? Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (nativeShader != null && (clonedMat.shader == null || clonedMat.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                clonedMat.shader = nativeShader;
            }

            clonedMat.name = sourceMat != null
                ? $"{targetFont.name}_{sourceMat.name}_Preserved"
                : $"{targetFont.name}_Universal_Preserved";
            clonedMat.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;

            // 2. Gán Texture Atlas của targetFont vào Material
            Texture? targetAtlas = targetFont.material != null ? targetFont.material.GetTexture(ShaderUtilities.ID_MainTex) : null;
            if (targetAtlas == null) targetAtlas = targetFont.atlasTexture;

            if (targetAtlas != null)
            {
                clonedMat.SetTexture(ShaderUtilities.ID_MainTex, targetAtlas);
                clonedMat.mainTexture = targetAtlas;
                clonedMat.SetFloat(ShaderUtilities.ID_TextureWidth, targetAtlas.width);
                clonedMat.SetFloat(ShaderUtilities.ID_TextureHeight, targetAtlas.height);
                clonedMat.SetFloat("_TextureWidth", targetAtlas.width);
                clonedMat.SetFloat("_TextureHeight", targetAtlas.height);
            }

            // 3. GradientScale của atlas đích
            float targetGradientScale = 10.0f;
            if (targetFont.material != null)
            {
                if (targetFont.material.HasProperty(ShaderUtilities.ID_GradientScale))
                {
                    float tgs = targetFont.material.GetFloat(ShaderUtilities.ID_GradientScale);
                    if (tgs > 0.01f) targetGradientScale = tgs;
                }
                else if (targetFont.material.HasProperty("_GradientScale"))
                {
                    float tgs = targetFont.material.GetFloat("_GradientScale");
                    if (tgs > 0.01f) targetGradientScale = tgs;
                }
            }

            clonedMat.SetFloat(ShaderUtilities.ID_GradientScale, targetGradientScale);
            if (clonedMat.HasProperty("_GradientScale")) clonedMat.SetFloat("_GradientScale", targetGradientScale);

            // 4. HIỆU ỨNG UNDERLAY (ĐỔ BÓNG MỀM CHUẨN XÁC CỦA GAME PLATEUP)
            // Cân chỉnh tỉ lệ SDF cho Texture 2048: Offset(0.55, -0.80), Dilate=0.30, Softness=0.40, Color đen 85%
            float underlayOffsetX = 0.55f;
            float underlayOffsetY = -0.80f;
            float underlayDilate = 0.30f;
            float underlaySoftness = 0.40f;
            Color underlayColor = new Color(0.02f, 0.01f, 0.01f, 0.85f);

            if (sourceMat != null)
            {
                if (sourceMat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                {
                    float ox = sourceMat.GetFloat(ShaderUtilities.ID_UnderlayOffsetX);
                    if (Math.Abs(ox) > 0.05f) underlayOffsetX = Mathf.Sign(ox) * Mathf.Clamp(Math.Abs(ox), 0.45f, 0.85f);
                }
                if (sourceMat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                {
                    float oy = sourceMat.GetFloat(ShaderUtilities.ID_UnderlayOffsetY);
                    if (Math.Abs(oy) > 0.05f) underlayOffsetY = Mathf.Sign(oy) * Mathf.Clamp(Math.Abs(oy), 0.55f, 0.95f);
                }
                if (sourceMat.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                {
                    float dil = sourceMat.GetFloat(ShaderUtilities.ID_UnderlayDilate);
                    if (dil > 0.02f) underlayDilate = Mathf.Clamp(dil * 2.0f, 0.20f, 0.40f);
                }
                if (sourceMat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                {
                    float soft = sourceMat.GetFloat(ShaderUtilities.ID_UnderlaySoftness);
                    if (soft > 0.02f) underlaySoftness = Mathf.Clamp(soft, 0.25f, 0.45f);
                }
                if (sourceMat.HasProperty(ShaderUtilities.ID_UnderlayColor))
                {
                    var col = sourceMat.GetColor(ShaderUtilities.ID_UnderlayColor);
                    if (col.a > 0.1f) underlayColor = new Color(col.r, col.g, col.b, Mathf.Max(col.a, 0.80f));
                }
            }

            clonedMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, underlayOffsetX);
            clonedMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, underlayOffsetY);
            clonedMat.SetFloat(ShaderUtilities.ID_UnderlayDilate, underlayDilate);
            clonedMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, underlaySoftness);
            clonedMat.SetColor(ShaderUtilities.ID_UnderlayColor, underlayColor);

            clonedMat.SetFloat("_UnderlayOffsetX", underlayOffsetX);
            clonedMat.SetFloat("_UnderlayOffsetY", underlayOffsetY);
            clonedMat.SetFloat("_UnderlayDilate", underlayDilate);
            clonedMat.SetFloat("_UnderlaySoftness", underlaySoftness);
            clonedMat.SetColor("_UnderlayColor", underlayColor);

            // 5. HIỆU ỨNG OUTLINE (VIỀN ĐẬM RÕ NÉT CỦA GAME PLATEUP)
            float outlineWidth = 0.30f;
            Color outlineColor = new Color(0.02f, 0.02f, 0.02f, 1.0f);

            if (sourceMat != null)
            {
                if (sourceMat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                {
                    float ow = sourceMat.GetFloat(ShaderUtilities.ID_OutlineWidth);
                    if (ow > 0.02f) outlineWidth = Mathf.Clamp(ow * 2.5f, 0.25f, 0.45f);
                }
                if (sourceMat.HasProperty(ShaderUtilities.ID_OutlineColor))
                {
                    var oc = sourceMat.GetColor(ShaderUtilities.ID_OutlineColor);
                    if (oc.a > 0.1f) outlineColor = new Color(oc.r, oc.g, oc.b, 1.0f);
                }
            }

            clonedMat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            clonedMat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            clonedMat.SetFloat("_OutlineWidth", outlineWidth);
            clonedMat.SetColor("_OutlineColor", outlineColor);

            // 6. THIẾT LẬP KEYWORDS ĐỒNG BỘ TUYỆT ĐỐI CHO SHADER (LUÔN BẬT OUTLINE VÀ UNDERLAY)
            var kwList = new List<string>();
            if (sourceMat != null && sourceMat.shaderKeywords != null)
            {
                foreach (var kw in sourceMat.shaderKeywords)
                {
                    if (!string.IsNullOrEmpty(kw) && kw != "UNDERLAY_ON" && kw != "UNDERLAY_INNER" && kw != "OUTLINE_ON")
                        kwList.Add(kw);
                }
            }
            kwList.Add("UNDERLAY_ON");
            kwList.Add("OUTLINE_ON");

            clonedMat.shaderKeywords = kwList.Distinct().ToArray();
            clonedMat.EnableKeyword("UNDERLAY_ON");
            clonedMat.EnableKeyword("OUTLINE_ON");

            try { ShaderUtilities.UpdateShaderRatios(clonedMat); } catch { }

            VietnamesePlugin.ModLog($"[CreatePreservedMaterialPreset] Áp dụng bóng đổ & viền PlateUp: '{clonedMat.name}', OutlineWidth={outlineWidth:F2}, Underlay=({underlayOffsetX:F2},{underlayOffsetY:F2}, Soft={underlaySoftness:F2}), Keywords='{string.Join(",", clonedMat.shaderKeywords)}'");

            return clonedMat;
        }

        public static void SyncMaterialProperties(Material sourceMat, Material destMat)
        {
            if (sourceMat == null || destMat == null || sourceMat == destMat) return;
            try
            {
                var destTex = destMat.mainTexture;
                TMP_MaterialManager.CopyMaterialPresetProperties(sourceMat, destMat);
                if (destTex != null)
                {
                    destMat.mainTexture = destTex;
                    destMat.SetTexture(ShaderUtilities.ID_MainTex, destTex);
                }

                var kwList = new List<string>();
                if (sourceMat.shaderKeywords != null)
                {
                    foreach (var kw in sourceMat.shaderKeywords)
                    {
                        if (!string.IsNullOrEmpty(kw) && kw != "UNDERLAY_ON" && kw != "UNDERLAY_INNER" && kw != "OUTLINE_ON")
                            kwList.Add(kw);
                    }
                }
                kwList.Add("UNDERLAY_ON");
                kwList.Add("OUTLINE_ON");

                destMat.shaderKeywords = kwList.Distinct().ToArray();
                destMat.EnableKeyword("UNDERLAY_ON");
                destMat.EnableKeyword("OUTLINE_ON");

                ShaderUtilities.UpdateShaderRatios(destMat);
            }
            catch { }
        }

        public static void SynchronizeSubMeshMaterials(TMP_Text textComponent, Material? parentMat)
        {
            if (textComponent == null || parentMat == null) return;
            try
            {
                var sub3D = textComponent.GetComponentsInChildren<TMP_SubMesh>(true);
                if (sub3D != null)
                {
                    foreach (var s in sub3D)
                    {
                        if (s != null && s.sharedMaterial != null && s.sharedMaterial != parentMat)
                        {
                            // Chỉ đồng bộ thuộc tính hiệu ứng nếu submesh dùng texture khác (fallback font)
                            // TUYỆT ĐỐI KHÔNG ghi đè s.sharedMaterial = parentMat vì sẽ làm lệch texture atlas gây vệt đen
                            SyncMaterialProperties(parentMat, s.sharedMaterial);
                        }
                    }
                }

                var subUI = textComponent.GetComponentsInChildren<TMP_SubMeshUI>(true);
                if (subUI != null)
                {
                    foreach (var s in subUI)
                    {
                        if (s != null && s.sharedMaterial != null && s.sharedMaterial != parentMat)
                        {
                            SyncMaterialProperties(parentMat, s.sharedMaterial);
                        }
                    }
                }
            }
            catch { }
        }
    }
}

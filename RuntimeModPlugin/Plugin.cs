using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using KitchenData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VietnameseLocalizationMod
{
    [BepInPlugin("com.vietnam.localization", "Vietnamese Localization Mod", "1.0.0")]
    public class VietnamesePlugin : BaseUnityPlugin
    {
        private Harmony? _harmony;
        public static string DebugLogPath = @"C:\Users\HP\Desktop\GeminiUnity\Output\debug_mod_runtime.log";

        private float _scanTimer = 0f;
        private float _telemetryTimer = 0f;
        private bool _firstUpdate = true;
        private bool _gameDataInjected = false;

        public static void ModLog(string msg)
        {
            try
            {
                File.AppendAllText(DebugLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }

        private void Awake()
        {
            try { if (File.Exists(DebugLogPath)) File.Delete(DebugLogPath); } catch { }
            ModLog(">>> VietnamesePlugin.Awake() STARTED");
            Logger.LogInfo(">>> [Vietnamese Localization Mod] Khởi động...");

            try
            {
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            catch { }

            // 0. Preload các assembly cốt lõi của game để đảm bảo AppDomain nhận diện đầy đủ Type
            PreloadGameAssemblies();

            string pluginDir = Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath;
            string datFile = Path.Combine(pluginDir, "Vietnamese.dat");

            // 1. Giải mã trực tiếp từ file nhị phân vào RAM
            LocalizationManager.Initialize(datFile, pluginDir, msg => Logger.LogInfo(msg));

            // 2. Khởi tạo Fallback Font tiếng Việt cho TextMeshPro
            FontFallbackPatcher.Initialize(pluginDir, msg => Logger.LogInfo(msg));

            // 2b. Nạp cấu hình Chế độ Mod (Step5_Capture / Step6_SampleText / Step7_SampleFont / Step9_Production)
            LoadModModeConfiguration(pluginDir);

            // 2c. Khởi tạo hệ thống Live Font Telemetry để theo dõi render thực tế trên màn hình
            LiveFontTelemetry.Initialize(pluginDir);

            // 3. Thực hiện Harmony Patching mạnh mẽ và an toàn
            _harmony = new Harmony("com.vietnam.localization.harmony");
            ApplyHooks();

            // 4. Lắng nghe sự kiện chuyển scene để quét text tự động
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 5. Tự động chụp Telemetry ban đầu
            try
            {
                var initSnap = LiveFontTelemetry.CaptureCurrentScreenSnapshot("PlateUp!");
                LiveFontTelemetry.DumpSnapshotToFile(initSnap);
            }
            catch { }

            ModLog(">>> VietnamesePlugin.Awake() FINISHED");
        }

        private static string _currentModMode = "Normal";

        private void LoadModModeConfiguration(string pluginDir)
        {
            string modeFile = Path.Combine(pluginDir, "mod_mode.json");
            if (File.Exists(modeFile))
            {
                try
                {
                    string content = File.ReadAllText(modeFile);
                    if (content.IndexOf("Step5_Capture", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _currentModMode = "Step5_Capture";
                        LocalizationManager.IsEnabled = false;
                        LocalizationManager.UseSampleMockText = false;
                        LocalizationManager.DumpNewStringsEnabled = true;
                        FontFallbackPatcher.IsEnabled = false;
                        ModLog(">>> [ModMode] Chế độ BƯỚC 5: Baseline Capture (Không dịch, Không đổi font, Tự động capture UI)");
                    }
                    else if (content.IndexOf("Step6_SampleText", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _currentModMode = "Step6_SampleText";
                        LocalizationManager.IsEnabled = true;
                        LocalizationManager.UseSampleMockText = true;
                        LocalizationManager.DumpNewStringsEnabled = true;
                        FontFallbackPatcher.IsEnabled = false;
                        ModLog(">>> [ModMode] Chế độ BƯỚC 6: Live Test Text Mẫu (Dịch LIVETEST_X, Giữ nguyên font gốc)");
                    }
                    else if (content.IndexOf("Step7_SampleFont", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _currentModMode = "Step7_SampleFont";
                        LocalizationManager.IsEnabled = false;
                        LocalizationManager.UseSampleMockText = false;
                        LocalizationManager.DumpNewStringsEnabled = true;
                        FontFallbackPatcher.IsEnabled = true;
                        ModLog(">>> [ModMode] Chế độ BƯỚC 7: Live Test Font Mẫu (Ép font sang ARIAL SDF, Không dịch text)");
                    }
                    else if (content.IndexOf("Step9_Production", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _currentModMode = "Step9_Production";
                        LocalizationManager.IsEnabled = true;
                        LocalizationManager.UseSampleMockText = false;
                        LocalizationManager.DumpNewStringsEnabled = true;
                        FontFallbackPatcher.IsEnabled = true;
                        ModLog(">>> [ModMode] Chế độ BƯỚC 9: Live Test Sản Phẩm Việt Hóa (Dịch tiếng Việt thật + Đổi font chữ mới)");
                    }
                    else
                    {
                        _currentModMode = "Normal";
                        LocalizationManager.IsEnabled = true;
                        LocalizationManager.UseSampleMockText = false;
                        FontFallbackPatcher.IsEnabled = true;
                        LocalizationManager.DumpNewStringsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    ModLog($"[ModMode] Lỗi đọc mod_mode.json: {ex.Message}");
                }
            }
            else
            {
                _currentModMode = "Normal";
                LocalizationManager.IsEnabled = true;
                LocalizationManager.UseSampleMockText = false;
                FontFallbackPatcher.IsEnabled = true;
                LocalizationManager.DumpNewStringsEnabled = true;
            }
        }

        private void PreloadGameAssemblies()
        {
            try
            {
                string managed = Path.Combine(Paths.GameRootPath, "PlateUp_Data", "Managed");
                string[] toLoad = { "Kitchen.Common.dll", "Kitchen.GameData.dll", "Kitchen.Layouts.dll", "Unity.InputSystem.dll" };
                foreach (var dll in toLoad)
                {
                    string p = Path.Combine(managed, dll);
                    if (File.Exists(p))
                    {
                        Assembly.LoadFrom(p);
                        ModLog($"[Preload] Đã nạp trước assembly: {dll}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog($"[Preload] Lỗi preload assemblies: {ex.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ModLog($">>> [SceneLoaded] Scene mới: {scene.name} (mode={mode}). Thực hiện dịch lại toàn bộ text...");
            try
            {
                FontFallbackPatcher.EnsureFontsLoaded();
                FontFallbackPatcher.FindAndCacheBestGameTemplateMaterial();
                try
                {
                    var mainGd = GameData.Main;
                    if (mainGd != null && mainGd.GlobalLocalisation != null)
                    {
                        InjectGameDataDirectly(mainGd);
                    }
                }
                catch { }

                if (this != null && isActiveAndEnabled)
                {
                    StartCoroutine(DelayedSceneTranslate());
                }
                else
                {
                    TranslateAllSceneTexts();
                    DumpTelemetryNow();
                }
            }
            catch (Exception ex)
            {
                ModLog($"[OnSceneLoaded] Lỗi Coroutine: {ex.Message}. Thực thi trực tiếp.");
                TranslateAllSceneTexts();
                DumpTelemetryNow();
            }
        }

        private IEnumerator DelayedSceneTranslate()
        {
            yield return null;
            yield return new WaitForSeconds(0.2f);
            TranslateAllSceneTexts();
            DumpTelemetryNow();
        }

        public static void DumpTelemetryNow()
        {
            try
            {
                var snap = LiveFontTelemetry.CaptureCurrentScreenSnapshot("PlateUp!");
                LiveFontTelemetry.DumpSnapshotToFile(snap);
            }
            catch { }
        }

        private void SafePatch(string name, Action patchAction)
        {
            try
            {
                patchAction();
                ModLog($">>> [SafePatch] Hook thành công: {name}");
                Logger.LogInfo($">>> [Vietnamese Localization Mod] Hook thành công: {name}");
            }
            catch (Exception ex)
            {
                ModLog($">>> [SafePatch] Bỏ qua hook {name}: {ex.Message}");
                Logger.LogWarning($">>> [Vietnamese Localization Mod] Bỏ qua hook {name}: {ex.Message}");
            }
        }

        private void ApplyHooks()
        {
            if (_harmony == null) return;

            // ==========================================
            // A. TEXTMESHPRO BASE & DERIVED HOOKS
            // ==========================================
            // 1. TMP_Text.set_text
            SafePatch("TMP_Text.set_text", () =>
            {
                var target = AccessTools.PropertySetter(typeof(TMP_Text), "text");
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_TMP_SetText)));
            });

            // 1b. TMP_Text.set_font (Interception trực tiếp khi gán font)
            SafePatch("TMP_Text.set_font", () =>
            {
                var target = AccessTools.PropertySetter(typeof(TMP_Text), "font");
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, 
                    prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_TMP_SetFont)),
                    postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_TMP_SetFont)));
            });

            // 2. TextMeshProUGUI.GenerateTextMesh
            SafePatch("TextMeshProUGUI.GenerateTextMesh", () =>
            {
                var target = AccessTools.Method(typeof(TextMeshProUGUI), "GenerateTextMesh");
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_GenerateTextMesh)));
            });

            // 3. TextMeshPro.GenerateTextMesh
            SafePatch("TextMeshPro.GenerateTextMesh", () =>
            {
                var target = AccessTools.Method(typeof(TextMeshPro), "GenerateTextMesh");
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_GenerateTextMesh)));
            });

            // 3b. TMP_SubMesh.UpdateMaterial (Đồng bộ Material Preset Outline/Shadow cho chữ 3D)
            SafePatch("TMP_SubMesh.UpdateMaterial", () =>
            {
                var target = AccessTools.Method(typeof(TMP_SubMesh), "UpdateMaterial");
                if (target != null)
                {
                    _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_SubMesh_UpdateMaterial)));
                }
            });

            // 3c. TMP_SubMeshUI.UpdateMaterial (Đồng bộ Material Preset Outline/Shadow cho chữ UI Canvas)
            SafePatch("TMP_SubMeshUI.UpdateMaterial", () =>
            {
                var target = AccessTools.Method(typeof(TMP_SubMeshUI), "UpdateMaterial");
                if (target != null)
                {
                    _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_SubMeshUI_UpdateMaterial)));
                }
            });

            // ==========================================
            // B. PLATEUP UI MODULE HOOKS (MENU & BUTTONS)
            // 4. ButtonElement.SetLabel
            SafePatch("ButtonElement.SetLabel", () =>
            {
                var target = AccessTools.Method(typeof(ButtonElement), nameof(ButtonElement.SetLabel), new[] { typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, 
                    prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_ButtonElement_SetLabel)),
                    postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_ButtonElement_SetLabel)));
            });

            // 4b. ButtonElement.SetStyle
            SafePatch("ButtonElement.SetStyle", () =>
            {
                var target = AccessTools.Method(typeof(ButtonElement), nameof(ButtonElement.SetStyle), new[] { typeof(ElementStyle) });
                if (target != null)
                {
                    _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_ButtonElement_SetStyle)));
                }
            });

            // 5. HeadingElement.SetHeading
            SafePatch("HeadingElement.SetHeading", () =>
            {
                var target = AccessTools.Method(typeof(HeadingElement), nameof(HeadingElement.SetHeading), new[] { typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_HeadingElement_SetHeading)));
            });

            // 6. LabelElement.SetLabel
            SafePatch("LabelElement.SetLabel", () =>
            {
                var target = AccessTools.Method(typeof(LabelElement), nameof(LabelElement.SetLabel), new[] { typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_LabelElement_SetLabel)));
            });

            // 7. TextPanelElement.SetText
            SafePatch("TextPanelElement.SetText", () =>
            {
                var target = AccessTools.Method(typeof(TextPanelElement), nameof(TextPanelElement.SetText), new[] { typeof(string), typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_TextPanelElement_SetText)));
            });

            // 8. PromptedButtonModule.SetLabel
            SafePatch("PromptedButtonModule.SetLabel", () =>
            {
                var target = AccessTools.Method(typeof(PromptedButtonModule), nameof(PromptedButtonModule.SetLabel), new[] { typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Prefix_PromptedButtonModule_SetLabel)));
            });

            // 9. StartMainMenu.Setup
            SafePatch("StartMainMenu.Setup", () =>
            {
                var target = AccessTools.Method(typeof(StartMainMenu), "Setup", new[] { typeof(int) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_StartMainMenu_Setup)));
            });

            // 10. MainMenu.Setup
            SafePatch("MainMenu.Setup", () =>
            {
                var target = AccessTools.Method(typeof(MainMenu), "Setup", new[] { typeof(int) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_MainMenu_Setup)));
            });

            // ==========================================
            // C. PLATEUP LOCALISATION & GAMEDATA HOOKS
            // ==========================================
            // 11. GlobalLocalisation.get_Item(string)
            SafePatch("GlobalLocalisation.get_Item(string)", () =>
            {
                var target = AccessTools.Method(typeof(GlobalLocalisation), "get_Item", new[] { typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_GlobalLocalisation_get_Item)));
            });

            // 12. DictionaryLocalisation.get_Item(string)
            SafePatch("DictionaryLocalisation.get_Item(string)", () =>
            {
                var target = AccessTools.Method(typeof(DictionaryLocalisation), "get_Item", new[] { typeof(string) });
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_DictionaryLocalisation_get_Item)));
            });

            // 13. AutoGlobalLocal.Awake (Lobby elements)
            SafePatch("AutoGlobalLocal.Awake", () =>
            {
                var target = AccessTools.Method(typeof(AutoGlobalLocal), "Awake");
                if (target == null) throw new InvalidOperationException("Target method not found");
                _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_AutoGlobalLocal_Awake)));
            });

            // 14. GameDataConstructor.BuildGameData
            SafePatch("GameDataConstructor.BuildGameData", () =>
            {
                Type? gdcType = AccessTools.TypeByName("KitchenData.GameDataConstructor");
                if (gdcType != null)
                {
                    var target = AccessTools.Method(gdcType, "BuildGameData");
                    if (target != null)
                    {
                        _harmony.Patch(target, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_GameDataConstructor_BuildGameData)));
                    }
                }
            });

            // 15. Unlock.Name & Description
            SafePatch("Unlock.Name and Description", () =>
            {
                var nameTarget = AccessTools.PropertyGetter(typeof(Unlock), "Name");
                if (nameTarget != null)
                {
                    _harmony.Patch(nameTarget, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_Unlock_Getter)));
                }
                var descTarget = AccessTools.PropertyGetter(typeof(Unlock), "Description");
                if (descTarget != null)
                {
                    _harmony.Patch(descTarget, postfix: new HarmonyMethod(typeof(VietnamesePlugin), nameof(Postfix_Unlock_Getter)));
                }
            });
        }

        private void Update()
        {
            if (_firstUpdate)
            {
                _firstUpdate = false;
                FontFallbackPatcher.FindAndCacheBestGameTemplateMaterial();
                ModLog(">>> VietnamesePlugin.Update() FIRST FRAME RUNNING!");
            }

            // 1. Phím tắt F8 hoặc Alt + T bật/tắt việt hóa
            bool toggle = false;
            try
            {
                if (Input.GetKeyDown(KeyCode.F8) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.T)))
                {
                    toggle = true;
                }
            }
            catch { }

            if (toggle)
            {
                LocalizationManager.IsEnabled = !LocalizationManager.IsEnabled;
                string state = LocalizationManager.IsEnabled ? "BẬT (Tiếng Việt)" : "TẮT (Tiếng Anh)";
                ModLog($">>> [HOTKEY F8] Đã chuyển đổi trạng thái Việt hóa: {state}");
                TranslateAllSceneTexts();
                DumpTelemetryNow();
            }

            // 2. Tiêm dữ liệu vào KitchenData.GameData.Main khi game đã khởi tạo xong
            try
            {
                var mainGd = GameData.Main;
                if (mainGd != null && mainGd.GlobalLocalisation != null)
                {
                    if (!_gameDataInjected)
                    {
                        InjectGameDataDirectly(mainGd);
                        _gameDataInjected = true;
                    }
                }
            }
            catch { }

            // 3. Quét định kỳ toàn bộ TextMeshPro trên màn hình mỗi 0.8s
            _scanTimer += Time.unscaledDeltaTime;
            if (_scanTimer >= 0.8f)
            {
                _scanTimer = 0f;
                if (LocalizationManager.IsEnabled || FontFallbackPatcher.IsEnabled)
                {
                    TranslateAllSceneTexts();
                }
                DumpTelemetryNow();
            }
        }

        public static void TranslateAllSceneTexts()
        {
            try
            {
                var allTMP = Resources.FindObjectsOfTypeAll<TMP_Text>();
                if (allTMP == null || allTMP.Length == 0) return;

                int translatedCount = 0;
                foreach (var tmp in allTMP)
                {
                    if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;

                    // 1. Luôn kiểm tra và áp dụng font 1-1 / fallback font chuẩn nếu bật chế độ font
                    if (FontFallbackPatcher.IsEnabled)
                    {
                        FontFallbackPatcher.AttachFallbackFont(tmp);
                    }

                    // 2. Dịch văn bản nếu cần
                    string cur = tmp.text;
                    if (string.IsNullOrEmpty(cur)) continue;

                    if (LocalizationManager.IsEnabled)
                    {
                        string tr = LocalizationManager.Translate(cur, tmp.transform);
                        if (!string.IsNullOrEmpty(tr) && tr != cur)
                        {
                            tmp.text = tr;
                            translatedCount++;
                        }
                    }
                }

                if (translatedCount > 0)
                {
                    ModLog($"[TranslateAllSceneTexts] Đã cập nhật {translatedCount} thành phần giao diện trên màn hình.");
                }
            }
            catch (Exception ex)
            {
                ModLog($"[TranslateAllSceneTexts] Lỗi: {ex.Message}");
            }
        }

        private static void InjectGameDataDirectly(GameData mainGd)
        {
            try
            {
                if (LocalizationManager.IsEnabled && mainGd.GlobalLocalisation != null && mainGd.GlobalLocalisation.Text != null)
                {
                    var textDict = mainGd.GlobalLocalisation.Text;
                    int injected = 0;
                    foreach (var kvp in LocalizationManager.DictKeyMap)
                    {
                        textDict[kvp.Key] = kvp.Value;
                        injected++;
                    }
                    foreach (var key in textDict.Keys.ToList())
                    {
                        string cur = textDict[key];
                        string trans = LocalizationManager.Translate(cur, null);
                        if (!string.IsNullOrEmpty(trans) && trans != cur)
                        {
                            textDict[key] = trans;
                        }
                    }
                    ModLog($">>> [InjectGameDataDirectly] Đã nạp {injected} DictKeys vào GlobalLocalisation (Tổng: {textDict.Count} mục)!");
                }

                // Thẻ Unlock được dịch qua Hook Unlock.get_Name và Unlock.get_Description

                // Gán đúng từng phông tương ứng vào từ điển Font nội tại của game (KitchenData.Font)
                if (FontFallbackPatcher.IsEnabled && mainGd.GlobalLocalisation != null && mainGd.GlobalLocalisation.Fonts != null)
                {
                    FontFallbackPatcher.EnsureFontsLoaded();
                    var mainMenuFont = FontFallbackPatcher.GetReplacementFontFor("Reference - Main Menu Main Text")
                        ?? FontFallbackPatcher.GetReplacementFontFor("English - Main Menu Main Text")
                        ?? FontFallbackPatcher.GetReplacementFontFor("Chewy SDF")
                        ?? FontFallbackPatcher.GetAnyCustomFont();

                    var defaultFont = FontFallbackPatcher.GetReplacementFontFor("Reference - Primary Text")
                        ?? FontFallbackPatcher.GetReplacementFontFor("English - Primary Text")
                        ?? FontFallbackPatcher.GetReplacementFontFor("Capriola-Regular SDF")
                        ?? FontFallbackPatcher.GetAnyCustomFont();

                    if (mainMenuFont != null)
                    {
                        mainGd.GlobalLocalisation.Fonts[KitchenData.Font.MainMenu] = mainMenuFont;
                        ModLog($">>> [InjectGameDataDirectly] Gán GlobalLocalisation.Fonts[MainMenu] = {mainMenuFont.name}");
                    }

                    if (defaultFont != null)
                    {
                        mainGd.GlobalLocalisation.Fonts[KitchenData.Font.Default] = defaultFont;
                        ModLog($">>> [InjectGameDataDirectly] Gán GlobalLocalisation.Fonts[Default] = {defaultFont.name}");
                    }

                    ModLog($">>> [InjectGameDataDirectly] ĐÃ CẬP NHẬT GlobalLocalisation.Fonts theo ma trận font thành công!");
                }
            }
            catch (Exception ex)
            {
                ModLog($">>> [InjectGameDataDirectly] Lỗi: {ex}");
            }
        }

        public static void Prefix_TMP_SetFont(TMP_Text __instance, ref TMP_FontAsset value)
        {
            if (__instance == null || !FontFallbackPatcher.IsEnabled) return;
            if (value == null) return;

            string valName = value.name ?? "";
            string cleanName = valName.Replace("(Instance)", "").Trim();

            if (FontFallbackPatcher.IsProtectedFontPublic(cleanName)) return;
            if (FontFallbackPatcher.IsCustomFont(value)) return;

            // Lưu lại font gốc và material gốc trước khi bị thay
            if (!FontFallbackPatcher.OriginalFontTracker.TryGetValue(__instance, out _))
            {
                FontFallbackPatcher.OriginalFontTracker.Add(__instance, cleanName);
            }
            if (!FontFallbackPatcher.OriginalMaterialTracker.TryGetValue(__instance, out _) && __instance.fontSharedMaterial != null)
            {
                FontFallbackPatcher.OriginalMaterialTracker.Add(__instance, __instance.fontSharedMaterial);
            }

            FontFallbackPatcher.EnsureFontsLoaded();
            var replacement = FontFallbackPatcher.GetReplacementFontFor(cleanName);
            if (replacement != null && replacement != value)
            {
                value = replacement;
                ModLog($"[TMP.set_font] Chuyển hướng font sang '{replacement.name}' (Gốc: '{valName}', Obj: {__instance.gameObject.name})");
            }
        }

        public static void Postfix_TMP_SetFont(TMP_Text __instance)
        {
            if (__instance == null || !FontFallbackPatcher.IsEnabled) return;
            FontFallbackPatcher.AttachFallbackFont(__instance);
        }

        // ==========================================
        // HOOK CALLBACK IMPLEMENTATIONS
        // ==========================================
        public static void Postfix_SubMesh_UpdateMaterial(TMP_SubMesh __instance)
        {
            if (__instance == null || __instance.sharedMaterial == null) return;
            var parentTmp = __instance.GetComponentInParent<TMP_Text>();
            if (parentTmp != null)
            {
                var parentMat = parentTmp.fontSharedMaterial;
                if (parentMat != null && parentMat != __instance.sharedMaterial)
                {
                    FontFallbackPatcher.SyncMaterialProperties(parentMat, __instance.sharedMaterial);
                }
            }
        }

        public static void Postfix_SubMeshUI_UpdateMaterial(TMP_SubMeshUI __instance)
        {
            if (__instance == null || __instance.sharedMaterial == null) return;
            var parentTmp = __instance.GetComponentInParent<TMP_Text>();
            if (parentTmp != null)
            {
                var parentMat = parentTmp.fontSharedMaterial;
                if (parentMat != null && parentMat != __instance.sharedMaterial)
                {
                    FontFallbackPatcher.SyncMaterialProperties(parentMat, __instance.sharedMaterial);
                }
            }
        }

        public static void Postfix_Unlock_Getter(ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || !LocalizationManager.IsEnabled) return;
            string tr = LocalizationManager.Translate(__result, null);
            if (!string.IsNullOrEmpty(tr) && tr != __result)
            {
                __result = tr;
            }
        }

        public static void Prefix_TMP_SetText(TMP_Text __instance, ref string value)
        {
            if (__instance == null) return;

            // Luôn hook font nếu chế độ FontFallbackPatcher được bật (Bước 7, Bước 9)
            if (FontFallbackPatcher.IsEnabled)
            {
                FontFallbackPatcher.AttachFallbackFont(__instance);
            }

            if (LocalizationManager.IsEnabled && !string.IsNullOrEmpty(value))
            {
                string translated = LocalizationManager.Translate(value, __instance.transform);
                if (!string.IsNullOrEmpty(translated) && translated != value)
                {
                    ModLog($"[TMP.set_text] '{value}' -> '{translated}'");
                    value = translated;
                }
            }
        }

        public static void Prefix_GenerateTextMesh(TMP_Text __instance)
        {
            if (__instance == null) return;

            // Luôn đảm bảo font 1-1 / fallback chuẩn trước khi TMP tạo mesh glyphs
            if (FontFallbackPatcher.IsEnabled)
            {
                FontFallbackPatcher.AttachFallbackFont(__instance);
            }

            if (LocalizationManager.IsEnabled && !string.IsNullOrEmpty(__instance.text))
            {
                string cur = __instance.text;
                string tr = LocalizationManager.Translate(cur, __instance.transform);
                if (!string.IsNullOrEmpty(tr) && tr != cur)
                {
                    ModLog($"[GenerateTextMesh] '{cur}' -> '{tr}'");
                    __instance.text = tr;
                }
            }
        }

        public static void Prefix_ButtonElement_SetLabel(ButtonElement __instance, ref string label)
        {
            if (string.IsNullOrEmpty(label) || !LocalizationManager.IsEnabled) return;
            string translated = LocalizationManager.Translate(label, __instance != null ? __instance.transform : null);
            if (!string.IsNullOrEmpty(translated) && translated != label)
            {
                ModLog($"[ButtonElement.SetLabel] '{label}' -> '{translated}'");
                label = translated;
            }
        }

        private static TMP_Text? GetButtonLabel(ButtonElement btn)
        {
            if (btn == null) return null;
            try
            {
                var lbl = Traverse.Create(btn).Field("Label").GetValue<TextMeshPro>();
                if (lbl != null) return lbl;
            }
            catch { }
            return btn.GetComponentInChildren<TMP_Text>(true);
        }

        public static void Postfix_ButtonElement_SetLabel(ButtonElement __instance)
        {
            if (__instance == null) return;
            var label = GetButtonLabel(__instance);
            if (label != null)
            {
                FontFallbackPatcher.AttachFallbackFont(label);
            }
        }

        public static void Postfix_ButtonElement_SetStyle(ButtonElement __instance)
        {
            if (__instance == null) return;
            var label = GetButtonLabel(__instance);
            if (label != null)
            {
                FontFallbackPatcher.AttachFallbackFont(label);
            }
        }

        public static void Prefix_HeadingElement_SetHeading(HeadingElement __instance, ref string heading)
        {
            if (string.IsNullOrEmpty(heading) || !LocalizationManager.IsEnabled) return;
            string translated = LocalizationManager.Translate(heading, __instance != null ? __instance.transform : null);
            if (!string.IsNullOrEmpty(translated) && translated != heading)
            {
                ModLog($"[HeadingElement.SetHeading] '{heading}' -> '{translated}'");
                heading = translated;
            }
        }

        public static void Prefix_LabelElement_SetLabel(LabelElement __instance, ref string label)
        {
            if (string.IsNullOrEmpty(label) || !LocalizationManager.IsEnabled) return;
            string translated = LocalizationManager.Translate(label, __instance != null ? __instance.transform : null);
            if (!string.IsNullOrEmpty(translated) && translated != label)
            {
                ModLog($"[LabelElement.SetLabel] '{label}' -> '{translated}'");
                label = translated;
            }
        }

        public static void Prefix_TextPanelElement_SetText(TextPanelElement __instance, ref string title, ref string text)
        {
            if (!LocalizationManager.IsEnabled) return;
            if (!string.IsNullOrEmpty(title))
            {
                string t = LocalizationManager.Translate(title, __instance != null ? __instance.transform : null);
                if (!string.IsNullOrEmpty(t) && t != title) title = t;
            }
            if (!string.IsNullOrEmpty(text))
            {
                string t = LocalizationManager.Translate(text, __instance != null ? __instance.transform : null);
                if (!string.IsNullOrEmpty(t) && t != text) text = t;
            }
        }

        public static void Prefix_PromptedButtonModule_SetLabel(PromptedButtonModule __instance, ref string msg)
        {
            if (string.IsNullOrEmpty(msg) || !LocalizationManager.IsEnabled) return;
            string translated = LocalizationManager.Translate(msg, __instance != null ? __instance.transform : null);
            if (!string.IsNullOrEmpty(translated) && translated != msg)
            {
                ModLog($"[PromptedButtonModule.SetLabel] '{msg}' -> '{translated}'");
                msg = translated;
            }
        }

        public static void Postfix_StartMainMenu_Setup(StartMainMenu __instance)
        {
            ModLog(">>> Postfix_StartMainMenu_Setup chạy!");
            TranslateAllSceneTexts();
            DumpTelemetryNow();
        }

        public static void Postfix_MainMenu_Setup(MainMenu __instance)
        {
            ModLog(">>> Postfix_MainMenu_Setup chạy!");
            TranslateAllSceneTexts();
            DumpTelemetryNow();
        }

        public static void Postfix_GlobalLocalisation_get_Item(string i, ref string __result)
        {
            if (!LocalizationManager.IsEnabled) return;
            if (!string.IsNullOrEmpty(i))
            {
                string byKey = LocalizationManager.TranslateKey(i);
                if (!string.IsNullOrEmpty(byKey))
                {
                    __result = byKey;
                    return;
                }
            }
            if (!string.IsNullOrEmpty(__result))
            {
                __result = LocalizationManager.Translate(__result, null);
            }
        }

        public static void Postfix_DictionaryLocalisation_get_Item(string i, ref string __result)
        {
            if (!LocalizationManager.IsEnabled) return;
            if (!string.IsNullOrEmpty(i))
            {
                string byKey = LocalizationManager.TranslateKey(i);
                if (!string.IsNullOrEmpty(byKey))
                {
                    __result = byKey;
                    return;
                }
            }
            if (!string.IsNullOrEmpty(__result))
            {
                __result = LocalizationManager.Translate(__result, null);
            }
        }

        public static void Postfix_AutoGlobalLocal_Awake(AutoGlobalLocal __instance)
        {
            if (__instance == null || !LocalizationManager.IsEnabled) return;
            var tmp = __instance.GetComponent<TMP_Text>();
            if (tmp != null && !string.IsNullOrEmpty(tmp.text))
            {
                FontFallbackPatcher.AttachFallbackFont(tmp);
                string cur = tmp.text;
                string trans = LocalizationManager.Translate(cur, tmp.transform);
                if (!string.IsNullOrEmpty(trans) && trans != cur)
                {
                    ModLog($"[AutoGlobalLocal.Awake] '{cur}' -> '{trans}'");
                    tmp.text = trans;
                }
            }
        }

        public static void Postfix_GameDataConstructor_BuildGameData(object __result)
        {
            if (__result == null) return;
            ModLog(">>> Postfix_GameDataConstructor_BuildGameData TRIGGERED! Tiêm dữ liệu bản dịch...");
            if (__result is GameData gd)
            {
                InjectGameDataDirectly(gd);
            }
        }
    }
}

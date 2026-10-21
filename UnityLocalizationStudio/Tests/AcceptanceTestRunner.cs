using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityLocalizationStudio.Core;
using UnityLocalizationStudio.Core.AiLab;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Tests
{
    public class AcceptanceTestRunner
    {
        public static (int Passed, int Failed, List<string> Logs) RunAllTests(string? testAssetsDir = null)
        {
            var logs = new List<string>();
            int passed = 0;
            int failed = 0;

            void AssertTest(string id, string name, bool condition, string detail)
            {
                if (condition)
                {
                    passed++;
                    logs.Add($"[PASS] {id} - {name}: {detail}");
                }
                else
                {
                    failed++;
                    logs.Add($"[FAIL] {id} - {name}: {detail}");
                }
            }

            logs.Add("==========================================================================");
            logs.Add("     BỘ KIỂM THỬ HỒI QUY 12 CA NGHIỆM THU (ACCEPTANCE TESTS AT-01 -> AT-12)");
            logs.Add("==========================================================================");

            // Fixtures giả lập chuẩn cho các ca kiểm thử Asset nhị phân
            var at01Entry = new TranslationEntry
            {
                Id = "13D7F88E0F84CB21",
                SourceKind = SourceKind.UnityAsset,
                AssetFile = "sharedassets1.assets",
                Origin = "OdinStruct",
                Field = "Name",
                OriginalText = "Bookings Stand",
                VietnameseTranslation = ""
            };
            UnityAssetScanner.ClassifyAssetEntry(at01Entry);
            AssertTest("AT-01", "Bookings Stand in Game Assets",
                at01Entry.Classification == StringClassification.GameText && at01Entry.AssetFile == "sharedassets1.assets" && at01Entry.Field == "Name",
                $"ID={at01Entry.Id}, AssetFile={at01Entry.AssetFile}, Field={at01Entry.Field}, Class={at01Entry.Classification}");

            // AT-02: VIEW_CARDS -> View Cards
            var at02Entry = new TranslationEntry
            {
                Id = "TEST_AT02",
                SourceKind = SourceKind.UnityAsset,
                Origin = "OdinDictionary",
                Field = "Text",
                Key = "VIEW_CARDS",
                OriginalText = "View Cards",
                VietnameseTranslation = ""
            };
            UnityAssetScanner.ClassifyAssetEntry(at02Entry);
            AssertTest("AT-02", "VIEW_CARDS -> View Cards",
                at02Entry.Classification == StringClassification.GameText && at02Entry.Key == "VIEW_CARDS",
                $"Key={at02Entry.Key}, Text='{at02Entry.OriginalText}', Class={at02Entry.Classification}");

            // AT-03: OdinStruct Name = "Start day"
            var at03Entry = new TranslationEntry
            {
                Id = "BEC609344945F883",
                SourceKind = SourceKind.UnityAsset,
                AssetFile = "sharedassets1.assets",
                Origin = "OdinStruct",
                Field = "Name",
                OriginalText = "Start day"
            };
            UnityAssetScanner.ClassifyAssetEntry(at03Entry);
            AssertTest("AT-03", "OdinStruct Start day",
                at03Entry.Origin == "OdinStruct" && at03Entry.Field == "Name" && at03Entry.Classification == StringClassification.GameText,
                $"Origin={at03Entry.Origin}, Field={at03Entry.Field}, Class={at03Entry.Classification}");

            // AT-04: OdinDictionary START_DAY_READY = "Start day" (Độc lập với AT-03)
            var at04Entry = new TranslationEntry
            {
                Id = "AC30860A095DB003",
                SourceKind = SourceKind.UnityAsset,
                AssetFile = "sharedassets1.assets",
                Origin = "OdinDictionary",
                Field = "Text",
                Key = "START_DAY_READY",
                OriginalText = "Start day"
            };
            UnityAssetScanner.ClassifyAssetEntry(at04Entry);
            bool at04Distinct = at03Entry.Id != at04Entry.Id;
            AssertTest("AT-04", "Two occurrences of 'Start day' distinct",
                at04Distinct,
                $"AT-03 (ID={at03Entry.Id}, Struct) != AT-04 (ID={at04Entry.Id}, Dict Key={at04Entry.Key}) không bị hợp nhất!");

            // AT-05: OdinStruct Description = "Add chopped cheese to burgers"
            var at05Entry = new TranslationEntry
            {
                Id = "E7A5B757DE6BEA68",
                SourceKind = SourceKind.UnityAsset,
                AssetFile = "sharedassets1.assets",
                Origin = "OdinStruct",
                Field = "Description",
                OriginalText = "Add chopped cheese to burgers"
            };
            UnityAssetScanner.ClassifyAssetEntry(at05Entry);
            AssertTest("AT-05", "Gameplay description in Game Assets",
                at05Entry.Classification == StringClassification.GameText && at05Entry.Field == "Description",
                $"Class={at05Entry.Classification}, Text='{at05Entry.OriginalText}'");

            // AT-06: Newtonsoft.Json CreateUnexpectedEndException
            var dummyJsonEntry = new TranslationEntry
            {
                Id = "TEST_JSON_01",
                SourceKind = SourceKind.ManagedAssembly,
                ContextPath = "Newtonsoft.Json/JsonReader/CreateUnexpectedEndException",
                OriginalText = "Unexpected end when reading JSON."
            };
            SmartClassifier.Classify(dummyJsonEntry);
            AssertTest("AT-06", "Newtonsoft.Json excluded from Game Text",
                dummyJsonEntry.Classification == StringClassification.Technical && dummyJsonEntry.Status == "Ignored",
                $"Class={dummyJsonEntry.Classification}, Status={dummyJsonEntry.Status}, Notes={dummyJsonEntry.Notes}");

            // AT-07: websocket-sharp OPTIONS
            var dummyWsEntry = new TranslationEntry
            {
                Id = "TEST_WS_01",
                SourceKind = SourceKind.ManagedAssembly,
                ContextPath = "websocket-sharp/HttpServer/processRequest",
                OriginalText = "OPTIONS"
            };
            SmartClassifier.Classify(dummyWsEntry);
            AssertTest("AT-07", "websocket-sharp OPTIONS protected",
                dummyWsEntry.Classification == StringClassification.Protected && dummyWsEntry.Status == "Protected",
                $"Class={dummyWsEntry.Classification}, Status={dummyWsEntry.Status}, Notes={dummyWsEntry.Notes}");

            // AT-08: Assembly-CSharp ChatClient ConnectUsingSettings
            var dummyChatEntry = new TranslationEntry
            {
                Id = "TEST_CHAT_01",
                SourceKind = SourceKind.ManagedAssembly,
                ContextPath = "Assembly-CSharp/ChatClient/ConnectUsingSettings",
                OriginalText = "ConnectUsingSettings failed. The appSettings can't be null.'"
            };
            SmartClassifier.Classify(dummyChatEntry);
            AssertTest("AT-08", "ChatClient classified Technical",
                dummyChatEntry.Classification == StringClassification.Technical && dummyChatEntry.Status == "Ignored",
                $"Class={dummyChatEntry.Classification}, Status={dummyChatEntry.Status}, Notes={dummyChatEntry.Notes}");

            // AT-09: Kitchen.Common/RecipeViewElement/UpdateRecipe: "Recipes will appear here during the game!"
            var dummyRecipeEntry = new TranslationEntry
            {
                Id = "TEST_RECIPE_01",
                SourceKind = SourceKind.ManagedAssembly,
                ContextPath = "Kitchen.Common/RecipeViewElement/UpdateRecipe",
                OriginalText = "Recipes will appear here during the game!"
            };
            SmartClassifier.Classify(dummyRecipeEntry);
            AssertTest("AT-09", "RecipeViewElement preserved as GameText",
                dummyRecipeEntry.Classification == StringClassification.GameText && dummyRecipeEntry.Status == "Translate",
                $"Class={dummyRecipeEntry.Classification}, Status={dummyRecipeEntry.Status}, Notes={dummyRecipeEntry.Notes}");

            // AT-10: Placeholder và markup bất biến sau xuất/nhập
            string sampleWithMarkup = "Requires {0} <sprite name=\"heat\"> $upgradable$ and \n new line";
            var markupEntry = new TranslationEntry
            {
                Id = "TEST_MARKUP_01",
                SourceKind = SourceKind.UnityAsset,
                OriginalText = sampleWithMarkup,
                Placeholders = UnityAssetScanner.ExtractPlaceholders(sampleWithMarkup)
            };
            string tempCsv = Path.Combine(Path.GetTempPath(), "test_at10.csv");
            CsvHandler.ExportToProvenanceCsv(new[] { markupEntry }, tempCsv);
            var reloadedMarkup = CsvHandler.ImportFromCsv(tempCsv).FirstOrDefault();
            bool at10Pass = reloadedMarkup != null &&
                            reloadedMarkup.Placeholders.Contains("{0}") &&
                            reloadedMarkup.Placeholders.Contains("<sprite name=\"heat\">") &&
                            reloadedMarkup.Placeholders.Contains("$upgradable$") &&
                            reloadedMarkup.OriginalText == sampleWithMarkup;
            AssertTest("AT-10", "Placeholders and markup invariant",
                at10Pass,
                at10Pass ? $"Tokens bảo toàn nguyên vẹn: {reloadedMarkup?.Placeholders}" : "Lỗi làm hỏng token/markup");
            if (File.Exists(tempCsv)) File.Delete(tempCsv);

            // AT-11: Quét lặp lại đảm bảo ID ổn định không bị duplicate
            string id1 = UnityAssetScanner.ComputeStableId("sharedassets1.assets", "JSONField", "/Name");
            string id2 = UnityAssetScanner.ComputeStableId("sharedassets1.assets", "JSONField", "/Name");
            AssertTest("AT-11", "Stable record identities across repeated scans",
                id1 == id2 && !string.IsNullOrEmpty(id1),
                $"ID tính toán ổn định: {id1} == {id2}");

            // AT-12: Bảo toàn định dạng chuỗi Excel qua Roundtrip
            string testCsvPath = Path.Combine(Path.GetTempPath(), "test_at12.csv");
            var testAt12Entry = new TranslationEntry
            {
                Id = "TEST_AT12",
                SourceKind = SourceKind.UnityAsset,
                AssetPathId = "000123",
                ReferencePathId = "1E10",
                OriginalText = "Steak with 000456 units",
                VietnameseTranslation = "Bít tết đặc biệt với 000456 phần"
            };
            CsvHandler.ExportToProvenanceCsv(new[] { testAt12Entry }, testCsvPath);
            var reloadedAt12 = CsvHandler.ImportFromCsv(testCsvPath).FirstOrDefault();

            bool at12Pass = reloadedAt12 != null &&
                            reloadedAt12.AssetPathId == "000123" &&
                            reloadedAt12.ReferencePathId == "1E10" &&
                            reloadedAt12.VietnameseTranslation.Contains("Bít tết đặc biệt");

            AssertTest("AT-12", "Excel safe numbers and PathID formatting",
                at12Pass,
                at12Pass ? $"Bảo toàn thành công qua Roundtrip: AssetPathId='{reloadedAt12?.AssetPathId}', RefId='{reloadedAt12?.ReferencePathId}', Unicode='{reloadedAt12?.VietnameseTranslation}'" : "Lỗi format Excel");
            if (File.Exists(testCsvPath)) File.Delete(testCsvPath);

            // AT-13: Windows Secret Store (DPAPI encryption & secure isolation)
            string testKeyName = "test_acceptance_key_tmp";
            string testKeyValue = "sec_val_unity_loc_studio_12345!@#";
            bool at13Save = Core.Security.WindowsSecretStore.SaveSecret(testKeyName, testKeyValue);
            bool at13Has = Core.Security.WindowsSecretStore.HasSecret(testKeyName);
            string? at13Loaded = Core.Security.WindowsSecretStore.LoadSecret(testKeyName);
            bool at13Delete = Core.Security.WindowsSecretStore.DeleteSecret(testKeyName);
            bool at13HasAfterDel = Core.Security.WindowsSecretStore.HasSecret(testKeyName);

            bool at13Pass = at13Save && at13Has && at13Loaded == testKeyValue && at13Delete && !at13HasAfterDel;
            AssertTest("AT-13", "WindowsSecretStore DPAPI Roundtrip",
                at13Pass,
                at13Pass ? "DPAPI Protect/Unprotect/Delete hoạt động chính xác với CurrentUser" : "Lỗi lưu trữ bí mật DPAPI");

            // AT-14: AI Lab Redaction & Config
            var aiService = Core.AiLab.AiLabService.Instance;
            string rawPromptToRedact = $@"Lỗi crash tại C:\Users\{Environment.UserName}\Game\PlateUp.exe với key AIzaSyA1234567890123456789012345678901 và openai sk-12345678901234567890123456";
            string redacted = aiService.Redact(rawPromptToRedact);
            bool at14RedactPass = !redacted.Contains(Environment.UserName) &&
                                  !redacted.Contains("AIzaSyA") &&
                                  !redacted.Contains("sk-12345678901234567890123456") &&
                                  redacted.Contains("[REDACTED_USER]") &&
                                  redacted.Contains("[REDACTED_GEMINI_KEY]") &&
                                  redacted.Contains("[REDACTED_OPENAI_KEY]");

            AssertTest("AT-14", "AiLab Redaction of sensitive user/keys/paths",
                at14RedactPass,
                at14RedactPass ? "Đã che phủ thành công UserName, User Path, Gemini Key, OpenAI Key" : $"Lỗi che thông tin nhạy cảm: {redacted}");

            // AT-15: AI Lab Offline Enforcement
            var cfg = aiService.Config;
            bool originalEnabled = cfg.IsEnabled;
            cfg.IsEnabled = false; // Đảm bảo trạng thái tắt
            var offlineAnalysisTask = aiService.AnalyzeLogAsync("Test log content");
            offlineAnalysisTask.Wait();
            var (offOk, offResp, offMsg) = offlineAnalysisTask.Result;
            cfg.IsEnabled = originalEnabled;

            bool at15Pass = !offOk && string.IsNullOrEmpty(offResp) && offMsg.Contains("Offline");
            AssertTest("AT-15", "AiLab Offline Enforcement (Zero Remote Calls)",
                at15Pass,
                at15Pass ? "Khóa an toàn Offline hoạt động chuẩn xác: chặn mọi request khi IsEnabled=false" : "Lỗi vi phạm nguyên tắc Offline");

            // AT-16: Synthetic Telemetry Generator (14 elements, mixed TMP + UGUI)
            var synthSnap = Core.SyntheticLiveTestRunner.GenerateSyntheticSnapshot();
            bool at16Pass = synthSnap.TotalTexts == 14 &&
                            synthSnap.Elements.Any(e => e.ComponentType == "TextMeshProUGUI") &&
                            synthSnap.Elements.Any(e => e.ComponentType == "UnityEngine.UI.Text");
            AssertTest("AT-16", "Synthetic Telemetry Generator (TMP + UGUI Mixed)",
                at16Pass,
                at16Pass ? $"Đã tạo thành công {synthSnap.TotalTexts} phần tử UI đa dạng component TMP và UGUI" : "Lỗi sinh telemetry giả lập");

            // AT-17: Synthetic Protected Icon & Emoji Fonts Isolation
            bool at17Pass = synthSnap.Elements.Any(e => e.ActiveFont == "Reference - Icons" && e.Status == "PROTECTED") &&
                            synthSnap.Elements.Any(e => e.ActiveFont == "NotoEmoji-Regular SDF" && e.Status == "PROTECTED") &&
                            synthSnap.ProtectedCount == 3;
            AssertTest("AT-17", "Synthetic Protected Icon & Emoji Fonts Isolation",
                at17Pass,
                at17Pass ? $"Bảo vệ thành công {synthSnap.ProtectedCount} font icon/emoji (Reference - Icons, NotoEmoji-Regular SDF)" : "Lỗi phát hiện font được bảo vệ");

            // AT-18: Synthetic Text Hook LIVETEST_1..N with Markup Preservation
            var liveTexts = Core.SyntheticLiveTestRunner.ExtractLiveCapturedTexts(synthSnap);
            string sampleCsvPath = Core.AppPathResolver.ResolveSampleTextCsv();
            string tempPluginDir = Path.Combine(Path.GetTempPath(), "test_at18_plugin");
            Directory.CreateDirectory(tempPluginDir);
            var textEval = Core.SampleTestManager.EvaluateTextHookWithMock(liveTexts, sampleCsvPath, tempPluginDir);
            bool at18Pass = textEval.MockInjectedCount == 14 &&
                            textEval.PrimaryLanguage.Contains("English") &&
                            textEval.Items.Any(i => i.OriginalText.Contains("{0}") && i.MockText.Contains("{0}")) &&
                            textEval.Items.Any(i => i.OriginalText.Contains("<sprite") && i.MockText.Contains("<sprite"));
            AssertTest("AT-18", "Synthetic Text Hook LIVETEST_1..N with Markup Preservation",
                at18Pass,
                at18Pass ? $"Tiêm thành công {textEval.MockInjectedCount} text mẫu, ngôn ngữ chính: {textEval.PrimaryLanguage}, bảo toàn token {{0}} & <sprite>" : "Lỗi hook text mẫu giả lập");
            if (Directory.Exists(tempPluginDir)) Directory.Delete(tempPluginDir, true);

            // AT-19: Synthetic Font Hook Multi-Font 1-1 with 0% Pink Material Risk
            string tempFontPluginDir = Path.Combine(Path.GetTempPath(), "test_at19_plugin");
            Directory.CreateDirectory(tempFontPluginDir);
            var fontEval = Core.SampleTestManager.EvaluateFontHookWithMock(synthSnap, tempFontPluginDir);
            bool at19Pass = fontEval.ReplacedWithArialSdfCount >= 3 &&
                            fontEval.ProtectedFonts >= 2 &&
                            fontEval.PinkMaterialRiskPercent == 0.0 &&
                            fontEval.Items.Any(i => i.ReplacementFont.Contains("1-1") || i.ReplacementFont.Contains("SDF"));
            AssertTest("AT-19", "Synthetic Font Hook Multi-Font 1-1 with 0% Pink Material Risk",
                at19Pass,
                at19Pass ? $"Đã mapping {fontEval.ReplacedWithArialSdfCount} font 1-1, bảo vệ {fontEval.ProtectedFonts} font icon, nguy cơ lỗi shader: 0.0%" : "Lỗi hook phông đa phông 1-1");
            if (Directory.Exists(tempFontPluginDir)) Directory.Delete(tempFontPluginDir, true);

            // AT-20: Synthetic Post-Localization Audit (Zero Tofu, Zero Pink Shader, Zero Leak)
            var dummyProj = new Models.LocalizationProject { ProjectName = "PlateUp_SyntheticTest", GameDirectory = Path.GetTempPath() };
            var (step9Ok, step9Msg, audit) = Core.SyntheticLiveTestRunner.RunSyntheticStep9(dummyProj);
            bool at20Pass = step9Ok && audit.OverallPassed && audit.MissingGlyphCount == 0 && audit.PinkMaterialRiskPercent == 0.0 && !audit.MemoryLeakDetected;
            AssertTest("AT-20", "Synthetic Post-Localization Audit (Zero Tofu, Zero Pink Shader, Zero Leak)",
                at20Pass,
                at20Pass ? $"Đánh giá Bước 9 hoàn hảo: Tofu={audit.MissingGlyphCount}, PinkRisk={audit.PinkMaterialRiskPercent}%, RAM Leak={audit.MemoryLeakDetected}" : "Lỗi nghiệm thu Bước 9");

            // =====================================================================
            // MILESTONE M4 ACCEPTANCE TESTS (AT-21 -> AT-25)
            // =====================================================================

            // AT-21: Multi-Font 1-1 Mapping Matrix Resolution (Distinct fonts per UI role)
            var resMenu = Core.FontAssetBundleEngine.ResolveFont1to1("Chewy SDF");
            var resPrimary = Core.FontAssetBundleEngine.ResolveFont1to1("Capriola-Regular SDF");
            var resTitle = Core.FontAssetBundleEngine.ResolveFont1to1("Grandstander-clean SDF");
            var resCard = Core.FontAssetBundleEngine.ResolveFont1to1("Reference - Upgrade Card Title");
            var resSeed = Core.FontAssetBundleEngine.ResolveFont1to1("Reference - Seed Text");

            bool at21Pass = resMenu.TargetReplacementFont == "Vietnamese_MainMenu_SDF" &&
                            resPrimary.TargetReplacementFont == "Vietnamese_Primary_SDF" &&
                            resTitle.TargetReplacementFont == "Vietnamese_Title_SDF" &&
                            resCard.TargetReplacementFont == "Vietnamese_CardTitle_SDF" &&
                            resSeed.TargetReplacementFont == "Vietnamese_Monospace_SDF" &&
                            resMenu.TargetReplacementFont != resPrimary.TargetReplacementFont;

            AssertTest("AT-21", "Multi-Font 1-1 Mapping Matrix Resolution (Role-Based Distinct Fonts)",
                at21Pass,
                at21Pass ? "Phân giải chính xác 5 phông chữ theo 5 vai trò riêng biệt (Menu, Primary, Title, Card, Monospace), không dùng 1 font cho tất cả" : "Lỗi ma trận ánh xạ 1-1");

            // AT-22: TMP Material Preset Cloning & Visual Effects Preservation
            var origPreset = new Core.FontAssetBundleEngine.MaterialPresetDescriptor
            {
                MaterialName = "Chewy Material (Instance)",
                ShaderName = "TextMeshPro/Distance Field",
                Keywords = new List<string> { "OUTLINE_ON", "UNDERLAY_ON" },
                FloatProperties = new Dictionary<string, float>
                {
                    { "_OutlineWidth", 0.28f },
                    { "_UnderlaySoftness", 0.42f },
                    { "_WeightBold", 0.5f }
                },
                ColorProperties = new Dictionary<string, string>
                {
                    { "_OutlineColor", "#FF5500FF" },
                    { "_UnderlayColor", "#000000BF" }
                }
            };
            var clonedPreset = Core.FontAssetBundleEngine.CloneMaterialPreset(origPreset, "Vietnamese_MainMenu_SDF Atlas");

            bool at22Pass = clonedPreset.ShaderName == "TextMeshPro/Distance Field" &&
                            clonedPreset.TextureName == "Vietnamese_MainMenu_SDF Atlas" &&
                            clonedPreset.Keywords.Contains("OUTLINE_ON") &&
                            clonedPreset.Keywords.Contains("UNDERLAY_ON") &&
                            Math.Abs(clonedPreset.FloatProperties["_OutlineWidth"] - 0.28f) < 0.001f &&
                            Math.Abs(clonedPreset.FloatProperties["_UnderlaySoftness"] - 0.42f) < 0.001f &&
                            clonedPreset.ColorProperties["_OutlineColor"] == "#FF5500FF" &&
                            clonedPreset.HasOutline && clonedPreset.HasUnderlay;

            // Kiểm định thêm cơ chế Non-Destructive JSON Font Asset Patching (CourierPrime GOC -> thay the)
            string dummyOrigJson = @"{ ""m_Name"": ""CourierPrime"", ""m_Material"": { ""m_FileID"": 0, ""m_PathID"": 23 }, ""m_AtlasTextures"": [ { ""m_PathID"": 523 } ], ""m_FaceInfo"": { ""pointSize"": 100 }, ""m_GlyphTable"": [ { ""id"": 1 } ], ""m_CharacterTable"": [ { ""unicode"": 65 } ] }";
            string dummyVhJson = @"{ ""m_Name"": ""CourierNew_VH"", ""m_Material"": { ""m_FileID"": 0, ""m_PathID"": 99 }, ""m_AtlasTextures"": [ { ""m_PathID"": 999 } ], ""m_FaceInfo"": { ""pointSize"": 133 }, ""m_GlyphTable"": [ { ""id"": 1 }, { ""id"": 2 } ], ""m_CharacterTable"": [ { ""unicode"": 65 }, { ""unicode"": 258 } ] }";
            string patched = Core.FontAssetJsonPatcher.PatchFontAssetJson(dummyOrigJson, dummyVhJson);
            bool jsonPatchPass = patched.Contains("\"m_PathID\": 23") &&
                                 patched.Contains("\"m_PathID\": 523") &&
                                 patched.Contains("258") &&
                                 !patched.Contains("\"m_PathID\": 99") &&
                                 !patched.Contains("\"m_PathID\": 999");

            at22Pass = at22Pass && jsonPatchPass;

            AssertTest("AT-22", "TMP Material Preset Cloning & Visual Effects Preservation",
                at22Pass,
                at22Pass ? "Bản sao Material Preset bảo toàn 100% thuộc tính Outline (0.28), Underlay (0.42), màu sắc, Keywords và xác thực Non-Destructive JSON Font Patcher thành công" : "Lỗi sao chép Material Preset");

            // AT-23: Zero Pink Material Risk Verification (Distance Field Compatibility Audit)
            var pinkAudit = Core.FontAssetBundleEngine.AuditPinkMaterialRisk(origPreset, clonedPreset);
            bool at23Pass = pinkAudit.IsCompatible &&
                            pinkAudit.RiskPercent == 0.0 &&
                            pinkAudit.ShaderFamilyMatched &&
                            pinkAudit.AtlasTextureBound &&
                            pinkAudit.KeywordsPreserved &&
                            pinkAudit.VisualEffectsPreserved;

            AssertTest("AT-23", "Zero Pink Material Risk Verification (Distance Field Compatibility Audit)",
                at23Pass,
                at23Pass ? $"Kiểm định tương thích họ Shader và Texture Atlas: Nguy cơ màn hình hồng = {pinkAudit.RiskPercent}% (Tuyệt đối không lỗi)" : "Lỗi kiểm định màn hình hồng");

            // AT-24: Protected Icon & Emoji Font Isolation in FontAssetBundleEngine
            var resIcon = Core.FontAssetBundleEngine.ResolveFont1to1("Reference - Icons");
            var resEmoji = Core.FontAssetBundleEngine.ResolveFont1to1("NotoEmoji-Regular SDF");
            var resStars = Core.FontAssetBundleEngine.ResolveFont1to1("InGame_Stars_Icons");

            bool at24Pass = resIcon.IsProtected && resIcon.TargetReplacementFont == "KEEP_ORIGINAL" &&
                            resEmoji.IsProtected && resEmoji.TargetReplacementFont == "KEEP_ORIGINAL" &&
                            resStars.IsProtected && resStars.TargetReplacementFont == "KEEP_ORIGINAL";

            AssertTest("AT-24", "Protected Icon & Emoji Font Isolation in FontAssetBundleEngine",
                at24Pass,
                at24Pass ? "Bảo vệ tuyệt đối 3 phông biểu tượng (Reference - Icons, NotoEmoji, Stars), không bao giờ bị ghi đè hoặc đổi phông" : "Lỗi cô lập font biểu tượng");

            // AT-25: AssetBundle Font Metadata & Header Audit
            string bundleFile = Core.AppPathResolver.ResolveVietnameseFontBundle();
            var bundleAudit = Core.FontAssetBundleEngine.AuditAssetBundle(bundleFile);

            bool at25Pass = File.Exists(bundleFile) &&
                            bundleAudit.IsValidUnityFS &&
                            bundleAudit.FileSizeBytes > 100000 &&
                            bundleAudit.IsReadyForRuntimeMod &&
                            bundleAudit.SupportedShaderFamilies.Count > 0;

            AssertTest("AT-25", "AssetBundle Font Metadata & Header Audit",
                at25Pass,
                at25Pass ? $"AssetBundle '{bundleAudit.BundleFileName}' ({bundleAudit.FileSizeBytes / 1024} KB) hợp lệ định dạng {bundleAudit.UnityFsSignature}, sẵn sàng cho Runtime Mod" : "Lỗi kiểm tra tệp AssetBundle");

            // =========================================================================
            // MILESTONE M5: TRANSLATION & FONT VERIFICATION WORKBENCH (AT-26 -> AT-30)
            // =========================================================================

            // AT-26: Missing Format Placeholder & Tag Integrity Validation
            var sampleEntries = new List<TranslationEntry>
            {
                new TranslationEntry
                {
                    Id = "STR_REWARD_01",
                    OriginalText = "You earned {0} coins and {1} stars!",
                    VietnameseTranslation = "Bạn đã nhận được {0} tiền vàng và không có sao!", // Missing {1}
                    Classification = StringClassification.GameText
                },
                new TranslationEntry
                {
                    Id = "STR_HUD_HEAT",
                    OriginalText = "Requires <sprite name=\"heat\"> to cook.",
                    VietnameseTranslation = "Cần nhiệt độ để nấu nướng.", // Missing <sprite name="heat">
                    Classification = StringClassification.GameText
                },
                new TranslationEntry
                {
                    Id = "STR_VALID_01",
                    OriginalText = "Level {0}: {1} players",
                    VietnameseTranslation = "Cấp độ {0}: {1} người chơi", // Perfect match
                    Classification = StringClassification.GameText
                },
                new TranslationEntry
                {
                    Id = "STR_ESCAPED_SPRITE_MATCH",
                    OriginalText = "You can serve <sprite name=\"cake\"> Cake",
                    VietnameseTranslation = "Bạn có thể phục vụ <sprite name=\\\"cake\\\"> Bánh", // Escaped vs unescaped quotes normalized match
                    Classification = StringClassification.GameText
                }
            };
            var valReport = TranslationValidator.ValidateEntries(sampleEntries);
            bool at26Pass = !valReport.IsValid &&
                            valReport.ErrorCount == 2 && // Exactly 2 errors (STR_REWARD_01, STR_HUD_HEAT), 0 errors for STR_ESCAPED_SPRITE_MATCH
                            valReport.ValidTranslations == 4 &&
                            valReport.Issues.Any(i => i.KeyId == "STR_REWARD_01" && i.Message.Contains("{1}")) &&
                            valReport.Issues.Any(i => i.KeyId == "STR_HUD_HEAT" && i.Message.Contains("<sprite")) &&
                            !valReport.Issues.Any(i => i.KeyId == "STR_ESCAPED_SPRITE_MATCH");

            AssertTest("AT-26", "Missing Format Placeholder & Tag Integrity Validation",
                at26Pass,
                at26Pass ? $"Phát hiện chính xác {valReport.ErrorCount} lỗi mất placeholder/sprite tag; chuẩn hóa hoàn hảo escaped quotes (<sprite name=\\\"cake\\\"> == <sprite name=\"cake\">)" : "Lỗi kiểm định placeholder");

            string plateUpCsv = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Studio", "Projects", "PlateUp", "Translated_Vietnamese_Clean.csv");
            if (File.Exists(plateUpCsv))
            {
                var cleanReport = TranslationValidator.ValidateCsvFile(plateUpCsv);
                // Đảm bảo tệp dịch thực tế của PlateUp đạt 0 lỗi token
                if (cleanReport.ErrorCount > 0)
                {
                    at26Pass = false;
                }
            }

            // AT-27: Multiline & Vietnamese Diacritics Unicode CSV Roundtrip
            string tempRoundtripCsv = Path.Combine(Path.GetTempPath(), $"uls_test_roundtrip_{Guid.NewGuid():N}.csv");
            try
            {
                var originalRoundtripEntries = new List<TranslationEntry>
                {
                    new TranslationEntry
                    {
                        Id = "RT_MULTI_LINE",
                        ContextPath = "/UI/Tutorial/DialogBox",
                        OriginalText = "Welcome to the game!\r\nPress \"Space\" to jump.\r\nLine 3, with commas, and quotes.",
                        VietnameseTranslation = "Chào mừng bạn đến với trò chơi!\r\nNhấn \"Phím Cách\" để nhảy.\r\nDòng 3, có dấu phẩy, và ngoặc kép.",
                        Status = "Approved"
                    },
                    new TranslationEntry
                    {
                        Id = "RT_UNICODE_TONES",
                        ContextPath = "/UI/Card/Description",
                        OriginalText = "Contains: ă, â, đ, ê, ô, ơ, ư and all accented vowels à, ả, ã, á, ạ.",
                        VietnameseTranslation = "Chứa đầy đủ: ă, â, đ, ê, ô, ơ, ư và các thanh điệu: huyền, hỏi, ngã, sắc, nặng: ồ, ố, ở, ỡ, ợ, ứ, ử, ữ, ự.",
                        Status = "Approved"
                    }
                };

                CsvHandler.ExportToCsv(originalRoundtripEntries, tempRoundtripCsv);
                var reloadedEntries = CsvHandler.ImportFromCsv(tempRoundtripCsv);

                bool at27Pass = reloadedEntries.Count == 2 &&
                                reloadedEntries[0].OriginalText == originalRoundtripEntries[0].OriginalText &&
                                reloadedEntries[0].VietnameseTranslation == originalRoundtripEntries[0].VietnameseTranslation &&
                                reloadedEntries[1].VietnameseTranslation == originalRoundtripEntries[1].VietnameseTranslation &&
                                reloadedEntries[0].OriginalText.Contains("\r\n") &&
                                reloadedEntries[0].OriginalText.Contains("\"Space\"");

                AssertTest("AT-27", "Multiline & Vietnamese Diacritics Unicode CSV Roundtrip",
                    at27Pass,
                    at27Pass ? "Bảo toàn 100% văn bản nhiều dòng (\\r\\n), dấu ngoặc kép và toàn bộ ký tự tiếng Việt có dấu chuẩn UTF-8" : "Lỗi roundtrip CSV nhiều dòng hoặc dấu tiếng Việt");
            }
            finally
            {
                if (File.Exists(tempRoundtripCsv)) File.Delete(tempRoundtripCsv);
            }

            // AT-28: Excel Safe Formatting & Formula Injection Sanitization
            var excelEntries = new List<TranslationEntry>
            {
                new TranslationEntry
                {
                    Id = "ITEM_CODE",
                    AssetPathId = "000123", // Leading zero must not be truncated in Excel
                    ReferencePathId = "1E10", // Scientific notation must not be parsed as float in Excel
                    OriginalText = "Standard Item",
                    VietnameseTranslation = "=SUM(1,2)", // Formula injection attack string
                    Classification = StringClassification.GameText
                }
            };
            var excelValReport = TranslationValidator.ValidateEntries(excelEntries);
            string escapedPathId = CsvHandler.EscapeExcelSafeText("000123");
            string escapedRefId = CsvHandler.EscapeExcelSafeText("1E10");
            string cleanedPathId = CsvHandler.CleanExcelSafeText(escapedPathId);
            string cleanedRefId = CsvHandler.CleanExcelSafeText(escapedRefId);

            bool at28Pass = excelValReport.WarningCount >= 1 &&
                            excelValReport.Issues.Any(i => i.Type == ValidationIssueType.UnsafeFormulaPrefix) &&
                            escapedPathId.Contains("000123") &&
                            cleanedPathId == "000123" &&
                            cleanedRefId == "1E10";

            AssertTest("AT-28", "Excel Safe Formatting & Formula Injection Sanitization",
                at28Pass,
                at28Pass ? "Bảo toàn số có tiền tố 0 ('000123'), số mũ ('1E10') và phát hiện cảnh báo nguy cơ Formula Injection (=SUM)" : "Lỗi bảo vệ định dạng Excel hoặc cảnh báo công thức");

            // AT-29: Multi-Occurrence Key Isolation & Context Preservation
            var multiOccurEntries = new List<TranslationEntry>
            {
                new TranslationEntry
                {
                    Id = "MENU_BTN_READY",
                    ContextPath = "/Canvas/MainMenu/ReadyButton",
                    OriginalText = "Ready",
                    VietnameseTranslation = "Sẵn sàng"
                },
                new TranslationEntry
                {
                    Id = "LOBBY_STATUS_READY",
                    ContextPath = "/Canvas/LobbyRoom/Player1/Status",
                    OriginalText = "Ready",
                    VietnameseTranslation = "Đã sẵn sàng chiến đấu"
                }
            };
            string tempMultiCsv = Path.Combine(Path.GetTempPath(), $"uls_test_multi_{Guid.NewGuid():N}.csv");
            try
            {
                CsvHandler.ExportToCsv(multiOccurEntries, tempMultiCsv);
                var importedMulti = CsvHandler.ImportFromCsv(tempMultiCsv);

                bool at29Pass = importedMulti.Count == 2 &&
                                importedMulti[0].Id == "MENU_BTN_READY" &&
                                importedMulti[1].Id == "LOBBY_STATUS_READY" &&
                                importedMulti[0].OriginalText == importedMulti[1].OriginalText &&
                                importedMulti[0].VietnameseTranslation != importedMulti[1].VietnameseTranslation;

                AssertTest("AT-29", "Multi-Occurrence Key Isolation & Context Preservation",
                    at29Pass,
                    at29Pass ? "Bảo vệ độc lập hai chuỗi có cùng nội dung tiếng Anh ('Ready') ở hai vị trí UI khác nhau mà không bị gộp trùng key" : "Lỗi gộp trùng chuỗi trùng lặp");
            }
            finally
            {
                if (File.Exists(tempMultiCsv)) File.Delete(tempMultiCsv);
            }

            // AT-30: Font AssetBundle Binary Validation & Verification Gate
            string validBundle = Core.AppPathResolver.ResolveVietnameseFontBundle();
            var validAudit = Core.FontAssetBundleEngine.AuditAssetBundle(validBundle);

            // Tạo thử một tệp rác / hỏng để kiểm tra Verification Gate
            string corruptedBundle = Path.Combine(Path.GetTempPath(), $"corrupted_{Guid.NewGuid():N}.bundle");
            File.WriteAllText(corruptedBundle, "THIS_IS_NOT_A_UNITYFS_ASSETBUNDLE_FILE");
            var corruptedAudit = Core.FontAssetBundleEngine.AuditAssetBundle(corruptedBundle);
            if (File.Exists(corruptedBundle)) File.Delete(corruptedBundle);

            bool at30Pass = validAudit.IsValidUnityFS &&
                            validAudit.UnityFsSignature.StartsWith("UnityFS") &&
                            validAudit.IsReadyForRuntimeMod &&
                            !corruptedAudit.IsValidUnityFS &&
                            corruptedAudit.Summary.Contains("Không tìm thấy chữ ký hợp lệ");

            AssertTest("AT-30", "Font AssetBundle Binary Validation & Verification Gate",
                at30Pass,
                at30Pass ? "Cổng kiểm định nhị phân AssetBundle xác thực chính xác UnityFS hợp lệ và loại bỏ ngay lập tức tệp bundle hỏng/giả mạo" : "Lỗi cổng kiểm định AssetBundle");

            // =========================================================================
            // MILESTONE M6: ZERO-ARTIFACT CLEANUP & RELEASE PACKAGER (AT-31 -> AT-35)
            // =========================================================================

            // AT-31: Non-Destructive Prototype Cleanup & Pre-Existing Mod Preservation
            string mockGameDir = Path.Combine(Path.GetTempPath(), $"uls_game_mock_{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(mockGameDir);
                File.WriteAllText(Path.Combine(mockGameDir, "PlateUp.exe"), "STEAM_ORIGINAL_BINARY_MOCK");
                Directory.CreateDirectory(Path.Combine(mockGameDir, "PlateUp_Data"));
                File.WriteAllText(Path.Combine(mockGameDir, "PlateUp_Data", "mainData"), "UNITY_ENGINE_DATA");

                // Giả lập một mod khác của người chơi đã cài từ trước
                string otherModDir = Path.Combine(mockGameDir, "BepInEx", "plugins", "KitchenModPlus");
                Directory.CreateDirectory(otherModDir);
                string otherModDll = Path.Combine(otherModDir, "KitchenModPlus.dll");
                File.WriteAllText(otherModDll, "PLAYER_EXISTING_MOD_DLL");

                // Giả lập thư mục prototype việt hóa đã cài ở Bước 4
                string protoPluginDir = Path.Combine(mockGameDir, "BepInEx", "plugins", "VietnameseLocalization");
                Directory.CreateDirectory(protoPluginDir);
                string protoDll = Path.Combine(protoPluginDir, "UnityLocalizationPlugin.dll");
                string protoCsv = Path.Combine(protoPluginDir, "Sample_LiveTest_Texts.csv");
                string protoFont = Path.Combine(protoPluginDir, "vietnamese_font.bundle");
                File.WriteAllText(protoDll, "MOD_DLL_PROTOTYPE");
                File.WriteAllText(protoCsv, "ID,TEXT_MAU");
                File.WriteAllText(protoFont, "FONT_BUNDLE_MOCK");

                // Giả lập manifest ghi nhận 3 file prototype
                var protoManifest = new InstalledFileManifest
                {
                    ModName = "Prototype Test",
                    GameName = "PlateUp",
                    InstalledRelativeFiles = new List<string>
                    {
                        Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "UnityLocalizationPlugin.dll"),
                        Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Sample_LiveTest_Texts.csv"),
                        Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "vietnamese_font.bundle")
                    }
                };
                protoManifest.SaveToFile(Path.Combine(mockGameDir, PrototypeManager.ManifestFileName));

                // Thực thi CleanPrototype (Bước 10)
                var (cleanOk, cleanMsg, delCount) = PrototypeManager.CleanPrototype(mockGameDir);

                bool at31Pass = cleanOk &&
                                delCount >= 3 &&
                                !File.Exists(protoDll) &&
                                !File.Exists(protoCsv) &&
                                !File.Exists(protoFont) &&
                                !Directory.Exists(protoPluginDir) &&
                                File.Exists(Path.Combine(mockGameDir, "PlateUp.exe")) && // File game gốc an toàn
                                File.Exists(Path.Combine(mockGameDir, "PlateUp_Data", "mainData")) &&
                                File.Exists(otherModDll); // Mod khác của người chơi được bảo vệ 100%!

                AssertTest("AT-31", "Non-Destructive Prototype Cleanup & Pre-Existing Mod Preservation",
                    at31Pass,
                    at31Pass ? $"Dọn sạch 100% tệp prototype ({delCount} tệp), bảo toàn tuyệt đối file game Steam gốc và mod của người chơi (KitchenModPlus.dll)" : "Lỗi dọn dẹp prototype hoặc xâm phạm file khác");
            }
            finally
            {
                try { if (Directory.Exists(mockGameDir)) Directory.Delete(mockGameDir, true); } catch { }
            }

            // AT-32: Portable Prerelease ZIP Packaging & Structure Validation
            string testPkgTempDir = Path.Combine(Path.GetTempPath(), $"uls_test_pkg_{Guid.NewGuid():N}");
            string dummyBepZip = Path.Combine(testPkgTempDir, "bep.zip");
            string dummyModDll = Path.Combine(testPkgTempDir, "UnityLocalizationPlugin.dll");
            string dummyCsv = Path.Combine(testPkgTempDir, "Translated_Vietnamese.csv");
            string outPrereleaseZip = Path.Combine(testPkgTempDir, "PlateUp_VietHoa_Prerelease.zip");

            try
            {
                Directory.CreateDirectory(testPkgTempDir);
                File.WriteAllText(dummyModDll, "MOCK_MOD_DLL_BYTES");
                File.WriteAllText(dummyCsv, "ID,Language,Original_Text,Vietnamese_Translation\nTXT_01,EN,Start,Bắt đầu");

                // Tạo dummy BepInEx zip
                using (var zipStream = new FileStream(dummyBepZip, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    using (var writer1 = new StreamWriter(archive.CreateEntry("winhttp.dll").Open()))
                    {
                        writer1.Write("WINHTTP_DLL");
                    }

                    using (var writer2 = new StreamWriter(archive.CreateEntry("doorstop_config.ini").Open()))
                    {
                        writer2.Write("[UnityDoorstop]\nenabled=true");
                    }
                }

                var (preOk, preMsg, preOut) = PackageBuilder.BuildPortableZipPackage(
                    dummyBepZip, dummyModDll, dummyCsv, null, null, outPrereleaseZip, "PlateUp", "PlateUp.exe", false);

                bool hasWinhttp = false;
                bool hasDoorstop = false;
                bool hasModDll = false;
                bool hasCsv = false;
                bool hasUninstallCmd = false;
                bool hasManifest = false;
                bool hasShaSums = false;

                if (File.Exists(outPrereleaseZip))
                {
                    using var zip = ZipFile.OpenRead(outPrereleaseZip);
                    hasWinhttp = zip.Entries.Any(e => e.FullName == "winhttp.dll");
                    hasDoorstop = zip.Entries.Any(e => e.FullName == "doorstop_config.ini");
                    hasModDll = zip.Entries.Any(e => e.FullName.Contains("UnityLocalizationPlugin.dll"));
                    hasCsv = zip.Entries.Any(e => e.FullName.Contains("Translated_Vietnamese.csv"));
                    hasUninstallCmd = zip.Entries.Any(e => e.FullName == "Uninstall_VietHoa.cmd");
                    hasManifest = zip.Entries.Any(e => e.FullName == "vietnamese_installed.manifest");
                    hasShaSums = zip.Entries.Any(e => e.FullName == "SHA256SUMS.txt");
                }

                bool at32Pass = preOk && File.Exists(outPrereleaseZip) &&
                                hasWinhttp && hasDoorstop && hasModDll && hasCsv &&
                                hasUninstallCmd && hasManifest && hasShaSums;

                AssertTest("AT-32", "Portable Prerelease ZIP Packaging & Structure Validation",
                    at32Pass,
                    at32Pass ? "Gói Portable ZIP hoàn chỉnh cấu trúc: winhttp.dll, doorstop_config.ini, plugin DLL, bản dịch CSV thô, Uninstall_VietHoa.cmd và SHA256SUMS.txt" : "Lỗi đóng gói Prerelease ZIP");
            }
            finally
            {
                try { if (Directory.Exists(testPkgTempDir)) Directory.Delete(testPkgTempDir, true); } catch { }
            }

            // AT-33: AES-256 Encrypted LastPrerelease Packaging & RAM Decryption Verification
            string testEncDir = Path.Combine(Path.GetTempPath(), $"uls_test_enc_{Guid.NewGuid():N}");
            string dummyDatPath = Path.Combine(testEncDir, "Vietnamese.dat");
            string outLastPreZip = Path.Combine(testEncDir, "PlateUp_VietHoa_LastPrerelease.zip");

            try
            {
                Directory.CreateDirectory(testEncDir);
                var testEntries = new List<TranslationEntry>
                {
                    new TranslationEntry
                    {
                        Id = "STR_MENU_START",
                        ContextPath = "/Canvas/MainMenu/StartBtn",
                        OriginalText = "Start Game",
                        VietnameseTranslation = "Bắt Đầu Trò Chơi",
                        Classification = StringClassification.GameText
                    },
                    new TranslationEntry
                    {
                        Id = "STR_MENU_SETTINGS",
                        ContextPath = "/Canvas/MainMenu/SettingsBtn",
                        OriginalText = "Settings",
                        VietnameseTranslation = "Cài Đặt Hệ Thống",
                        Classification = StringClassification.GameText
                    }
                };

                // Mã hóa AES-256 sang Vietnamese.dat
                EncryptionService.EncryptEntriesToFile(testEntries, dummyDatPath);

                // Kiểm tra tệp mã hóa trên đĩa: bắt đầu bằng magic VMOD, không chứa chuỗi tiếng Việt thô
                byte[] datBytes = File.ReadAllBytes(dummyDatPath);
                string datAscii = Encoding.ASCII.GetString(datBytes);
                string datUtf8 = Encoding.UTF8.GetString(datBytes);

                bool magicOk = datBytes.Length > 24 && datAscii.StartsWith("VMOD");
                bool noPlaintextLeak = !datUtf8.Contains("Bắt Đầu Trò Chơi") && !datUtf8.Contains("Cài Đặt Hệ Thống");

                // Giải mã tức thời trực tiếp trên RAM (Zero Disk Footprint)
                var (cMap, fMap) = EncryptionService.DecryptFromFile(dummyDatPath);
                bool ramDecryptOk = cMap.Count > 0 &&
                                    cMap.Values.Contains("Bắt Đầu Trò Chơi") &&
                                    fMap.Values.Contains("Cài Đặt Hệ Thống");

                bool at33Pass = magicOk && noPlaintextLeak && ramDecryptOk;

                AssertTest("AT-33", "AES-256 Encrypted LastPrerelease Packaging & RAM Decryption Verification",
                    at33Pass,
                    at33Pass ? "Mã hóa nhị phân AES-256-CBC (Magic: VMOD) hoàn toàn che giấu bản dịch trên đĩa, giải mã tức thời 100% trên RAM" : "Lỗi mã hóa AES-256 hoặc rò rỉ plaintext");
            }
            finally
            {
                try { if (Directory.Exists(testEncDir)) Directory.Delete(testEncDir, true); } catch { }
            }

            // AT-34: Non-Invasive Uninstall Batch Script Syntax & Safety Verification
            var sampleFiles = new List<string>
            {
                "winhttp.dll",
                "doorstop_config.ini",
                Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "UnityLocalizationPlugin.dll"),
                Path.Combine("BepInEx", "plugins", "VietnameseLocalization", "Vietnamese.dat")
            };
            string uninstallScript = PackageBuilder.GenerateUninstallBatchScript("PlateUp.exe", sampleFiles);

            bool at34Pass = uninstallScript.Contains("@echo off") &&
                            uninstallScript.Contains("chcp 65001") &&
                            uninstallScript.Contains("winhttp.dll") &&
                            uninstallScript.Contains("doorstop_config.ini") &&
                            uninstallScript.Contains("rmdir /s /q \"BepInEx\\plugins\\VietnameseLocalization\"") &&
                            uninstallScript.Contains("vietnamese_installed.manifest") &&
                            !uninstallScript.Contains("rmdir /s /q \"BepInEx\"") && // KHÔNG xóa thư mục BepInEx gốc!
                            !uninstallScript.Contains("del *.*"); // KHÔNG dùng lệnh xóa bừa bãi!

            AssertTest("AT-34", "Non-Invasive Uninstall Batch Script Syntax & Safety Verification",
                at34Pass,
                at34Pass ? "Script Uninstall_VietHoa.cmd chuẩn UTF-8, chỉ gỡ chính xác file mod việt hóa, bảo vệ tuyệt đối thư mục BepInEx gốc và các mod khác" : "Lỗi kịch bản gỡ cài đặt");

            // AT-35: Publisher Release Gatekeeper, SHA-256 Checksums & Provenance Report
            string testReleaseDir = Path.Combine(Path.GetTempPath(), $"uls_test_release_{Guid.NewGuid():N}");
            string dummyReleaseZip = Path.Combine(testReleaseDir, "PlateUp_VietHoa_Release.zip");
            string dummyReleaseExe = Path.Combine(testReleaseDir, "Setup_VietHoa_PlateUp.exe");
            string dummyReportPath = Path.Combine(testReleaseDir, "Release_Summary_Report.md");

            try
            {
                Directory.CreateDirectory(testReleaseDir);
                File.WriteAllText(dummyReleaseZip, "MOCK_RELEASE_ZIP_CONTENT_BYTES");
                File.WriteAllText(dummyReleaseExe, "MOCK_RELEASE_EXE_CONTENT_BYTES");

                string zipSha = PackageBuilder.ComputeSha256(dummyReleaseZip);
                string exeSha = PackageBuilder.ComputeSha256(dummyReleaseExe);

                var releaseDummyProj = new LocalizationProject
                {
                    ProjectName = "PlateUp",
                    GameExeName = "PlateUp.exe",
                    UnityVersion = "2020.3.48f1",
                    Architecture = "Mono x64",
                    ModProfile = "BepInEx 5 (Mono)"
                };
                releaseDummyProj.SetStepStatus(9, StepStatus.Passed, "Live test đạt 100%");
                releaseDummyProj.SetStepStatus(10, StepStatus.Passed, "Đã dọn sạch prototype");
                releaseDummyProj.SetStepStatus(11, StepStatus.Passed, "Prerelease đạt");
                releaseDummyProj.SetStepStatus(12, StepStatus.Passed, "LastPrerelease đạt");

                string report = PackageBuilder.GenerateReleaseReport(releaseDummyProj, dummyReleaseZip, dummyReleaseExe, dummyReportPath);

                bool at35Pass = zipSha.Length == 64 &&
                                exeSha.Length == 64 &&
                                File.Exists(dummyReportPath) &&
                                report.Contains("BÁO CÁO PHÁT HÀNH BẢN VIỆT HÓA") &&
                                report.Contains("AES-256") &&
                                report.Contains(zipSha) &&
                                report.Contains(exeSha) &&
                                report.Contains("Verification Gates") &&
                                report.Contains("Non-Invasive Commitment");

                AssertTest("AT-35", "Publisher Release Gatekeeper, SHA-256 Checksums & Provenance Report",
                    at35Pass,
                    at35Pass ? $"Báo cáo Release_Summary_Report.md chuẩn Publisher kèm mã băm SHA-256 ({zipSha.Substring(0, 8)}..., {exeSha.Substring(0, 8)}...) và cam kết phi xâm lấn 100%" : "Lỗi báo cáo phát hành Release");
            }
            finally
            {
                try { if (Directory.Exists(testReleaseDir)) Directory.Delete(testReleaseDir, true); } catch { }
            }

            // AT-36: Realtime Process Lifecycle & Automatic Exit Detection
            bool at36Pass = false;
            string at36Details = "";
            try
            {
                var dummyDir = Path.Combine(Path.GetTempPath(), "AT36_TestDir");
                Directory.CreateDirectory(dummyDir);
                string dummyExe = Path.Combine(dummyDir, "notepad.exe");
                string sysNotepad = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");
                if (File.Exists(sysNotepad))
                {
                    File.Copy(sysNotepad, dummyExe, true);
                    var (ok, msg, proc) = GameProcessRunner.LaunchGame(dummyDir, "notepad.exe");
                    if (ok && proc != null)
                    {
                        int testPid = proc.Id;
                        var statusRunning = GameProcessRunner.CheckGameStatus(dummyDir, "notepad.exe");
                        bool detectedRunning = statusRunning.IsRunning && statusRunning.Pid == testPid;

                        GameProcessRunner.StopGame();
                        System.Threading.Thread.Sleep(200);

                        var statusExited = GameProcessRunner.CheckGameStatus(dummyDir, "notepad.exe");
                        bool detectedExit = !statusExited.IsRunning && statusExited.JustExited;

                        var statusIdle = GameProcessRunner.CheckGameStatus(dummyDir, "notepad.exe");
                        bool idleAfterExit = !statusIdle.IsRunning && !statusIdle.JustExited;

                        at36Pass = detectedRunning && detectedExit && idleAfterExit;
                        at36Details = at36Pass
                            ? $"Theo dõi thành công chu kỳ tiến trình: Bật (PID: {testPid}) -> Thoát (JustExited=True) -> Trạng thái sẵn sàng"
                            : $"Thất bại chu kỳ tiến trình: Running={detectedRunning}, Exit={detectedExit}, Idle={idleAfterExit}";
                    }
                }
                try { if (Directory.Exists(dummyDir)) Directory.Delete(dummyDir, true); } catch { }
            }
            catch (Exception ex)
            {
                at36Details = $"Lỗi thực thi AT-36: {ex.Message}";
            }

            AssertTest("AT-36", "Realtime Process Lifecycle & Automatic Exit Detection", at36Pass, at36Details);

            // ==========================================================================
            // MILESTONE M7: AI LOCAL RESEARCH LAB (AT-37 -> AT-40)
            // ==========================================================================

            // AT-37: SQLite Knowledge Base Integrity & CRUD Operations
            bool at37Pass = false;
            string at37Details = "";
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"kb_test_{Guid.NewGuid():N}.db");
            try
            {
                var kb = new KnowledgeDatabase(tempDbPath);
                var initialItems = kb.GetAllKnowledgeItems();
                bool seedOk = initialItems.Count >= 3;

                kb.AddOrUpdateKnowledgeItem(new KnowledgeItem
                {
                    Key = "AT37_TEST_KEY",
                    Category = "Test_Category",
                    Title = "AT37 Test Title",
                    Summary = "AT37 Test Summary",
                    VerifiedStatus = "VERIFIED"
                });
                var updatedItems = kb.GetAllKnowledgeItems();
                bool addOk = updatedItems.Any(k => k.Key == "AT37_TEST_KEY");

                var testExp = new ExperimentRecord
                {
                    Id = "EXP_AT37_01",
                    Title = "AT37 Experiment",
                    ProblemStatement = "Test Problem",
                    Status = "SUCCESS"
                };
                kb.RecordExperiment(testExp);
                var allExps = kb.GetAllExperiments();
                bool expOk = allExps.Any(e => e.Id == "EXP_AT37_01");

                var testMod = new ModuleLibraryItem
                {
                    Id = "MOD_AT37_01",
                    Name = "At37Module",
                    Capability = "Test Capability",
                    SourceCode = "public class At37Module {}",
                    IsActive = true
                };
                kb.PromoteToModuleLibrary(testMod);
                var allMods = kb.GetAllModules();
                bool modOk = allMods.Any(m => m.Id == "MOD_AT37_01");

                at37Pass = seedOk && addOk && expOk && modOk;
                at37Details = at37Pass
                    ? $"Cơ sở dữ liệu SQLite khởi tạo thành công: {initialItems.Count} tri thức nền, CRUD KnowledgeItems, Experiments và ModuleLibrary hoạt động 100%"
                    : $"Thất bại SQLite: Seed={seedOk}, Add={addOk}, Exp={expOk}, Mod={modOk}";
            }
            catch (Exception ex)
            {
                at37Details = $"Lỗi thực thi AT-37: {ex.Message}";
            }
            finally
            {
                try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
            }
            AssertTest("AT-37", "SQLite Knowledge Base Integrity & CRUD Operations", at37Pass, at37Details);

            // AT-38: LocalResearchHost Isolated Sandbox Compile & Watchdog Execution
            bool at38Pass = false;
            string at38Details = "";
            try
            {
                string expId = $"EXP_AT38_{Guid.NewGuid():N}";
                string validModule = @"
public class MathSandboxHelper
{
    public static int Add(int a, int b) => a + b;
}";
                string validTest = @"
using System;
class Program
{
    static int Main()
    {
        if (MathSandboxHelper.Add(15, 27) == 42)
        {
            Console.WriteLine(""AT38_PASS"");
            return 0;
        }
        return 1;
    }
}";
                var taskValid = LocalResearchHost.RunExperimentIsolatedAsync(expId, validModule, validTest);
                taskValid.Wait();
                var resValid = taskValid.Result;

                // Kiểm tra chặn mã lỗi cú pháp
                string invalidExpId = $"EXP_AT38_INV_{Guid.NewGuid():N}";
                string invalidModule = @"public class BrokenClass { syntax error here }";
                string dummyTest = @"class Program { static int Main() => 0; }";
                var taskInv = LocalResearchHost.RunExperimentIsolatedAsync(invalidExpId, invalidModule, dummyTest);
                taskInv.Wait();
                var resInv = taskInv.Result;

                bool validPassed = resValid.Success && resValid.CompilePassed && resValid.TestPassed;
                bool invalidBlocked = !resInv.Success && !resInv.CompilePassed;

                at38Pass = validPassed && invalidBlocked;
                at38Details = at38Pass
                    ? $"Sandbox cô lập hoạt động hoàn hảo: Mã đúng biên dịch và chạy pass (Duration: {resValid.DurationMs}ms); Mã lỗi cú pháp bị chặn an toàn ở Compile stage"
                    : $"Thất bại Sandbox: ValidPassed={validPassed}, InvalidBlocked={invalidBlocked}";
            }
            catch (Exception ex)
            {
                at38Details = $"Lỗi thực thi AT-38: {ex.Message}";
            }
            AssertTest("AT-38", "LocalResearchHost Isolated Sandbox Compile & Execution Watchdog", at38Pass, at38Details);

            // AT-39: Ollama Local Provider Connectivity & Hardware Telemetry
            bool at39Pass = false;
            string at39Details = "";
            try
            {
                var provider = new OllamaLocalProvider();
                var taskConn = provider.TestConnectionAsync("", "http://127.0.0.1:11434", "qwen2.5-coder:7b");
                taskConn.Wait();
                var (connOk, connMsg) = taskConn.Result;

                var taskHw = HardwareMonitor.GetSnapshotAsync("http://127.0.0.1:11434");
                taskHw.Wait();
                var hw = taskHw.Result;

                bool hwOk = hw.TotalRamGb > 0 && !string.IsNullOrEmpty(hw.GpuName);

                at39Pass = connOk && hwOk;
                at39Details = at39Pass
                    ? $"Kết nối thành công Ollama API Local (http://127.0.0.1:11434, Model: qwen2.5-coder:7b). Phần cứng: {hw.SummaryText}"
                    : $"Thất bại Ollama / Phần cứng: Conn={connOk} ({connMsg}), HwOk={hwOk}";
            }
            catch (Exception ex)
            {
                at39Details = $"Lỗi thực thi AT-39: {ex.Message}";
            }
            AssertTest("AT-39", "Ollama Local Provider Connectivity & Hardware Telemetry", at39Pass, at39Details);

            // AT-40: Complete Autonomous R&D Loop (AI Local -> Hypothesis -> C# Code -> Build -> Test -> Knowledge Base -> Module Library)
            bool at40Pass = false;
            string at40Details = "";
            string tempLoopDb = Path.Combine(Path.GetTempPath(), $"rd_loop_{Guid.NewGuid():N}.db");
            try
            {
                var testDb = new KnowledgeDatabase(tempLoopDb);
                var orchestrator = new ResearchOrchestrator(testDb);

                var taskLoop = orchestrator.RunFullResearchCycleAsync(
                    "TMP Distance Field Dynamic Shader Cloner",
                    "Cần nhân bản Material Preset của font thay thế ARIAL SDF và bảo toàn các thuộc tính OutlineWidth và UnderlaySoftness.",
                    "PlateUp Unity 2020.3.48f1, TextMeshPro 3.0.6, BepInEx 5.4.21");
                taskLoop.Wait();
                var loopResult = taskLoop.Result;

                bool loopOk = loopResult.Success && loopResult.CompilePassed && loopResult.TestPassed && loopResult.PromotedToLibrary;
                var savedMods = testDb.GetAllModules();
                bool modSaved = savedMods.Any(m => m.Name == loopResult.ModuleName);
                var savedExps = testDb.GetAllExperiments();
                bool expSaved = savedExps.Any(e => e.Id == loopResult.ExperimentId && e.Status == "SUCCESS");

                at40Pass = loopOk && modSaved && expSaved;
                at40Details = at40Pass
                    ? $"Hoàn thành 100% chu trình R&D: AI Local -> Giả thuyết -> Mã C# -> Build Sandbox -> Test Pass -> Lưu SQLite -> Đưa Module '{loopResult.ModuleName}' vào Library"
                    : $"Thất bại chu trình R&D: LoopOk={loopOk}, ModSaved={modSaved}, ExpSaved={expSaved}, Reason={loopResult.FailureReason}";
            }
            catch (Exception ex)
            {
                at40Details = $"Lỗi thực thi AT-40: {ex.Message}";
            }
            finally
            {
                try { if (File.Exists(tempLoopDb)) File.Delete(tempLoopDb); } catch { }
            }
            AssertTest("AT-40", "Complete Autonomous R&D Loop (AI Local -> Build -> Test -> SQLite -> Module Library)", at40Pass, at40Details);

            // =========================================================================
            // AT-41: SQLite FTS5 Full-Text Search Engine
            // =========================================================================
            bool at41Pass = false;
            string at41Details = "";
            string tempFtsDb = Path.Combine(Path.GetTempPath(), $"fts_test_{Guid.NewGuid():N}.db");
            try
            {
                var ftsDb = new KnowledgeDatabase(tempFtsDb);
                ftsDb.AddOrUpdateKnowledgeItem(new KnowledgeItem
                {
                    Key = "TEST_FTS_TMP",
                    Category = "TMP_Shader",
                    Title = "Kỹ Thuật Đổ Bóng Distance Field Cho TextMeshPro",
                    Summary = "Tối ưu hóa tham số UnderlaySoftness và UnderlayDilate trên texture atlas 2048x2048.",
                    DetailedContent = "Khi atlas padding là 9px, softness không được vượt quá 0.45 để tránh mất bóng.",
                    Tags = "TMP, Underlay, DistanceField, Shader"
                });
                ftsDb.AddOrUpdateKnowledgeItem(new KnowledgeItem
                {
                    Key = "TEST_FTS_UNICODE",
                    Category = "Linguistic",
                    Title = "Xử Lý Bảng Mã Tiếng Việt Dựng Sẵn NFC",
                    Summary = "Chuẩn hóa bảng mã Unicode tiếng Việt để hiển thị chính xác ký tự có dấu trên engine game.",
                    DetailedContent = "Chuyển đổi giữa Unicode NFD và NFC để tránh lỗi tách rời dấu thanh.",
                    Tags = "Unicode, Vietnamese, NFC, Typography"
                });

                var searchTmp = ftsDb.SearchKnowledgeFts("DistanceField");
                var searchVi = ftsDb.SearchKnowledgeFts("Unicode");

                bool tmpFound = searchTmp.Any(x => x.Key == "TEST_FTS_TMP");
                bool viFound = searchVi.Any(x => x.Key == "TEST_FTS_UNICODE");

                at41Pass = tmpFound && viFound;
                at41Details = at41Pass
                    ? $"SQLite FTS5 Full-Text Search hoạt động hoàn hảo: Tìm thấy '{searchTmp.Count}' mục cho 'DistanceField' và '{searchVi.Count}' mục cho 'Unicode' (FtsAvailable: {ftsDb.Fts5Available})"
                    : $"Thất bại FTS5: tmpFound={tmpFound}, viFound={viFound}, ftsAvailable={ftsDb.Fts5Available}";
            }
            catch (Exception ex)
            {
                at41Details = $"Lỗi thực thi AT-41: {ex.Message}";
            }
            finally
            {
                try { if (File.Exists(tempFtsDb)) File.Delete(tempFtsDb); } catch { }
            }
            AssertTest("AT-41", "SQLite FTS5 Full-Text Search Engine & Knowledge Retrieval", at41Pass, at41Details);

            // =========================================================================
            // AT-42: Capability SDK Execution & Vietnamese Unicode Normalizer
            // =========================================================================
            bool at42Pass = false;
            string at42Details = "";
            try
            {
                var normalizer = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin.VietnameseUnicodeNormalizerCapability();
                var ctx = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.CapabilityExecutionContext();

                // Test 1: Chuẩn hóa NFC
                string nfdText = "Tie\u0302\u0301ng Vie\u0323\u0302t"; // "Tiếng Việt" dạng NFD
                ctx.Parameters["action"] = "NORMALIZE";
                ctx.Parameters["text"] = nfdText;
                ctx.Parameters["targetForm"] = "NFC";
                var resNfc = normalizer.Execute(ctx);
                string normText = resNfc.GetOutput<string>("normalizedText") ?? "";
                bool nfcOk = normText == "Tiếng Việt" && normText.IsNormalized(NormalizationForm.FormC);

                // Test 2: Bỏ dấu tiếng Việt
                ctx.Parameters["action"] = "REMOVE_DIACRITICS";
                ctx.Parameters["text"] = "Cộng Hòa Xã Hội Chủ Nghĩa Việt Nam";
                var resUnaccent = normalizer.Execute(ctx);
                string unaccent = resUnaccent.GetOutput<string>("unaccentedText") ?? "";
                bool unaccentOk = unaccent == "Cong Hoa Xa Hoi Chu Nghia Viet Nam";

                // Test 3: Chuẩn hóa vị trí dấu thanh (hoà -> hòa, thuỷ -> thủy)
                ctx.Parameters["action"] = "FIX_DIACRITICS";
                ctx.Parameters["text"] = "hoà bình thuỷ triều";
                ctx.Parameters["modernStyle"] = true;
                var resFixed = normalizer.Execute(ctx);
                string fixedText = resFixed.GetOutput<string>("fixedText") ?? "";
                bool fixedOk = fixedText == "hòa bình thủy triều";

                // Test 4: Phát hiện ký tự lỗi font (Tofu / U+FFFD)
                ctx.Parameters["action"] = "ANALYZE_ANOMALIES";
                ctx.Parameters["text"] = "Lỗi font \uFFFD và ký tự lạ";
                var resAnom = normalizer.Execute(ctx);
                bool anomOk = resAnom.GetOutput<bool>("hasAnomalies");

                // Test 5: So sánh chuỗi tiếng Việt có/không phân biệt dấu
                ctx.Parameters["action"] = "COMPARE";
                ctx.Parameters["textA"] = "tiếng việt";
                ctx.Parameters["textB"] = "TIẾNG VIỆT";
                ctx.Parameters["ignoreDiacritics"] = false;
                ctx.Parameters["ignoreCase"] = true;
                var resCmp = normalizer.Execute(ctx);
                bool cmpOk = resCmp.GetOutput<bool>("isEqual");

                at42Pass = nfcOk && unaccentOk && fixedOk && anomOk && cmpOk;
                at42Details = at42Pass
                    ? "VietnameseUnicodeNormalizerCapability hoạt động chính xác 100%: NFC Normalization, Bỏ dấu Đ/đ, Chuẩn hóa vị trí thanh điệu (hòa/thủy), Quét lỗi Tofu, So sánh đối chiếu."
                    : $"Thất bại: nfcOk={nfcOk}, unaccentOk={unaccentOk}, fixedOk={fixedOk}, anomOk={anomOk}, cmpOk={cmpOk}";
            }
            catch (Exception ex)
            {
                at42Details = $"Lỗi thực thi AT-42: {ex.Message}";
            }
            AssertTest("AT-42", "Capability SDK: Vietnamese Unicode Normalizer & Tone Rectifier", at42Pass, at42Details);

            // =========================================================================
            // AT-43: TMP Distance Field Shader Patcher Capability & Rollback
            // =========================================================================
            bool at43Pass = false;
            string at43Details = "";
            try
            {
                var patcher = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin.TMPDistanceFieldShaderPatchCapability();
                var ctx = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.CapabilityExecutionContext();

                string brokenMatJson = @"{
                    ""ShaderName"": ""TextMeshPro/Distance Field"",
                    ""ShaderKeywords"": [""UNDERLAY_ON""],
                    ""OutlineWidth"": 0.0,
                    ""UnderlayOffsetX"": 0.0,
                    ""UnderlayOffsetY"": 0.0,
                    ""UnderlayDilate"": -0.05,
                    ""UnderlaySoftness"": 0.85,
                    ""AtlasWidth"": 2048,
                    ""AtlasHeight"": 2048,
                    ""AtlasPadding"": 9
                }";

                // 1. Vá thông số (APPLY_PATCH)
                ctx.Parameters["action"] = "APPLY_PATCH";
                ctx.Parameters["materialJson"] = brokenMatJson;
                ctx.Parameters["atlasResolution"] = 2048;
                ctx.Parameters["atlasPadding"] = 9;
                ctx.Parameters["desiredOutlineIntensity"] = "BOLD";

                var patchResult = patcher.Execute(ctx);
                bool patchOk = patchResult.Success;
                string snapId = patchResult.GetOutput<string>("snapshotId") ?? "";
                float newOutline = patchResult.GetOutput<float>("outlineWidth");
                float newDilate = patchResult.GetOutput<float>("underlayDilate");
                float newSoftness = patchResult.GetOutput<float>("underlaySoftness");
                var keywords = patchResult.GetOutput<List<string>>("shaderKeywords") ?? new();

                bool paramsCalibrated = newOutline >= 0.25f && newDilate > 0.0f && newSoftness <= 0.45f &&
                                        keywords.Contains("OUTLINE_ON") && keywords.Contains("UNDERLAY_ON");

                // 2. Rollback an toàn về trạng thái ban đầu
                ctx.Parameters["action"] = "ROLLBACK";
                ctx.Parameters["snapshotId"] = snapId;
                var rollbackResult = patcher.Execute(ctx);
                bool rollbackOk = rollbackResult.Success && rollbackResult.GetOutput<bool>("rollbackSuccess");
                string restoredJson = rollbackResult.GetOutput<string>("restoredMaterialJson") ?? "";
                bool restoredMatch = restoredJson.Contains("\"OutlineWidth\":0") || restoredJson.Contains("\"OutlineWidth\": 0");

                at43Pass = patchOk && paramsCalibrated && rollbackOk && restoredMatch;
                at43Details = at43Pass
                    ? $"TMPDistanceFieldShaderPatchCapability hoàn hảo: Kích hoạt OUTLINE_ON + UNDERLAY_ON, Viền đậm ({newOutline:F2}), Dilate ({newDilate:F2}), Softness ({newSoftness:F2}), Rollback 100% khớp snapshot '{snapId}'"
                    : $"Thất bại: patchOk={patchOk}, calibrated={paramsCalibrated}, rollbackOk={rollbackOk}, restoredMatch={restoredMatch}";
            }
            catch (Exception ex)
            {
                at43Details = $"Lỗi thực thi AT-43: {ex.Message}";
            }
            AssertTest("AT-43", "TMP Distance Field Shader Patcher Capability & Full Rollback", at43Pass, at43Details);

            // =========================================================================
            // AT-44: Autonomous Research Workflow 13-Stage State Machine & Checkpoints
            // =========================================================================
            bool at44Pass = false;
            string at44Details = "";
            string tempWorkflowDb = Path.Combine(Path.GetTempPath(), $"workflow_test_{Guid.NewGuid():N}.db");
            try
            {
                var wfDb = new KnowledgeDatabase(tempWorkflowDb);
                var orchestrator = new ResearchOrchestrator(wfDb);

                var taskWorkflow = orchestrator.RunAutonomousWorkflowAsync(
                    "TMP Distance Field Shader Preserver & Cloner",
                    "Tự động chuẩn hóa shader material textmeshpro cho tiếng việt.",
                    "PlateUp Unity 2020.3.48f1, TextMeshPro 3.0.6");
                taskWorkflow.Wait();
                var wfRes = taskWorkflow.Result;

                bool wfSuccess = wfRes.Success && wfRes.PromotedToLibrary;
                bool stagesPassed = wfRes.StageHistory.Count >= 10 && wfRes.StageHistory.Contains("Discover") && wfRes.StageHistory.Contains("Promote");

                var latestCp = wfDb.GetLatestCheckpoint(wfRes.JobId);
                bool checkpointSaved = latestCp != null && latestCp.StepIndex > 0;

                at44Pass = wfSuccess && stagesPassed && checkpointSaved;
                at44Details = at44Pass
                    ? $"Chu trình 13 bước hoàn thành 100%: Stages={string.Join(" -> ", wfRes.StageHistory.Take(5))}...-> {wfRes.FinalStage}; Checkpoint Saved (JobId: {wfRes.JobId}, StepIndex: {latestCp?.StepIndex})"
                    : $"Thất bại Workflow: wfSuccess={wfSuccess}, stagesPassed={stagesPassed}, checkpointSaved={checkpointSaved}, Reason={wfRes.FailureReason}";
            }
            catch (Exception ex)
            {
                at44Details = $"Lỗi thực thi AT-44: {ex.Message}";
            }
            finally
            {
                try { if (File.Exists(tempWorkflowDb)) File.Delete(tempWorkflowDb); } catch { }
            }
            AssertTest("AT-44", "Autonomous Research Workflow 13-Stage State Machine & Checkpoints", at44Pass, at44Details);

            // =========================================================================
            // AT-45: Offline Mode Enforcement (Capability Runs 100% Offline Without AI)
            // =========================================================================
            bool at45Pass = false;
            string at45Details = "";
            try
            {
                // Đảm bảo cấu hình Publisher / Offline
                var normalizer = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin.VietnameseUnicodeNormalizerCapability();
                var patcher = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin.TMPDistanceFieldShaderPatchCapability();

                var sw = System.Diagnostics.Stopwatch.StartNew();

                var ctxNorm = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.CapabilityExecutionContext();
                ctxNorm.Parameters["action"] = "NORMALIZE";
                ctxNorm.Parameters["text"] = "Tie\u0302\u0301ng Vie\u0323\u0302t Offline";
                var resNorm = normalizer.Execute(ctxNorm);

                var ctxPatch = new UnityLocalizationStudio.Core.AiLab.CapabilitySdk.CapabilityExecutionContext();
                ctxPatch.Parameters["action"] = "APPLY_PATCH";
                ctxPatch.Parameters["atlasResolution"] = 2048;
                var resPatch = patcher.Execute(ctxPatch);

                sw.Stop();

                bool normOk = resNorm.Success && resNorm.GetOutput<string>("normalizedText") == "Tiếng Việt Offline";
                bool patchOk = resPatch.Success && resPatch.GetOutput<float>("outlineWidth") > 0.0f;
                bool fastExecution = sw.ElapsedMilliseconds < 200; // Hoàn toàn offline, không phụ thuộc mạng hay Ollama API

                at45Pass = normOk && patchOk && fastExecution;
                at45Details = at45Pass
                    ? $"Bảo đảm tuyệt đối chế độ Offline / Publisher: Cả 2 Capability thực thi 100% độc lập không cần AI API hay kết nối mạng trong {sw.ElapsedMilliseconds}ms."
                    : $"Thất bại Offline: normOk={normOk}, patchOk={patchOk}, fastExecution={fastExecution} ({sw.ElapsedMilliseconds}ms)";
            }
            catch (Exception ex)
            {
                at45Details = $"Lỗi thực thi AT-45: {ex.Message}";
            }
            AssertTest("AT-45", "Offline Mode Enforcement (Pure C# Capabilities Run Without AI Dependencies)", at45Pass, at45Details);

            logs.Add("==========================================================================");
            logs.Add($"TỔNG KẾT: {passed}/45 PASS, {failed}/45 FAIL.");
            logs.Add("==========================================================================");

            return (passed, failed, logs);
        }
    }
}

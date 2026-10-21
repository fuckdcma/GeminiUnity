using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin
{
    public class TMPMaterialProperties
    {
        public string ShaderName { get; set; } = "TextMeshPro/Distance Field";
        public List<string> ShaderKeywords { get; set; } = new();
        public float OutlineWidth { get; set; } = 0.0f;
        public string OutlineColor { get; set; } = "#050505FF";
        public float UnderlayOffsetX { get; set; } = 0.0f;
        public float UnderlayOffsetY { get; set; } = 0.0f;
        public float UnderlayDilate { get; set; } = 0.0f;
        public float UnderlaySoftness { get; set; } = 0.0f;
        public string UnderlayColor { get; set; } = "#050303D9";
        public int AtlasWidth { get; set; } = 2048;
        public int AtlasHeight { get; set; } = 2048;
        public int AtlasPadding { get; set; } = 9;
    }

    public class MaterialPatchSnapshot
    {
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString("N");
        public string OriginalJson { get; set; } = string.Empty;
        public string PatchedJson { get; set; } = string.Empty;
        public string OriginalChecksum { get; set; } = string.Empty;
        public string PatchedChecksum { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    public class TMPDistanceFieldShaderPatchCapability : ICapability
    {
        public string Id => "CAP_TMP_DISTANCE_FIELD_SHADER_PATCH";
        public string Name => "TextMeshPro Distance Field Shader & Material Patcher";
        public string SemanticVersion => "1.1.0";

        private readonly CapabilityManifest _manifest;
        private readonly Dictionary<string, MaterialPatchSnapshot> _snapshotRegistry = new();

        public CapabilityManifest Manifest => _manifest;

        public TMPDistanceFieldShaderPatchCapability()
        {
            _manifest = new CapabilityManifest
            {
                Id = Id,
                Name = Name,
                SemanticVersion = SemanticVersion,
                Description = "Tự động phân tích, tính toán hiệu ứng viền đen & bóng đổ và vá thông số TextMeshPro Distance Field Material tương thích độ phân giải Atlas với hỗ trợ Rollback 100%.",
                Author = "Antigravity AI Lab",
                Category = "TMP_Shader_Engine",
                InputContractJson = @"{
                    ""action"": ""ANALYZE_MATERIAL|CALCULATE_PATCH|APPLY_PATCH|ROLLBACK|ALL"",
                    ""materialJson"": ""string"",
                    ""atlasResolution"": 2048,
                    ""atlasPadding"": 9,
                    ""desiredOutlineIntensity"": ""BOLD|MEDIUM|SUBTLE"",
                    ""snapshotId"": ""string""
                }",
                OutputContractJson = @"{
                    ""patchedMaterialJson"": ""string"",
                    ""snapshotId"": ""string"",
                    ""checksum"": ""string"",
                    ""recommendations"": [""string""],
                    ""rollbackSuccess"": false
                }",
                Tags = new List<string> { "TMP", "DistanceField", "Shader", "Material", "Outline", "Underlay", "DropShadow" },
                Dependencies = new List<string>(),
                ContentHash = "SHA256:TMP_DF_CAP_20261017_V1",
                CreatedAt = DateTime.UtcNow,
                IsVerified = true
            };
        }

        public CapabilityExecutionResult Execute(CapabilityExecutionContext context)
        {
            var sw = Stopwatch.StartNew();
            string action = context.GetParameter<string>("action", "ALL")?.ToUpperInvariant() ?? "ALL";
            string matJson = context.GetParameter<string>("materialJson", string.Empty) ?? string.Empty;
            int atlasRes = context.GetParameter<int>("atlasResolution", 2048);
            int atlasPad = context.GetParameter<int>("atlasPadding", 9);
            string outlineIntensity = context.GetParameter<string>("desiredOutlineIntensity", "BOLD")?.ToUpperInvariant() ?? "BOLD";

            var outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                switch (action)
                {
                    case "ANALYZE_MATERIAL":
                    {
                        var props = ParseMaterial(matJson, atlasRes, atlasPad);
                        var analysis = AnalyzeMaterialProperties(props);
                        outputs["analysis"] = analysis;
                        outputs["currentOutlineWidth"] = props.OutlineWidth;
                        outputs["currentUnderlaySoftness"] = props.UnderlaySoftness;
                        outputs["hasOutlineKeyword"] = props.ShaderKeywords.Contains("OUTLINE_ON");
                        outputs["hasUnderlayKeyword"] = props.ShaderKeywords.Contains("UNDERLAY_ON");
                        break;
                    }
                    case "CALCULATE_PATCH":
                    {
                        var props = ParseMaterial(matJson, atlasRes, atlasPad);
                        var recommended = CalculateRecommendedProperties(props, outlineIntensity);
                        outputs["recommendedProperties"] = recommended;
                        break;
                    }
                    case "APPLY_PATCH":
                    case "ALL":
                    {
                        var props = ParseMaterial(matJson, atlasRes, atlasPad);
                        string originalJsonClean = JsonSerializer.Serialize(props);
                        string origHash = ComputeHash(originalJsonClean);

                        var patched = CalculateRecommendedProperties(props, outlineIntensity);
                        string patchedJsonClean = JsonSerializer.Serialize(patched);
                        string patchedHash = ComputeHash(patchedJsonClean);

                        var snapshot = new MaterialPatchSnapshot
                        {
                            OriginalJson = originalJsonClean,
                            PatchedJson = patchedJsonClean,
                            OriginalChecksum = origHash,
                            PatchedChecksum = patchedHash
                        };
                        _snapshotRegistry[snapshot.SnapshotId] = snapshot;

                        outputs["snapshotId"] = snapshot.SnapshotId;
                        outputs["patchedMaterialJson"] = patchedJsonClean;
                        outputs["originalChecksum"] = origHash;
                        outputs["patchedChecksum"] = patchedHash;
                        outputs["outlineWidth"] = patched.OutlineWidth;
                        outputs["underlayDilate"] = patched.UnderlayDilate;
                        outputs["underlaySoftness"] = patched.UnderlaySoftness;
                        outputs["shaderKeywords"] = patched.ShaderKeywords;
                        break;
                    }
                    case "ROLLBACK":
                    {
                        string snapId = context.GetParameter<string>("snapshotId", string.Empty) ?? string.Empty;
                        if (_snapshotRegistry.TryGetValue(snapId, out var snapshot))
                        {
                            outputs["restoredMaterialJson"] = snapshot.OriginalJson;
                            outputs["restoredChecksum"] = snapshot.OriginalChecksum;
                            outputs["rollbackSuccess"] = true;
                        }
                        else
                        {
                            sw.Stop();
                            return CapabilityExecutionResult.Fail($"Không tìm thấy Snapshot ID '{snapId}' trong registry để rollback.", sw.ElapsedMilliseconds);
                        }
                        break;
                    }
                }

                sw.Stop();
                string outputHash = outputs.TryGetValue("patchedChecksum", out var ph) ? ph?.ToString() ?? "OK" : "OK";
                return CapabilityExecutionResult.Ok(outputs, outputHash, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return CapabilityExecutionResult.Fail($"Lỗi thực thi TMPDistanceFieldShaderPatchCapability: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        public Task<CapabilityExecutionResult> ExecuteAsync(CapabilityExecutionContext context, CancellationToken ct = default)
        {
            return Task.FromResult(Execute(context));
        }

        private static TMPMaterialProperties ParseMaterial(string json, int defaultRes, int defaultPad)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new TMPMaterialProperties
                {
                    AtlasWidth = defaultRes,
                    AtlasHeight = defaultRes,
                    AtlasPadding = defaultPad
                };
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<TMPMaterialProperties>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (parsed != null)
                {
                    if (parsed.AtlasWidth <= 0) parsed.AtlasWidth = defaultRes;
                    if (parsed.AtlasHeight <= 0) parsed.AtlasHeight = defaultRes;
                    if (parsed.AtlasPadding <= 0) parsed.AtlasPadding = defaultPad;
                    return parsed;
                }
            }
            catch { }

            return new TMPMaterialProperties { AtlasWidth = defaultRes, AtlasHeight = defaultRes, AtlasPadding = defaultPad };
        }

        public static List<string> AnalyzeMaterialProperties(TMPMaterialProperties props)
        {
            var notes = new List<string>();

            if (!props.ShaderKeywords.Contains("OUTLINE_ON"))
            {
                notes.Add("CẢNH BÁO: Keyword 'OUTLINE_ON' chưa được kích hoạt. Shader TextMeshPro sẽ loại bỏ hoàn toàn mã vẽ đường viền (#if defined(OUTLINE_ON)).");
            }

            if (props.OutlineWidth <= 0.05f)
            {
                notes.Add($"CẢNH BÁO: _OutlineWidth hiện tại = {props.OutlineWidth:F2} (quá mỏng hoặc bằng 0), mắt thường khó nhìn thấy viền chữ.");
            }

            if (!props.ShaderKeywords.Contains("UNDERLAY_ON"))
            {
                notes.Add("CẢNH BÁO: Keyword 'UNDERLAY_ON' chưa được kích hoạt. Bóng đổ không được tính toán.");
            }

            if (props.UnderlaySoftness >= 0.70f)
            {
                notes.Add($"CẢNH BÁO: _UnderlaySoftness = {props.UnderlaySoftness:F2} (quá lớn đối với padding {props.AtlasPadding}px), làm bóng đổ bị mờ thành vô hình (opacity < 5%).");
            }

            if (props.UnderlayDilate < 0.0f)
            {
                notes.Add($"CẢNH BÁO: _UnderlayDilate = {props.UnderlayDilate:F2} (âm), làm bóng đổ co vào phía trong thân chữ thay vì nở ra ngoài.");
            }

            if (notes.Count == 0)
            {
                notes.Add("Vật liệu TextMeshPro đạt chuẩn: Viền đậm rõ nét và bóng đổ sắc sảo.");
            }

            return notes;
        }

        public static TMPMaterialProperties CalculateRecommendedProperties(TMPMaterialProperties source, string intensity = "BOLD")
        {
            var res = new TMPMaterialProperties
            {
                ShaderName = source.ShaderName,
                AtlasWidth = source.AtlasWidth,
                AtlasHeight = source.AtlasHeight,
                AtlasPadding = source.AtlasPadding,
                ShaderKeywords = new List<string>(source.ShaderKeywords)
            };

            if (!res.ShaderKeywords.Contains("OUTLINE_ON")) res.ShaderKeywords.Add("OUTLINE_ON");
            if (!res.ShaderKeywords.Contains("UNDERLAY_ON")) res.ShaderKeywords.Add("UNDERLAY_ON");

            // Hiệu chuẩn theo độ phân giải Atlas
            // Atlas 2048x2048: hệ số co giãn ~ 1.0f
            // Atlas 1024x1024: hệ số co giãn ~ 0.65f
            // Atlas 512x512: hệ số co giãn ~ 0.45f
            float scaleFactor = source.AtlasWidth switch
            {
                >= 2048 => 1.0f,
                >= 1024 => 0.65f,
                _ => 0.45f
            };

            if (intensity == "BOLD")
            {
                // Thông số chuẩn hoạt hình đậm nét (PlateUp & Comic style)
                res.OutlineWidth = Math.Clamp(0.30f * scaleFactor, 0.15f, 0.35f);
                res.OutlineColor = "#050505FF";
                res.UnderlayOffsetX = 0.55f * scaleFactor;
                res.UnderlayOffsetY = -0.80f * scaleFactor;
                res.UnderlayDilate = Math.Clamp(0.30f * scaleFactor, 0.15f, 0.35f);
                res.UnderlaySoftness = Math.Clamp(0.40f * scaleFactor, 0.20f, 0.45f);
                res.UnderlayColor = "#050303D9";
            }
            else if (intensity == "MEDIUM")
            {
                res.OutlineWidth = Math.Clamp(0.20f * scaleFactor, 0.10f, 0.25f);
                res.OutlineColor = "#0A0A0AFF";
                res.UnderlayOffsetX = 0.45f * scaleFactor;
                res.UnderlayOffsetY = -0.60f * scaleFactor;
                res.UnderlayDilate = Math.Clamp(0.20f * scaleFactor, 0.10f, 0.25f);
                res.UnderlaySoftness = Math.Clamp(0.30f * scaleFactor, 0.15f, 0.35f);
                res.UnderlayColor = "#050505BF";
            }
            else // SUBTLE
            {
                res.OutlineWidth = Math.Clamp(0.12f * scaleFactor, 0.06f, 0.15f);
                res.OutlineColor = "#111111FF";
                res.UnderlayOffsetX = 0.35f * scaleFactor;
                res.UnderlayOffsetY = -0.45f * scaleFactor;
                res.UnderlayDilate = Math.Clamp(0.10f * scaleFactor, 0.05f, 0.15f);
                res.UnderlaySoftness = Math.Clamp(0.25f * scaleFactor, 0.12f, 0.30f);
                res.UnderlayColor = "#11111199";
            }

            return res;
        }

        private static string ComputeHash(string text)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}

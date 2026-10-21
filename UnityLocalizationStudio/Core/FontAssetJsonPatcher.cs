using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace UnityLocalizationStudio.Core
{
    /// <summary>
    /// Tiện ích thay thế Font Asset cấp độ tệp cấu hình JSON / Assets (tương tự CourierPrime GOC.json -> CourierPrime thay the.json).
    /// Giữ nguyên 100% Material gốc (m_Material) và Atlas Textures (m_AtlasTextures) của game,
    /// chỉ sao chép m_FaceInfo, m_GlyphTable, m_CharacterTable, m_UsedGlyphRects, m_FreeGlyphRects từ font Việt hóa.
    /// Giúp toàn bộ hiệu ứng viền, đổ bóng, shader của game được bảo toàn trọn vẹn mà không cần tạo Material mới.
    /// </summary>
    public static class FontAssetJsonPatcher
    {
        public static string PatchFontAssetJson(string originalJsonContent, string vietnameseJsonContent)
        {
            if (string.IsNullOrWhiteSpace(originalJsonContent))
                throw new ArgumentNullException(nameof(originalJsonContent));
            if (string.IsNullOrWhiteSpace(vietnameseJsonContent))
                throw new ArgumentNullException(nameof(vietnameseJsonContent));

            var originalNode = JsonNode.Parse(originalJsonContent) as JsonObject
                ?? throw new InvalidOperationException("originalJsonContent không phải là JsonObject hợp lệ");
            var vietnameseNode = JsonNode.Parse(vietnameseJsonContent) as JsonObject
                ?? throw new InvalidOperationException("vietnameseJsonContent không phải là JsonObject hợp lệ");

            // Danh sách các trường glyph và bảng ký tự cần sao chép từ font Việt hóa
            string[] fieldsToCopy = new[]
            {
                "m_FaceInfo",
                "m_GlyphTable",
                "m_CharacterTable",
                "m_UsedGlyphRects",
                "m_FreeGlyphRects"
            };

            foreach (var field in fieldsToCopy)
            {
                if (vietnameseNode.ContainsKey(field) && vietnameseNode[field] != null)
                {
                    // Clone node để tránh lỗi parent attachment trong System.Text.Json
                    originalNode[field] = JsonNode.Parse(vietnameseNode[field]!.ToJsonString());
                }
            }

            // Đồng bộ CreationSettings nếu có
            if (vietnameseNode.ContainsKey("m_CreationSettings") && vietnameseNode["m_CreationSettings"] is JsonObject vhCreation)
            {
                if (originalNode.ContainsKey("m_CreationSettings") && originalNode["m_CreationSettings"] is JsonObject origCreation)
                {
                    if (vhCreation.ContainsKey("pointSize")) origCreation["pointSize"] = vhCreation["pointSize"]?.GetValue<int>();
                    if (vhCreation.ContainsKey("padding")) origCreation["padding"] = vhCreation["padding"]?.GetValue<int>();
                    if (vhCreation.ContainsKey("characterSequence")) origCreation["characterSequence"] = vhCreation["characterSequence"]?.GetValue<string>();
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            return originalNode.ToJsonString(options);
        }

        public static void PatchFontAssetJsonFile(string originalJsonPath, string vietnameseJsonPath, string outputJsonPath)
        {
            if (!File.Exists(originalJsonPath))
                throw new FileNotFoundException("Không tìm thấy tệp font gốc", originalJsonPath);
            if (!File.Exists(vietnameseJsonPath))
                throw new FileNotFoundException("Không tìm thấy tệp font Việt hóa", vietnameseJsonPath);

            string origContent = File.ReadAllText(originalJsonPath);
            string vhContent = File.ReadAllText(vietnameseJsonPath);

            string patchedJson = PatchFontAssetJson(origContent, vhContent);

            string? outDir = Path.GetDirectoryName(outputJsonPath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            File.WriteAllText(outputJsonPath, patchedJson);
        }
    }
}

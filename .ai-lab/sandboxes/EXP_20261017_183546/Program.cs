using System;

public class Program
{
    public static int Main()
    {
        // Tạo một Material Preset mẫu
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/SDF"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);
        originalMaterial.EnableKeyword("_OUTLINE_ON");
        originalMaterial.EnableKeyword("_UNDERLAY_ON");

        // Nhân bản Material Preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterial(originalMaterial);

        // Kiểm tra các thuộc tính đã được sao chép
        if (clonedMaterial.GetFloat("_OutlineWidth") == 0.1f &&
            clonedMaterial.GetFloat("_UnderlaySoftness") == 0.05f &&
            clonedMaterial.IsKeywordEnabled("_OUTLINE_ON") &&
            clonedMaterial.IsKeywordEnabled("_UNDERLAY_ON"))
        {
            Console.WriteLine("PASS: Các thuộc tính đã được sao chép chính xác.");
            return 0;
        }
        else
        {
            Console.WriteLine("FAIL: Có thuộc tính chưa được sao chép chính xác.");
            return 1;
        }
    }
}
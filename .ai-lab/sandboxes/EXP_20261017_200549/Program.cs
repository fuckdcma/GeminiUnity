using System;

public class Program
{
    public static int Main()
    {
        // Tạo một Material Preset mẫu
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/SDF"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

        // Nhân bản Material Preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial);

        // Kiểm tra thuộc tính OutlineWidth
        if (Math.Abs(clonedMaterial.GetFloat("_OutlineWidth") - 0.1f) > 0.001f)
        {
            Console.WriteLine("OutlineWidth không đúng.");
            return 1;
        }

        // Kiểm tra thuộc tính UnderlaySoftness
        if (Math.Abs(clonedMaterial.GetFloat("_UnderlaySoftness") - 0.05f) > 0.001f)
        {
            Console.WriteLine("UnderlaySoftness không đúng.");
            return 1;
        }

        // Kiểm tra Shader Keywords
        if (clonedMaterial.shaderKeywords != originalMaterial.shaderKeywords)
        {
            Console.WriteLine("Shader Keywords không đúng.");
            return 1;
        }

        Console.WriteLine("All checks passed.");
        return 0;
    }
}
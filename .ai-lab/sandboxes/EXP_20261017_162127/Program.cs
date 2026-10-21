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
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial, "TextMeshPro/SDF");

        // Kiểm tra thuộc tính OutlineWidth
        if (clonedMaterial.GetFloat("_OutlineWidth") != 0.1f)
        {
            Console.WriteLine("OutlineWidth không đúng.");
            return 1;
        }

        // Kiểm tra thuộc tính UnderlaySoftness
        if (clonedMaterial.GetFloat("_UnderlaySoftness") != 0.05f)
        {
            Console.WriteLine("UnderlaySoftness không đúng.");
            return 1;
        }

        Console.WriteLine("Kiểm tra thành công.");
        return 0;
    }
}
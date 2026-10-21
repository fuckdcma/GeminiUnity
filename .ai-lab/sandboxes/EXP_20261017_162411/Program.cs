using System;

public class Program
{
    public static int Main()
    {
        // Tạo một Material Preset mẫu
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

        // Nhân bản Material Preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial);

        // Kiểm tra thuộc tính OutlineWidth
        if (clonedMaterial.GetFloat("_OutlineWidth") != originalMaterial.GetFloat("_OutlineWidth"))
        {
            Console.WriteLine("OutlineWidth không được sao chép đúng.");
            return 1;
        }

        // Kiểm tra thuộc tính UnderlaySoftness
        if (clonedMaterial.GetFloat("_UnderlaySoftness") != originalMaterial.GetFloat("_UnderlaySoftness"))
        {
            Console.WriteLine("UnderlaySoftness không được sao chép đúng.");
            return 1;
        }

        Console.WriteLine("Sao chép Material Preset thành công.");
        return 0;
    }
}
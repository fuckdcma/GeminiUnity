using System;

public class Program
{
    public static int Main()
    {
        // Tạo một Material Preset mẫu
        Material presetMaterial = new Material(Shader.Find("TextMeshPro/TextMeshPro - Outline"));
        presetMaterial.SetColor("_OutlineColor", Color.red);
        presetMaterial.SetFloat("_OutlineWidth", 0.1f);
        presetMaterial.SetColor("_UnderlayColor", Color.green);
        presetMaterial.SetVector("_UnderlayOffset", new Vector2(0.05f, 0.05f));
        presetMaterial.EnableKeyword("_UNDERLAY_ON");

        // Nhân bản Material Preset
        Material clonedMaterial = TMPMaterialPresetCloner.CloneMaterialPreset(presetMaterial);

        // Kiểm tra thuộc tính shader
        if (clonedMaterial.GetColor("_OutlineColor") != Color.red ||
            clonedMaterial.GetFloat("_OutlineWidth") != 0.1f ||
            clonedMaterial.GetColor("_UnderlayColor") != Color.green ||
            clonedMaterial.GetVector("_UnderlayOffset") != new Vector2(0.05f, 0.05f) ||
            !clonedMaterial.IsKeywordEnabled("_UNDERLAY_ON"))
        {
            Console.WriteLine("Test FAIL");
            return 1;
        }

        Console.WriteLine("Test PASS");
        return 0;
    }
}
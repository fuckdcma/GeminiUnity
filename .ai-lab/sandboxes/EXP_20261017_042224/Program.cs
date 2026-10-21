using System;
using TMPro;
using UnityEngine;
using NUnit.Framework;

public class TMPDistanceFieldDynamicShaderClonerTest
{
    public static int Main()
    {
        // Tạo một Material Preset với ARIAL SDF
        Material originalMaterial = Resources.Load<Material>("Fonts/ARIAL SDF");

        // Tạo bản sao Material Preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial, "TextMeshPro/Dynamic Distance Field");

        // Kiểm tra các thuộc tính đã được sao chép
        Assert.AreEqual(originalMaterial.GetFloat("_OutlineWidth"), clonedMaterial.GetFloat("_OutlineWidth"), "OutlineWidth không đúng");
        Assert.AreEqual(originalMaterial.GetFloat("_UnderlaySoftness"), clonedMaterial.GetFloat("_UnderlaySoftness"), "UnderlaySoftness không đúng");

        // Kiểm tra Shader
        Assert.AreEqual("TextMeshPro/Dynamic Distance Field", clonedMaterial.shader.name, "Shader không đúng");

        // Kiểm tra Shader Keywords
        Assert.IsTrue(clonedMaterial.HasKeyword("_OUTLINE_ON"), "Shader Keyword _OUTLINE_ON không đúng");
        Assert.IsTrue(clonedMaterial.HasKeyword("_UNDERLAY_ON"), "Shader Keyword _UNDERLAY_ON không đúng");

        Console.WriteLine("PASS");
        return 0;
    }
}
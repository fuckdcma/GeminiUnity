using System;
using TMPro;
using UnityEngine;
using NUnit.Framework;

public class TMPDistanceFieldDynamicShaderClonerTest
{
    public static int Main()
    {
        // Tạo một Material Preset với ARIAL SDF
        TMP_SDFShaderUtilities.CreateMaterialPreset("Arial SDF", TMP_SDFShaderUtilities.SDFShaderType.ARIAL);

        // Lấy Material Preset của ARIAL SDF
        Material originalMaterial = Resources.Load<Material>("Fonts/ARIAL SDF");

        // Nhân bản Material Preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial);

        // Kiểm định các thuộc tính
        Assert.AreEqual(originalMaterial.GetFloat("_OutlineWidth"), clonedMaterial.GetFloat("_OutlineWidth"), "OutlineWidth không khớp");
        Assert.AreEqual(originalMaterial.GetFloat("_UnderlaySoftness"), clonedMaterial.GetFloat("_UnderlaySoftness"), "UnderlaySoftness không khớp");

        // In ra thông báo
        Console.WriteLine("Kiểm định thành công!");

        return 0;
    }
}
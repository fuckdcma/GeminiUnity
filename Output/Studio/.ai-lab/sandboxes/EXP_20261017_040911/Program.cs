using System;
using TMPro;
using UnityEngine;
using NUnit.Framework;

public class TMPDistanceFieldDynamicShaderClonerTest
{
    [Test]
    public void TestCloneMaterialPreset()
    {
        // Create a sample material with TMP Distance Field Dynamic Shader
        Shader shader = Shader.Find("TextMeshPro/Dynamic Distance Field");
        Material originalMaterial = new Material(shader);
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

        // Clone the material preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial);

        // Check if the cloned material is not null
        Assert.IsNotNull(clonedMaterial, "Cloned material is null.");

        // Check if the OutlineWidth and UnderlaySoftness properties are copied
        Assert.AreEqual(originalMaterial.GetFloat("_OutlineWidth"), clonedMaterial.GetFloat("_OutlineWidth"), "OutlineWidth is not copied.");
        Assert.AreEqual(originalMaterial.GetFloat("_UnderlaySoftness"), clonedMaterial.GetFloat("_UnderlaySoftness"), "UnderlaySoftness is not copied.");

        // Check if the Shader Keywords are updated
        Assert.IsTrue(clonedMaterial.shaderKeywords.Contains("TMP_DISTANCE_FIELD"), "Shader Keywords are not updated.");

        Console.WriteLine("Test passed.");
    }

    public static int Main()
    {
        try
        {
            TestContext.RunAllTests();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed: " + ex.Message);
            return 1;
        }
    }
}
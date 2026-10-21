using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material with ARIAL SDF and some properties
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);
        originalMaterial.EnableKeyword("OUTLINE_ON");
        originalMaterial.EnableKeyword("UNDERLAY_ON");

        // Clone the material
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterial(originalMaterial);

        // Check if the cloned material has the same properties
        if (clonedMaterial.GetFloat("_OutlineWidth") != originalMaterial.GetFloat("_OutlineWidth") ||
            clonedMaterial.GetFloat("_UnderlaySoftness") != originalMaterial.GetFloat("_UnderlaySoftness") ||
            clonedMaterial.IsKeywordEnabled("OUTLINE_ON") != originalMaterial.IsKeywordEnabled("OUTLINE_ON") ||
            clonedMaterial.IsKeywordEnabled("UNDERLAY_ON") != originalMaterial.IsKeywordEnabled("UNDERLAY_ON"))
        {
            Console.WriteLine("Test failed: Cloned material properties do not match original material.");
            return 1;
        }

        Console.WriteLine("Test passed: Cloned material properties match original material.");
        return 0;
    }
}
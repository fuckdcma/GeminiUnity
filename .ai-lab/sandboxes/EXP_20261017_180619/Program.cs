using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material preset
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/SDF"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);
        originalMaterial.EnableKeyword("_OUTLINE_ON");
        originalMaterial.EnableKeyword("_UNDERLAY_ON");

        // Clone the material preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial);

        // Check if the cloned material has the same properties
        if (clonedMaterial.GetFloat("_OutlineWidth") == 0.1f &&
            clonedMaterial.GetFloat("_UnderlaySoftness") == 0.05f &&
            clonedMaterial.IsKeywordEnabled("_OUTLINE_ON") &&
            clonedMaterial.IsKeywordEnabled("_UNDERLAY_ON"))
        {
            Console.WriteLine("PASS: Cloned material has the same properties as the original material.");
            return 0;
        }
        else
        {
            Console.WriteLine("FAIL: Cloned material does not have the same properties as the original material.");
            return 1;
        }
    }
}
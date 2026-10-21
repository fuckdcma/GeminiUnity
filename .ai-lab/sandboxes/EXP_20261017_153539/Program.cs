using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material preset
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

        // Clone the material preset
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial);

        // Check if the cloned material has the same properties
        if (clonedMaterial.GetFloat("_OutlineWidth") == 0.1f &&
            clonedMaterial.GetFloat("_UnderlaySoftness") == 0.05f &&
            clonedMaterial.shader.name == "TextMeshPro/Dynamic Distance Field")
        {
            Console.WriteLine("PASS: Cloned material has the same properties and shader.");
            return 0;
        }
        else
        {
            Console.WriteLine("FAIL: Cloned material does not have the correct properties or shader.");
            return 1;
        }
    }
}
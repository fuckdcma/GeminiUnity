using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/SDF"));
        originalMaterial.name = "OriginalMaterial";
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);
        originalMaterial.EnableKeyword("_OUTLINE_ON");

        // Clone the material
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterialPreset(originalMaterial, "ClonedMaterial");

        // Check if the cloned material has the same properties
        if (clonedMaterial == null)
        {
            Console.WriteLine("Cloned material is null.");
            return 1;
        }

        if (clonedMaterial.name != "ClonedMaterial")
        {
            Console.WriteLine("Cloned material name is incorrect.");
            return 1;
        }

        if (clonedMaterial.GetFloat("_OutlineWidth") != 0.1f)
        {
            Console.WriteLine("Cloned material OutlineWidth is incorrect.");
            return 1;
        }

        if (clonedMaterial.GetFloat("_UnderlaySoftness") != 0.05f)
        {
            Console.WriteLine("Cloned material UnderlaySoftness is incorrect.");
            return 1;
        }

        if (!clonedMaterial.IsKeywordEnabled("_OUTLINE_ON"))
        {
            Console.WriteLine("Cloned material Outline keyword is not enabled.");
            return 1;
        }

        Console.WriteLine("All checks passed.");
        return 0;
    }
}
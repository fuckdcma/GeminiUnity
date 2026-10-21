using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);
        originalMaterial.EnableKeyword("OUTLINE");

        // Clone the material
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterial(originalMaterial);

        // Check if the cloned material has the same properties
        if (clonedMaterial.GetFloat("_OutlineWidth") == 0.1f &&
            clonedMaterial.GetFloat("_UnderlaySoftness") == 0.05f &&
            clonedMaterial.IsKeywordEnabled("OUTLINE"))
        {
            Console.WriteLine("PASS: Cloned material has the same properties.");
            return 0;
        }
        else
        {
            Console.WriteLine("FAIL: Cloned material does not have the same properties.");
            return 1;
        }
    }
}
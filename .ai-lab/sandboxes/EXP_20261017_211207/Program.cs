using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material with TMP Distance Field Dynamic Shader
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

        // Clone the material
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterial(originalMaterial);

        // Check if the cloned material has the same properties
        if (clonedMaterial.GetFloat("_OutlineWidth") == 0.1f &&
            clonedMaterial.GetFloat("_UnderlaySoftness") == 0.05f &&
            clonedMaterial.shaderKeywords == originalMaterial.shaderKeywords)
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
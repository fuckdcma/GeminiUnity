using System;

public class Program
{
    public static int Main()
    {
        // Create a sample material with ARIAL SDF and some properties
        Material originalMaterial = new Material(Shader.Find("TextMeshPro/SDF"));
        originalMaterial.SetFloat("_OutlineWidth", 0.1f);
        originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

        // Clone the material
        Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterial(originalMaterial);

        // Check if the properties are cloned correctly
        if (clonedMaterial.GetFloat("_OutlineWidth") == 0.1f &&
            clonedMaterial.GetFloat("_UnderlaySoftness") == 0.05f &&
            clonedMaterial.shaderKeywords == originalMaterial.shaderKeywords)
        {
            Console.WriteLine("PASS: Material properties and shader keywords are cloned correctly.");
            return 0;
        }
        else
        {
            Console.WriteLine("FAIL: Material properties or shader keywords are not cloned correctly.");
            return 1;
        }
    }
}
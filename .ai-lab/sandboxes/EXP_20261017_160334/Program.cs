using System;

public class Program
{
    public static int Main()
    {
        try
        {
            // Example usage of the module
            string originalMaterialPath = "path/to/original/material.mat";
            string clonedMaterialPath = "path/to/cloned/material.mat";

            // Load the original material
            Material originalMaterial = new Material(Shader.Find("TextMeshPro/SDF"));

            // Clone and preserve the shader material
            TMPDistanceFieldShaderPreserverCloner.CloneAndPreserveShaderMaterial(originalMaterial, clonedMaterialPath);

            Console.WriteLine("Shader material cloned and preserved successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed: " + ex.Message);
            return 1;
        }
    }
}
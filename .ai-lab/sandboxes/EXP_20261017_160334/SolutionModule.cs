using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPDistanceFieldShaderPreserverCloner
{
    public static void CloneAndPreserveShaderMaterial(Material originalMaterial, string outputPath)
    {
        try
        {
            // Create a new material instance
            Material clonedMaterial = new Material(originalMaterial);

            // Preserve shader keywords
            foreach (string keyword in originalMaterial.shaderKeywords)
            {
                clonedMaterial.EnableKeyword(keyword);
            }

            // Save the cloned material to the specified output path
            File.WriteAllBytes(outputPath, clonedMaterial.EncodeToMemory());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error cloning and preserving shader material: " + ex.Message);
        }
    }
}
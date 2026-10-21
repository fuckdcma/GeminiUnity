using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterial(Material originalMaterial)
    {
        if (originalMaterial == null)
        {
            throw new ArgumentNullException(nameof(originalMaterial));
        }

        // Create a new material instance from the original material
        Material clonedMaterial = new Material(originalMaterial);

        // Copy OutlineWidth and UnderlaySoftness properties
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        // Update Shader Keywords
        clonedMaterial.shaderKeywords = originalMaterial.shaderKeywords;

        return clonedMaterial;
    }
}
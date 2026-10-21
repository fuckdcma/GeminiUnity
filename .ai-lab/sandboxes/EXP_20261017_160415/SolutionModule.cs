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

        // Create a new material from the original material
        Material clonedMaterial = new Material(originalMaterial);

        // Clone OutlineWidth and UnderlaySoftness
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        // Clone Shader Keywords
        clonedMaterial.shaderKeywords = originalMaterial.shaderKeywords;

        return clonedMaterial;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterialPreset(Material originalMaterial, string newShaderName)
    {
        if (originalMaterial == null)
        {
            throw new ArgumentNullException(nameof(originalMaterial));
        }

        // Create a new material with the same shader
        Material clonedMaterial = new Material(originalMaterial.shader);
        clonedMaterial.name = newShaderName;

        // Copy properties
        clonedMaterial.CopyPropertiesFromMaterial(originalMaterial);

        // Ensure OutlineWidth and UnderlaySoftness are preserved
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        // Copy Shader Keywords
        foreach (string keyword in originalMaterial.shaderKeywords)
        {
            clonedMaterial.EnableKeyword(keyword);
        }

        return clonedMaterial;
    }
}
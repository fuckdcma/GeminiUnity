using TMPro;
using UnityEngine;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterialPreset(Material originalMaterial)
    {
        if (originalMaterial == null)
        {
            Debug.LogError("Original material is null.");
            return null;
        }

        // Create a new material instance from the original material
        Material clonedMaterial = new Material(originalMaterial);

        // Copy OutlineWidth and UnderlaySoftness properties
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        // Update Shader Keywords to use TMP Distance Field Dynamic Shader
        clonedMaterial.shaderKeywords = originalMaterial.shaderKeywords;
        if (!clonedMaterial.shaderKeywords.Contains("TMP_DISTANCE_FIELD"))
        {
            clonedMaterial.EnableKeyword("TMP_DISTANCE_FIELD");
        }

        return clonedMaterial;
    }
}
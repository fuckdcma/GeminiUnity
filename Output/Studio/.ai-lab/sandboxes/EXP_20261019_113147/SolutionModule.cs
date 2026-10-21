using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPMaterialPresetCloner
{
    public static Material CloneMaterialPreset(Material originalMaterial)
    {
        if (originalMaterial == null)
        {
            throw new ArgumentNullException(nameof(originalMaterial));
        }

        Material clonedMaterial = new Material(originalMaterial);
        clonedMaterial.name = originalMaterial.name + "_Cloned";

        // Clone shader keywords
        foreach (string keyword in originalMaterial.shaderKeywords)
        {
            clonedMaterial.EnableKeyword(keyword);
        }

        // Clone Outline/Underlay properties
        if (originalMaterial.HasProperty("_OutlineWidth"))
        {
            clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
        }
        if (originalMaterial.HasProperty("_OutlineColor"))
        {
            clonedMaterial.SetColor("_OutlineColor", originalMaterial.GetColor("_OutlineColor"));
        }
        if (originalMaterial.HasProperty("_UnderlayColor"))
        {
            clonedMaterial.SetColor("_UnderlayColor", originalMaterial.GetColor("_UnderlayColor"));
        }
        if (originalMaterial.HasProperty("_UnderlayOffset"))
        {
            clonedMaterial.SetFloat("_UnderlayOffset", originalMaterial.GetFloat("_UnderlayOffset"));
        }

        return clonedMaterial;
    }
}
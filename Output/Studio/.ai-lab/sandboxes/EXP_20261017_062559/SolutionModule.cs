using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPMaterialPresetCloner
{
    public static Material CloneMaterialPreset(Material presetMaterial)
    {
        if (presetMaterial == null)
        {
            throw new ArgumentNullException(nameof(presetMaterial));
        }

        // Tạo bản sao của Material Preset
        Material clonedMaterial = new Material(presetMaterial);

        // Sao chép thuộc tính shader Outline/Underlay
        if (presetMaterial.HasProperty("_OutlineColor"))
        {
            clonedMaterial.SetColor("_OutlineColor", presetMaterial.GetColor("_OutlineColor"));
        }
        if (presetMaterial.HasProperty("_OutlineWidth"))
        {
            clonedMaterial.SetFloat("_OutlineWidth", presetMaterial.GetFloat("_OutlineWidth"));
        }
        if (presetMaterial.HasProperty("_UnderlayColor"))
        {
            clonedMaterial.SetColor("_UnderlayColor", presetMaterial.GetColor("_UnderlayColor"));
        }
        if (presetMaterial.HasProperty("_UnderlayOffset"))
        {
            clonedMaterial.SetVector("_UnderlayOffset", presetMaterial.GetVector("_UnderlayOffset"));
        }

        // Sao chép Shader Keywords
        foreach (string keyword in presetMaterial.shaderKeywords)
        {
            clonedMaterial.EnableKeyword(keyword);
        }

        return clonedMaterial;
    }
}
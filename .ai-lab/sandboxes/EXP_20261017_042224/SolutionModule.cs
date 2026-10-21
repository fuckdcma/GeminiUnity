using TMPro;
using UnityEngine;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterialPreset(Material originalMaterial, string newShaderName)
    {
        // Tạo bản sao Material Preset
        Material clonedMaterial = new Material(originalMaterial);

        // Cập nhật Shader
        clonedMaterial.shader = Shader.Find(newShaderName);

        // Sao chép thuộc tính OutlineWidth
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));

        // Sao chép thuộc tính UnderlaySoftness
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        // Sao chép Shader Keywords
        clonedMaterial.CopyPropertiesFromMaterial(originalMaterial);

        return clonedMaterial;
    }
}
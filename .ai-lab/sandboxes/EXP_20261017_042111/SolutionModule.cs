using TMPro;
using UnityEngine;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterialPreset(Material originalMaterial)
    {
        // Tạo bản sao của Material Preset
        Material clonedMaterial = new Material(originalMaterial);

        // Sao chép thuộc tính OutlineWidth
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));

        // Sao chép thuộc tính UnderlaySoftness
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        return clonedMaterial;
    }
}
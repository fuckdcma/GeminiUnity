using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterialPreset(Material originalMaterial)
    {
        if (originalMaterial == null)
        {
            throw new ArgumentNullException(nameof(originalMaterial));
        }

        // Tạo bản sao của Material
        Material clonedMaterial = new Material(originalMaterial);

        // Sao chép thuộc tính OutlineWidth
        clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));

        // Sao chép thuộc tính UnderlaySoftness
        clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

        return clonedMaterial;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class TMPDistanceFieldDynamicShaderCloner
{
    public static Material CloneMaterial(Material originalMaterial)
    {
        try
        {
            // Tạo bản sao của Material Preset
            Material clonedMaterial = new Material(originalMaterial);

            // Thay đổi shader của bản sao thành TMP Distance Field Dynamic Shader
            clonedMaterial.shader = Shader.Find("TextMeshPro/Dynamic Distance Field");

            // Sao chép các thuộc tính OutlineWidth và UnderlaySoftness từ Material gốc sang bản sao
            clonedMaterial.SetFloat("_OutlineWidth", originalMaterial.GetFloat("_OutlineWidth"));
            clonedMaterial.SetFloat("_UnderlaySoftness", originalMaterial.GetFloat("_UnderlaySoftness"));

            return clonedMaterial;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error cloning material: " + ex.Message);
            return null;
        }
    }
}
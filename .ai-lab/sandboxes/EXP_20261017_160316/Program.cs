using System;

public class Program
{
    public static int Main()
    {
        try
        {
            // Tạo một Material Preset với shader ARIAL SDF
            Material originalMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
            originalMaterial.SetFloat("_OutlineWidth", 0.1f);
            originalMaterial.SetFloat("_UnderlaySoftness", 0.05f);

            // Nhân bản Material Preset
            Material clonedMaterial = TMPDistanceFieldDynamicShaderCloner.CloneMaterial(originalMaterial);

            if (clonedMaterial != null)
            {
                // Kiểm tra bounding boxes
                if (clonedMaterial.GetFloat("_OutlineWidth") == originalMaterial.GetFloat("_OutlineWidth") &&
                    clonedMaterial.GetFloat("_UnderlaySoftness") == originalMaterial.GetFloat("_UnderlaySoftness"))
                {
                    Console.WriteLine("Material cloning successful.");
                    return 0;
                }
                else
                {
                    Console.WriteLine("Material cloning failed.");
                    return 1;
                }
            }
            else
            {
                Console.WriteLine("Material cloning failed.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during testing: " + ex.Message);
            return 1;
        }
    }
}
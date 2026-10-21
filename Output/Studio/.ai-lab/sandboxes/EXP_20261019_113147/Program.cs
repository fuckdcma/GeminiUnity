using System;

public class Program
{
    public static int Main()
    {
        try
        {
            // Create a sample material
            Material originalMaterial = new Material(Shader.Find("TextMeshPro/Outline"));
            originalMaterial.name = "SampleMaterial";
            originalMaterial.EnableKeyword("OUTLINE_ON");
            originalMaterial.SetFloat("_OutlineWidth", 0.05f);
            originalMaterial.SetColor("_OutlineColor", Color.black);
            originalMaterial.SetColor("_UnderlayColor", Color.white);
            originalMaterial.SetFloat("_UnderlayOffset", 0.01f);

            // Clone the material
            Material clonedMaterial = TMPMaterialPresetCloner.CloneMaterialPreset(originalMaterial);

            // Check if the cloned material is not null
            if (clonedMaterial == null)
            {
                Console.WriteLine("Cloned material is null.");
                return 1;
            }

            // Check if the cloned material has the same properties
            if (clonedMaterial.name != originalMaterial.name + "_Cloned")
            {
                Console.WriteLine("Cloned material name is incorrect.");
                return 1;
            }

            if (!clonedMaterial.IsKeywordEnabled("OUTLINE_ON"))
            {
                Console.WriteLine("Cloned material does not have the correct shader keyword.");
                return 1;
            }

            if (clonedMaterial.GetFloat("_OutlineWidth") != 0.05f)
            {
                Console.WriteLine("Cloned material has incorrect OutlineWidth.");
                return 1;
            }

            if (clonedMaterial.GetColor("_OutlineColor") != Color.black)
            {
                Console.WriteLine("Cloned material has incorrect OutlineColor.");
                return 1;
            }

            if (clonedMaterial.GetColor("_UnderlayColor") != Color.white)
            {
                Console.WriteLine("Cloned material has incorrect UnderlayColor.");
                return 1;
            }

            if (clonedMaterial.GetFloat("_UnderlayOffset") != 0.01f)
            {
                Console.WriteLine("Cloned material has incorrect UnderlayOffset.");
                return 1;
            }

            Console.WriteLine("All checks passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
            return 1;
        }
    }
}
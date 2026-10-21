using System;

public class Program
{
    public static int Main()
    {
        string validFilePath = "path_to_valid_unityfs_font_assetbundle";
        string invalidFilePath = "path_to_invalid_file";

        bool isValid = UnityFSBinaryHeaderValidator.ValidateHeader(validFilePath);
        bool isInvalid = UnityFSBinaryHeaderValidator.ValidateHeader(invalidFilePath);

        if (isValid && !isInvalid)
        {
            Console.WriteLine("Validation passed.");
            return 0;
        }
        else
        {
            Console.WriteLine("Validation failed.");
            return 1;
        }
    }
}
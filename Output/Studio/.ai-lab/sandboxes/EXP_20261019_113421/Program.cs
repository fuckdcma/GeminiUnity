using System;

public class Program
{
    public static int Main()
    {
        string validFilePath = "path_to_valid_unityfs_font_assetbundle";
        string invalidFilePath = "path_to_invalid_unityfs_font_assetbundle";

        bool isValid = UnityFSBinaryHeaderValidator.ValidateHeader(validFilePath);
        bool isInvalid = UnityFSBinaryHeaderValidator.ValidateHeader(invalidFilePath);

        if (isValid && !isInvalid)
        {
            Console.WriteLine("PASS: Header validation works correctly.");
            return 0;
        }
        else
        {
            Console.WriteLine("FAIL: Header validation does not work correctly.");
            return 1;
        }
    }
}
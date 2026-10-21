using System;
using System.IO;

public static class UnityFSBinaryHeaderValidator
{
    private const byte[] HEADER_SIGNATURE = { 0x55, 0x4E, 0x49, 0x46, 0x53, 0x20, 0x46, 0x53 };

    public static bool ValidateHeader(string filePath)
    {
        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] header = new byte[HEADER_SIGNATURE.Length];
                fs.Read(header, 0, header.Length);

                for (int i = 0; i < header.Length; i++)
                {
                    if (header[i] != HEADER_SIGNATURE[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
}
using System;
using System.IO;

public static class UnityFSBinaryHeaderValidator
{
    private const string HEADER = "UnityFS";
    private const int HEADER_LENGTH = 7;

    public static bool ValidateHeader(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] headerBytes = new byte[HEADER_LENGTH];
                int bytesRead = fs.Read(headerBytes, 0, HEADER_LENGTH);

                if (bytesRead != HEADER_LENGTH)
                {
                    return false;
                }

                string header = System.Text.Encoding.ASCII.GetString(headerBytes);
                return header == HEADER;
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return false;
        }
    }
}
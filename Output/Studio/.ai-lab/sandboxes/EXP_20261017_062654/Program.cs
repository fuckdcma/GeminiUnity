using System;

public class Program
{
    public static int Main()
    {
        try
        {
            string fontPath = "path/to/icon/emoji/font.ttf";
            Font font = IconEmojiFontIsolator.LoadFont(fontPath);
            Console.WriteLine("Font loaded successfully: " + font.name);
            IconEmojiFontIsolator.UnloadFont(fontPath);
            Console.WriteLine("Font unloaded successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed: " + ex.Message);
            return 1;
        }
    }
}
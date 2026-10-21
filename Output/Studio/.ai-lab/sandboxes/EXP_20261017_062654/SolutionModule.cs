using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class IconEmojiFontIsolator
{
    private static Dictionary<string, Font> fontCache = new Dictionary<string, Font>();

    public static Font LoadFont(string fontPath)
    {
        if (fontCache.ContainsKey(fontPath))
        {
            return fontCache[fontPath];
        }

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException("Font file not found", fontPath);
        }

        Font font = new Font(fontPath);
        fontCache[fontPath] = font;
        return font;
    }

    public static void UnloadFont(string fontPath)
    {
        if (fontCache.ContainsKey(fontPath))
        {
            Font font = fontCache[fontPath];
            fontCache.Remove(fontPath);
            DestroyFont(font);
        }
    }

    private static void DestroyFont(Font font)
    {
        // Logic to destroy the font, if necessary
        // For example, if the font is a managed resource, you might need to call Dispose
        // font.Dispose();
    }
}
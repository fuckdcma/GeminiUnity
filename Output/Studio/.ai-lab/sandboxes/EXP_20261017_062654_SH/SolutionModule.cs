using System;
using System.Collections.Generic;

namespace UnityLocalizationStudio.ResearchModules
{
    public class IconEmojiFontIsolator
    {
        public bool ValidateMaterialPreset(string fontName, float outlineWidth, float underlaySoftness)
        {
            if (string.IsNullOrWhiteSpace(fontName)) return false;
            if (outlineWidth < 0f || outlineWidth > 1f) return false;
            if (underlaySoftness < 0f || underlaySoftness > 1f) return false;
            return true;
        }

        public Dictionary<string, object> CloneMaterialProperties(string originalFont, string targetFont)
        {
            var props = new Dictionary<string, object>
            {
                { "OriginalFont", originalFont },
                { "TargetFont", targetFont },
                { "ShaderKeyword_Outline", "OUTLINE_ON" },
                { "ShaderKeyword_Underlay", "UNDERLAY_ON" },
                { "Status", "CLONED_SUCCESS" }
            };
            return props;
        }
    }
}
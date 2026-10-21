using System;

namespace UnityLocalizationStudio.Models
{
    public enum FontCategory
    {
        MainMenu,
        PrimaryText,
        TitleText,
        LargeText,
        CardTitle,
        CardDescription,
        InWorldLabel,
        SeedCode,
        Decorative,
        IconsProtected,
        FallbackOtherLanguages
    }

    public class FontMappingEntry
    {
        public string FontName { get; set; } = string.Empty;
        public string AssetSource { get; set; } = string.Empty;
        public FontCategory Category { get; set; } = FontCategory.PrimaryText;
        public string RoleDescription { get; set; } = string.Empty;
        public string OriginalBaseFont { get; set; } = string.Empty;
        public string TargetReplacement { get; set; } = string.Empty;
        public bool IsProtected { get; set; } = false;
        public int Priority { get; set; } = 1;
        public string SuggestedStyle { get; set; } = string.Empty;
    }
}

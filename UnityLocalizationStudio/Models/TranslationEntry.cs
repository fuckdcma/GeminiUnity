using System;

namespace UnityLocalizationStudio.Models
{
    public enum SourceKind
    {
        UnityAsset,
        ManagedAssembly,
        Unknown
    }

    public enum StringClassification
    {
        GameText,
        Technical,
        Protected,
        NeedsReview
    }

    public class TranslationEntry
    {
        // Định danh duy nhất và ổn định
        public string Id { get; set; } = string.Empty;

        // Trường tương thích ngược
        public string KeyId
        {
            get => !string.IsNullOrEmpty(Id) ? Id : _keyId;
            set { _keyId = value; if (string.IsNullOrEmpty(Id)) Id = value; }
        }
        private string _keyId = string.Empty;

        // Phân loại ngôn ngữ nguồn (EN, JA, ZH_CN, ZH_TW, KO, RU, DE, FR, ES, PT_BR, PL, TR)
        public string Language { get; set; } = "EN";

        // Nguồn gốc bản ghi (Provenance)
        public SourceKind SourceKind { get; set; } = SourceKind.Unknown;
        public string AssetFile { get; set; } = string.Empty;
        public string AssetPathId { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty; // JSONField, OdinDictionary, OdinStruct, ManagedString
        public string Field { get; set; } = string.Empty; // Name, Description, Title, FlavourText...
        public string Key { get; set; } = string.Empty; // Dictionary Key (VIEW_CARDS, START_DAY_READY...)
        public string ReferencePathId { get; set; } = string.Empty;
        public string JsonPointer { get; set; } = string.Empty; // /Name, /Sections/Array/0/Description
        public string OdinValueOffset { get; set; } = string.Empty;
        public string OdinUtf16Units { get; set; } = string.Empty;

        // Ngữ cảnh Managed Assembly (nếu có)
        public string ContextPath { get; set; } = string.Empty; // Assembly/Type/Method

        // Nội dung văn bản
        public string OriginalText { get; set; } = string.Empty;
        public string VietnameseTranslation { get; set; } = string.Empty;
        public string Placeholders { get; set; } = string.Empty; // $upgradable$, {0}, <sprite...>

        // Phân loại và độ tin cậy
        public StringClassification Classification { get; set; } = StringClassification.NeedsReview;
        public string Status { get; set; } = "Pending"; // Translate, Pending, Approved, Protected, Review, Ignored
        public string Confidence { get; set; } = "Medium"; // High, Medium, Low
        public string Notes { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Id}] ({Origin}:{Field}|{Key}) \"{OriginalText}\" [{Classification}/{Status}]";
        }
    }
}

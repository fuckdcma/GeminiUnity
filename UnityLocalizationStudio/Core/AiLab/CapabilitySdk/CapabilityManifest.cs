using System;
using System.Collections.Generic;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk
{
    public class CapabilityManifest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SemanticVersion { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = "ULS Engineering Lab";
        public string Category { get; set; } = "General";
        public string InputContractJson { get; set; } = "{}";
        public string OutputContractJson { get; set; } = "{}";
        public List<string> Tags { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string ContentHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = true;
    }
}

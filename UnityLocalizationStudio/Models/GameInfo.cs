using System;
using System.Collections.Generic;

namespace UnityLocalizationStudio.Models
{
    public enum GameArchitecture
    {
        Unknown,
        Mono,
        IL2CPP
    }

    public class GameInfo
    {
        public string GameRootPath { get; set; } = string.Empty;
        public string ExePath { get; set; } = string.Empty;
        public string ExeName { get; set; } = string.Empty;
        public string DataDirectory { get; set; } = string.Empty;
        public string UnityVersion { get; set; } = string.Empty;
        public GameArchitecture Architecture { get; set; } = GameArchitecture.Unknown;
        public bool HasTextMeshPro { get; set; }
        public bool HasLegacyText { get; set; }
        public List<string> ManagedAssemblies { get; set; } = new();
        public List<string> AssetFiles { get; set; } = new();
    }
}

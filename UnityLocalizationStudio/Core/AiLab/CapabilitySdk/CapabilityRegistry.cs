using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityLocalizationStudio.Core.AiLab.CapabilitySdk.Builtin;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk
{
    public class CapabilityRegistry
    {
        private static readonly Lazy<CapabilityRegistry> _instance = new(() => new CapabilityRegistry());
        public static CapabilityRegistry Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, ICapability> _capabilities = new(StringComparer.OrdinalIgnoreCase);
        private readonly KnowledgeDatabase _knowledgeBase;

        public CapabilityRegistry(KnowledgeDatabase? db = null)
        {
            _knowledgeBase = db ?? new KnowledgeDatabase();
            RegisterBuiltins();
        }

        private void RegisterBuiltins()
        {
            Register(new VietnameseUnicodeNormalizerCapability());
            Register(new TMPDistanceFieldShaderPatchCapability());
        }

        public void Register(ICapability capability)
        {
            if (capability == null) throw new ArgumentNullException(nameof(capability));
            _capabilities[capability.Id] = capability;

            // Đồng bộ phiên bản vào SQLite Knowledge Base
            try
            {
                var rec = new CapabilityVersionRecord
                {
                    Id = $"{capability.Id}_V{capability.SemanticVersion.Replace('.', '_')}",
                    CapabilityId = capability.Id,
                    Name = capability.Name,
                    SemanticVersion = capability.SemanticVersion,
                    InputContract = capability.Manifest.InputContractJson,
                    OutputContract = capability.Manifest.OutputContractJson,
                    SourceCode = "// Built-in compiled C# capability",
                    Status = "RELEASE_APPROVED",
                    ContentHash = capability.Manifest.ContentHash,
                    PromotedAt = DateTime.UtcNow
                };
                _knowledgeBase.SaveCapabilityVersion(rec);
            }
            catch { }
        }

        public ICapability? GetCapability(string id)
        {
            if (_capabilities.TryGetValue(id, out var cap))
            {
                return cap;
            }
            return null;
        }

        public IReadOnlyCollection<ICapability> GetAllCapabilities()
        {
            return _capabilities.Values.ToList().AsReadOnly();
        }

        public bool HasCapability(string id) => _capabilities.ContainsKey(id);

        public List<CapabilityVersionRecord> GetHistory(string capabilityId)
        {
            return _knowledgeBase.GetCapabilityHistory(capabilityId);
        }

        public bool Rollback(string capabilityId, string targetVersion)
        {
            return _knowledgeBase.RollbackCapability(capabilityId, targetVersion);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace UnityLocalizationStudio.Core.AiLab.CapabilitySdk
{
    public interface ICapability
    {
        string Id { get; }
        string Name { get; }
        string SemanticVersion { get; }
        CapabilityManifest Manifest { get; }

        CapabilityExecutionResult Execute(CapabilityExecutionContext context);
        Task<CapabilityExecutionResult> ExecuteAsync(CapabilityExecutionContext context, CancellationToken ct = default);
    }
}

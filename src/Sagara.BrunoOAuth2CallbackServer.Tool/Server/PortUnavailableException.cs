namespace Sagara.BrunoOAuth2CallbackServer.Tool.Server;

/// <summary>
/// Thrown when none of the candidate ports could be bound.
/// </summary>
internal sealed class PortUnavailableException(IReadOnlyList<int> candidatePorts)
    : Exception($"None of the candidate ports are available: {string.Join(", ", candidatePorts)}.")
{
    public IReadOnlyList<int> CandidatePorts { get; } = candidatePorts;
}

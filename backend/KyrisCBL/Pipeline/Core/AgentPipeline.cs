using KyrisCBL.Pipeline.Core;
using Microsoft.Extensions.Logging;

namespace KyrisCBL.Pipeline.Core;

/// <summary>
/// Executes a sequence of <see cref="IAgent"/>s in order, passing a shared
/// <see cref="AgentContext"/> through each step. Any agent may short-circuit the
/// pipeline by setting <see cref="AgentContext.IsComplete"/> = true.
/// </summary>
public sealed class AgentPipeline
{
    private readonly IReadOnlyList<IAgent> _agents;
    private readonly ILogger<AgentPipeline> _logger;

    public AgentPipeline(IEnumerable<IAgent> agents, ILogger<AgentPipeline> logger)
    {
        _agents = agents.ToList();
        _logger = logger;
    }

    /// <summary>Runs all agents in registration order and returns the populated context.</summary>
    public async Task<AgentContext> RunAsync(AgentContext context, CancellationToken ct = default)
    {
        foreach (var agent in _agents)
        {
            if (context.IsComplete)
            {
                _logger.LogDebug("Pipeline short-circuited after agent '{Agent}'.", agent.Name);
                break;
            }

            _logger.LogInformation("▶ Running agent: {Agent}", agent.Name);

            try
            {
                await agent.ExecuteAsync(context, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Pipeline cancelled during agent '{Agent}'.", agent.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent '{Agent}' threw an unhandled exception.", agent.Name);
                context.FinalResponse = "An unexpected error occurred. Please try again.";
                context.IsComplete = true;
            }
        }

        return context;
    }
}

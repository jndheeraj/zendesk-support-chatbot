namespace KyrisCBL.Pipeline.Core;

/// <summary>Represents a single step in the multi-level agent pipeline.</summary>
public interface IAgent
{
    string Name { get; }

    /// <summary>
    /// Executes this agent's logic. Agents may short-circuit the pipeline by setting
    /// <see cref="AgentContext.IsComplete"/> = true.
    /// </summary>
    Task ExecuteAsync(AgentContext context, CancellationToken ct = default);
}

using KyrisCBL.Pipeline.Core;
using KyrisCBL.Services;
using Microsoft.Extensions.Logging;

namespace KyrisCBL.Pipeline.Agents;

/// <summary>
/// Level-2 agent: retrieves relevant documents from the vector store when the intent
/// is knowledge-based (faq / other / rewards). Workflow intents skip this agent.
/// Populates <see cref="AgentContext.RetrievedDocuments"/>.
/// </summary>
public sealed class KnowledgeRetrievalAgent : IAgent
{
    public string Name => "KnowledgeRetrieval";

    // TODO: Replace with your own OpenAI Vector Store IDs
    private const string FaqStoreId     = "YOUR_FAQ_VECTOR_STORE_ID";
    private const string WebsiteStoreId = "YOUR_WEBSITE_VECTOR_STORE_ID";

    private static readonly HashSet<string> KnowledgeIntents =
        new(StringComparer.OrdinalIgnoreCase) { "faq", "other", "rewards" };

    private static readonly HashSet<string> WorkflowIntents =
        new(StringComparer.OrdinalIgnoreCase)
        { "reset_password", "unsubscribe", "do_not_sell", "update_profile" };

    private readonly RetrievalService _retrieval;
    private readonly ILogger<KnowledgeRetrievalAgent> _logger;

    public KnowledgeRetrievalAgent(RetrievalService retrieval, ILogger<KnowledgeRetrievalAgent> logger)
    {
        _retrieval = retrieval;
        _logger = logger;
    }

    public async Task ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var intent = context.DetectedIntent ?? "other";

        // Skip retrieval for workflow intents and small talk
        if (!KnowledgeIntents.Contains(intent))
        {
            _logger.LogDebug("[KnowledgeRetrieval] Skipped for intent '{Intent}'.", intent);
            return;
        }

        var storeId = intent.Equals("faq", StringComparison.OrdinalIgnoreCase)
            ? FaqStoreId
            : WebsiteStoreId;

        try
        {
            var docs = await _retrieval.SearchAsync(context.UserMessage, storeId, ct);
            context.RetrievedDocuments = docs;
            _logger.LogInformation("[KnowledgeRetrieval] Retrieved {Count} docs from store '{Store}'.", docs.Count, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[KnowledgeRetrieval] Retrieval failed; continuing without documents.");
        }
    }
}

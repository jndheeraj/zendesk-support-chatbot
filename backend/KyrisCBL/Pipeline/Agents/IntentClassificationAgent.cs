using KyrisCBL.Config;
using KyrisCBL.Pipeline.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.RegularExpressions;

namespace KyrisCBL.Pipeline.Agents;

/// <summary>
/// Level-1 agent: classifies the user's intent using a fast, cheap model (gpt-4o-mini).
/// Populates <see cref="AgentContext.DetectedIntent"/>, <see cref="AgentContext.EscalationRequested"/>,
/// and <see cref="AgentContext.ClassifierResponse"/>.
/// </summary>
public sealed class IntentClassificationAgent : IAgent
{
    public string Name => "IntentClassification";

    private readonly ChatClient _client;
    private readonly ILogger<IntentClassificationAgent> _logger;

    private const string ClassifierPrompt = @"
You are an intent classifier for a customer support chatbot.

Choose ONE intent from: reset_password, unsubscribe, do_not_sell, update_profile, rewards, faq, small_talk, other.

Rules:
  - Greetings / thanks → small_talk
  - Points / redeeming / loyalty → rewards
  - Knowledge about site / product / policy → faq (or other if unclear)
  - Clearly none of the above → other
  - Memory signals: clarify_attempts={clarify_attempts}, last_intent=""{last_intent}""
  - If clarify_attempts ≥ 2 and the message is still vague → set escalation: true
  - For reset_password / update_profile / unsubscribe / do_not_sell: if email is missing, ask for it instead of escalating

Return EXACTLY 4 lines (no extra text):
query: ""<echo the user's query>""
intent: ""<one_of_the_intents_above>""
escalation: <true|false>
response: ""<short helpful next step>""";

    public IntentClassificationAgent(IOptions<ChatbotSettings> settings, ILogger<IntentClassificationAgent> logger)
    {
        _client = new ChatClient(model: "gpt-4o-mini", apiKey: settings.Value.ApiKey);
        _logger = logger;
    }

    public async Task ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var memory = context.Memory;
        var prompt = ClassifierPrompt
            .Replace("{clarify_attempts}", memory.ClarifyAttempts.ToString())
            .Replace("{last_intent}", memory.LastIntent ?? "");

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(prompt),
            new SystemChatMessage($"has_email: {(context.UserEmail != null ? "true" : "false")}"),
            new UserChatMessage(context.UserMessage)
        };

        try
        {
            var result = await _client.CompleteChatAsync(
                messages,
                new ChatCompletionOptions { MaxOutputTokenCount = 256 },
                cancellationToken: ct);

            var raw = string.Concat(
                result?.Value?.Content?
                      .Select(p => p.Text)
                      .Where(t => !string.IsNullOrWhiteSpace(t))
                ?? Enumerable.Empty<string>());

            if (string.IsNullOrWhiteSpace(raw))
                raw = Fallback(context.UserMessage);

            _logger.LogInformation("[IntentClassification] Raw:\n{Raw}", raw);
            ParseIntoContext(raw, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IntentClassification] Failed; using fallback.");
            context.DetectedIntent = "other";
            context.EscalationRequested = false;
            context.ClassifierResponse = "Could you tell me a bit more so I can help you?";
        }
    }

    private static void ParseIntoContext(string raw, AgentContext ctx)
    {
        foreach (var line in raw.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            if (line.StartsWith("intent:", StringComparison.OrdinalIgnoreCase))
                ctx.DetectedIntent = Extract(line, "intent:").ToLowerInvariant();
            else if (line.StartsWith("escalation:", StringComparison.OrdinalIgnoreCase))
                ctx.EscalationRequested = Extract(line, "escalation:").Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (line.StartsWith("response:", StringComparison.OrdinalIgnoreCase))
                ctx.ClassifierResponse = Extract(line, "response:");
        }

        ctx.DetectedIntent ??= "other";
        ctx.ClassifierResponse ??= "How can I help you?";
    }

    private static string Extract(string line, string prefix) =>
        line[prefix.Length..].Trim().Trim('"');

    private static string Fallback(string query) =>
        $"query: \"{query}\"\nintent: \"other\"\nescalation: false\nresponse: \"We hit a temporary issue. Please try again.\"";
}

using KyrisCBL.Config;
using KyrisCBL.Models;
using KyrisCBL.Pipeline.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.RegularExpressions;

namespace KyrisCBL.Pipeline.Agents;

/// <summary>
/// Level-3 agent: generates the final user-facing response using a reasoning model (gpt-4o).
/// Uses the system prompt from disk and optionally grounds the answer in retrieved documents.
/// Skipped for small_talk (classifier response is sufficient) and workflow intents.
/// </summary>
public sealed class ResponseGenerationAgent : IAgent
{
    public string Name => "ResponseGeneration";

    private readonly ChatClient _client;
    private readonly string _systemPrompt;
    private readonly ILogger<ResponseGenerationAgent> _logger;

    private static readonly HashSet<string> WorkflowIntents =
        new(StringComparer.OrdinalIgnoreCase)
        { "reset_password", "unsubscribe", "do_not_sell", "update_profile" };

    public ResponseGenerationAgent(
        IOptions<ChatbotSettings> settings,
        IWebHostEnvironment env,
        ILogger<ResponseGenerationAgent> logger)
    {
        _client = new ChatClient(model: "gpt-4o", apiKey: settings.Value.ApiKey);
        _logger = logger;

        var promptPath = Path.Combine(env.ContentRootPath, "Data", "Prompts", "system_prompt.txt");
        _systemPrompt = File.Exists(promptPath)
            ? File.ReadAllText(promptPath)
            : "You are a helpful support assistant.";
    }

    public async Task ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var intent = context.DetectedIntent ?? "other";

        // Workflow intents are handled by WorkflowExecutionAgent; small_talk reuses classifier response
        if (WorkflowIntents.Contains(intent) || intent == "small_talk")
        {
            context.FinalResponse = context.ClassifierResponse ?? string.Empty;
            context.Solved = intent == "small_talk";
            return;
        }

        var hasEmail = !string.IsNullOrWhiteSpace(context.UserEmail);
        var memory   = context.Memory;

        var contextBlock = context.RetrievedDocuments.Count > 0
            ? $"\n\nRetrieved Documents (use as sources if relevant):\n{string.Join("\n\n---\n\n", context.RetrievedDocuments)}"
            : string.Empty;

        var ctx = $"""

            CONTEXT
            - has_email: {(hasEmail ? "true" : "false")}
            - intent: {intent}
            - clarify_attempts: {memory.ClarifyAttempts}
            - last_intent: {memory.LastIntent ?? ""}
            {contextBlock}

            Behavior
            - If an email is required and has_email=false, ask for it; do NOT escalate.
            - Never invent or echo the user's actual email.
            - Answer in EXACTLY this 4-line format:

            query: "<original user query>"
            intent: "<one_of: reset_password | unsubscribe | do_not_sell | update_profile | rewards | faq | other | small_talk>"
            escalation: <true|false>
            response: "<final answer to show the user>"
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(_systemPrompt),
            new SystemChatMessage(ctx),
            new UserChatMessage(context.UserMessage)
        };

        try
        {
            var result = await _client.CompleteChatAsync(
                messages,
                new ChatCompletionOptions { Temperature = 0.2f, MaxOutputTokenCount = 500 },
                cancellationToken: ct);

            var raw = result.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
            _logger.LogInformation("[ResponseGeneration] Raw:\n{Raw}", raw);

            var parsed = ParseStructured(raw);
            context.StructuredResponse = parsed;
            context.EscalationRequested = parsed.Escalation;
            context.FinalResponse = parsed.Response ?? context.ClassifierResponse ?? string.Empty;
            context.Solved = !parsed.Escalation && IsDefinitive(parsed.Response);

            ApplyClarificationPolicy(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResponseGeneration] Failed.");
            context.FinalResponse = context.ClassifierResponse ?? "We hit a temporary issue. Please try again.";
        }
    }

    private static GptStructuredResponse ParseStructured(string raw)
    {
        var result = new GptStructuredResponse();
        foreach (var line in raw.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            if (line.StartsWith("intent:", StringComparison.OrdinalIgnoreCase))
                result.Intent = line["intent:".Length..].Trim().Trim('"');
            else if (line.StartsWith("escalation:", StringComparison.OrdinalIgnoreCase))
                result.Escalation = line["escalation:".Length..].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (line.StartsWith("response:", StringComparison.OrdinalIgnoreCase))
                result.Response = line["response:".Length..].Trim().Trim('"');
        }
        if (string.IsNullOrWhiteSpace(result.Response))
            result.Response = raw;
        return result;
    }

    private static void ApplyClarificationPolicy(AgentContext ctx)
    {
        var response = ctx.FinalResponse;
        var isClarify = IsClarification(response);

        if (string.Equals(ctx.DetectedIntent, "other", StringComparison.OrdinalIgnoreCase) && isClarify)
        {
            ctx.Memory.ClarifyAttempts++;
            if (ctx.Memory.ClarifyAttempts >= 2)
                ctx.EscalationRequested = true;
        }
        else
        {
            ctx.Memory.ClarifyAttempts = 0;
        }

        ctx.Memory.LastIntent = ctx.DetectedIntent;
    }

    private static bool IsDefinitive(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s.Trim().EndsWith("?")) return false;
        return !Regex.IsMatch(s, @"\b(please|could you|would you|provide|share|tell me|what|which|when|how)\b",
                              RegexOptions.IgnoreCase);
    }

    private static bool IsClarification(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (Regex.IsMatch(s, @"\b(done|updated|sent|processed|created|confirmed)\b", RegexOptions.IgnoreCase)) return false;
        if (s.Trim().EndsWith("?")) return true;
        return Regex.IsMatch(s, @"\b(please|could you|would you|provide|share|tell me|what|which|when|how|can you)\b",
                             RegexOptions.IgnoreCase);
    }
}

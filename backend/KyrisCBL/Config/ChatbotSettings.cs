namespace KyrisCBL.Config;

/// <summary>
/// Configuration settings for the OpenAI integration.
/// Bind from the "OpenAI" section in appsettings.json.
/// </summary>
public sealed class ChatbotSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "text-embedding-3-small";
}

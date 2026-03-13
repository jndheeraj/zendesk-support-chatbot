using OpenAI.Embeddings;
using OpenAI;
using KyrisCBL.Config;

namespace KyrisCBL.Services.Embedding;

/// <summary>
/// Generates vector embeddings for text using OpenAI's embedding models.
/// Configured via <see cref="ChatbotSettings"/> (ApiKey + ModelName).
/// </summary>
public class EmbeddingsService
{
    private readonly EmbeddingClient _client;

    public EmbeddingsService(ChatbotSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ModelName))
            throw new ArgumentException("ModelName must be configured", nameof(settings));

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new ArgumentException("ApiKey must be configured", nameof(settings));

        _client = new EmbeddingClient(settings.ModelName, settings.ApiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Embedding input text must be non-empty.", nameof(text));

        var response = await _client.GenerateEmbeddingsAsync(new[] { text });

        var collection    = response.Value;
        var embeddingItem = collection[0];
        return embeddingItem.ToFloats().ToArray();
    }
}

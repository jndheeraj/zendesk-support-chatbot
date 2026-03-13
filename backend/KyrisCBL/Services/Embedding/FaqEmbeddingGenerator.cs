using KyrisCBL.Models;
using System.Text.Json;

namespace KyrisCBL.Services.Embedding;

/// <summary>
/// Generates and persists vector embeddings for FAQ items.
/// Reads faqs.json from Data/ and writes faqs_with_embeddings.json alongside it.
/// Trigger via POST /api/admin/faqadmin/generate-embeddings.
/// </summary>
public class FaqEmbeddingGenerator
{
    private readonly EmbeddingsService _embeddingService;
    private readonly IWebHostEnvironment _env;

    public FaqEmbeddingGenerator(EmbeddingsService embeddingService, IWebHostEnvironment env)
    {
        _embeddingService = embeddingService;
        _env = env;
    }

    public async Task GenerateEmbeddingsAsync()
    {
        string basePath   = Path.Combine(_env.ContentRootPath, "Data");
        string faqPath    = Path.Combine(basePath, "faqs.json");
        string outputPath = Path.Combine(basePath, "faqs_with_embeddings.json");

        if (!File.Exists(faqPath))
            throw new FileNotFoundException($"FAQ file not found at {faqPath}");

        var rawJson = await File.ReadAllTextAsync(faqPath);
        var faqs    = JsonSerializer.Deserialize<List<FaqItem>>(rawJson)
                      ?? throw new InvalidOperationException("Failed to deserialize faqs.json");

        foreach (var faq in faqs)
        {
            var vector = await _embeddingService.GetEmbeddingAsync(faq.Question);
            faq.Embedding = vector.ToList();
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(faqs, options));
    }
}

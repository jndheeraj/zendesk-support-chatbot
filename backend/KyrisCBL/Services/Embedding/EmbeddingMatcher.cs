using KyrisCBL.Helpers;
using KyrisCBL.Models;
using System.Text.Json;

namespace KyrisCBL.Services.Embedding;

/// <summary>
/// Finds the best-matching FAQ item for a user query using cosine similarity over pre-generated embeddings.
/// Loads the FAQ embeddings file from Data/ on first use.
/// </summary>
public class EmbeddingMatcher
{
    private readonly EmbeddingsService _embeddingService;
    private readonly IWebHostEnvironment _env;

    private List<FaqItem>? _faqItems;

    public EmbeddingMatcher(EmbeddingsService embeddingService, IWebHostEnvironment env)
    {
        _embeddingService = embeddingService;
        _env = env;
    }

    public async Task LoadFaqsAsync()
    {
        string path = Path.Combine(_env.ContentRootPath, "Data", "faqs_with_embeddings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Embedded FAQ file not found: {path}");

        var json = await File.ReadAllTextAsync(path);
        _faqItems = JsonSerializer.Deserialize<List<FaqItem>>(json);
    }

    public async Task<FaqItem?> FindBestMatchAsync(string userInput, double threshold = 0.83)
    {
        if (_faqItems == null)
            await LoadFaqsAsync();

        var userEmbedding = await _embeddingService.GetEmbeddingAsync(userInput);

        double bestScore = 0;
        FaqItem? bestMatch = null;

        foreach (var faq in _faqItems!)
        {
            if (faq.Embedding == null || faq.Embedding.Count == 0)
                continue;

            var score = CosineSimilarityHelper.Calculate(userEmbedding.ToList(), faq.Embedding);

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = faq;
            }
        }

        return bestScore >= threshold ? bestMatch : null;
    }
}

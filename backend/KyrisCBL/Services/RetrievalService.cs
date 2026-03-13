using System.Net.Http.Headers;
using System.Text.Json;

namespace KyrisCBL.Services
{
    public class RetrievalService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string? _project; // if you created the store under a project
        private readonly ILogger<RetrievalService> _logger;

        public RetrievalService(HttpClient http, IConfiguration cfg, ILogger<RetrievalService> logger)
        {
            _http = http;
            _apiKey = cfg["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey missing");
            _project = cfg["OpenAI:Project"]; // optional
            _logger = logger;
        }

        public async Task<List<string>> SearchAsync(
            string query,
            string vectorStoreId,
            CancellationToken ct = default)
        {
            // NOTE: vectorStoreId must look like "vs_..."
            var url = $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/search";

            var body = new
            {
                query,
                max_num_results = 5,
                rewrite_query = true,
                // filters = new { ... },            // optional
                // ranking_options = new {           // optional
                //     ranker = "auto",
                //     score_threshold = 0.55
                // }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(
                    body,
                    options: new JsonSerializerOptions { PropertyNamingPolicy = null }) // keep snake_case
            };

            // headers
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Some accounts still require this opt-in header; harmless if GA for you.
            req.Headers.TryAddWithoutValidation("OpenAI-Beta", "retrieval=v1");

            if (!string.IsNullOrWhiteSpace(_project))
                req.Headers.TryAddWithoutValidation("OpenAI-Project", _project);

            try
            {
                using var res = await _http.SendAsync(req, ct);
                var raw = await res.Content.ReadAsStringAsync(ct);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Retrieval search failed. Status={Status} Body={Body}", (int)res.StatusCode, raw);
                    res.EnsureSuccessStatusCode(); // throws with status
                }

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                var results = new List<string>();
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var part in content.EnumerateArray())
                            {
                                if (part.TryGetProperty("type", out var typeEl) &&
                                    string.Equals(typeEl.GetString(), "text", StringComparison.OrdinalIgnoreCase) &&
                                    part.TryGetProperty("text", out var textEl))
                                {
                                    var t = textEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(t)) results.Add(t!);
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("[Retrieval] {Count} chunks from store {StoreId} for \"{Query}\"",
                                       results.Count, vectorStoreId, query);
                return results;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("[Retrieval] Cancelled by caller.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Retrieval] Unexpected failure.");
                throw; // or return new List<string>() if you want to silently fall back
            }
        }
    }
}

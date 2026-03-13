using KyrisCBL.Services.Embedding;
using Microsoft.AspNetCore.Mvc;

namespace KyrisCBL.Controllers;

[Route("api/admin/[controller]")]
[ApiController]
public class FaqAdminController : ControllerBase
{
    /// <summary>
    /// Generates embeddings for all FAQ items and writes faqs_with_embeddings.json to Data/.
    /// Call this once after updating faqs.json.
    /// </summary>
    [HttpPost("generate-embeddings")]
    public async Task<IActionResult> GenerateFaqEmbeddings(
        [FromServices] FaqEmbeddingGenerator generator)
    {
        await generator.GenerateEmbeddingsAsync();
        return Ok("Embeddings generated successfully.");
    }
}

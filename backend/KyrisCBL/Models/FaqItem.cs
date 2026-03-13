using System.Text.Json.Serialization;

namespace KyrisCBL.Models
{
    public class FaqItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("answer")]
        public string Answer { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }
        public List<float> Embedding { get; set; } // Populated after embedding generation
    }
}

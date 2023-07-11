using System.Text.Json.Serialization;

namespace D4Companion.Entities
{
    public class Release
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("assets")]
        public List<Assets> Assets { get; set; } = new List<Assets>();
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
        [JsonPropertyName("tag_name")]
        public string Version { get; set; } = string.Empty;
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.MinValue;
        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; } = DateTime.MinValue;
    }

    public class Assets
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// zip: application/x-zip-compressed
        /// </summary>
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;
        [JsonPropertyName("size")]
        public int Size { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.MinValue;
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.MinValue;
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

    }
}
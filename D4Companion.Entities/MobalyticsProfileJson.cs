using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MobalyticsProfileJson
    {
        [JsonPropertyName("apollo")]
        public MobalyticsProfileApolloJson Apollo { get; set; } = new();
    }

    public class MobalyticsProfileApolloJson
    {
        [JsonPropertyName("graphql")]
        public Dictionary<string, object> Graphql { get; set; } = new();
    }

    public class MobalyticsProfileNgfDocumentAuthorJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class MobalyticsProfileDiablo4UserGeneratedDocumentJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("slugifiedName")]
        public string SlugifiedName { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public MobalyticsProfileDiablo4UserGeneratedDocumentDataJson Data { get; set; } = new();
    }

    public class MobalyticsProfileDiablo4UserGeneratedDocumentDataJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

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
        [JsonPropertyName("data")]
        public MobalyticsProfileDataJson Data { get; set; } = new();
    }

    public class MobalyticsProfileDataJson
    {
        [JsonPropertyName("game")]
        public MobalyticsProfileDataGameJson Game { get; set; } = new();
    }

    public class MobalyticsProfileDataGameJson
    {
        [JsonPropertyName("documents")]
        public MobalyticsProfileGameDocumentsJson Documents { get; set; } = new();

        [JsonPropertyName("profiles")]
        public MobalyticsProfileGameProfilesJson Profiles { get; set; } = new();
    }

    public class MobalyticsProfileGameDocumentsJson
    {
        [JsonPropertyName("userGeneratedDocuments")]
        public MobalyticsProfileUserGeneratedDocumentsJson UserGeneratedDocuments { get; set; } = new();
    }

    public class MobalyticsProfileUserGeneratedDocumentsJson
    {
        [JsonPropertyName("documents")]
        public List<MobalyticsProfileUserGeneratedDocumentsDocumentJson> Documents { get; set; } = new();
    }

    public class MobalyticsProfileUserGeneratedDocumentsDocumentJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public MobalyticsProfileDocumentsAuthorJson Author { get; set; } = new();

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public MobalyticsProfileDocumentsDataJson Data { get; set; } = new();
    }

    public class MobalyticsProfileDocumentsAuthorJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class MobalyticsProfileDocumentsDataJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class MobalyticsProfileGameProfilesJson
    {
        [JsonPropertyName("userProfile")]
        public MobalyticsProfileProfilesUserProfileJson UserProfile { get; set; } = new();
    }

    public class MobalyticsProfileProfilesUserProfileJson
    {
        [JsonPropertyName("profile")]
        public MobalyticsProfileUserProfileProfileJson Profile { get; set; } = new();
    }

    public class MobalyticsProfileUserProfileProfileJson
    {
        [JsonPropertyName("author")]
        public MobalyticsProfileProfileAuthorJson Author { get; set; } = new();
    }

    public class MobalyticsProfileProfileAuthorJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public MobalyticsProfileAuthorUserJson User { get; set; } = new();
    }

    public class MobalyticsProfileAuthorUserJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public List<MobalyticsProfileUserAttributeJson> Attributes { get; set; } = new();
    }

    public class MobalyticsProfileUserAttributeJson
    {
        /// <summary>
        /// Interested in the instance where key equals "ACCOUNT_NAME"
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}

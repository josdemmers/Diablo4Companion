using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class D2CoreBuildJson
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public D2CoreBuildDataString Data { get; set; } = new();
    }

    public class D2CoreBuildDataString
    {
        [JsonPropertyName("response_data")]
        public string ResponseData { get; set; } = string.Empty;
    }

    public class D2CoreBuildDataRootJson
    {
        [JsonPropertyName("data")]
        public D2CoreBuildDataJson Data { get; set; } = new();
    }

    public class D2CoreBuildDataJson
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("_createTime")]
        public long CreateTime { get; set; }

        [JsonPropertyName("variants")]
        public List<D2CoreBuildDataVariantJson> Variants { get; set; } = [];

        [JsonPropertyName("_updateTime")]
        public long UpdateTime { get; set; }
    }

    public class D2CoreBuildDataVariantJson
    {
        [JsonPropertyName("gear")]
        public Dictionary<string, D2CoreBuildVariantGearJson> Gear { get; set; } = [];

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("paragon")]
        public Dictionary<string, D2CoreBuildDataVariantParagonJson> Paragon { get; set; } = [];
    }

    public class D2CoreBuildVariantGearJson
    {
        [JsonPropertyName("itemType")]
        public string ItemType { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("mods")]
        public List<D2CoreBuildVariantGearModJson> Mods { get; set; } = [];

        [JsonPropertyName("sockets")]
        public List<D2CoreBuildVariantGearSocketJson> Sockets { get; set; } = [];

        /// <summary>
        /// Quality type of the gear (i.e., "rare", "legendary", "uniqueItem").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class D2CoreBuildVariantGearModJson
    {
        [JsonPropertyName("greater")]
        public bool Greater { get; set; } = false;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class D2CoreBuildVariantGearSocketJson
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Socket type (i.e., "gem", "rune").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class D2CoreBuildDataVariantParagonJson
    {
        [JsonPropertyName("data")]
        public List<string> Data { get; set; } = [];

        [JsonPropertyName("glyph")]
        public Dictionary<string, string> Glyph { get; set; } = [];

        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("rotate")]
        public int Rotate { get; set; } = 0;
    }
}
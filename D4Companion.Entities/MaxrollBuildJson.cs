using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MaxrollBuildJson
    {
        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class MaxrollBuildDataJson
    {
        [JsonPropertyName("profiles")]
        public List<MaxrollBuildDataProfileJson> Profiles { get; set; } = new();

        [JsonPropertyName("items")]
        public Dictionary<int, MaxrollBuildDataItemJson> Items { get; set; } = new();
    }

    public class MaxrollBuildDataProfileJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public Dictionary<int, int> Items { get; set; } = new(); // <itemslot, item>
    }

    public class MaxrollBuildDataItemJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("explicits")]
        public List<MaxrollBuildDataItemExplicitJson> Explicits { get; set; } = new();

        [JsonPropertyName("implicits")]
        public List<MaxrollBuildDataItemImplicitJson> Implicits { get; set; } = new();

        [JsonPropertyName("tempered")]
        public List<MaxrollBuildDataItemTemperedJson> Tempered { get; set; } = new();

        [JsonPropertyName("legendaryPower")]
        public MaxrollBuildDataItemLegendaryPowerJson LegendaryPower { get; set; } = new();
    }

    public class MaxrollBuildDataItemExplicitJson
    {
        [JsonPropertyName("nid")]
        public int Nid { get; set; }
        [JsonPropertyName("greater")]
        public bool Greater { get; set; } = false;
    }

    public class MaxrollBuildDataItemImplicitJson
    {
        [JsonPropertyName("nid")]
        public int Nid { get; set; }
    }

    public class MaxrollBuildDataItemTemperedJson
    {
        [JsonPropertyName("nid")]
        public int Nid { get; set; }
    }

    public class MaxrollBuildDataItemLegendaryPowerJson
    {
        [JsonPropertyName("nid")]
        public int Nid { get; set; }
    }
}
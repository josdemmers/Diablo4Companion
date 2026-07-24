using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace D4Companion.Entities
{
    public class InfinityBuildsContainerJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("children")]
        public List<object> Children { get; set; } = [];
    }

    public class InfinityBuildsWrapperJson
    {
        [JsonPropertyName("build")]
        public InfinityBuildsBuildJson Build { get; set; } = new();
    }

    public class InfinityBuildsBuildJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("shareSlug")]
        public string ShareSlug { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;

        [JsonPropertyName("variants")]
        public List<InfinityBuildsBuildVariantJson> Variants { get; set; } = [];
    }

    public class InfinityBuildsBuildVariantJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("gear")]
        public List<InfinityBuildsBuildVariantGearJson> Gear { get; set; } = [];

        [JsonPropertyName("paragon")]
        public InfinityBuildsBuildParagonJson Paragon { get; set; } = new();
    }

    public class InfinityBuildsBuildVariantGearJson
    {
        /// <summary>
        /// custom_legendary, mythic, unique.
        /// </summary>
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("slot")]
        public string Slot { get; set; } = string.Empty;

        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("affixes")]
        public List<InfinityBuildsBuildVariantAffixJson> Affixes { get; set; } = [];

        [JsonPropertyName("sockets")]
        public List<string> Sockets { get; set; } = [];

        [JsonPropertyName("aspectId")]
        public string AspectId { get; set; } = string.Empty;

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;
    }

    public class InfinityBuildsBuildVariantAffixJson
    {
        [JsonPropertyName("affixId")]
        public string AffixId { get; set; } = string.Empty;

        [JsonPropertyName("greater")]
        public bool Greater { get; set; } = false;

        [JsonPropertyName("swapped")]
        public bool Swapped { get; set; } = false;

        [JsonPropertyName("tempered")]
        public bool Tempered { get; set; } = false;
    }

    public class InfinityBuildsBuildParagonJson
    {
        [JsonPropertyName("slots")]
        public List<InfinityBuildsBuildParagonSlotJson> Slots { get; set; } = [];

        [JsonPropertyName("glyphs")]
        public Dictionary<string, string> Glyphs { get; set; } = [];

        [JsonPropertyName("activeNodes")]
        public List<string> ActiveNodes { get; set; } = [];
    }

    public class InfinityBuildsBuildParagonSlotJson
    {
        [JsonPropertyName("boardId")]
        public string BoardId { get; set; } = string.Empty;

        [JsonPropertyName("rotation")]
        public int Rotation { get; set; } = 0;

        [JsonPropertyName("selfGateId")]
        public string SelfGateId { get; set; } = string.Empty;

        [JsonPropertyName("parentGateId")]
        public string ParentGateId { get; set; } = string.Empty;
    }
}

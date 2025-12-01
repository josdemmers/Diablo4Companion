using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MobalyticsBuildJson
    {
        [JsonPropertyName("data")]
        public MobalyticsBuildDataJson Data { get; set; } = new();
    }

    public class MobalyticsBuildDataJson
    {
        [JsonPropertyName("game")]
        public MobalyticsBuildDataGameJson Game { get; set; } = new();
    }

    public class MobalyticsBuildDataGameJson
    {
        [JsonPropertyName("documents")]
        public MobalyticsBuildGameDocumentsJson Documents { get; set; } = new();
    }

    public class MobalyticsBuildGameDocumentsJson
    {
        [JsonPropertyName("userGeneratedDocumentById")]
        public MobalyticsBuildUserGeneratedDocumentByIdJson UserGeneratedDocumentById { get; set; } = new();
    }

    public class MobalyticsBuildUserGeneratedDocumentByIdJson
    {
        [JsonPropertyName("data")]
        public MobalyticsBuildUserGeneratedDocumentByIdDataJson Data { get; set; } = new();
    }

    public class MobalyticsBuildUserGeneratedDocumentByIdDataJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public MobalyticsBuildUserGeneratedDocumentByIdDataDataJson Data { get; set; } = new();

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;

        [JsonPropertyName("firstPublishedAt")]
        public string FirstPublishedAt { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<MobalyticsBuildUserGeneratedDocumentByIdDataContentJson> Content { get; set; } = new();
    }

    public class MobalyticsBuildUserGeneratedDocumentByIdDataDataJson
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("buildVariants")]
        public MobalyticsBuildDataBuildVariantsJson BuildVariants { get; set; } = new();
    }

    public class MobalyticsBuildDataBuildVariantsJson
    {
        [JsonPropertyName("values")]
        public List<MobalyticsBuildDataBuildVariantJson> values { get; set; } = new();
    }

    public class MobalyticsBuildDataBuildVariantJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("genericBuilder")]
        public MobalyticsBuildBuildVariantGenericBuilderJson GenericBuilder { get; set; } = new();

        [JsonPropertyName("paragon")]
        public MobalyticsBuildBuildVariantParagonJson Paragon { get; set; } = new();
    }

    public class MobalyticsBuildBuildVariantGenericBuilderJson
    {
        [JsonPropertyName("slots")]
        public List<MobalyticsBuildGenericBuilderSlotJson> Slots { get; set; } = new();
    }

    public class MobalyticsBuildGenericBuilderSlotJson
    {
        [JsonPropertyName("gameSlotSlug")]
        public string GameSlotSlug { get; set; } = string.Empty;

        [JsonPropertyName("gameEntity")]
        public MobalyticsBuildSlotGameEntityJson GameEntity { get; set; } = new();
    }

    public class MobalyticsBuildSlotGameEntityJson
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("modifiers")]
        public MobalyticsBuildGameEntityModifiersJson Modifiers { get; set; } = new();
    }

    public class MobalyticsBuildGameEntityModifiersJson
    {
        [JsonPropertyName("gearStats")]
        public List<MobalyticsBuildModifiersGearStatJson> GearStats { get; set; } = new();

        [JsonPropertyName("implicitStats")]
        public List<MobalyticsBuildModifiersImplicitStatJson> ImplicitStats { get; set; } = new();

        [JsonPropertyName("socketStats")]
        public List<MobalyticsBuildModifiersSocketStatJson> SocketStats { get; set; } = new();

        [JsonPropertyName("temperingStats")]
        public List<MobalyticsBuildModifiersTemperingStatJson> TemperingStats { get; set; } = new();
    }

    public class MobalyticsBuildModifiersGearStatJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("isGreater")]
        public bool IsGreater { get; set; } = false;
    }

    public class MobalyticsBuildModifiersImplicitStatJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class MobalyticsBuildModifiersSocketStatJson
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Type of the socket (e.g. "gems", "runes")
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class MobalyticsBuildModifiersTemperingStatJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class MobalyticsBuildBuildVariantParagonJson
    {
        [JsonPropertyName("nodes")]
        public List<MobalyticsBuildParagonNodeJson> Nodes { get; set; } = new();

        [JsonPropertyName("boards")]
        public List<MobalyticsBuildParagonBoardJson> Boards { get; set; } = new();
    }

    public class MobalyticsBuildParagonNodeJson
    {
        /// <summary>
        /// e.g. "barbarian-starting-board-x11-y14"
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
    }

    public class MobalyticsBuildParagonBoardJson
    {
        [JsonPropertyName("x")]
        public int X { get; set; } = 0;

        [JsonPropertyName("y")]
        public int Y { get; set; } = 0;

        [JsonPropertyName("rotation")]
        public int Rotation { get; set; } = 0;

        [JsonPropertyName("board")]
        public MobalyticsBuildBoardBoardJson Board { get; set; } = new();

        [JsonPropertyName("glyph")]
        public MobalyticsBuildBoardGlyphJson Glyph { get; set; } = new();

        [JsonPropertyName("glyphLevel")]
        public int GlyphLevel { get; set; } = 0;
    }

    public class MobalyticsBuildBoardBoardJson
    {
        /// <summary>
        /// e.g. "barbarian-starting-board"
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
    }

    public class MobalyticsBuildBoardGlyphJson
    {
        /// <summary>
        /// e.g. "barbarian-mortal-draw"
        /// </summary>
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
    }

    public class MobalyticsBuildUserGeneratedDocumentByIdDataContentJson
    {
        /// <summary>
        /// Type of "NgfDocumentCmWidgetContentVariantsV1" contains the build variants.
        /// </summary>
        [JsonPropertyName("__typename")]
        public string Typename { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public MobalyticsBuildContentDataJson Data { get; set; } = new();
    }

    public class MobalyticsBuildContentDataJson
    {
        /// <summary>
        /// Only interested in "Build Variants".
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("childrenVariants")]
        public List<MobalyticsBuildContentDataChildrenVariantJson> ChildrenVariants { get; set; } = new();
    }

    public class MobalyticsBuildContentDataChildrenVariantJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class MobalyticsBuildWrapperJson
    {
        [JsonPropertyName("apollo")]
        public MobalyticsBuildWrapperApolloJson Apollo { get; set; } = new();
    }
    
    public class MobalyticsBuildWrapperApolloJson
    {
        [JsonPropertyName("graphqlV2")]
        public MobalyticsBuildWrapperGraphqlV2Json GraphqlV2 { get; set; } = new();
    }

    public class MobalyticsBuildWrapperGraphqlV2Json
    {
        [JsonPropertyName("queries")]
        public List<MobalyticsBuildWrapperQuery> Queries { get; set; } = [];
    }

    public class MobalyticsBuildWrapperQuery
    {
        [JsonPropertyName("state")]
        public MobalyticsBuildWrapperQueryState State { get; set; } = new();
    }

    public class MobalyticsBuildWrapperQueryState
    {
        [JsonPropertyName("data")]
        //public List<MobalyticsBuildWrapperQueryData> Data { get; set; } = [];
        public object Data { get; set; } = new();
    }

    public class MobalyticsBuildWrapperQueryData
    {
        [JsonPropertyName("game")]
        public MobalyticsBuildWrapperGame Game { get; set; } = new();
    }

    public class MobalyticsBuildWrapperGame
    {
        [JsonPropertyName("documents")]
        public MobalyticsBuildWrapperDocuments Documents { get; set; } = new();
    }

    public class MobalyticsBuildWrapperDocuments
    {
        [JsonPropertyName("userGeneratedDocumentBySlug")]
        public MobalyticsBuildUserGeneratedDocumentByIdJson UserGeneratedDocumentBySlug { get; set; } = new();
        
        [JsonPropertyName("userGeneratedDocumentBySlugifiedName")]
        public MobalyticsBuildUserGeneratedDocumentByIdJson UserGeneratedDocumentBySlugifiedName { get; set; } = new();
    }
}

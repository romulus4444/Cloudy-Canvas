namespace Cloudy_Canvas.Service
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    // Just the parts of the Philomena (Manebooru) JSON API responses that the bot reads.
    // Collections can come back as JSON null, so they are treated as possibly null by the code that reads them.
    internal sealed class SearchResponse
    {
        [JsonPropertyName("images")]
        public List<ImageDto> Images { get; set; }

        [JsonPropertyName("total")]
        public long Total { get; set; }
    }

    internal sealed class ImageDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("spoilered")]
        public bool Spoilered { get; set; }

        [JsonPropertyName("tag_ids")]
        public List<long> TagIds { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    internal sealed class FeaturedResponse
    {
        [JsonPropertyName("image")]
        public ImageDto Image { get; set; }
    }

    internal sealed class FilterResponse
    {
        [JsonPropertyName("filter")]
        public FilterDto Filter { get; set; }
    }

    internal sealed class FilterDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("spoilered_tag_ids")]
        public List<long> SpoileredTagIds { get; set; }
    }

    internal sealed class TagSearchResponse
    {
        [JsonPropertyName("tags")]
        public List<TagDto> Tags { get; set; }
    }

    internal sealed class TagDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}

namespace Cloudy_Canvas.Settings
{
    using Newtonsoft.Json;

    /// <summary>A channel that uses its own filter instead of the server's default one.</summary>
    public class ChannelFilter
    {
        public ChannelFilter()
        {
        }

        public ChannelFilter(ulong channelId, int filterId)
        {
            ChannelId = channelId;
            FilterId = filterId;
        }

        public ulong ChannelId { get; set; }

        public int FilterId { get; set; }

        // Settings files written by earlier versions stored this as a Tuple<ulong, int> (Item1 / Item2).
        // These setter-only properties are read for compatibility and never written.
        [JsonProperty("Item1")]
        private ulong LegacyChannelId
        {
            set => ChannelId = value;
        }

        [JsonProperty("Item2")]
        private int LegacyFilterId
        {
            set => FilterId = value;
        }

        public void Deconstruct(out ulong channelId, out int filterId)
        {
            channelId = ChannelId;
            filterId = FilterId;
        }
    }
}

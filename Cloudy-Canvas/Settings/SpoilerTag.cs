namespace Cloudy_Canvas.Settings
{
    using Newtonsoft.Json;

    /// <summary>A tag that the server's filter spoilers.</summary>
    public class SpoilerTag
    {
        public SpoilerTag()
        {
            Name = string.Empty;
        }

        public SpoilerTag(long id, string name)
        {
            Id = id;
            Name = name;
        }

        public long Id { get; set; }

        public string Name { get; set; }

        // Settings files written by earlier versions stored this as a Tuple<long, string> (Item1 / Item2).
        // These setter-only properties are read for compatibility and never written.
        [JsonProperty("Item1")]
        private long LegacyId
        {
            set => Id = value;
        }

        [JsonProperty("Item2")]
        private string LegacyName
        {
            set => Name = value;
        }

        public void Deconstruct(out long id, out string name)
        {
            id = Id;
            name = Name;
        }
    }
}

namespace Cloudy_Canvas.Service
{
    using System;
    using System.Collections.Generic;

    /// <summary>How a request to the booru turned out.</summary>
    public enum BooruStatus
    {
        /// <summary>The request worked and found what was asked for.</summary>
        Ok,

        /// <summary>The request worked but nothing matched (no results, unknown or filtered image, unknown filter).</summary>
        NotFound,

        /// <summary>The site answered with an HTTP error status; <see cref="BooruResult.HttpCode"/> says which.</summary>
        HttpError,

        /// <summary>The site could not be reached or gave an unusable answer (network error, timeout, bad JSON).</summary>
        Unavailable,
    }

    /// <summary>The outcome of an image lookup. Which fields are filled in depends on the lookup; see <see cref="BooruService"/>.</summary>
    public sealed class BooruResult
    {
        public BooruStatus Status { get; init; }

        /// <summary>The HTTP status code when <see cref="Status"/> is <see cref="BooruStatus.HttpError"/>.</summary>
        public int? HttpCode { get; init; }

        public long ImageId { get; init; } = -1;

        /// <summary>Number of images that matched the search.</summary>
        public long Total { get; init; }

        /// <summary>Whether the server's filter spoilers this image.</summary>
        public bool Spoilered { get; init; }

        /// <summary>The names of the spoilered tags on the image.</summary>
        public IReadOnlyList<string> SpoilerTags { get; init; } = Array.Empty<string>();

        /// <summary>The image's tags that are not spoilered (only filled in by the tag lookup).</summary>
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

        /// <summary>True when the site could not give an answer at all (an HTTP error or unreachable).</summary>
        public bool Failed => Status is BooruStatus.HttpError or BooruStatus.Unavailable;

        public static BooruResult NotFound() => new() { Status = BooruStatus.NotFound };
    }

    /// <summary>The outcome of checking that a filter exists and is usable.</summary>
    public sealed record FilterCheckResult(BooruStatus Status, int? HttpCode, int FilterId);

    /// <summary>The outcome of fetching the spoilered tags of a filter.</summary>
    public sealed record SpoilerListResult(BooruStatus Status, int? HttpCode, IReadOnlyList<Settings.SpoilerTag> Tags);
}

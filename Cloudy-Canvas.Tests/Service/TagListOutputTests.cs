namespace Cloudy_Canvas.Tests.Service
{
    using Cloudy_Canvas.Modules;
    using Xunit;

    public class TagListOutputTests
    {
        [Fact]
        public void TagsAreGroupedAndSorted()
        {
            var output = BooruModule.SetupTagListOutput(new[]
            {
                "safe", "species:earth pony", "artist:zed", "character:twilight sparkle", "oc", "artist:alpha", "editor:eddy", "episode:s01e01",
            });

            Assert.Equal(
                "`artist:alpha`, `artist:zed`, `editor:eddy`, `character:twilight sparkle`, `species:earth pony`, `episode:s01e01`, `oc`, `safe`",
                output);
        }

        [Fact]
        public void NoTagsGivesAnEmptyString()
        {
            Assert.Equal(string.Empty, BooruModule.SetupTagListOutput(new string[0]));
        }

        [Fact]
        public void TheInputListIsNotModified()
        {
            var tags = new System.Collections.Generic.List<string> { "b", "a" };

            BooruModule.SetupTagListOutput(tags);

            Assert.Equal(new[] { "b", "a" }, tags);
        }
    }
}

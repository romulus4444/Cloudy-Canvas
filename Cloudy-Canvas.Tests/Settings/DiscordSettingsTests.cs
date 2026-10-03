namespace Cloudy_Canvas.Tests.Settings
{
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class DiscordSettingsTests
    {
        [Fact]
        public void ThereIsNoPrefixOverrideByDefault()
        {
            Assert.Null(new DiscordSettings().PrefixOverride);
            using var provider = TestServices.Build();

            Assert.Null(provider.GetRequiredService<IOptions<DiscordSettings>>().Value.PrefixOverride);
        }

        [Theory]
        [InlineData("?", '?')]
        [InlineData("!", '!')]
        [InlineData("$", '$')]
        public void ThePrefixOverrideComesFromConfiguration(string configured, char expected)
        {
            using var provider = TestServices.Build(("DiscordSettings:PrefixOverride", configured));

            Assert.Equal(expected, provider.GetRequiredService<IOptions<DiscordSettings>>().Value.PrefixOverride);
        }

        [Fact]
        public void AnEmptyPrefixOverrideMeansNoOverride()
        {
            using var provider = TestServices.Build(("DiscordSettings:PrefixOverride", string.Empty));

            Assert.Null(provider.GetRequiredService<IOptions<DiscordSettings>>().Value.PrefixOverride);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("7")]
        [InlineData(" ")]
        [InlineData("@")]
        [InlineData("#")]
        [InlineData("<")]
        [InlineData("`")]
        public void APrefixThatCouldNotBeSetWithSetprefixIsRejectedAtStartup(string configured)
        {
            using var provider = TestServices.Build(("DiscordSettings:PrefixOverride", configured));

            var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DiscordSettings>>().Value);
            Assert.Contains("PrefixOverride", error.Message);
        }

        [Fact]
        public void TheOverrideWinsOverTheServersPrefix()
        {
            Assert.Equal('?', DiscordHelper.EffectivePrefix(';', '?'));
        }

        [Fact]
        public void WithoutAnOverrideTheServersPrefixIsUsed()
        {
            Assert.Equal(';', DiscordHelper.EffectivePrefix(';', null));
            Assert.Equal('!', DiscordHelper.EffectivePrefix('!', null));
        }
    }
}

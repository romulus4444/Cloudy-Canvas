namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for interfacing with Manebooru")]
    public class BooruModule : BotModuleBase
    {
        private static readonly string[] TagGroupPrefixes = { "artist:", "editor:", "character:", "species:", "episode:" };

        // Each of these commands costs a request to Manebooru, so one user can only run them this often.
        private static readonly TimeSpan CommandCooldown = TimeSpan.FromSeconds(2);

        private readonly BooruService _booru;
        private readonly LoggingService _logger;
        private readonly MixinsService _mixins;
        private readonly CooldownService _cooldown;

        public BooruModule(BooruService booru, LoggingService logger, MixinsService mixins, CooldownService cooldown)
        {
            _booru = booru;
            _logger = logger;
            _mixins = mixins;
            _cooldown = cooldown;
        }

        [Command("pick", RunMode = RunMode.Async)]
        [Summary("Selects an image at random")]
        public Task PickCommandAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            return SearchCommandAsync("pick", query, _booru.GetRandomImageByQueryAsync);
        }

        [Command("pickrecent", RunMode = RunMode.Async)]
        [Summary("Selects first image in a search")]
        public Task PickRecentCommandAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            return SearchCommandAsync("pickrecent", query, _booru.GetFirstRecentImageByQueryAsync);
        }

        [Command("id", RunMode = RunMode.Async)]
        [Summary("Selects an image by image id")]
        public async Task IdCommandAsync([Summary("The image Id")] long id = 4010266)
        {
            var settings = await LoadAllowedSettingsAsync();
            if (settings == null)
            {
                return;
            }

            var (filterId, usesChannelFilter) = ResolveFilter(settings);
            if (!usesChannelFilter && !await CheckBadlistsAsync("id", id.ToString(), settings))
            {
                return;
            }

            var result = await _booru.GetImageByIdAsync(id, settings, filterId);
            if (await ReplyIfFailedAsync("id", id.ToString(), result))
            {
                return;
            }

            if (result.Status == BooruStatus.NotFound)
            {
                await ReplyAsync("I could not find that image.");
                await _logger.Log($"id: requested {id}, NOT FOUND", Context);
                return;
            }

            await ReplyWithImageAsync("id", $"requested {id}, found {result.ImageId}", result);
        }

        [Command("tags", RunMode = RunMode.Async)]
        [Summary("Selects a tag list by image Id")]
        public async Task TagsCommandAsync([Summary("The image Id")] long id = 4010266)
        {
            var settings = await LoadAllowedSettingsAsync();
            if (settings == null)
            {
                return;
            }

            var (filterId, usesChannelFilter) = ResolveFilter(settings);
            if (!usesChannelFilter && !await CheckBadlistsAsync("tags", id.ToString(), settings))
            {
                return;
            }

            var result = await _booru.GetImageTagsIdAsync(id, settings, filterId);
            if (await ReplyIfFailedAsync("tags", id.ToString(), result))
            {
                return;
            }

            if (result.Status == BooruStatus.NotFound)
            {
                await ReplyAsync("I could not find that image.");
                await _logger.Log($"tags: requested {id}, NOT FOUND", Context);
                return;
            }

            var tagStrings = SetupTagListOutput(result.Tags);
            var output = $"Image #{id} has the tags {tagStrings}";
            if (result.Spoilered)
            {
                var spoilerStrings = SetupTagListOutput(result.SpoilerTags);
                output += $" including the spoiler tags {spoilerStrings}";
                await _logger.Log($"tags: requested {id}, found {tagStrings} SPOILERED {spoilerStrings}", Context);
            }
            else
            {
                await _logger.Log($"tags: requested {id}, found {tagStrings}", Context);
            }

            await ReplyAsync(output);
        }

        [Command("getspoilers", RunMode = RunMode.Async)]
        [Summary("Gets the list of spoiler tags")]
        public async Task GetSpoilersCommandAsync()
        {
            var settings = await LoadAllowedSettingsAsync(callsBooru: false);
            if (settings == null)
            {
                return;
            }

            var output = $"__Spoilered tags for Filter {settings.DefaultFilterId}:__{Environment.NewLine}";
            output += string.Join(", ", settings.SpoilerList.Select(tag => $"`{tag.Name}`"));

            await _logger.Log("getspoilers", Context);
            await ReplyAsync(output);
        }

        [Command("featured", RunMode = RunMode.Async)]
        [Summary("Selects the current Featured Image on Manebooru")]
        public async Task FeaturedCommandAsync()
        {
            var settings = await LoadAllowedSettingsAsync();
            if (settings == null)
            {
                return;
            }

            var (filterId, _) = ResolveFilter(settings);
            var result = await _booru.GetFeaturedImageIdAsync(settings, filterId);
            if (await ReplyIfFailedAsync("featured", null, result))
            {
                return;
            }

            if (result.Status == BooruStatus.NotFound)
            {
                await _logger.Log("featured: FILTERED", Context);
                await ReplyAsync("The Featured Image has been filtered!");
                return;
            }

            await _logger.Log("featured", Context);
            await ReplyWithImageAsync("featured", $"found {result.ImageId}", result);
        }

        [Command("report", RunMode = RunMode.Async)]
        [Summary("Reports an image id to the admin channel")]
        public async Task ReportAsync(long reportedImageId, [Remainder] string reason = "")
        {
            if (Context.Guild == null)
            {
                await ReplyAsync("Reports can only be made from a server.");
                return;
            }

            var settings = await LoadAllowedSettingsAsync();
            if (settings == null)
            {
                return;
            }

            var (filterId, usesChannelFilter) = ResolveFilter(settings);
            if (!usesChannelFilter && BadlistHelper.CheckWatchList(reportedImageId.ToString(), settings) != "")
            {
                await ReplyAsync("That image is already blocked.");
                return;
            }

            var result = await _booru.GetImageByIdAsync(reportedImageId, settings, filterId);
            if (await ReplyIfFailedAsync("report", reportedImageId.ToString(), result))
            {
                return;
            }

            if (result.Status == BooruStatus.NotFound)
            {
                await ReplyAsync("I could not find that image.");
                return;
            }

            var reportChannel = Context.Guild.GetTextChannel(settings.ReportChannel);
            if (reportChannel == null)
            {
                await ReplyAsync("I can't find the channel where reports go, so I couldn't pass this on. Please tell an admin.");
                await _logger.Log($"report: {reportedImageId} <FAIL> report channel {settings.ReportChannel} not found", Context, true);
                return;
            }

            var output = $"<@{Context.User.Id}> has reported Image #{reportedImageId}";
            if (reason != "")
            {
                // Backticks are swapped out so the reason can't break out of its code span.
                output += $" with reason `{reason.Replace('`', '\'')}`";
            }

            output += $" || <https://manebooru.art/images/{result.ImageId}> ||";
            await _logger.Log($"report: {reportedImageId} <SUCCESS>", Context, true);
            if (settings.ReportRole != 0)
            {
                output = $"<@&{settings.ReportRole}> " + output;
            }

            await reportChannel.SendMessageAsync(output, allowedMentions: AlertMentions(settings.ReportRole));
            await ReplyAsync("Admins have been notified. Thank you for your report.");
        }

        /// <summary>Formats tags as a comma separated list of code spans: artists, editors, characters, species and episodes first.</summary>
        public static string SetupTagListOutput(IEnumerable<string> tags)
        {
            var sorted = tags.ToList();
            sorted.Sort();
            var ordered = TagGroupPrefixes
                .SelectMany(prefix => sorted.Where(tag => tag.StartsWith(prefix)))
                .Concat(sorted.Where(tag => !TagGroupPrefixes.Any(prefix => tag.StartsWith(prefix))));
            return string.Join(", ", ordered.Select(tag => $"`{tag}`"));
        }

        private async Task SearchCommandAsync(string label, string query, Func<string, ServerSettings, int, Task<BooruResult>> search)
        {
            var settings = await LoadAllowedSettingsAsync();
            if (settings == null)
            {
                return;
            }

            query = _mixins.Transpile(query);

            var (filterId, usesChannelFilter) = ResolveFilter(settings);
            if (!usesChannelFilter && !await CheckBadlistsAsync(label, query, settings))
            {
                return;
            }

            var result = await search(query, settings, filterId);
            if (await ReplyIfFailedAsync(label, query, result))
            {
                return;
            }

            if (result.Status == BooruStatus.NotFound)
            {
                await _logger.Log($"{label}: {query}, total: 0", Context);
                await ReplyAsync("I could not find any images with that query.");
                return;
            }

            var totalString = $"[{result.Total} result{(result.Total == 1 ? string.Empty : "s")}] [Id# {result.ImageId}] ";
            if (result.Spoilered)
            {
                var spoilerStrings = SetupTagListOutput(result.SpoilerTags);
                var output = totalString + $"Spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{result.ImageId} ||";
                await _logger.Log($"{label}: {query}, total: {result.Total} result: {result.ImageId} SPOILERED {spoilerStrings}", Context);
                await ReplyAsync(output);
            }
            else
            {
                await _logger.Log($"{label}: {query}, total: {result.Total} result: {result.ImageId}", Context);
                await ReplyAsync(totalString + $"https://manebooru.art/images/{result.ImageId}");
            }
        }

        /// <summary>The reply for a single image found by id or as the featured image.</summary>
        private async Task ReplyWithImageAsync(string label, string logDetail, BooruResult result)
        {
            if (result.Spoilered)
            {
                var spoilerStrings = SetupTagListOutput(result.SpoilerTags);
                var output = $"[Id# {result.ImageId}] Result is a spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{result.ImageId} ||";
                await _logger.Log($"{label}: {logDetail} SPOILERED {spoilerStrings}", Context);
                await ReplyAsync(output);
            }
            else
            {
                await _logger.Log($"{label}: {logDetail}", Context);
                await ReplyAsync($"[Id# {result.ImageId}] https://manebooru.art/images/{result.ImageId}");
            }
        }

        /// <summary>
        /// Loads the server's settings, or returns null if the command should stop: the user isn't allowed to run commands here, or
        /// (for commands that call the booru) they ran one too recently.
        /// </summary>
        private async Task<ServerSettings> LoadAllowedSettingsAsync(bool callsBooru = true)
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return null;
            }

            if (callsBooru)
            {
                var cooldown = _cooldown.Check(Context.User.Id, CommandCooldown);
                if (!cooldown.Allowed)
                {
                    if (cooldown.ShouldNotify)
                    {
                        await ReplyAsync("Slow down a little, I'll be ready again in a moment.");
                    }

                    return null;
                }
            }

            return settings;
        }

        /// <summary>The filter to use in this channel. A channel with its own filter is not subject to the watchlist.</summary>
        private (int FilterId, bool UsesChannelFilter) ResolveFilter(ServerSettings settings)
        {
            var channelFilter = settings.FilteredChannels.LastOrDefault(filter => filter.ChannelId == Context.Channel.Id);
            return channelFilter == null ? (settings.DefaultFilterId, false) : (channelFilter.FilterId, true);
        }

        /// <summary>Tells the user when the site couldn't answer. Returns true if the lookup failed and the command should stop.</summary>
        private async Task<bool> ReplyIfFailedAsync(string label, string subject, BooruResult result)
        {
            if (!result.Failed)
            {
                return false;
            }

            var code = result.HttpCode;
            var reply = code switch
            {
                429 => $"I'm sending too many requests to the site, please try again in a moment (HTTP {code})",
                >= 300 and < 400 => $"Something is giving me the runaround (HTTP {code})",
                >= 400 and < 500 => $"I think you may have entered in something incorrectly (HTTP {code})",
                >= 500 => $"I'm having trouble accessing the site, please try again later (HTTP {code})",
                _ => "I can't reach the site right now, please try again later.",
            };

            var what = string.IsNullOrEmpty(subject) ? label : $"{label}: {subject}";
            await ReplyAsync(reply);
            await _logger.Log(code != null ? $"{what}, HTTP ERROR {code}" : $"{what}, SITE UNAVAILABLE", Context);
            return true;
        }

        private async Task<bool> CheckBadlistsAsync(string label, string query, ServerSettings settings)
        {
            var watchTerms = BadlistHelper.CheckWatchList(query, settings);
            if (watchTerms == "")
            {
                return true;
            }

            await _logger.Log($"{label}: {query}, WATCHLISTED {watchTerms}", Context, true);
            await ReplyAsync("I'm not gonna go look for that.");

            // Watchlists can also be set up in DMs, where there is no server (and no alert channel) to notify.
            var watchChannel = Context.Guild?.GetTextChannel(settings.WatchAlertChannel);
            if (watchChannel == null)
            {
                return false;
            }

            var alert = $"<@{Context.User.Id}> searched for a naughty term in <#{Context.Channel.Id}> WATCH TERMS: {watchTerms}";
            if (settings.WatchAlertRole != 0)
            {
                alert = $"<@&{settings.WatchAlertRole}> " + alert;
            }

            await watchChannel.SendMessageAsync(alert, allowedMentions: AlertMentions(settings.WatchAlertRole));
            return false;
        }

        /// <summary>Allowed mentions for an alert: the configured alert role (if any) may be pinged, nothing else.</summary>
        private static AllowedMentions AlertMentions(ulong roleId)
        {
            return roleId == 0
                ? AllowedMentions.None
                : new AllowedMentions(AllowedMentionTypes.Roles) { RoleIds = new List<ulong> { roleId } };
        }
    }
}

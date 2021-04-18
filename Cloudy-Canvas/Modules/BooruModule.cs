namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

    [Summary("Module for interfacing with Maneboorud")]
    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;
        private readonly BlacklistService _blacklistService;
        private readonly LoggingHelperService _logger;

        public BooruModule(BooruService booru, BlacklistService blacklistService, LoggingHelperService logger)
        {
            _booru = booru;
            _blacklistService = blacklistService;
            _logger = logger;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            _blacklistService.InitializeList(Context);
            var badTerms = _blacklistService.CheckList(query);
            if (badTerms != "")
            {
                await _logger.Log($"pick: {query}, BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var (imageId, total, spoilered, spoilerList) = await _booru.GetRandomImageByQueryAsync(query);
                if (total == 0)
                {
                    await _logger.Log($"pick: {query}, total: {total}", Context);
                    await ReplyAsync("I could not find any images with that query.");
                }
                else
                {
                    var totalString = $"[{total} result";
                    if (total == 1)
                    {
                        totalString += "] ";
                    }
                    else
                    {
                        totalString += "s] ";
                    }

                    if (spoilered)
                    {
                        var spoilerStrings = SetupTagListOutput(spoilerList);
                        var output = totalString + $"Spoiler for {spoilerStrings}:\n|| https://manebooru.art/images/{imageId} ||";
                        await _logger.Log($"pick: {query}, total: {total} result: {imageId} SPOILERED {spoilerStrings}", Context);
                        await ReplyAsync(output);
                    }
                    else
                    {
                        var output = totalString + $"https://manebooru.art/images/{imageId}";
                        await _logger.Log($"pick: {query}, total: {total} result: {imageId}", Context);
                        await ReplyAsync(output);
                    }
                }
            }
        }

        [Command("pickrecent")]
        [Summary("Selects first image in a search")]
        public async Task PickFirstAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            _blacklistService.InitializeList(Context);
            var badTerms = _blacklistService.CheckList(query);
            if (badTerms != "")
            {
                await _logger.Log($"pickrecent: {query}, BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var (imageId, total, spoilered, spoilerList) = await _booru.GetFirstRecentImageByQueryAsync(query);
                if (total == 0)
                {
                    await _logger.Log($"pickrecent: {query}, total: {total}", Context);
                    await ReplyAsync("I could not find any images with that query.");
                }
                else
                {
                    var totalString = $"[{total} result";
                    if (total == 1)
                    {
                        totalString += "] ";
                    }
                    else
                    {
                        totalString += "s] ";
                    }

                    if (spoilered)
                    {
                        var spoilerStrings = SetupTagListOutput(spoilerList);
                        var output = totalString + $"Spoiler for {spoilerStrings}:\n|| https://manebooru.art/images/{imageId} ||";
                        await _logger.Log($"pickrecent: {query}, total: {total} result: {imageId} SPOILERED {spoilerStrings}", Context);
                        await ReplyAsync(output);
                    }
                    else
                    {
                        var output = totalString + $"https://manebooru.art/images/{imageId}";
                        await _logger.Log($"pickrecent: {query}, total: {total} result: {imageId}", Context);
                        await ReplyAsync(output);
                    }
                }
            }
        }

        [Command("id")]
        [Summary("Selects an image by image id")]
        public async Task IdAsync([Summary("The image Id")] long id = 4010266)
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            _blacklistService.InitializeList(Context);
            var badTerms = _blacklistService.CheckList(id.ToString());
            if (badTerms != "")
            {
                await _logger.Log($"id: {id} BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var (imageId, spoilered, spoilerList) = await _booru.GetImageByIdAsync(id);
                if (imageId == -1)
                {
                    await ReplyAsync("I could not find that image.");
                    await _logger.Log($"id: requested {id}, NOT FOUND", Context);
                }
                else
                {
                    if (spoilered)
                    {
                        var spoilerStrings = SetupTagListOutput(spoilerList);
                        var output = $"Result is a spoiler for {spoilerStrings}:\n|| https://manebooru.art/images/{imageId} ||";
                        await _logger.Log($"id: requested {id}, found {imageId} SPOILERED {spoilerStrings}", Context);
                        await ReplyAsync(output);
                    }
                    else
                    {
                        await _logger.Log($"id: requested {id}, found {imageId}", Context);
                        await ReplyAsync($"https://manebooru.art/images/{imageId}");
                    }
                }
            }
        }

        [Command("tags")]
        [Summary("Selects a tag list by image Id")]
        public async Task TagsAsync([Summary("The image Id")] long id = 4010266)
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            _blacklistService.InitializeList(Context);
            var badTerms = _blacklistService.CheckList(id.ToString());
            if (badTerms != "")
            {
                await _logger.Log($"tags: {id} BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var (tagList, spoilered, spoilerList) = await _booru.GetImageTagsIdAsync(id);
                if (tagList.Count == 0)
                {
                    await ReplyAsync("I could not find that image.");
                    await _logger.Log($"tags: requested {id}, NOT FOUND", Context);
                }
                else
                {
                    var tagStrings = SetupTagListOutput(tagList);
                    var output = $"Image {id} has the tags {tagStrings}";
                    if (spoilered)
                    {
                        var spoilerStrings = SetupTagListOutput(spoilerList);
                        output += $" including the spoiler tags {spoilerStrings}";
                        await _logger.Log($"tags: requested {id}, found {tagStrings} SPOILERED {spoilerStrings}", Context);
                    }
                    else
                    {
                        await _logger.Log($"tags: requested {id}, found {tagStrings}", Context);
                    }

                    await ReplyAsync(output);
                }
            }
        }

        [Command("getspoilers")]
        [Summary("Gets the list of spoiler tags")]
        public async Task GetSpoilersAsync()
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            var spoilerList = await _booru.GetSpoilerTagsAsync();
            var output = "__Spoilered tags:__\n";
            for (var x = 0; x < spoilerList.Count; x++)
            {
                output += $"`{spoilerList[x].Item2}`";
                if (x < spoilerList.Count - 1)
                {
                    output += ", ";
                }
            }

            await _logger.Log("getspoilers", Context);
            await ReplyAsync(output);
        }

        [Command("featured")]
        [Summary("Selects the current Featured Image on Manebooru")]
        public async Task FeaturedAsync()
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            var featured = await _booru.GetFeaturedImageIdAsync();
            await _logger.Log("featured", Context);
            await ReplyAsync($"https://manebooru.art/images/{featured}");
        }

        private static string SetupTagListOutput(IReadOnlyList<string> tagList)
        {
            var output = "";
            for (var x = 0; x < tagList.Count; x++)
            {
                var spoilerTerm = tagList[x];
                output += $"`{spoilerTerm}`";
                if (x < tagList.Count - 1)
                {
                    output += ", ";
                }
            }

            return output;
        }
    }
}

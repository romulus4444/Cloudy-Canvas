namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

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
            _blacklistService.InitializeList(Context);
            var badTerms = _blacklistService.CheckList(query);
            if (badTerms != "")
            {
                await _logger.Log($"query: {query}, BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var (imageId, spoilered, spoilerList) = await _booru.GetRandomImageByQueryAsync(query);
                if (imageId == -1)
                {
                    await ReplyAsync("I could not find any images with that query.");
                }
                else
                {
                    if (spoilered)
                    {
                        var spoilerStrings = SetupSpoilerOutput(spoilerList);
                        var output = $"Result is a spoiler for {spoilerStrings}:\n|| https://manebooru.art/images/{imageId} ||";
                        await _logger.Log($"query: {query}, result: {imageId} SPOILERED {spoilerStrings}", Context);
                        await ReplyAsync(output);
                    }
                    else
                    {
                        await _logger.Log($"query: {query}, result: {imageId}", Context);
                        await ReplyAsync($"https://manebooru.art/images/{imageId}");
                    }
                }
            }
        }

        [Command("id")]
        [Summary("Selects an image by image id")]
        public async Task IdAsync([Summary("The image ID")] long id = 4010266)
        {
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
                }
                else
                {
                    if (spoilered)
                    {
                        var spoilerStrings = SetupSpoilerOutput(spoilerList);
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

        [Command("getspoilers")]
        public async Task GetSpoilersAsync()
        {
            var spoilerList = await _booru.GetSpoilerTagsAsync();
            var output = "__Spoilered tags:__\n";
            foreach (var (tagId, tagName) in spoilerList)
            {
                output += $"{tagId}, `{tagName}`\n";
            }

            await ReplyAsync(output);
        }

        [Command("featured")]
        public async Task FeaturedAsync()
        {
            var featured = await _booru.GetFeaturedImageIdAsync();
            await ReplyAsync($"https://manebooru.art/images/{featured}");
        }

        private static string SetupSpoilerOutput(List<string> spoilerList)
        {
            var output = "";
            for (var x = 0; x < spoilerList.Count; x++)
            {
                var spoilerTerm = spoilerList[x];
                output += $"`{spoilerTerm}`";
                if (x < spoilerList.Count - 1)
                {
                    output += ", ";
                }
            }

            return output;
        }
    }
}

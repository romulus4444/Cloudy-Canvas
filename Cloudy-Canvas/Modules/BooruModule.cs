namespace Cloudy_Canvas.Modules
{
    using System;
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
                var (imageId, spoilered) = await _booru.GetRandomImageByQueryAsync(query);
                if (imageId == -1)
                {
                    await ReplyAsync("I could not find any images with that query.");
                }
                else
                {
                    if (spoilered)
                    {
                        await _logger.Log($"query: {query}, result: {imageId} SPOILERED", Context);
                        await ReplyAsync($"Result is a spoiler for <term>:\n|| https://manebooru.art/images/{imageId} ||");
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
                var (imageId, spoilered) = await _booru.GetImageByIdAsync(id);
                if (imageId == -1)
                {
                    await ReplyAsync("I could not find that image.");
                }
                else
                {
                    if (spoilered)
                    {
                        await _logger.Log($"id: requested {id}, found {imageId} SPOILERED", Context);
                        await ReplyAsync($"Result is a spoiler for <term>:\n|| https://manebooru.art/images/{imageId} ||");
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
            foreach (var tag in spoilerList)
            {
                output += $"{tag}\n";
            }

            await ReplyAsync(output);
        }
    }
}

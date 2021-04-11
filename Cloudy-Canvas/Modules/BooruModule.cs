namespace Cloudy_Canvas.Modules
{
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
            var badTerms = _blacklistService.CheckList(query);
            if (badTerms != "")
            {
                await _logger.Log($"query: {query}, BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var id = await _booru.GetRandomFirstPageImageByQuery(query);
                await _logger.Log($"query: {query}, result: {id}", Context);
                if (id == -1)
                {
                    await ReplyAsync("I could not find any images with that query.");
                }
                else
                {
                    await ReplyAsync($"https://manebooru.art/images/{id}");
                }
            }
        }

        [Command("id")]
        [Summary("Selects an image by image id")]
        public async Task IdAsync([Summary("The image ID")] long id = 4010266)
        {
            var badTerms = _blacklistService.CheckList(id.ToString());
            if (badTerms != "")
            {
                await _logger.Log($"id: {id} BLACKLISTED {badTerms}", Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var result = await _booru.GetImageById(id);
                await _logger.Log($"id: requested {id}, found {result}", Context);
                if (result == -1)
                {
                    await ReplyAsync("I could not find that image.");
                }
                else
                {
                    await ReplyAsync($"https://manebooru.art/images/{id}");
                }
            }
        }
    }
}

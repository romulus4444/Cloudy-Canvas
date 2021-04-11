namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;
        private readonly Blacklist _blacklist;
        private readonly LoggingHelperService _logger;

        public BooruModule(BooruService booru, Blacklist blacklist, LoggingHelperService logger)
        {
            _booru = booru;
            _blacklist = blacklist;
            _logger = logger;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var badTerms = _blacklist.CheckList(query);
            var logStringPrefix = _logger.SetUpLogStringPrefix(Context);
            if (badTerms != "")
            {
                logStringPrefix += $"query: {query}, BLACKLISTED {badTerms}";
                await _logger.Log(logStringPrefix, Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var id = await _booru.GetRandomFirstPageImageByQuery(query);
                logStringPrefix += $"query: {query}, result: {id}";
                await _logger.Log(logStringPrefix, Context);
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
            var badTerms = _blacklist.CheckList(id.ToString());
            var logStringPrefix = _logger.SetUpLogStringPrefix(Context);
            if (badTerms != "")
            {
                logStringPrefix += $"id: {id} BLACKLISTED {badTerms}";
                await _logger.Log(logStringPrefix, Context);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var result = await _booru.GetImageById(id);
                logStringPrefix += $"id: requested {id}, found {result}";
                await _logger.Log(logStringPrefix, Context);
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

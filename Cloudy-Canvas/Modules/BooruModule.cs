namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;

    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;
        private readonly ILogger<Worker> _logger;
        private readonly Blacklist _blacklist;
        private readonly LoggingService _logparser;

        public BooruModule(ILogger<Worker> logger, BooruService booru, Blacklist blacklist, LoggingService logparser)
        {
            _logger = logger;
            _booru = booru;
            _blacklist = blacklist;
            _logparser = logparser;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var badTerms = _blacklist.CheckList(query);
            var logStringPrefix = _logparser.SetUpLogStringPrefix(Context);
            if (badTerms != "")
            {
                logStringPrefix += $"query: {query}, BLACKLISTED {badTerms}";
                _logger.LogInformation(logStringPrefix);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var id = await _booru.GetRandomFirstPageImageByQuery(query);
                logStringPrefix += $"query: {query}, result: {id}";
                _logger.LogInformation(logStringPrefix);
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
            var logStringPrefix = _logparser.SetUpLogStringPrefix(Context);
            if (badTerms != "")
            {
                logStringPrefix += $"id: {id} BLACKLISTED {badTerms}";
                _logger.LogInformation(logStringPrefix);
                await ReplyAsync("I'm not gonna go look for that.");
            }
            else
            {
                var result = await _booru.GetImageById(id);
                logStringPrefix += $"id: requested {id}, found {result}";
                _logger.LogInformation(logStringPrefix);
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

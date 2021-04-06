namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Service;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;

    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;
        private readonly ILogger<Worker> _logger;

        public BooruModule(ILogger<Worker> logger, BooruService booru)
        {
            _logger = logger;
            _booru = booru;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var id = await _booru.GetRandomFirstPageImageByQuery(query);
            _logger.LogInformation($"query: {query}, result: {id}");
            if (id == -1)
            {
                await ReplyAsync("I could not find any images with that query.");
            }
            else
            {
                await ReplyAsync($"https://manebooru.art/images/{id}");
            }
        }

        [Command("id")]
        [Summary("Selects an image by image id")]
        public async Task IdAsync([Summary("The image ID")] long id = 4010266)
        {
            var result = await _booru.GetImageById(id);
            _logger.LogInformation($"id: {id}, {result}");
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

namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;

        public BooruModule(BooruService booru)
        {
            _booru = booru;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickAsync([Remainder][Summary("Query string")] string query = "*")
        {
            var id = await _booru.GetRandomFirstPageImageByQuery(query);
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
        public async Task IdAsync([Summary("The image ID")] int id)
        {
            await ReplyAsync($"https://manebooru.art/images/{id}");
        }
    }
}

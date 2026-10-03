namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Admin;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

    [Summary("Submodule for managing the watchlist")]
    [RequireBotAdmin]
    public class WatchlistModule : BotModuleBase
    {
        private readonly LoggingService _logger;

        public WatchlistModule(LoggingService logger)
        {
            _logger = logger;
        }

        [Command("watchlist", RunMode = RunMode.Async)]
        [Summary("Manages the search term watchlist")]
        public async Task WatchListCommandAsync([Summary("Subcommand")] string command = "", [Remainder] [Summary("Search term")] string term = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            var result = WatchListOperations.Run(command, term, settings);
            if (result.Changed)
            {
                await FileHelper.SaveServerSettingsAsync(settings, Context);
            }

            await ReplyAsync(result.Message);
            await _logger.Log(result.LogText, Context, result.LogToFile);
        }
    }
}

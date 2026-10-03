namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Threading.Tasks;
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
            switch (command)
            {
                case "":
                    await ReplyAsync("You must specify a subcommand.");
                    await _logger.Log("watchlist: <FAIL>", Context);
                    break;
                case "add":
                    var (addList, failList) = await BadlistHelper.AddWatchTerm(term, settings, Context);
                    if (failList.Count == 0)
                    {
                        var addOutput = "";
                        for (var x = 0; x < addList.Count; x++)
                        {
                            var addedTerm = addList[x];
                            addOutput += $"`{addedTerm}`";
                            if (x < addList.Count - 2)
                            {
                                addOutput += ", ";
                            }

                            if (x == addList.Count - 2)
                            {
                                addOutput += ", and ";
                            }
                        }

                        await ReplyAsync($"Added {addOutput} to the watchlist.");
                        await _logger.Log($"watchlist: add {addOutput} <SUCCESS>", Context, true);
                    }
                    else if (addList.Count == 0)
                    {
                        await ReplyAsync("All terms entered are already on the watchlist.");
                        await _logger.Log($"watchlist: add <FAIL> {term}", Context);
                    }
                    else
                    {
                        var failOutput = "";
                        var addOutput = "";
                        for (var x = 0; x < addList.Count; x++)
                        {
                            var addedTerm = addList[x];
                            addOutput += $"`{addedTerm}`";
                            if (x < addList.Count - 2)
                            {
                                addOutput += ", ";
                            }

                            if (x == addList.Count - 2)
                            {
                                addOutput += ", and ";
                            }
                        }

                        for (var x = 0; x < failList.Count; x++)
                        {
                            var failedTerm = failList[x];
                            failOutput += $"`{failedTerm}`";
                            if (x < failList.Count - 2)
                            {
                                failOutput += ", ";
                            }

                            if (x == failList.Count - 2)
                            {
                                failOutput += ", and ";
                            }
                        }

                        await ReplyAsync($"Added {addOutput} to the watchlist, and the watchlist already contained {failOutput}.");
                        await _logger.Log($"watchlist: add {addOutput} <FAIL> {failOutput}", Context);
                    }

                    break;
                case "remove":
                    var removed = await BadlistHelper.RemoveWatchTerm(term, settings, Context);
                    if (removed)
                    {
                        await ReplyAsync($"Removed `{term}` from the watchlist.");
                        await _logger.Log($"watchlist: remove {term} <SUCCESS>", Context, true);
                    }
                    else
                    {
                        await ReplyAsync($"`{term}` was not on the watchlist.");
                        await _logger.Log($"watchlist: remove {term} <FAIL>", Context);
                    }

                    break;
                case "get":
                    var output = "The watchlist is currently empty.";
                    foreach (var item in settings.WatchList)
                    {
                        if (output == "The watchlist is currently empty.")
                        {
                            output = $"`{item}`";
                        }
                        else
                        {
                            output += $", `{item}`";
                        }
                    }

                    await ReplyAsync($"__Watchlist Terms:__{Environment.NewLine}{output}");
                    await _logger.Log("watchlist: get", Context);
                    break;
                case "clear":
                    settings.WatchList.Clear();
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync("Watchlist cleared");
                    await _logger.Log("watchlist: clear", Context, true);
                    break;
                default:
                    await ReplyAsync("Invalid subcommand");
                    await _logger.Log($"watchlist: {command} <FAIL>", Context);
                    break;
            }
        }
    }
}

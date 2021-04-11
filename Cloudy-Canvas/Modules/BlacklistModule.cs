namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord;
    using Discord.Commands;

    public class BlacklistModule : ModuleBase<SocketCommandContext>
    {
        private readonly Blacklist _blacklist;

        private readonly LoggingHelperService _logger;

        public BlacklistModule(Blacklist blacklist, LoggingHelperService logger)
        {
            _blacklist = blacklist;
            _logger = logger;
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("blacklist")]
        [Summary("Blacklist base command")]
        public async Task Blacklist(string arg = null, [Remainder] string term = null)
        {
            var logStringPrefix = _logger.SetUpLogStringPrefix(Context);
            logStringPrefix += "blacklist ";
            switch (arg)
            {
                case null:
                    await ReplyAsync("You must specify a subcommand.");
                    logStringPrefix += "null";
                    await _logger.Log(logStringPrefix, Context);
                    break;
                case "add":
                    var added = _blacklist.AddTerm(term);
                    if (added)
                    {
                        await ReplyAsync($"Added {term} to the blacklist.");
                        logStringPrefix += $"add (success): {term}";
                        await _logger.Log(logStringPrefix, Context);
                    }
                    else
                    {
                        await ReplyAsync($"{term} is already on the blacklist.");
                        logStringPrefix += $"add (fail): {term}";
                        await _logger.Log(logStringPrefix, Context);
                    }

                    break;
                case "remove":
                    var removed = _blacklist.RemoveTerm(term);
                    if (removed)
                    {
                        await ReplyAsync($"Removed {term} from the blacklist.");
                        logStringPrefix += $"remove (success): {term}";
                        await _logger.Log(logStringPrefix, Context);
                    }
                    else
                    {
                        await ReplyAsync($"{term} was not on the blacklist.");
                        logStringPrefix += $"remove (fail): {term}";
                        await _logger.Log(logStringPrefix, Context);
                    }

                    break;
                case "get":
                    var output = "The blacklist is currently empty.";
                    var blacklist = _blacklist.GetList();
                    foreach (var item in blacklist)
                    {
                        if (output == "The blacklist is currently empty.")
                        {
                            output = item;
                        }
                        else
                        {
                            output += $", {item}";
                        }
                    }

                    await ReplyAsync($"__Blacklist Terms:__\n{output}");
                    logStringPrefix += "get";
                    await _logger.Log(logStringPrefix, Context);
                    break;
                case "clear":
                    _blacklist.ClearList();
                    await ReplyAsync("Blacklist cleared");
                    logStringPrefix += "clear";
                    await _logger.Log(logStringPrefix, Context);
                    break;
                default:
                    await ReplyAsync("Invalid subcommand");
                    logStringPrefix += $"invalid: {arg}";
                    await _logger.Log(logStringPrefix, Context);
                    break;
            }
        }
    }
}

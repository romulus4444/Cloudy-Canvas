namespace Cloudy_Canvas.Modules
{
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;

    public class BlacklistModule : ModuleBase<SocketCommandContext>
    {
        private readonly Blacklist _blacklist;
        private readonly ILogger<Worker> _logger;
        private readonly LoggingService _logparser;

        public BlacklistModule(Blacklist blacklist, ILogger<Worker> logger, LoggingService logparser)
        {
            _blacklist = blacklist;
            _logger = logger;
            _logparser = logparser;
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("blacklist")]
        [Summary("Blacklist base command")]
        public async Task Blacklist(string arg = null, [Remainder] string term = null)
        {
            var logStringPrefix = _logparser.SetUpLogStringPrefix(Context);
            logStringPrefix += "blacklist ";
            switch (arg)
            {
                case null:
                    await ReplyAsync("You must specify a subcommand.");
                    logStringPrefix += "null";
                    _logger.LogInformation(logStringPrefix);
                    break;
                case "add":
                    var added = _blacklist.AddTerm(term);
                    if (added)
                    {
                        await ReplyAsync($"Added {term} to the blacklist.");
                        logStringPrefix += $"add (success): {term}";
                        _logger.LogInformation(logStringPrefix);
                    }
                    else
                    {
                        await ReplyAsync($"{term} is already on the blacklist.");
                        logStringPrefix += $"add (fail): {term}";
                        _logger.LogInformation(logStringPrefix);
                    }

                    break;
                case "remove":
                    var removed = _blacklist.RemoveTerm(term);
                    if (removed)
                    {
                        await ReplyAsync($"Removed {term} from the blacklist.");
                        logStringPrefix += $"remove (success): {term}";
                        _logger.LogInformation(logStringPrefix);
                    }
                    else
                    {
                        await ReplyAsync($"{term} was not on the blacklist.");
                        logStringPrefix += $"remove (fail): {term}";
                        _logger.LogInformation(logStringPrefix);
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
                    _logger.LogInformation(logStringPrefix);
                    break;
                case "clear":
                    _blacklist.ClearList();
                    await ReplyAsync("Blacklist cleared");
                    logStringPrefix += "clear";
                    _logger.LogInformation(logStringPrefix);
                    break;
                default:
                    await ReplyAsync("Invalid subcommand");
                    logStringPrefix += $"invalid: {arg}";
                    _logger.LogInformation(logStringPrefix);
                    break;
            }
        }
    }
}

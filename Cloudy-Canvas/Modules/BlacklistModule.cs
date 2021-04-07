﻿namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Discord;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;

    public class BlacklistModule : ModuleBase<SocketCommandContext>
    {
        private readonly Blacklist _blacklist;
        private readonly ILogger<Worker> _logger;

        public BlacklistModule(Blacklist blacklist, ILogger<Worker> logger)
        {
            _blacklist = blacklist;
            _logger = logger;
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("blacklist")]
        [Summary("Blacklist base command")]
        public async Task Blacklist(string arg = null, [Remainder] string term = null)
        {
            switch (arg)
            {
                case null:
                    await ReplyAsync("You must specify a subcommand.");
                    _logger.LogInformation($"blacklist - null");
                    break;
                case "add":
                    var added = _blacklist.AddTerm(term);
                    if (added)
                    {
                        await ReplyAsync($"Added {term} to the blacklist.");
                        _logger.LogInformation($"blacklist - add: {term} - success");
                    }
                    else
                    {
                        await ReplyAsync($"{term} is already on the blacklist.");
                        _logger.LogInformation($"blacklist - add: {term} - fail");
                    }

                    break;
                case "remove":
                    var removed = _blacklist.RemoveTerm(term);
                    if (removed)
                    {
                        await ReplyAsync($"Removed {term} from the blacklist.");
                        _logger.LogInformation($"blacklist - remove: {term} - success");
                    }
                    else
                    {
                        await ReplyAsync($"{term} was not on the blacklist.");
                        _logger.LogInformation($"blacklist - remove: {term} - fail");
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
                    _logger.LogInformation($"blacklist - get");
                    break;
                case "clear":
                    _blacklist.ClearList();
                    await ReplyAsync("Blacklist cleared");
                    _logger.LogInformation($"blacklist - clear");
                    break;
                default:
                    await ReplyAsync("Invalid subcommand");
                    _logger.LogInformation($"blacklist - invalid: {arg}");
                    break;
            }
        }
    }
}
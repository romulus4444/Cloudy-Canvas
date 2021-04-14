namespace Cloudy_Canvas.Modules
{
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord;
    using Discord.Commands;

    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("admin")]
        public async Task AdminAsync(string commandOne = "", [Remainder] string commandTwo = "")
        {
            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    break;
                case "adminchannel":
                    if (commandTwo == "")
                    {
                        var channelId = await GetAdminChannel(Context);
                        if (channelId > 0)
                        {
                            await ReplyAsync($"Admin channel is <#{channelId}>");
                        }
                        else
                        {
                            await ReplyAsync("Admin channel not set yet.");
                        }
                    }
                    else
                    {
                        var exists = await DiscordHelper.CheckIfChannelExistsAsync(commandTwo, Context);
                        if (exists > 0)
                        {
                            await SetAdminChannel(exists, commandTwo);
                            await ReplyAsync($"Admin channel set to <#{exists}>");
                        }
                        else
                        {
                            await ReplyAsync($"Invalid channel name #{commandTwo}.");
                        }
                    }

                    break;
                default:
                    await ReplyAsync($"Invalid command `{commandOne}`");
                    break;
            }
        }

        private async Task SetAdminChannel(ulong channelId, string channelName)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "Settings", "txt", Context);
            await File.WriteAllTextAsync(filepath, $"adminchannel: <#{channelId}> #{channelName}");
            await Task.CompletedTask;
        }

        private async Task<ulong> GetAdminChannel(SocketCommandContext context)
        {
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "Settings", "txt", Context);
            if (!File.Exists(filepath))
            {
                return 0;
            }

            var settings = await File.ReadAllLinesAsync(filepath);
            ulong channelId = 0;
            foreach (var setting in settings)
            {
                if (!setting.Contains("adminchannel:"))
                {
                    continue;
                }

                var split = setting.Split(" #", 2);
                channelId = await DiscordHelper.CheckIfChannelExistsAsync(split[1], context);
            }

            return channelId;
        }

        public class BlacklistModule : ModuleBase<SocketCommandContext>
        {
            private readonly BlacklistService _blacklistService;

            private readonly LoggingHelperService _logger;

            public BlacklistModule(BlacklistService blacklistService, LoggingHelperService logger)
            {
                _blacklistService = blacklistService;
                _logger = logger;
            }

            [RequireUserPermission(GuildPermission.Administrator)]
            [Command("blacklist")]
            [Summary("Blacklist base command")]
            public async Task Blacklist(string arg = null, [Remainder] string term = null)
            {
                _blacklistService.InitializeList(Context);
                switch (arg)
                {
                    case null:
                        await ReplyAsync("You must specify a subcommand.");
                        await _logger.Log("blacklist null", Context);
                        break;
                    case "add":
                        var added = _blacklistService.AddTerm(term);
                        if (added)
                        {
                            await ReplyAsync($"Added `{term}` to the blacklist.");
                            await _logger.Log($"blacklist add (success): {term}", Context);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` is already on the blacklist.");
                            await _logger.Log($"blacklist add (fail): {term}", Context);
                        }

                        break;
                    case "remove":
                        var removed = _blacklistService.RemoveTerm(term);
                        if (removed)
                        {
                            await ReplyAsync($"Removed `{term}` from the blacklist.");
                            await _logger.Log($"blacklist remove (success): {term}", Context);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` was not on the blacklist.");
                            await _logger.Log($"blacklist remove (fail): {term}", Context);
                        }

                        break;
                    case "get":
                        var output = "The blacklist is currently empty.";
                        var blacklist = _blacklistService.GetList();
                        foreach (var item in blacklist)
                        {
                            if (output == "The blacklist is currently empty.")
                            {
                                output = $"`{item}`";
                            }
                            else
                            {
                                output += $", `{item}`";
                            }
                        }

                        await ReplyAsync($"__Blacklist Terms:__\n{output}");
                        await _logger.Log("blacklist get", Context);
                        break;
                    case "clear":
                        _blacklistService.ClearList();
                        await ReplyAsync("Blacklist cleared");
                        await _logger.Log("blacklist clear", Context);
                        break;
                    default:
                        await ReplyAsync("Invalid subcommand");
                        await _logger.Log($"blacklist invalid: {arg}", Context);
                        break;
                }
            }
        }
    }
}

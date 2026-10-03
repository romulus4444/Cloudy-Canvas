namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for managing admin functions")]
    public class AdminModule : BotModuleBase
    {
        private readonly LoggingService _logger;
        private readonly BooruService _booru;
        private readonly AllPreloadedSettings _servers;

        // ;echo may mention users and roles, but never @everyone / @here.
        private static readonly AllowedMentions EchoMentions = new(AllowedMentionTypes.Users | AllowedMentionTypes.Roles);

        public AdminModule(LoggingService logger, BooruService booru, AllPreloadedSettings servers)
        {
            _logger = logger;
            _booru = booru;
            _servers = servers;
        }

        [Command("setup", RunMode = RunMode.Async)]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupCommandAsync(
            int filterId,
            [Summary("Admin channel name")] string adminChannelName = "",
            [Remainder] [Summary("Admin role name")] string adminRoleName = "")
        {
            var settings = new ServerSettings();
            ulong channelSetId;
            var filterCheck = await _booru.CheckFilterAsync(filterId);
            if (filterCheck.Status == BooruStatus.NotFound)
            {
                await ReplyAsync(
                    "I could not find that filter; please make sure it exists and is set to public. You may change the filter later with `;admin filter set <filterId>`. Continuing setup with my default filter of 175.");
                filterId = 175;
            }
            else if (filterCheck.Status != BooruStatus.Ok)
            {
                await ReplyAsync(
                    "I can't reach Manebooru right now to check that filter. You may change the filter later with `;admin filter set <filterId>`. Continuing setup with my default filter of 175.");
                filterId = 175;
            }

            settings.Name = Context.Guild.Name;
            settings.DefaultFilterId = filterId;
            await ReplyAsync($"Using <https://manebooru.art/filters/{filterId}>");
            await ReplyAsync("Moving in to my new place...");
            if (adminChannelName == "")
            {
                channelSetId = Context.Channel.Id;
            }
            else
            {
                channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(adminChannelName, Context);
            }

            if (channelSetId > 0)
            {
                settings.AdminChannel = channelSetId;
                await ReplyAsync($"Moved into <#{channelSetId}>!");
                var adminChannel = Context.Guild.GetTextChannel(settings.AdminChannel);
                _servers.GuildList[Context.Guild.Id] = adminChannel.Id;
                await FileHelper.SaveAllPresettingsAsync(_servers);
                await adminChannel.SendMessageAsync("Howdy neighbors! I will send important message here now.");
            }
            else
            {
                await ReplyAsync($"I couldn't find a place called #{adminChannelName}. Continuing with this channel <#{Context.Channel.Id}> as the admin channel.");
                await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <FAIL>, role {adminRoleName} <NOT CHECKED>", Context);
                settings.AdminChannel = Context.Channel.Id;
            }

            await ReplyAsync("Looking for the bosses...");
            var roleSetId = DiscordHelper.GetRoleId(adminRoleName, Context);
            if (roleSetId > 0)
            {
                settings.AdminRole = roleSetId;
                await ReplyAsync($"<@&{roleSetId}> is in charge now!", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"I couldn't find @{adminRoleName}. Please assign an admin role with ;admin adminrole set role. Continuing without an admin role; until one is set, only members with the Administrator or Manage Server permission can use admin commands.");
                await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL>", Context, true);
            }

            await ReplyAsync("Setting the remaining admin settings to default values (all alerts will post to the admin channel, and no roles will be pinged)...");
            settings.WatchAlertChannel = settings.AdminChannel;
            settings.LogPostChannel = settings.AdminChannel;
            settings.ReportChannel = settings.AdminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync(
                "Settings saved. Now building the spoiler list. This may take a few minutes, depending on how many tags are spoilered in the filter. Please wait until they are completed; I will let you know when I am finished.");
            if (await _booru.RefreshListsAsync(Context, settings) == BooruStatus.Ok)
            {
                await ReplyAsync("The lists have been built. I'm all set! Type `;help admin` for a list of other admin setup commands.");
            }
            else
            {
                await ReplyAsync(
                    "I couldn't reach Manebooru to build the spoiler list, but everything else is set up. Please run `;refreshlists` in a little while to build it. Type `;help admin` for a list of other admin setup commands.");
            }

            await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <SUCCESS>", Context, true);
        }

        [Command("echo", RunMode = RunMode.Async)]
        [Summary("Posts a message to a specified channel")]
        public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "", [Remainder] [Summary("The message to send")] string message = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }

            if (channelName == "")
            {
                await ReplyAsync("You must specify a channel name or a message.");
                await _logger.Log("echo: <FAIL>", Context);
                return;
            }

            var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);

            if (channelId > 0)
            {
                var channel = Context.Guild.GetTextChannel(channelId);
                if (message == "")
                {
                    await ReplyAsync("There's no message to send there.");
                    await _logger.Log($"echo: {channelName} <FAIL>", Context);
                    return;
                }

                if (channel != null)
                {
                    await channel.SendMessageAsync(message, allowedMentions: EchoMentions);
                    await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
                    return;
                }


                await ReplyAsync("I can't send a message there.");
                await _logger.Log($"echo: {channelName} {message} <FAIL>", Context);
                return;
            }

            await ReplyAsync($"{channelName} {message}", allowedMentions: EchoMentions);
            await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
        }

        [Command("setprefix", RunMode = RunMode.Async)]
        [Summary("Sets the bot listen prefix")]
        public async Task SetPrefixCommandAsync([Summary("The prefix character")] char prefix = ';')
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }

            if (!DiscordHelper.IsValidPrefix(prefix))
            {
                await ReplyAsync("The prefix must be a single punctuation or symbol character (not `@`, `#`, `` ` ``, `<` or `>`).");
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            serverPresettings.Prefix = prefix;
            await ReplyAsync($"I will now listen for '{prefix}' on this server.");
            _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
            await FileHelper.SaveAllPresettingsAsync(_servers);
        }

        [Command("listentobots", RunMode = RunMode.Async)]
        [Summary("Sets whether the bot responds to commands from other bots")]
        public async Task ListenToBotsCommandAsync([Summary("yes or no")] string command = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            switch (command.ToLowerInvariant())
            {
                case "":
                    var not = "";
                    if (!serverPresettings.ListenToBots)
                    {
                        not = " not";
                    }

                    await ReplyAsync($"Currently{not} listening to bots.");
                    break;
                case "y":
                case "yes":
                case "on":
                case "true":
                    await ReplyAsync("Now listening to bots.");
                    serverPresettings.ListenToBots = true;
                    _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    break;
                case "n":
                case "no":
                case "off":
                case "false":
                    await ReplyAsync("Not listening to bots.");
                    serverPresettings.ListenToBots = false;
                    _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    break;
                default:
                    await ReplyAsync("Invalid command.");
                    break;
            }
        }

        [Command("safemode", RunMode = RunMode.Async)]
        [Summary("Sets the safemode")]
        public async Task SafeModeCommandAsync([Summary("yes or no")] string command = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }


            switch (command.ToLowerInvariant())
            {
                case "":
                    var not = "";
                    if (!settings.SafeMode)
                    {
                        not = " not";
                    }

                    await ReplyAsync($"Currently{not} in Safe Mode.");
                    break;
                case "y":
                case "yes":
                case "on":
                case "true":
                    await ReplyAsync("Now in Safe Mode.");
                    settings.SafeMode = true;
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    break;
                case "n":
                case "no":
                case "off":
                case "false":
                    await ReplyAsync("Now leaving Safe Mode.");
                    settings.SafeMode = false;
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    break;
                default:
                    await ReplyAsync("Invalid command.");
                    break;
            }
        }

        [Command("alias", RunMode = RunMode.Async)]
        [Summary("Sets an alias")]
        public async Task AliasCommandAsync(string subcommand = "", string shortForm = "", [Remainder] string longForm = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            switch (subcommand)
            {
                case "":
                    await ReplyAsync("You must enter a subcommand");
                    break;
                case "get":
                    var output = $"__Current aliases:__{Environment.NewLine}";
                    foreach (var (shortFormA, longFormA) in serverPresettings.Aliases)
                    {
                        output += $"`{shortFormA}`: `{longFormA}`{Environment.NewLine}";
                    }

                    await ReplyAsync(output);
                    break;
                case "add":
                    var replacing = !serverPresettings.Aliases.TryAdd(shortForm, longForm);
                    if (replacing)
                    {
                        serverPresettings.Aliases[shortForm] = longForm;
                    }

                    _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    await ReplyAsync(replacing
                        ? $"`{shortForm}` now aliased to `{longForm}`, replacing what was there before."
                        : $"`{shortForm}` now aliased to `{longForm}`");
                    break;
                case "remove":
                    serverPresettings.Aliases.Remove(shortForm);
                    _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    await ReplyAsync($"`{shortForm}` alias cleared.");
                    break;
                case "clear":
                    serverPresettings.Aliases.Clear();
                    _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    await ReplyAsync("All aliases cleared.");
                    break;
                default:
                    await ReplyAsync($"Invalid subcommand {subcommand}");
                    break;
            }
        }

        [Command("getsettings", RunMode = RunMode.Async)]
        [Summary("Posts the settings file to the log channel")]
        public async Task GetSettingsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }

            if (Context.IsPrivate)
            {
                await ReplyAsync("Cannot get settings in a DM.");
                return;
            }

            var errorMessage = await SettingsGetAsync(Context, settings);
            if (errorMessage.Contains("<ERROR>"))
            {
                await ReplyAsync(errorMessage);
                await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
                return;
            }

            await _logger.Log("getsettings: <SUCCESS>", Context);
        }

        [Command("refreshlists", RunMode = RunMode.Async)]
        [Summary("Refreshes the spoiler list and server settings")]
        public async Task RefreshListsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            var prefix = serverPresettings.Prefix;
            if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
            {
                return;
            }

            await ReplyAsync("Refreshing spoiler list. This may take a few minutes.");
            var refreshed = await _booru.RefreshListsAsync(Context, settings) == BooruStatus.Ok;
            if (!refreshed)
            {
                await ReplyAsync("I couldn't reach Manebooru to refresh the spoiler list, so I kept the old one. Please try again in a little while.");
            }

            await ReplyAsync("Checking and saving server settings.");
            if (Context.IsPrivate)
            {
                settings.Name = $"{Context.User.Username}";
            }
            else
            {
                if (settings.AdminChannel == 0)
                {
                    await ReplyAsync($"WARNING! There is no admin channel set! Please set one up now with `{prefix}setup <filter> <adminchannel> <adminrole>`");
                    await ReplyAsync("Setting the admin channel to the current channel for now. Other alert channels will be set to here as well.");
                    settings.AdminChannel = Context.Channel.Id;
                }

                settings.Name = Context.Guild.Name;
                if (!_servers.GuildList.ContainsKey(Context.Guild.Id))
                {
                    _servers.GuildList[Context.Guild.Id] = settings.AdminChannel;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                }

                if (settings.WatchAlertChannel == 0)
                {
                    settings.WatchAlertChannel = settings.AdminChannel;
                }

                if (settings.LogPostChannel == 0)
                {
                    settings.LogPostChannel = settings.AdminChannel;
                }

                if (settings.ReportChannel == 0)
                {
                    settings.ReportChannel = settings.AdminChannel;
                }
            }

            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync(refreshed ? "Spoiler list and server settings refreshed!" : "Server settings refreshed!");
        }

        [Command("broadcast", RunMode = RunMode.Async)]
        [Summary("Broadcasts a message to all servers")]
        [RequireBroadcastUser]
        public async Task BroadcastCommandAsync([Remainder] string message = "")
        {
            message = message.Trim();
            if (message == string.Empty)
            {
                await ReplyAsync("Cannot broadcast a blank message!");
                return;
            }

            var sent = 0;
            foreach (var (guildId, adminChannelId) in _servers.GuildList)
            {
                var channel = Context.Client.GetGuild(guildId)?.GetTextChannel(adminChannelId);
                if (channel == null)
                {
                    await _logger.Log($"broadcast: skipped guild {guildId}, its admin channel {adminChannelId} is unavailable", Context);
                    continue;
                }

                await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
                sent++;
            }

            await _logger.Log($"broadcast: {message} (sent to {sent} of {_servers.GuildList.Count} servers)", Context, true);
            await ReplyAsync($"Message broadcasted to {sent} of {_servers.GuildList.Count} servers' admin channels.");
        }

        [Command("<blank message>", RunMode = RunMode.Async)]
        [Summary("Runs on a blank message")]
        public async Task BlankMessageCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.CanUserRunCommandsAsync(Context, settings))
            {
                return;
            }

            await ReplyAsync("Did you need something?");
        }

        [Command("<mention>", RunMode = RunMode.Async)]
        [Summary("Runs on a name ping")]
        public Task MentionCommandAsync()
        {
            //removed ping reply, add custom replies here if desired
            return Task.CompletedTask;
        }

        private async Task<string> SettingsGetAsync(SocketCommandContext context, ServerSettings settings)
        {
            await ReplyAsync("Retrieving settings file...");
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "settings", "conf", Context);
            if (!File.Exists(filepath))
            {
                return "<ERROR> File does not exist";
            }

            var logPostChannel = context.Guild.GetTextChannel(settings.LogPostChannel);
            await logPostChannel.SendFileAsync(filepath, $"{context.Guild.Name}-settings.conf");
            return "SUCCESS";
        }

        [Summary("Submodule for managing the watchlist")]
        public class BadlistModule : BotModuleBase
        {
            private readonly LoggingService _logger;

            public BadlistModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("watchlist", RunMode = RunMode.Async)]
            [Summary("Manages the search term watchlist")]
            public async Task WatchListCommandAsync([Summary("Subcommand")] string command = "", [Remainder] [Summary("Search term")] string term = "")
            {
                var settings = await FileHelper.LoadServerSettingsAsync(Context);
                if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
                {
                    return;
                }

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

        [Summary("Submodule for retrieving log files")]
        public class LogModule : BotModuleBase
        {
            private readonly LoggingService _logger;

            public LogModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("log", RunMode = RunMode.Async)]
            [Summary("Retrieves a log file")]
            public async Task LogCommandAsync(
                [Summary("The channel to get the log from")] string channel = "",
                [Summary("The date (in format (YYYY-MM-DD) to get the log from")] string date = "")
            {
                var settings = await FileHelper.LoadServerSettingsAsync(Context);
                if (!await DiscordHelper.IsBotAdminAsync(Context, settings))
                {
                    return;
                }

                if (Context.IsPrivate)
                {
                    await ReplyAsync("Cannot get logs in a DM.");
                    return;
                }

                if (channel == "")
                {
                    await ReplyAsync("You need to enter a channel and date.");
                    await _logger.Log("log: <FAIL>", Context);
                    return;
                }

                if (date == "")
                {
                    await ReplyAsync("You need to enter a date.");
                    await _logger.Log($"log: {channel} <FAIL>", Context);
                    return;
                }

                var errorMessage = await LogGetAsync(channel, date, Context, settings);
                if (errorMessage.Contains("<ERROR>"))
                {
                    await ReplyAsync(errorMessage);
                    await _logger.Log($"log: {channel} {date} {errorMessage} <FAIL>", Context);
                    return;
                }

                await _logger.Log($"log: {channel} {date} <SUCCESS>", Context);
            }

            private async Task<string> LogGetAsync(string channelName, string date, SocketCommandContext context, ServerSettings settings)
            {
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || !FileHelper.IsSafePathSegment(date))
                {
                    return "<ERROR> Invalid date. Dates must be formatted as YYYY-MM-DD.";
                }

                var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, context);
                var channel = channelId > 0 ? context.Guild.GetTextChannel(channelId) : null;
                if (channel == null)
                {
                    return "<ERROR> Invalid channel";
                }

                if (settings.LogPostChannel <= 0)
                {
                    return "<ERROR> Log post channel not set.";
                }

                await ReplyAsync($"Retrieving log from {channel.Name} on {date}...");

                // Logs are stored per channel id, in a folder that can only be inside this server's own directory.
                var filepath = FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, channelId.ToString(CultureInfo.InvariantCulture), date);
                if (!File.Exists(filepath))
                {
                    return "<ERROR> File does not exist";
                }

                var logPostChannel = context.Guild.GetTextChannel(settings.LogPostChannel);
                await logPostChannel.SendFileAsync(filepath, $"{channel.Name}-{date}.log");
                return "SUCCESS";
            }
        }
    }
}

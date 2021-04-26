namespace Cloudy_Canvas.Modules
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for managing admin functions")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly BooruService _booru;

        public AdminModule(LoggingService logger, BooruService booru)
        {
            _logger = logger;
            _booru = booru;
        }

        [Command("setup")]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupAsync(
            int filterId,
            [Summary("Admin channel name")] string adminChannelName = "",
            [Remainder] [Summary("Admin role name")] string adminRoleName = "")
        {
            var settings = new ServerSettings();
            ulong channelSetId;
            settings.filterId = filterId;
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
                settings.adminChannel = channelSetId;
                await ReplyAsync($"Moved into <#{channelSetId}>!");
                var adminChannel = Context.Guild.GetTextChannel(settings.adminChannel);
                await adminChannel.SendMessageAsync("Howdy neighbors! I will send important message here now.");
            }
            else
            {
                await ReplyAsync($"I couldn't find a place called #{adminChannelName}.");
                await _logger.Log($"setup: channel {adminChannelName} <FAIL>, role {adminRoleName} NOT CHECKED", Context);
                return;
            }

            await ReplyAsync("Looking for the bosses...");
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(adminRoleName, Context);
            if (roleSetId > 0)
            {
                settings.adminRole = roleSetId;
                await ReplyAsync($"<@&{roleSetId}> is in charge now!", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"I couldn't find @{adminRoleName}.");
                await _logger.Log($"setup: channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL>", Context, true);
                return;
            }

            await ReplyAsync("Setting the other remaining settings to default values.");
            settings.redAlertChannel = settings.adminChannel;
            settings.redAlertRole = settings.adminRole;
            settings.yellowAlertChannel = settings.adminChannel;
            settings.yellowAlertRole = settings.adminRole;
            settings.logPostChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync("Settings saved. Now building the spoiler list and redlist. This may take a few minutes to complete.");
            await ReplyAsync("I'm all set! Type `;help admin` for a list of other admin setup commands.");
            await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <SUCCESS>", Context, true);
            await _booru.RefreshListsAsync(Context);
            await ReplyAsync("The lists have been built.");
        }

        [Command("admin")]
        [Summary("Manages admin commands")]
        public async Task AdminAsync(
            [Summary("First subcommand")] string commandOne = "",
            [Summary("Second subcommand")] string commandTwo = "",
            [Remainder] [Summary("Third subcommand")] string commandThree = "")
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    await _logger.Log("admin: <FAIL>", Context);
                    break;
                case "adminchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "adminrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "ignorechannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await IgnoreChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreChannelAddAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await IgnoreChannelRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.ignoredChannels.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Ignored channels list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "ignorerole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await IgnoreRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreRoleAddAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await IgnoreRoleRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.ignoredRoles.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Ignored roles list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "allowuser":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AllowUserGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await AllowUserAddAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await AllowUserRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.allowedUsers.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Allowed users list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "yellowchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await YellowChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await YellowChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await YellowChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                default:
                    await ReplyAsync($"Invalid command `{commandOne}`");
                    await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                    break;
            }
        }

        [Command("echo")]
        [Summary("Posts a message to a specified channel")]
        public async Task EchoAsync([Summary("The channel to send to")] string channelName = "", [Remainder] [Summary("The message to send")] string message = "")
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
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
                    await channel.SendMessageAsync(message);
                    await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
                    return;
                }


                await ReplyAsync("I can't send a message there.");
                await _logger.Log($"echo: {channelName} {message} <FAIL>", Context);
                return;
            }

            await ReplyAsync($"{channelName} {message}");
            await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
        }

        private async Task YellowChannelClearAsync(ServerSettings settings)
        {
            settings.yellowAlertChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Yellow alert channel reset to the current admin channel, <#{settings.yellowAlertChannel}>");
        }

        private async Task YellowChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.yellowAlertChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Yellow alert channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task YellowChannelGetAsync(ServerSettings settings)
        {
            if (settings.yellowAlertChannel > 0)
            {
                await ReplyAsync($"Yellow alerts are being posted in <#{settings.adminChannel}>");
            }
            else
            {
                await ReplyAsync("Yellow alert channel not set yet.");
            }
        }

        private async Task AdminChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.adminChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Admin channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task AdminChannelGetAsync(ServerSettings settings)
        {
            if (settings.adminChannel > 0)
            {
                await ReplyAsync($"Admin channel is <#{settings.adminChannel}>");
            }
            else
            {
                await ReplyAsync("Admin channel not set yet.");
            }
        }

        private async Task AdminRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.adminRole = roleSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Admin role set to <@&{roleSetId}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.");
            }
        }

        private async Task AdminRoleGetAsync(ServerSettings settings)
        {
            if (settings.adminRole > 0)
            {
                await ReplyAsync($"Admin role is <@&{settings.adminRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Admin role not set yet.");
            }
        }

        private async Task AllowUserRemoveAsync(string userName, ServerSettings settings)
        {
            var userRemoveId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userRemoveId > 0)
            {
                for (var x = settings.allowedUsers.Count - 1; x >= 0; x--)
                {
                    var user = settings.allowedUsers[x];
                    if (user != userRemoveId)
                    {
                        continue;
                    }

                    settings.allowedUsers.Remove(user);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <@{userRemoveId}> from allow list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                await ReplyAsync($"<@{userRemoveId}> was not on the list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid user name @{userName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task AllowUserAddAsync(string userName, ServerSettings settings)
        {
            var userAddId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userAddId > 0)
            {
                foreach (var user in settings.allowedUsers)
                {
                    if (user != userAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@{userAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                settings.allowedUsers.Add(userAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <@{userAddId}> to allow list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid user name @{userName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task AllowUserGetAsync(ServerSettings settings)
        {
            if (settings.allowedUsers.Count > 0)
            {
                var output = $"__Allowed User List:__{Environment.NewLine}";
                foreach (var user in settings.allowedUsers)
                {
                    output += $"<@{user}>{Environment.NewLine}";
                }

                await ReplyAsync(output, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("No users on allow list.");
            }
        }

        private async Task IgnoreRoleRemoveAsync(string roleName, ServerSettings settings)
        {
            var roleRemoveId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleRemoveId > 0)
            {
                for (var x = settings.ignoredRoles.Count - 1; x > 0; x--)
                {
                    var role = settings.ignoredRoles[x];
                    if (role != roleRemoveId)
                    {
                        continue;
                    }

                    settings.ignoredRoles.Remove(role);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <@&{roleRemoveId}> from ignore list.", allowedMentions: AllowedMentions.None);
                }

                await ReplyAsync($"<@&{roleRemoveId}> was not on the list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid channel name @{roleName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task IgnoreRoleAddAsync(string roleName, ServerSettings settings)
        {
            var roleAddId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleAddId > 0)
            {
                foreach (var role in settings.ignoredRoles)
                {
                    if (role != roleAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@&{roleAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                settings.ignoredRoles.Add(roleAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <@&{roleAddId}> to ignore list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task IgnoreRoleGetAsync(ServerSettings settings)
        {
            if (settings.ignoredRoles.Count > 0)
            {
                var output = $"__Role Ignore List:__{Environment.NewLine}";
                foreach (var role in settings.ignoredRoles)
                {
                    output += $"<@&{role}>{Environment.NewLine}";
                }

                await ReplyAsync(output, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("No roles on ignore list.");
            }
        }

        private async Task IgnoreChannelGetAsync(ServerSettings settings)
        {
            if (settings.ignoredChannels.Count > 0)
            {
                var output = $"__Channel Ignore List:__{Environment.NewLine}";
                foreach (var channel in settings.ignoredChannels)
                {
                    output += $"<#{channel}>{Environment.NewLine}";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("No channels on ignore list.");
            }
        }

        private async Task IgnoreChannelRemoveAsync(string channelName, ServerSettings settings)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelRemoveId > 0)
            {
                for (var x = settings.ignoredChannels.Count - 1; x > 0; x--)
                {
                    var channel = settings.ignoredChannels[x];
                    if (channel != channelRemoveId)
                    {
                        continue;
                    }

                    settings.ignoredChannels.Remove(channel);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <#{channelRemoveId}> from ignore list.");
                }

                await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreChannelAddAsync(string channelName, ServerSettings settings)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelAddId > 0)
            {
                foreach (var channel in settings.ignoredChannels)
                {
                    if (channel != channelAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<#{channelAddId}> is already on the list.");
                    return;
                }

                settings.ignoredChannels.Add(channelAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <#{channelAddId}> to ignore list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        [Summary("Submodule for managing the yellowlist")]
        public class BadlistModule : ModuleBase<SocketCommandContext>
        {
            private readonly LoggingService _logger;

            public BadlistModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("yellowlist")]
            [Summary("Manages the search term yellowlist")]
            public async Task YellowListAsync([Summary("Subcommand")] string command = "", [Remainder] [Summary("Search term")] string term = "")
            {
                var settings = await FileHelper.LoadServerSettings(Context);
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
                {
                    return;
                }

                switch (command)
                {
                    case "":
                        await ReplyAsync("You must specify a subcommand.");
                        await _logger.Log("yellowlist: <FAIL>", Context);
                        break;
                    case "add":
                        var added = await BadlistHelper.AddYellowTerm(term, settings, Context);
                        if (added)
                        {
                            await ReplyAsync($"Added `{term}` to the yellowlist.");
                            await _logger.Log($"yellowlist: add {term} <SUCCESS>", Context, true);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` is already on the yellowlist.");
                            await _logger.Log($"yellowlist: add {term} <FAIL>", Context);
                        }

                        break;
                    case "remove":
                        var removed = await BadlistHelper.RemoveYellowTerm(term, settings, Context);
                        if (removed)
                        {
                            await ReplyAsync($"Removed `{term}` from the yellowlist.");
                            await _logger.Log($"yellowlist: remove {term} <SUCCESS>", Context, true);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` was not on the yellowlist.");
                            await _logger.Log($"yellowlist: remove {term} <FAIL>", Context);
                        }

                        break;
                    case "get":
                        var output = "The yellowlist is currently empty.";
                        foreach (var item in settings.yellowList)
                        {
                            if (output == "The yellowlist is currently empty.")
                            {
                                output = $"`{item}`";
                            }
                            else
                            {
                                output += $", `{item}`";
                            }
                        }

                        await ReplyAsync($"__Yellowlist Terms:__{Environment.NewLine}{output}");
                        await _logger.Log("yellowlist: get", Context);
                        break;
                    case "clear":
                        settings.yellowList.Clear();
                        await FileHelper.SaveServerSettingsAsync(settings, Context);
                        await ReplyAsync("Yellowlist cleared");
                        await _logger.Log("yellowlist: clear", Context, true);
                        break;
                    default:
                        await ReplyAsync("Invalid subcommand");
                        await _logger.Log($"yellowlist: {command} <FAIL>", Context);
                        break;
                }
            }
        }

        [Summary("Submodule for retreiving log files")]
        public class LogModule : ModuleBase<SocketCommandContext>
        {
            private readonly LoggingService _logger;

            public LogModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("log")]
            [Summary("Retrieves a log file")]
            public async Task LogAsync(
                [Summary("The channel to get the log from")] string channel = "",
                [Summary("The date (in format (YYYY-MM-DD) to get the log from")] string date = "")
            {
                var settings = await FileHelper.LoadServerSettings(Context);
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
                {
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

                var errorMessage = await LogGetAsync(channel, date, Context);
                if (errorMessage.Contains("<ERROR>"))
                {
                    await ReplyAsync(errorMessage);
                    await _logger.Log($"log: {channel} {date} {errorMessage} <FAIL>", Context);
                    return;
                }

                await _logger.Log($"log: {channel} {date} <SUCCESS>", Context);
            }

            private async Task<string> LogGetAsync(string channelName, string date, SocketCommandContext context)
            {
                var settings = await FileHelper.LoadServerSettings(Context);
                await ReplyAsync($"Retrieving log from {channelName} on {date}...");
                var confirmedName = DiscordHelper.ConvertChannelPingToName(channelName, context);
                if (confirmedName.Contains("<ERROR>"))
                {
                    return confirmedName;
                }

                if (settings.adminChannel <= 0)
                {
                    return "<ERROR> Admin channel not set.";
                }

                var filepath = FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, confirmedName, date);
                if (!File.Exists(filepath))
                {
                    return "<ERROR> File does not exist";
                }

                var logPostChannel = context.Guild.GetTextChannel(settings.logPostChannel);
                await logPostChannel.SendFileAsync(filepath, $"{confirmedName}-{date}.log");
                return "SUCCESS";
            }
        }
    }
}

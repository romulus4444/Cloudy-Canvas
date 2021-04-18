namespace Cloudy_Canvas.Modules
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    [Summary("Module for managing admin functions")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingHelperService _logger;

        public AdminModule(LoggingHelperService logger)
        {
            _logger = logger;
        }

        [Command("setup")]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupAsync([Summary("Admin channel name")] string adminChannelName = "", [Remainder] [Summary("Admin role name")] string adminRoleName = "")
        {
            SocketTextChannel adminChannel;
            SocketRole adminRole;
            await ReplyAsync("Moving in to my new place...");
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(adminChannelName, Context);
            var channelSetName = DiscordHelper.ConvertChannelPingToName(adminChannelName, Context);
            if (channelSetId > 0)
            {
                if (channelSetName.Contains("<ERROR>"))
                {
                    await SetAdminChannelAsync(channelSetId, adminChannelName);
                }
                else
                {
                    await SetAdminChannelAsync(channelSetId, channelSetName);
                }

                await ReplyAsync("Choosing my room...");
            }
            else
            {
                await ReplyAsync($"I couldn't find a room called #{adminChannelName}.");
                await _logger.Log($"setup: channel {adminChannelName} <FAIL> 1, role {adminRoleName} NOT CHECKED", Context);
                return;
            }

            var adminChannelId = await GetAdminChannelAsync(Context);
            if (adminChannelId > 0)
            {
                adminChannel = Context.Guild.GetTextChannel(adminChannelId);
                await adminChannel.SendMessageAsync($"Moved into <#{adminChannelId}>!");
            }
            else
            {
                await ReplyAsync("Admin channel unable to be set. Please try again.");
                await _logger.Log($"setup: channel {adminChannelName} <FAIL> 2, role {adminRoleName} NOT CHECKED", Context);
                return;
            }

            await adminChannel.SendMessageAsync("Getting to know the neighbors...");
            var roleSetId = await DiscordHelper.GetRoleIdIfAccessAsync(adminRoleName, Context);
            var roleSetName = DiscordHelper.ConvertRolePingToNameAsync(adminRoleName, Context);
            if (roleSetId > 0)
            {
                if (roleSetName.Contains("<ERROR>"))
                {
                    await SetAdminRoleAsync(roleSetId, adminRoleName);
                }
                else
                {
                    await SetAdminRoleAsync(roleSetId, roleSetName);
                }

                await ReplyAsync("Finding the mayor...");
            }
            else
            {
                await ReplyAsync($"I couldn't find @{adminRoleName}.");
                await _logger.Log($"setup: channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL> 1", Context);
                return;
            }

            var adminRoleId = await DiscordHelper.GetAdminRoleAsync(Context);
            if (adminRoleId > 0)
            {
                await adminChannel.SendMessageAsync($"<@&{adminRoleId}> is in charge now!");
            }
            else
            {
                await adminChannel.SendMessageAsync("Admin role Unable to be set. Please try again.");
                await _logger.Log($"setup: channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL> 2", Context);
                return;
            }

            await adminChannel.SendMessageAsync("I'm all set! Type `;help admin` for a list of other admin setup commands.");
            await _logger.Log($"setup: channel {adminChannelName} <SUCCESS>, role {adminRoleName} <SUCCESS>", Context);
        }

        [Command("admin")]
        [Summary("Manages admin commands")]
        public async Task AdminAsync(
            [Summary("First subcommand")] string commandOne = "",
            [Summary("Second subcommand")] string commandTwo = "",
            [Remainder] [Summary("Third subcommand")] string commandThree = "")
        {
            if (!await DiscordHelper.DoesUserHaveAdminRoleAsync(Context))
            {
                return;
            }

            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    await _logger.Log($"admin: <FAIL>", Context);
                    break;
                case "adminchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminChannelGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminChannelSetAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context);
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
                            await IgnoreChannelGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreChannelAddAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context);
                            break;
                        case "remove":
                            await IgnoreChannelRemoveAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context);
                            break;
                        case "clear":
                            await IgnoreChannelClearAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
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
                            await IgnoreRoleGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo}  <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreRoleAddAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context);
                            break;
                        case "remove":
                            await IgnoreRoleRemoveAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context);
                            break;
                        case "clear":
                            await IgnoreRoleClearAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
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
                            await AdminRoleGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminRoleSetAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context);
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
            if (!await DiscordHelper.DoesUserHaveAdminRoleAsync(Context))
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
                    await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context);
                    return;
                }


                await ReplyAsync("I can't send a message there.");
                await _logger.Log($"echo: {channelName} {message} <FAIL>", Context);
                return;
            }

            await ReplyAsync($"{channelName} {message}");
            await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context);
        }

        private static async Task ClearIgnoreRoleAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredRoles", "txt", context);
            await File.WriteAllTextAsync(filename, "");
        }

        private static async Task<bool> RemoveIgnoreRoleAsync(ulong roleRemoveId, SocketCommandContext context)
        {
            var removed = false;
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredRoles", "txt", context);
            if (!File.Exists(filename))
            {
                return false;
            }

            var roleList = (await File.ReadAllLinesAsync(filename)).ToList();
            for (var x = roleList.Count - 1; x >= 0; x--)
            {
                if (!roleList[x].Contains(roleRemoveId.ToString()))
                {
                    continue;
                }

                roleList.Remove(roleList[x]);
                removed = true;
            }

            if (removed)
            {
                await File.WriteAllLinesAsync(filename, roleList);
            }

            return removed;
        }

        private static async Task<ulong> GetAdminChannelAsync(SocketCommandContext context)
        {
            var setting = await FileHelper.GetSetting("adminchannel", context);
            ulong channelId = 0;
            if (setting.Contains("<ERROR>"))
            {
                return channelId;
            }

            var split = setting.Split("<#", 2)[1].Split('>', 2)[0];
            channelId = ulong.Parse(split);
            return channelId;
        }

        private static async Task<bool> AddIgnoreChannelAsync(ulong channelId, string channelName, SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                await File.WriteAllTextAsync(filename, $"<#{channelId}> #{channelName}\n");
                return true;
            }

            var channelList = await File.ReadAllLinesAsync(filename);
            foreach (var channel in channelList)
            {
                if (channel.Contains(channelId.ToString()))
                {
                    return false;
                }
            }

            await File.AppendAllTextAsync(filename, $"<#{channelId}> #{channelName}\n");
            return true;
        }

        private static async Task<bool> RemoveIgnoreChannelAsync(ulong channelId, SocketCommandContext context)
        {
            var removed = false;
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                return false;
            }

            var channelList = (await File.ReadAllLinesAsync(filename)).ToList();
            for (var x = channelList.Count - 1; x >= 0; x--)
            {
                if (!channelList[x].Contains(channelId.ToString()))
                {
                    continue;
                }

                channelList.Remove(channelList[x]);
                removed = true;
            }

            if (removed)
            {
                await File.WriteAllLinesAsync(filename, channelList);
            }

            return removed;
        }

        private static async Task ClearIgnoreChannelAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            await File.WriteAllTextAsync(filename, "");
        }

        private static async Task<bool> AddIgnoreRoleAsync(ulong roleId, string roleName, SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredRoles", "txt", context);
            if (!File.Exists(filename))
            {
                await File.WriteAllTextAsync(filename, $"<@&{roleId}> @{roleName}\n");
                return true;
            }

            var roleList = await File.ReadAllLinesAsync(filename);
            foreach (var role in roleList)
            {
                if (role.Contains(roleId.ToString()))
                {
                    return false;
                }
            }

            await File.AppendAllTextAsync(filename, $"<@&{roleId}> @{roleName}\n");
            return true;
        }

        private async Task AdminRoleSetAsync(string roleName)
        {
            var roleSetId = await DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            var roleSetName = DiscordHelper.ConvertRolePingToNameAsync(roleName, Context);
            if (roleSetId > 0)
            {
                if (roleSetName.Contains("<ERROR>"))
                {
                    await SetAdminRoleAsync(roleSetId, roleName);
                }
                else
                {
                    await SetAdminRoleAsync(roleSetId, roleSetName);
                }

                await ReplyAsync($"Admin role set to <@&{roleSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.");
            }
        }

        private async Task SetAdminRoleAsync(ulong roleId, string roleName)
        {
            await FileHelper.SetSetting("adminrole", $"<@&{roleId}> @{roleName}", Context);
        }

        private async Task AdminRoleGetAsync()
        {
            var roleGetId = await DiscordHelper.GetAdminRoleAsync(Context);
            if (roleGetId > 0)
            {
                await ReplyAsync($"Admin role is <@&{roleGetId}>");
            }
            else
            {
                await ReplyAsync("Admin role not set yet.");
            }
        }

        private async Task IgnoreRoleClearAsync()
        {
            await ClearIgnoreRoleAsync(Context);
            await ReplyAsync("Ignore role list cleared.");
        }

        private async Task IgnoreRoleRemoveAsync(string roleName)
        {
            var roleRemoveId = await DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleRemoveId > 0)
            {
                var removed = await RemoveIgnoreRoleAsync(roleRemoveId, Context);

                if (removed)
                {
                    await ReplyAsync($"Removed <@&{roleRemoveId}> from ignore list.");
                }
                else
                {
                    await ReplyAsync($"<@&{roleRemoveId}> was not on the list.");
                }
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.");
            }
        }

        private async Task IgnoreRoleAddAsync(string roleName)
        {
            var roleAddId = await DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            var roleAddName = DiscordHelper.ConvertRolePingToNameAsync(roleName, Context);
            if (roleAddId > 0)
            {
                bool added;
                if (roleAddName.Contains("<ERROR>"))
                {
                    added = await AddIgnoreRoleAsync(roleAddId, roleName, Context);
                }
                else
                {
                    added = await AddIgnoreRoleAsync(roleAddId, roleAddName, Context);
                }

                if (added)
                {
                    await ReplyAsync($"Added <@&{roleAddId}> to ignore list.");
                }
                else
                {
                    await ReplyAsync($"<@&{roleAddId}> is already on the list.");
                }
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.");
            }
        }

        private async Task IgnoreRoleGetAsync()
        {
            var roleList = await DiscordHelper.GetIgnoredRolesAsync(Context);
            if (roleList.Count > 0)
            {
                var output = "__Role Ignore List:__\n";
                foreach (var role in roleList)
                {
                    output += $"<@&{role}>\n";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("No roles on ignore list.");
            }
        }

        private async Task AdminChannelSetAsync(string channelName)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            var channelSetName = DiscordHelper.ConvertChannelPingToName(channelName, Context);
            if (channelSetId > 0)
            {
                if (channelSetName.Contains("<ERROR>"))
                {
                    await SetAdminChannelAsync(channelSetId, channelName);
                }
                else
                {
                    await SetAdminChannelAsync(channelSetId, channelSetName);
                }

                await ReplyAsync($"Admin channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task AdminChannelGetAsync()
        {
            var channelGetId = await GetAdminChannelAsync(Context);
            if (channelGetId > 0)
            {
                await ReplyAsync($"Admin channel is <#{channelGetId}>");
            }
            else
            {
                await ReplyAsync("Admin channel not set yet.");
            }
        }

        private async Task IgnoreChannelGetAsync()
        {
            var channelList = await DiscordHelper.GetIgnoredChannelsAsync(Context);
            if (channelList.Count > 0)
            {
                var output = "__Channel Ignore List:__\n";
                foreach (var channel in channelList)
                {
                    output += $"<#{channel}>\n";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("No channels on ignore list.");
            }
        }

        private async Task IgnoreChannelClearAsync()
        {
            await ClearIgnoreChannelAsync(Context);
            await ReplyAsync("Ignore channel list cleared.");
        }

        private async Task IgnoreChannelRemoveAsync(string channelName)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelRemoveId > 0)
            {
                var removed = await RemoveIgnoreChannelAsync(channelRemoveId, Context);

                if (removed)
                {
                    await ReplyAsync($"Removed <#{channelRemoveId}> from ignore list.");
                }
                else
                {
                    await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
                }
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreChannelAddAsync(string channelName)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            var channelAddName = DiscordHelper.ConvertChannelPingToName(channelName, Context);
            if (channelAddId > 0)
            {
                bool added;
                if (channelAddName.Contains("<ERROR>"))
                {
                    added = await AddIgnoreChannelAsync(channelAddId, channelName, Context);
                }
                else
                {
                    added = await AddIgnoreChannelAsync(channelAddId, channelAddName, Context);
                }

                if (added)
                {
                    await ReplyAsync($"Added <#{channelAddId}> to ignore list.");
                }
                else
                {
                    await ReplyAsync($"<#{channelAddId}> is already on the list.");
                }
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task SetAdminChannelAsync(ulong channelId, string channelName)
        {
            await FileHelper.SetSetting("adminchannel", $"<#{channelId}> #{channelName}", Context);
        }

        [Summary("Submodule for managing the blacklist")]
        public class BlacklistModule : ModuleBase<SocketCommandContext>
        {
            private readonly BlacklistService _blacklistService;

            private readonly LoggingHelperService _logger;

            public BlacklistModule(BlacklistService blacklistService, LoggingHelperService logger)
            {
                _blacklistService = blacklistService;
                _logger = logger;
            }

            [Command("blacklist")]
            [Summary("Manages the search term blacklist")]
            public async Task Blacklist([Summary("Subcommand")] string command = "", [Remainder] [Summary("Search term")] string term = "")
            {
                if (!await DiscordHelper.DoesUserHaveAdminRoleAsync(Context))
                {
                    return;
                }

                _blacklistService.InitializeList(Context);
                switch (command)
                {
                    case "":
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
                        await _logger.Log($"blacklist invalid: {command}", Context);
                        break;
                }
            }
        }
    }
}

namespace Cloudy_Canvas.Modules
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("setup")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupAsync(string adminChannelName = "", [Remainder]string adminRoleName = "")
        {
            SocketTextChannel adminChannel;
            SocketRole adminRole;
            await ReplyAsync("Moving in to my new place...");
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(adminChannelName, Context);
            var channelSetName = await DiscordHelper.ConvertChannelPingToNameAsync(adminChannelName, Context);
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
                return;
            }

            await adminChannel.SendMessageAsync("Getting to know the neighbors...");
            var roleSetId = await DiscordHelper.GetRoleIdIfAccessAsync(adminRoleName, Context);
            var roleSetName = await DiscordHelper.ConvertRolePingToNameAsync(adminRoleName, Context);
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
            }
            var adminRoleId = await GetAdminRoleAsync(Context);
            if (adminRoleId > 0)
            {
                await adminChannel.SendMessageAsync($"<@&{adminRoleId}> is in charge now!");
            }
            else
            {
                await adminChannel.SendMessageAsync("Admin role Unable to be set. Please try again.");
                return;
            }
            await adminChannel.SendMessageAsync("I'm all set! Type `;help admin` for a list of other admin setup commands.");
        }
        [Command("admin")]
        public async Task AdminAsync(string commandOne = "", string commandTwo = "", [Remainder] string commandThree = "")
        {
            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    break;
                case "adminchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            break;
                        case "get":
                            await AdminChannelGetAsync();
                            break;
                        case "set":
                            await AdminChannelSetAsync(commandThree);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            break;
                    }

                    break;
                case "ignorechannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            break;
                        case "get":
                            await IgnoreChannelGetAsync();
                            break;
                        case "add":
                            await IgnoreChannelAddAsync(commandThree);
                            break;
                        case "remove":
                            await IgnoreChannelRemoveAsync(commandThree);
                            break;
                        case "clear":
                            await IgnoreChannelClearAsync();
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            break;
                    }

                    break;
                case "ignorerole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            break;
                        case "get":
                            await IgnoreRoleGetAsync();
                            break;
                        case "add":
                            await IgnoreRoleAddAsync(commandThree);
                            break;
                        case "remove":
                            await IgnoreRoleRemoveAsync(commandThree);
                            break;
                        case "clear":
                            await IgnoreRoleClearAsync();
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            break;
                    }

                    break;
                case "adminrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            break;
                        case "get":
                            await AdminRoleGetAsync();
                            break;
                        case "set":
                            await AdminRoleSetAsync(commandThree);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            break;
                    }

                    break;
                default:
                    await ReplyAsync($"Invalid command `{commandOne}`");
                    break;
            }
        }

        private static async Task<ulong> GetAdminRoleAsync(SocketCommandContext context)
        {
            var setting = await FileHelper.GetSetting("adminrole", context);
            ulong roleId = 0;
            if (setting.Contains("<ERROR>"))
            {
                return roleId;
            }

            var split = setting.Split("<@&", 2)[1].Split('>', 2)[0];
            roleId = ulong.Parse(split);
            return roleId;
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

        private static async Task<List<ulong>> GetIgnoredRolesAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredRoles", "txt", context);
            if (!File.Exists(filename))
            {
                return new List<ulong>();
            }

            var roleList = await File.ReadAllLinesAsync(filename);
            var roleIdList = new List<ulong>();
            foreach (var role in roleList)
            {
                var two = "";
                var one = role.Split("> @", 2)[0].Substring(3);
                roleIdList.Add(ulong.Parse(one));
            }

            return roleIdList;
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

        private static async Task<List<ulong>> GetIgnoredChannelsAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                return new List<ulong>();
            }

            var channelList = await File.ReadAllLinesAsync(filename);
            var channelIdList = new List<ulong>();
            foreach (var channel in channelList)
            {
                channelIdList.Add(ulong.Parse(channel.Split("> #", 2)[0].Substring(2)));
            }

            return channelIdList;
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
            var roleSetName = await DiscordHelper.ConvertRolePingToNameAsync(roleName, Context);
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
            var roleGetId = await GetAdminRoleAsync(Context);
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
            var roleAddName = await DiscordHelper.ConvertRolePingToNameAsync(roleName, Context);
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
            var roleList = await GetIgnoredRolesAsync(Context);
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
            var channelSetName = await DiscordHelper.ConvertChannelPingToNameAsync(channelName, Context);
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
            var channelList = await GetIgnoredChannelsAsync(Context);
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

        private async Task IgnoreChannelAddAsync(string channeName)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channeName, Context);
            var channelAddName = await DiscordHelper.ConvertChannelPingToNameAsync(channeName, Context);
            if (channelAddId > 0)
            {
                bool added;
                if (channelAddName.Contains("<ERROR>"))
                {
                    added = await AddIgnoreChannelAsync(channelAddId, channeName, Context);
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
                await ReplyAsync($"Invalid channel name #{channeName}.");
            }
        }

        private async Task SetAdminChannelAsync(ulong channelId, string channelName)
        {
            await FileHelper.SetSetting("adminchannel", $"<#{channelId}> #{channelName}", Context);
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

namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Admin;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    /// <summary>
    /// ";admin &lt;setting&gt; &lt;action&gt; ..." for the server's settings. Only bot admins can use it (see <see cref="RequireBotAdminAttribute"/>).
    /// The commands only resolve names through Discord, save, reply and log; what each action does is in <see cref="SettingOperations"/>
    /// and the wording of each setting is in <see cref="AdminSettings"/>. Run mode is asynchronous by default (see AddCloudyCanvas).
    /// </summary>
    [Group("admin")]
    [RequireBotAdmin]
    [Summary("Manages the server's admin settings")]
    public class AdminSettingsModule : BotModuleBase
    {
        private readonly LoggingService _logger;
        private readonly BooruService _booru;
        private readonly AllPreloadedSettings _servers;

        public AdminSettingsModule(LoggingService logger, BooruService booru, AllPreloadedSettings servers)
        {
            _logger = logger;
            _booru = booru;
            _servers = servers;
        }

        /// <summary>Takes anything that no specific command below took: ";admin", an unknown setting or action, or unusable arguments.</summary>
        [Command]
        [Priority(-1)]
        public async Task UnrecognisedAsync([Remainder] string rest = "")
        {
            var reply = AdminGrammar.Explain(rest);
            await ReplyAsync(reply.Message);
            await _logger.Log(reply.LogText, Context);
        }

        // ---- filter ---------------------------------------------------------------------------------------------------

        [Command("filter get")]
        public Task FilterGetAsync([Remainder] string ignored = "") =>
            RunAsync("filter", "get", null, s => Task.FromResult(AdminOutcome.Info($"The current filter is <https://manebooru.art/filters/{s.DefaultFilterId}>")));

        [Command("filter set")]
        public async Task FilterSetAsync([Remainder] string filter = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            var prefix = (await FileHelper.LoadServerPresettingsAsync(Context)).Prefix;
            var succeeded = true;

            if (!int.TryParse(filter?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var requestedFilter))
            {
                await ReplyAsync($"Invalid filter {filter}. A filter is identified by its number.");
                succeeded = false;
            }
            else
            {
                var check = await _booru.CheckFilterAsync(requestedFilter);
                if (check.Status == BooruStatus.Ok)
                {
                    settings.DefaultFilterId = check.FilterId;
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Filter set to {check.FilterId}. Please wait while the spoiler list is rebuilt.");
                    if (await _booru.RefreshListsAsync(Context, settings) == BooruStatus.Ok)
                    {
                        await ReplyAsync($"The lists have been refreshed for Filter {check.FilterId}");
                    }
                    else
                    {
                        await ReplyAsync($"The filter is set, but I couldn't reach Manebooru to rebuild the spoiler list. Please run `{prefix}refreshlists` in a little while.");
                    }
                }
                else if (check.Status == BooruStatus.NotFound)
                {
                    await ReplyAsync($"Invalid filter {filter}. Make sure the requested filter exists and is set to public");
                    succeeded = false;
                }
                else
                {
                    await ReplyAsync("I can't reach Manebooru right now to check that filter. Please try again in a little while.");
                    succeeded = false;
                }
            }

            await LogAsync("filter", "set", filter, succeeded, true);
        }

        // ---- channels: admin, watch alerts, report alerts, logs -------------------------------------------------------

        [Command("adminchannel get")]
        public Task AdminChannelGetAsync([Remainder] string ignored = "") => GetChannelAsync(AdminSettings.AdminChannel);

        [Command("adminchannel set")]
        public Task AdminChannelSetAsync([Remainder] string channel = "") => SetChannelAsync(AdminSettings.AdminChannel, channel);

        [Command("watchchannel get")]
        public Task WatchChannelGetAsync([Remainder] string ignored = "") => GetChannelAsync(AdminSettings.WatchChannel);

        [Command("watchchannel set")]
        public Task WatchChannelSetAsync([Remainder] string channel = "") => SetChannelAsync(AdminSettings.WatchChannel, channel);

        [Command("watchchannel clear")]
        public Task WatchChannelClearAsync([Remainder] string ignored = "") => ClearChannelAsync(AdminSettings.WatchChannel);

        [Command("reportchannel get")]
        public Task ReportChannelGetAsync([Remainder] string ignored = "") => GetChannelAsync(AdminSettings.ReportChannel);

        [Command("reportchannel set")]
        public Task ReportChannelSetAsync([Remainder] string channel = "") => SetChannelAsync(AdminSettings.ReportChannel, channel);

        [Command("reportchannel clear")]
        public Task ReportChannelClearAsync([Remainder] string ignored = "") => ClearChannelAsync(AdminSettings.ReportChannel);

        [Command("logchannel get")]
        public Task LogChannelGetAsync([Remainder] string ignored = "") => GetChannelAsync(AdminSettings.LogChannel);

        [Command("logchannel set")]
        public Task LogChannelSetAsync([Remainder] string channel = "") => SetChannelAsync(AdminSettings.LogChannel, channel);

        [Command("logchannel clear")]
        public Task LogChannelClearAsync([Remainder] string ignored = "") => ClearChannelAsync(AdminSettings.LogChannel);

        // ---- roles: admin, watch alerts, report alerts ----------------------------------------------------------------

        [Command("adminrole get")]
        public Task AdminRoleGetAsync([Remainder] string ignored = "") => GetRoleAsync(AdminSettings.AdminRole);

        [Command("adminrole set")]
        public Task AdminRoleSetAsync([Remainder] string role = "") => SetRoleAsync(AdminSettings.AdminRole, role);

        [Command("watchrole get")]
        public Task WatchRoleGetAsync([Remainder] string ignored = "") => GetRoleAsync(AdminSettings.WatchRole);

        [Command("watchrole set")]
        public Task WatchRoleSetAsync([Remainder] string role = "") => SetRoleAsync(AdminSettings.WatchRole, role);

        [Command("watchrole clear")]
        public Task WatchRoleClearAsync([Remainder] string ignored = "") => ClearRoleAsync(AdminSettings.WatchRole);

        [Command("reportrole get")]
        public Task ReportRoleGetAsync([Remainder] string ignored = "") => GetRoleAsync(AdminSettings.ReportRole);

        [Command("reportrole set")]
        public Task ReportRoleSetAsync([Remainder] string role = "") => SetRoleAsync(AdminSettings.ReportRole, role);

        [Command("reportrole clear")]
        public Task ReportRoleClearAsync([Remainder] string ignored = "") => ClearRoleAsync(AdminSettings.ReportRole);

        // ---- lists: ignored channels, ignored roles, allowed users ----------------------------------------------------

        [Command("ignorechannel get")]
        public Task IgnoreChannelGetAsync([Remainder] string ignored = "") => GetListAsync(AdminSettings.IgnoreChannel);

        [Command("ignorechannel add")]
        public Task IgnoreChannelAddAsync([Remainder] string channel = "") => ChangeListAsync(AdminSettings.IgnoreChannel, "add", channel, SettingOperations.AddToList);

        [Command("ignorechannel remove")]
        public Task IgnoreChannelRemoveAsync([Remainder] string channel = "") => ChangeListAsync(AdminSettings.IgnoreChannel, "remove", channel, SettingOperations.RemoveFromList);

        [Command("ignorechannel clear")]
        public Task IgnoreChannelClearAsync([Remainder] string ignored = "") => ClearListAsync(AdminSettings.IgnoreChannel);

        [Command("ignorerole get")]
        public Task IgnoreRoleGetAsync([Remainder] string ignored = "") => GetListAsync(AdminSettings.IgnoreRole);

        [Command("ignorerole add")]
        public Task IgnoreRoleAddAsync([Remainder] string role = "") => ChangeListAsync(AdminSettings.IgnoreRole, "add", role, SettingOperations.AddToList);

        [Command("ignorerole remove")]
        public Task IgnoreRoleRemoveAsync([Remainder] string role = "") => ChangeListAsync(AdminSettings.IgnoreRole, "remove", role, SettingOperations.RemoveFromList);

        [Command("ignorerole clear")]
        public Task IgnoreRoleClearAsync([Remainder] string ignored = "") => ClearListAsync(AdminSettings.IgnoreRole);

        [Command("allowuser get")]
        public Task AllowUserGetAsync([Remainder] string ignored = "") => GetListAsync(AdminSettings.AllowUser);

        [Command("allowuser add")]
        public Task AllowUserAddAsync([Remainder] string user = "") => ChangeListAsync(AdminSettings.AllowUser, "add", user, SettingOperations.AddToList);

        [Command("allowuser remove")]
        public Task AllowUserRemoveAsync([Remainder] string user = "") => ChangeListAsync(AdminSettings.AllowUser, "remove", user, SettingOperations.RemoveFromList);

        [Command("allowuser clear")]
        public Task AllowUserClearAsync([Remainder] string ignored = "") => ClearListAsync(AdminSettings.AllowUser);

        // ---- channel-specific filters ---------------------------------------------------------------------------------

        [Command("filterchannel get")]
        public Task FilterChannelGetAsync([Remainder] string ignored = "") =>
            RunAsync("filterchannel", "get", null, s => Task.FromResult(SettingOperations.GetFilterChannels(s)));

        [Command("filterchannel add")]
        public Task FilterChannelAddAsync(string channel = "", int filterId = 175) =>
            RunAsync("filterchannel", "add", $"{channel} {filterId}".Trim(), async s =>
            {
                var channelId = string.IsNullOrWhiteSpace(channel) ? 0 : await DiscordHelper.GetChannelIdIfAccessAsync(channel, Context);
                var problem = SettingOperations.CheckChannelArgument(channelId, channel);
                if (problem != null)
                {
                    return problem;
                }

                var check = await _booru.CheckFilterAsync(filterId);
                if (check.Status == BooruStatus.NotFound)
                {
                    return AdminOutcome.Failure(
                        $"Invalid filter {filterId}. Please make sure that filter exists and is public. {Mentions.Channel(channelId)} will not be added to the list at this time.");
                }

                if (check.Status != BooruStatus.Ok)
                {
                    return AdminOutcome.Failure(
                        $"I can't reach Manebooru right now to check filter {filterId}. {Mentions.Channel(channelId)} will not be added to the list at this time; please try again in a little while.");
                }

                return SettingOperations.AddFilterChannel(s, channelId, check.FilterId);
            });

        [Command("filterchannel remove")]
        public Task FilterChannelRemoveAsync([Remainder] string channel = "") =>
            RunAsync("filterchannel", "remove", channel, async s =>
            {
                var channelId = string.IsNullOrWhiteSpace(channel) ? 0 : await DiscordHelper.GetChannelIdIfAccessAsync(channel, Context);
                return SettingOperations.RemoveFilterChannel(s, channelId, channel);
            });

        [Command("filterchannel clear")]
        public Task FilterChannelClearAsync([Remainder] string ignored = "") =>
            RunAsync("filterchannel", "clear", null, s => Task.FromResult(SettingOperations.ClearFilterChannels(s)));

        // ---- shared plumbing ------------------------------------------------------------------------------------------

        private Task GetChannelAsync(ChannelSetting setting) =>
            RunAsync(setting.Key, "get", null, s => Task.FromResult(SettingOperations.GetChannel(setting, s)));

        private Task SetChannelAsync(ChannelSetting setting, string typed) =>
            RunAsync(
                setting.Key,
                "set",
                typed,
                async s =>
                {
                    if (setting.UpdatesGuildList && Context.Guild == null)
                    {
                        return AdminOutcome.Failure("The admin channel can only be set in a server.");
                    }

                    var channelId = string.IsNullOrWhiteSpace(typed) ? 0 : await DiscordHelper.GetChannelIdIfAccessAsync(typed, Context);
                    return SettingOperations.SetChannel(setting, s, channelId, typed);
                },
                async s =>
                {
                    if (setting.UpdatesGuildList)
                    {
                        // The bot also keeps every server's admin channel in one list, used to announce things to all servers.
                        _servers.GuildList[Context.Guild.Id] = setting.Get(s);
                        await FileHelper.SaveAllPresettingsAsync(_servers);
                    }
                });

        private Task ClearChannelAsync(ChannelSetting setting) =>
            RunAsync(setting.Key, "clear", null, s => Task.FromResult(SettingOperations.ClearChannel(setting, s)));

        private Task GetRoleAsync(RoleSetting setting) =>
            RunAsync(setting.Key, "get", null, s => Task.FromResult(SettingOperations.GetRole(setting, s)));

        private Task SetRoleAsync(RoleSetting setting, string typed) =>
            RunAsync(setting.Key, "set", typed, async s => SettingOperations.SetRole(setting, s, string.IsNullOrWhiteSpace(typed) ? 0 : await DiscordHelper.GetRoleIdAsync(typed, Context), typed));

        private Task ClearRoleAsync(RoleSetting setting) =>
            RunAsync(setting.Key, "clear", null, s => Task.FromResult(SettingOperations.ClearRole(setting, s)));

        private Task GetListAsync(ListSetting setting) =>
            RunAsync(setting.Key, "get", null, s => Task.FromResult(SettingOperations.GetList(setting, s)));

        private Task ClearListAsync(ListSetting setting) =>
            RunAsync(setting.Key, "clear", null, s => Task.FromResult(SettingOperations.ClearList(setting, s)));

        private Task ChangeListAsync(ListSetting setting, string action, string typed, Func<ListSetting, ServerSettings, ulong, string, AdminOutcome> change) =>
            RunAsync(setting.Key, action, typed, async s => change(setting, s, string.IsNullOrWhiteSpace(typed) ? 0 : await ResolveAsync(setting, typed), typed));

        /// <summary>Turns what the admin typed into the id of a channel, role or user, or 0 if it matches nothing.</summary>
        private async Task<ulong> ResolveAsync(ListSetting setting, string typed)
        {
            return setting.Noun switch
            {
                "channel" => await DiscordHelper.GetChannelIdIfAccessAsync(typed, Context),
                "role" => await DiscordHelper.GetRoleIdAsync(typed, Context),
                _ => await DiscordHelper.GetUserIdAsync(typed, Context),
            };
        }

        /// <summary>Loads the server's settings, runs the operation, saves if it changed anything, then replies and logs.</summary>
        private async Task RunAsync(
            string key,
            string action,
            string argument,
            Func<ServerSettings, Task<AdminOutcome>> operation,
            Func<ServerSettings, Task> afterChange = null)
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            var outcome = await operation(settings);
            if (outcome.Changed)
            {
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                if (afterChange != null)
                {
                    await afterChange(settings);
                }
            }

            await ReplyAsync(outcome.Message);
            await LogAsync(key, action, argument, outcome.Success, action != "get");
        }

        private Task LogAsync(string key, string action, string argument, bool success, bool toFile)
        {
            var command = string.IsNullOrWhiteSpace(argument) ? $"admin: {key} {action}" : $"admin: {key} {action} {argument}";
            return _logger.Log($"{command} {(success ? "<SUCCESS>" : "<FAIL>")}", Context, toFile);
        }
    }
}

namespace Cloudy_Canvas.Modules
{
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Setting the bot up in a server and keeping its lists up to date")]
    public class SetupModule : BotModuleBase
    {
        private readonly LoggingService _logger;
        private readonly BooruService _booru;
        private readonly AllPreloadedSettings _servers;

        public SetupModule(LoggingService logger, BooruService booru, AllPreloadedSettings servers)
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

        [Command("getsettings", RunMode = RunMode.Async)]
        [Summary("Posts the settings file to the log channel")]
        [RequireBotAdmin]
        public async Task GetSettingsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
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
        [RequireBotAdmin]
        public async Task RefreshListsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            var prefix = serverPresettings.Prefix;
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
    }
}

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
        private readonly AllPreloadedSettings _servers;

        public AdminModule(LoggingService logger, BooruService booru, AllPreloadedSettings servers)
        {
            _logger = logger;
            _booru = booru;
            _servers = servers;
        }

        [Command("setup")]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupCommandAsync(
            int filterId,
            [Summary("Admin channel name")] string adminChannelName = "",
            [Remainder] [Summary("Admin role name")] string adminRoleName = "")
        {
            var settings = new ServerSettings();
            ulong channelSetId;
            var checkedFilterId = await _booru.CheckFilterAsync(filterId);
            if (checkedFilterId == 0)
            {
                await ReplyAsync(
                    "I could not find that filter; please make sure it exists and is set to public. You may change the filter later with `;admin filter set <filterId>`. Continuing setup with my default filter of 175.");
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
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(adminRoleName, Context);
            if (roleSetId > 0)
            {
                settings.AdminRole = roleSetId;
                await ReplyAsync($"<@&{roleSetId}> is in charge now!", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"I couldn't find @{adminRoleName}. Plese assign an admin role with ;admin adminrole set role. Continuing without an admin role.");
                await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL>", Context, true);
            }

            await ReplyAsync("Setting the remaining admin settings to default values (all alerts will post to the admin channel, and no roles will be pinged)...");
            settings.WatchAlertChannel = settings.AdminChannel;
            settings.LogPostChannel = settings.AdminChannel;
            settings.ReportChannel = settings.AdminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync(
                "Settings saved. Now building the spoiler list. This may take a few minutes, depending on how many tags are spoilered in the filter. Please wait until they are completed; I will let you know when I am finished.");
            await _booru.RefreshListsAsync(Context, settings);
            await ReplyAsync("The lists have been built. I'm all set! Type `;help admin` for a list of other admin setup commands.");
            await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <SUCCESS>", Context, true);
        }

        [Command("admin")]
        [Summary("Manages admin commands")]
        public async Task AdminCommandAsync(
            [Summary("First subcommand")] string commandOne = "",
            [Summary("Second subcommand")] string commandTwo = "",
            [Summary("Third subcommand")] string commandThree = "",
            [Remainder] [Summary("Fourth subcommand")] int commandFour = 175)
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
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
                case "filter":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await FilterGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await FilterSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

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
                            settings.IgnoredChannels.Clear();
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
                case "filterchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await FilterChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await FilterChannelAddAsync(commandThree, commandFour, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await FilterChannelRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.FilteredChannels.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Channel-specific filters cleared.");
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
                            settings.IgnoredRoles.Clear();
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
                            settings.AllowedUsers.Clear();
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
                case "watchchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await WatchChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await WatchChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await WatchChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "watchrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await WatchRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await WatchRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await WatchRoleClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "reportchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await ReportChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await ReportChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await ReportChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "reportrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await ReportRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await ReportRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await ReportRoleClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "logchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await LogChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await LogChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await LogChannelClearAsync(settings);
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
        public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "", [Remainder] [Summary("The message to send")] string message = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
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

        [Command("setprefix")]
        [Summary("Sets the bot listen prefix")]
        public async Task SetPrefixCommandAsync([Summary("The prefix character")] char prefix = ';')
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            serverPresettings.Prefix = prefix;
            await ReplyAsync($"I will now listen for '{prefix}' on this server.");
            _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
            await FileHelper.SaveAllPresettingsAsync(_servers);
        }

        [Command("listentobots")]
        [Summary("Sets the bot listen prefix")]
        public async Task ListenToBotsCommandAsync([Summary("yes or no")] string command = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            switch (command.ToLower())
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

        [Command("safemode")]
        [Summary("Sets the safemode")]
        public async Task SafeModeCommandAsync([Summary("yes or no")] string command = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }


            switch (command.ToLower())
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

        [Command("alias")]
        [Summary("Sets an alias")]
        public async Task AliasCommandAsync(string subcommand = "", string shortForm = "", [Remainder] string longForm = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
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
                    if (serverPresettings.Aliases.ContainsKey(shortForm))
                    {
                        serverPresettings.Aliases[shortForm] = longForm;
                        _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                        await FileHelper.SaveAllPresettingsAsync(_servers);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`, replacing what was there before.");
                    }
                    else
                    {
                        serverPresettings.Aliases.Add(shortForm, longForm);
                        _servers.Settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                        await FileHelper.SaveAllPresettingsAsync(_servers);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`");
                    }

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

        [Command("getsettings")]
        [Summary("Posts the settings file to the log channel")]
        public async Task GetSettingsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
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

        [Command("refreshlists")]
        [Summary("Refreshes the spoiler list and server settings")]
        public async Task RefreshListsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            var prefix = serverPresettings.Prefix;
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            await ReplyAsync("Refreshing spoiler list. This may take a few minutes.");
            await _booru.RefreshListsAsync(Context, settings);
            await ReplyAsync("Checking and saving server settings.");
            if (Context.IsPrivate)
            {
                settings.Name = $"{Context.User.Username}#{Context.User.Discriminator}";
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
            await ReplyAsync("Spoiler list and server settings refreshed!");
        }

        [Command("broadcast")]
        [Summary("Broadcasts a message to all servers")]
        [RequireOwner]
        public async Task BroadcastCommandAsync()
        {
            await ReplyAsync("Message broadcasted to each guild's admin channel.");
        }

        [Command("<blank message>")]
        [Summary("Runs on a blank message")]
        public async Task BlankMessageCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await ReplyAsync("Did you need something?");
        }

        [Command("<invalid command>")]
        [Summary("Runs on an invalid command")]
        public async Task InvalidCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await ReplyAsync("I don't know that command.");
        }

        [Command("<mention>")]
        [Summary("Runs on a name ping")]
        public async Task MentionCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            await ReplyAsync($"The current prefix is '{serverPresettings.Prefix}'. Type `{serverPresettings.Prefix}help` for a list of commands.");
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

        private async Task FilterSetAsync(string filter, ServerSettings settings)
        {
            var filterId = await _booru.CheckFilterAsync(int.Parse(filter));
            if (filterId > 0)
            {
                settings.DefaultFilterId = filterId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Filter set to {filterId}. Please wait while the spoiler list is rebuilt.");
                await _booru.RefreshListsAsync(Context, settings);
                await ReplyAsync($"The lists have been refreshed for Filter {filterId}");
            }
            else
            {
                await ReplyAsync($"Invalid filter {filter}. Make sure the requested filter exists and is set to public");
            }
        }

        private async Task FilterGetAsync(ServerSettings settings)
        {
            await ReplyAsync($"The current filter is <https://manebooru.art/filters/{settings.DefaultFilterId}>");
        }

        private async Task AdminChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.AdminChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                _servers.GuildList[Context.Guild.Id] = channelSetId;
                await FileHelper.SaveAllPresettingsAsync(_servers);
                await ReplyAsync($"Admin channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task AdminChannelGetAsync(ServerSettings settings)
        {
            if (settings.AdminChannel > 0)
            {
                await ReplyAsync($"Admin channel is <#{settings.AdminChannel}>");
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
                settings.AdminRole = roleSetId;
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
            if (settings.AdminRole > 0)
            {
                await ReplyAsync($"Admin role is <@&{settings.AdminRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Admin role not set yet.");
            }
        }

        private async Task IgnoreChannelGetAsync(ServerSettings settings)
        {
            if (settings.IgnoredChannels.Count > 0)
            {
                var output = $"__Channel Ignore List:__{Environment.NewLine}";
                foreach (var channel in settings.IgnoredChannels)
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
                for (var x = settings.IgnoredChannels.Count - 1; x >= 0; x--)
                {
                    var channel = settings.IgnoredChannels[x];
                    if (channel != channelRemoveId)
                    {
                        continue;
                    }

                    settings.IgnoredChannels.Remove(channel);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <#{channelRemoveId}> from ignore list.");
                    return;
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
                foreach (var channel in settings.IgnoredChannels)
                {
                    if (channel != channelAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<#{channelAddId}> is already on the list.");
                    return;
                }

                settings.IgnoredChannels.Add(channelAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <#{channelAddId}> to ignore list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task FilterChannelGetAsync(ServerSettings settings)
        {
            if (settings.FilteredChannels.Count > 0)
            {
                var output =
                    $"__Channel-Specific Filter List:__{Environment.NewLine}(Any channel not listed here uses the server filter {settings.DefaultFilterId}){Environment.NewLine}";
                foreach (var (channel, filter) in settings.FilteredChannels)
                {
                    output += $"<#{channel}>: Filter {filter}{Environment.NewLine}";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync($"No channel-specific filters are currently set. All channels use the server filter {settings.DefaultFilterId}.");
            }
        }

        private async Task FilterChannelRemoveAsync(string channelName, ServerSettings settings)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelRemoveId > 0)
            {
                for (var x = settings.FilteredChannels.Count - 1; x >= 0; x--)
                {
                    var channel = settings.FilteredChannels[x];
                    if (channel.Item1 != channelRemoveId)
                    {
                        continue;
                    }

                    settings.FilteredChannels.Remove(channel);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <#{channelRemoveId}> from channel-specific filter list.");
                    return;
                }

                await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task FilterChannelAddAsync(string channelName, int filterId, ServerSettings settings)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelAddId > 0)
            {
                var validFilter = await _booru.CheckFilterAsync(filterId);
                if (validFilter == 0)
                {
                    await ReplyAsync(
                        $"Invalid filter {filterId}. Please make sure that filter exists and is public. <#{channelAddId}> will not be added to the list at this time.");
                    return;
                }

                if (validFilter == settings.DefaultFilterId)
                {
                    await ReplyAsync("That's the server default filter already.");
                    return;
                }

                foreach (var channel in settings.FilteredChannels)
                {
                    if (channel.Item1 != channelAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"Updated the filter for <#{channelAddId}> from {channel.Item2} to {filterId}.");
                    settings.FilteredChannels.Remove(channel);
                    settings.FilteredChannels.Add(new Tuple<ulong, int>(channelAddId, filterId));
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    return;
                }

                settings.FilteredChannels.Add(new Tuple<ulong, int>(channelAddId, filterId));
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Set <#{channelAddId}> to use filter {filterId}.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreRoleRemoveAsync(string roleName, ServerSettings settings)
        {
            var roleRemoveId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleRemoveId > 0)
            {
                for (var x = settings.IgnoredRoles.Count - 1; x >= 0; x--)
                {
                    var role = settings.IgnoredRoles[x];
                    if (role != roleRemoveId)
                    {
                        continue;
                    }

                    settings.IgnoredRoles.Remove(role);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <@&{roleRemoveId}> from ignore list.", allowedMentions: AllowedMentions.None);
                    return;
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
                foreach (var role in settings.IgnoredRoles)
                {
                    if (role != roleAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@&{roleAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                settings.IgnoredRoles.Add(roleAddId);
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
            if (settings.IgnoredRoles.Count > 0)
            {
                var output = $"__Role Ignore List:__{Environment.NewLine}";
                foreach (var role in settings.IgnoredRoles)
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

        private async Task AllowUserRemoveAsync(string userName, ServerSettings settings)
        {
            var userRemoveId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userRemoveId > 0)
            {
                for (var x = settings.AllowedUsers.Count - 1; x >= 0; x--)
                {
                    var user = settings.AllowedUsers[x];
                    if (user != userRemoveId)
                    {
                        continue;
                    }

                    settings.AllowedUsers.Remove(user);
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
                foreach (var user in settings.AllowedUsers)
                {
                    if (user != userAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@{userAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                settings.AllowedUsers.Add(userAddId);
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
            if (settings.AllowedUsers.Count > 0)
            {
                var output = $"__Allowed User List:__{Environment.NewLine}";
                foreach (var user in settings.AllowedUsers)
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

        private async Task WatchChannelClearAsync(ServerSettings settings)
        {
            settings.WatchAlertChannel = settings.AdminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Watch alert channel reset to the current admin channel, <#{settings.WatchAlertChannel}>");
        }

        private async Task WatchChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.WatchAlertChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Watch alert channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task WatchChannelGetAsync(ServerSettings settings)
        {
            if (settings.WatchAlertChannel > 0)
            {
                await ReplyAsync($"Watch alerts are being posted in <#{settings.WatchAlertChannel}>");
            }
            else
            {
                await ReplyAsync("Watch alert channel not set yet.");
            }
        }

        private async Task WatchRoleClearAsync(ServerSettings settings)
        {
            settings.WatchAlertRole = 0;
            settings.WatchPing = false;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Watch alerts will not ping anyone now in <#{settings.WatchAlertChannel}>");
        }

        private async Task WatchRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.WatchAlertRole = roleSetId;
                settings.WatchPing = true;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Watch alerts will now ping <@&{settings.WatchAlertRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name #{roleName}.");
            }
        }

        private async Task WatchRoleGetAsync(ServerSettings settings)
        {
            if (settings.WatchAlertRole > 0)
            {
                await ReplyAsync($"Watch alerts will ping <@&{settings.WatchAlertRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Watch alert role not set yet.");
            }
        }

        private async Task ReportChannelClearAsync(ServerSettings settings)
        {
            settings.ReportChannel = settings.AdminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Report alert channel reset to the current admin channel, <#{settings.ReportChannel}>");
        }

        private async Task ReportChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.ReportChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Report alert channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task ReportChannelGetAsync(ServerSettings settings)
        {
            if (settings.ReportChannel > 0)
            {
                await ReplyAsync($"Report alerts are being posted in <#{settings.ReportChannel}>");
            }
            else
            {
                await ReplyAsync("Report alert channel not set yet.");
            }
        }

        private async Task ReportRoleClearAsync(ServerSettings settings)
        {
            settings.ReportRole = 0;
            settings.ReportPing = false;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Report alerts will not ping anyone now in <#{settings.ReportChannel}>");
        }

        private async Task ReportRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.ReportRole = roleSetId;
                settings.ReportPing = true;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Report alerts will now ping <@&{settings.ReportRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name #{roleName}.");
            }
        }

        private async Task ReportRoleGetAsync(ServerSettings settings)
        {
            if (settings.ReportRole > 0)
            {
                await ReplyAsync($"Report alerts will ping <@&{settings.ReportRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Report alert role not set yet.");
            }
        }

        private async Task LogChannelClearAsync(ServerSettings settings)
        {
            settings.LogPostChannel = settings.AdminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Report alert channel reset to the current admin channel, <#{settings.LogPostChannel}>");
        }

        private async Task LogChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.LogPostChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Retrieved logs will be sent to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task LogChannelGetAsync(ServerSettings settings)
        {
            if (settings.LogPostChannel > 0)
            {
                await ReplyAsync($"Logs are being posted in <#{settings.LogPostChannel}>");
            }
            else
            {
                await ReplyAsync("Log posting channel not set yet.");
            }
        }

        [Summary("Submodule for managing the watchlist")]
        public class BadlistModule : ModuleBase<SocketCommandContext>
        {
            private readonly LoggingService _logger;

            public BadlistModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("watchlist")]
            [Summary("Manages the search term watchlist")]
            public async Task WatchListCommandAsync([Summary("Subcommand")] string command = "", [Remainder] [Summary("Search term")] string term = "")
            {
                var settings = await FileHelper.LoadServerSettingsAsync(Context);
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
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
            public async Task LogCommandAsync(
                [Summary("The channel to get the log from")] string channel = "",
                [Summary("The date (in format (YYYY-MM-DD) to get the log from")] string date = "")
            {
                var settings = await FileHelper.LoadServerSettingsAsync(Context);
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
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
                await ReplyAsync($"Retrieving log from {channelName} on {date}...");
                var confirmedName = DiscordHelper.ConvertChannelPingToName(channelName, context);
                if (confirmedName.Contains("<ERROR>"))
                {
                    return confirmedName;
                }

                if (settings.LogPostChannel <= 0)
                {
                    return "<ERROR> Log post channel not set.";
                }

                var filepath = FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, confirmedName, date);
                if (!File.Exists(filepath))
                {
                    return "<ERROR> File does not exist";
                }

                var logPostChannel = context.Guild.GetTextChannel(settings.LogPostChannel);
                await logPostChannel.SendFileAsync(filepath, $"{confirmedName}-{date}.log");
                return "SUCCESS";
            }
        }
    }
}

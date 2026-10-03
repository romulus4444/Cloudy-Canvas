namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    [Summary("Per-server bot options: prefix, safe mode, aliases, whether to answer other bots")]
    [RequireBotAdmin]
    public class ServerOptionsModule : BotModuleBase
    {
        private readonly AllPreloadedSettings _servers;

        public ServerOptionsModule(AllPreloadedSettings servers)
        {
            _servers = servers;
        }

        [Command("setprefix", RunMode = RunMode.Async)]
        [Summary("Sets the bot listen prefix")]
        public async Task SetPrefixCommandAsync([Summary("The prefix character")] char prefix = ';')
        {
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
    }
}

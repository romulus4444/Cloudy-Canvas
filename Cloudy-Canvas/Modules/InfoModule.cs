namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for providing information")]
    public class InfoModule : BotModuleBase
    {
        private readonly LoggingService _logger;
        private readonly AllPreloadedSettings _servers;

        public InfoModule(LoggingService logger, AllPreloadedSettings servers)
        {
            _logger = logger;
            _servers = servers;
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Lists all commands")]
        public async Task HelpCommandAsync([Summary("First subcommand")] string command = "", [Remainder] [Summary("Second subcommand")] string subCommand = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.CanUserRunCommandsAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            var prefix = serverPresettings.Prefix;

            await _logger.Log($"help {command} {subCommand}", Context);

            var isAdmin = await DiscordHelper.IsBotAdminAsync(Context, settings);
            await ReplyAsync(HelpText.Reply(prefix, isAdmin, command, subCommand));
        }

        [Command("origin", RunMode = RunMode.Async)]
        [Summary("Displays the origin of Cloudy Canvas")]
        public async Task OriginCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.CanUserRunCommandsAsync(Context, settings))
            {
                return;
            }

            await _logger.Log("origin", Context);
            await ReplyAsync(
                $"Here is where I came from, thanks to ConfettiCakez!{Environment.NewLine}<https://www.deviantart.com/confetticakez>{Environment.NewLine}https://imgur.com/a/XUHhKz1");
        }

        [Command("about", RunMode = RunMode.Async)]
        [Summary("Displays information about Cloudy Canvas")]
        public async Task AboutCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.CanUserRunCommandsAsync(Context, settings))
            {
                return;
            }

            await _logger.Log("about", Context);
            await ReplyAsync(
                $"**__Cloudy Canvas__** <:cloudywink:871146664893743155>{Environment.NewLine}<http://cloudycanvas.art/>{Environment.NewLine}Created April 5th, 2021{Environment.NewLine}A Discord bot for interfacing with the <:manebooru:871148109240102942> <https://manebooru.art/> imageboard.{Environment.NewLine}Currently active on {_servers.GuildList.Count} servers.{Environment.NewLine}{Environment.NewLine}Written by Dr. Romulus in C# using Discord.net. Special thanks to Lunar aurora, Ember Heartshine, and CULTPONY{Environment.NewLine}{Environment.NewLine}**GitHub:** <https://github.com/romulus4444/Cloudy-Canvas>{Environment.NewLine}**Discord:** <https://discord.gg/K4pq9AnN8F>{Environment.NewLine}**Patreon:** <https://www.patreon.com/cloudy_canvas>",
                allowedMentions: AllowedMentions.None);
        }
    }
}

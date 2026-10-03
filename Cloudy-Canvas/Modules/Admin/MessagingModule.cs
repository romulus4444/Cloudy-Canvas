namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Posting messages on the bot's behalf")]
    public class MessagingModule : BotModuleBase
    {
        private readonly LoggingService _logger;
        private readonly AllPreloadedSettings _servers;

        // ;echo may mention users and roles, but never @everyone / @here.
        private static readonly AllowedMentions EchoMentions = new(AllowedMentionTypes.Users | AllowedMentionTypes.Roles);

        public MessagingModule(LoggingService logger, AllPreloadedSettings servers)
        {
            _logger = logger;
            _servers = servers;
        }

        [Command("echo", RunMode = RunMode.Async)]
        [Summary("Posts a message to a specified channel")]
        [RequireBotAdmin]
        public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "", [Remainder] [Summary("The message to send")] string message = "")
        {
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
    }
}

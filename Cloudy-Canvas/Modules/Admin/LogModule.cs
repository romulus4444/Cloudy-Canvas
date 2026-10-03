namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    [Summary("Submodule for retrieving log files")]
    [RequireBotAdmin]
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

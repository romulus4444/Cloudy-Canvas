namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Discord.Commands;

    [Summary("Replies to a bare prefix and to mentions of the bot")]
    public class PlaceholderCommandsModule : BotModuleBase
    {
        [Command("<blank message>", RunMode = RunMode.Async)]
        [Summary("Runs on a blank message")]
        public async Task BlankMessageCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!await DiscordHelper.CanUserRunCommandsAsync(Context, settings))
            {
                return;
            }

            await ReplyAsync("Did you need something?");
        }

        [Command("<mention>", RunMode = RunMode.Async)]
        [Summary("Runs on a name ping")]
        public Task MentionCommandAsync()
        {
            //removed ping reply, add custom replies here if desired
            return Task.CompletedTask;
        }
    }
}

namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Discord.Commands;

    // Keep in mind your module **must** be public and inherit ModuleBase.
    // If it isn't, it will not be discovered by AddModulesAsync!
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        //// ~say hello world -> hello world
        //[Command("echo")]
        //[Summary("Echoes a message.")]
        //public Task EchoAsync([Remainder] [Summary("The text to echo")] string echo) => ReplyAsync(echo);
        //// ReplyAsync is a method on ModuleBase 

        [Command("help")]
        [Summary("Lists all commands")]

        public async Task HelpAsync()
        {
            await ReplyAsync("All commands:\nAll searches use manechat-compliant filters.\n\n**;pick <query>**\nPosts random image from a Manebooru <query>.\n\n**;id <imageid>**\nPosts Manebooru image#<imageid>\n\n**;help**\nDisplays this help message.");
        }

        [Command("origin")]
        [Summary("Displays the origin url of Cloudy Canvas")]
        public async Task BirthdayAsync()
        {
            await ReplyAsync("Here is where I came from, thanks to RavenSunArt! https://discord.com/channels/729726993632985139/729727666063671332/732670355902038068");
        }
    }
}

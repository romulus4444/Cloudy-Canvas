namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

    // Keep in mind your module **must** be public and inherit ModuleBase.
    // If it isn't, it will not be discovered by AddModulesAsync!
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingHelperService _logger;

        public InfoModule(LoggingHelperService logger)
        {
            _logger = logger;
        }
        //// ~say hello world -> hello world
        //[Command("echo")]
        //[Summary("Echoes a message.")]
        //public Task EchoAsync([Remainder] [Summary("The text to echo")] string echo) => ReplyAsync(echo);
        //// ReplyAsync is a method on ModuleBase 

        [Command("help")]
        [Summary("Lists all commands")]
        public async Task HelpAsync(string command = "", [Remainder] string subcommands = "")
        {
            await _logger.Log($"help {command}", Context);

            switch (command)
            {
                case "":
                    await ReplyAsync(
                        "__All commands:__\nAll searches use manechat-compliant filters.\n\n`;pick <query>` Posts random image from a Manebooru <query>.\n\n`;id <imageid>` Posts Manebooru image#<imageid>\n\n`;blacklist` Manages the search term blacklist.\n\nUse `;help <command>` for more details on a particular command.");
                    break;
                case "pick":
                    await ReplyAsync(
                        "__;pick <query>__\nPosts random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma.");
                    break;
                case "id":
                    await ReplyAsync("__;id <imageid>__\nPosts Manebooru image# <imageid>, if it is available.");
                    break;
                case "help":
                    await ReplyAsync("<:sweetiegrump:642466824696627200>");
                    break;
                case "blacklist":
                    switch (subcommands)
                    {
                        case "":
                            await ReplyAsync(
                                "__;blacklist__\nManages the search term blacklist.\n\n`;blacklist add <term>` Adds <term> to the blacklist.\n\n`;blacklist remove <term>` Removes <term> from the blacklist.\n\n`;blacklist get` Displays the blacklist.\n\n`;blacklist clear` Empties the blacklist. Be very careful with this command.");
                            break;
                        default:
                            await ReplyAsync("No subcommand help available.");
                            break;
                    }

                    break;
                default:
                    await ReplyAsync("Invalid command. Use `;help` for a list of available commands.");
                    break;
            }
        }

        [Command("origin")]
        [Summary("Displays the origin url of Cloudy Canvas")]
        public async Task OriginAsync()
        {
            await _logger.Log("origin", Context);
            await ReplyAsync("Here is where I came from, thanks to RavenSunArt! https://imgur.com/a/RB16usb");
        }
    }
}

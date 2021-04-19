namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Discord.Commands;

    [Summary("Module for providing information")]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;

        public InfoModule(LoggingService logger)
        {
            _logger = logger;
        }

        [Command("help")]
        [Summary("Lists all commands")]
        public async Task HelpAsync([Summary("First subcommand")] string command = "", [Remainder] [Summary("Second subcommand")] string subCommand = "")
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            await _logger.Log($"help {command} {subCommand}", Context);

            switch (command)
            {
                case "":
                    await ReplyAsync(
                        "**__All Commands:__**\n\n**Booru Module:**\n*All searches use manechat-compliant filters*\n`;pick ...`\n`;pickrecent ...`\n`;id ...`\n`;tags ...`\n`;featured`\n`;getspoilers`\n`;report ...`\n\n**Admin Module:**\n`;setup ...`\n`;admin ...`\n`;yellowlist ...`\n`;log ...`\n`;echo ...`\n\n**Info Module:**\n`;origin`\n\nUse `;help <command>` for more details on a particular command.");
                    break;
                case "pick":
                    await ReplyAsync(
                        "`;pick <query>`\nPosts a random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "pickrecent":
                    await ReplyAsync(
                        "`;pick <query>`\nPosts the most recently posted image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "id":
                    await ReplyAsync(
                        "`;id <number>`\nPosts Image #<number> from Manebooru, if it is available. If the image includes spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "tags":
                    await ReplyAsync(
                        "`;tags <number>`\nPosts the list of tags on Image <number> from Manebooru, if it is available, including identifying any tags that are spoilered.");
                    break;
                case "featured":
                    await ReplyAsync("`;featured`\nPosts the current Featured Image from Manebooru.");
                    break;
                case "getspoilers":
                    await ReplyAsync("`;getspoilers`\nPosts a list of currently spoilered tags.");
                    break;
                case "report":
                    await ReplyAsync("`;report <id>`\nAlerts the admins about image#<id>. Only use this for images that violate the server rules!");
                    break;
                case "setup":
                    await ReplyAsync(
                        "`;setup <admin channel> <admin role>`\n*Only a server administrator may use this command.*\nInitial bot setup. Sets <admin channel> for important admin output messages and <admin role> as users who are allowed to use admin module commands.");
                    break;
                case "admin":
                    switch (subCommand)
                    {
                        case "":
                            await ReplyAsync(
                                "**__;admin Commands:__**\n*Only users with the specified admin role may use these commands*\n`;admin adminchannel ...`\n`;admin adminrole ...`\n`;admin ignorechannel ...`\n`;admin ignorerole ...`\n\nUse `;help admin <command>` for more details on a particular command.");
                            break;
                        case "adminchannel":
                            await ReplyAsync(
                                "__;admin adminchannel Commands:__\n*Manages the admin channel.*\n`;admin adminchannel get` Gets the current admin channel.\n`;admin adminchannel set <channel>` Sets the admin channel to <channel>. Accepts a channel ping or plain text.");
                            break;
                        case "adminrole":
                            await ReplyAsync(
                                "__;admin adminrole Commands:__\n*Manages the admin role.*\n`;admin adminrole get` Gets the current admin role.\n`;admin adminrole set <role>` Sets the admin role to <role>. Accepts a role ping or plain text.");
                            break;
                        case "ignorechannel":
                            await ReplyAsync(
                                "__;admin ignorechannel Commands:__\n*Manages the list of channels to ignore commands from.*\n`;admin ignorechannel get` Gets the current list of ignored channels.\n`;admin ignorechannel add <channel>` Adds <channel> to the list of ignored channels. Accepts a channel ping or plain text.\n`;admin ignorechannel remove <channel>` Removes <channel> from the list of ignored channels. Accepts a channel ping or plain text.\n`;admin ignorechannel clear` Clears the list of ignored channels.");
                            break;
                        case "ignorerole":
                            await ReplyAsync(
                                "__;admin ignorerole Commands:__\n*Manages the list of roles to ignore commands from.*\n`;admin ignorerole get` Gets the current list of ignored roles.\n`;admin ignorerole add <role>` Adds <role> to the list of ignored roles. Accepts a role ping or plain text.\n`;admin ignorerole remove <role>` Removes <role> from the list of ignored roles. Accepts a role ping or plain text.\n`;admin ignorerole clear` Clears the list of ignored roles.");
                            break;
                        default:
                            await ReplyAsync("Invalid subcommand. Use `;help admin` for a list of available subcommands.");
                            break;
                    }

                    break;
                case "yellowlist":
                    await ReplyAsync(
                        "**__;yellowlist Commands:__**\n*Only users with the specified admin role may use these commands.*\nManages the list of terms users are unable to search for.\n`;yellowlist add <term>` Add <term> to the yellowlist.\n`;yellowlist remove <term>` Removes <term> from the yellowlist.\n`;yellowlist get` Gets the current list of yellowlisted terms.\n`;yellowlist clear` Clears the yellowlist of all terms.");
                    break;
                case "log":
                    await ReplyAsync(
                        "`;log <channel> <date>`\n*Only users with the specified admin role may use this command.*\nPosts the log file from <channel> and <date> into the admin channel. Accepts a channel ping or plain text. <date> must be formatted as YYYY-MM-DD.");
                    break;
                case "echo":
                    await ReplyAsync(
                        "`;echo <channel> <message>`\n*Only users with the specified admin role may use this command.*\nPosts <message> to a valid <channel>. If <channel> is invalid, posts to the current channel instead. Accepts a channel ping or plain text.");
                    break;
                case "origin":
                    await ReplyAsync("`;origin`\nPosts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.");
                    break;
                default:
                    await ReplyAsync("Invalid command. Use `;help` for a list of available commands.");
                    break;
            }
        }

        [Command("origin")]
        [Summary("Displays the origin of Cloudy Canvas")]
        public async Task OriginAsync()
        {
            if (!await DiscordHelper.CanUserRunThisCommandAsync(Context))
            {
                return;
            }

            await _logger.Log("origin", Context);
            await ReplyAsync("Here is where I came from, thanks to RavenSunArt! https://imgur.com/a/RB16usb");
        }
    }
}

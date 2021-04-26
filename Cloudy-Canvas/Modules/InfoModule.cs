namespace Cloudy_Canvas.Modules
{
    using System;
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
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await _logger.Log($"help {command} {subCommand}", Context);

            switch (command)
            {
                case "":
                    await ReplyAsync(
                        $"**__All Commands:__**{Environment.NewLine}{Environment.NewLine}**Booru Module:**{Environment.NewLine}*All searches use manechat-compliant filters*{Environment.NewLine}`;pick ...`{Environment.NewLine}`;pickrecent ...`{Environment.NewLine}`;id ...`{Environment.NewLine}`;tags ...`{Environment.NewLine}`;featured`{Environment.NewLine}`;getspoilers`{Environment.NewLine}`;report ...`{Environment.NewLine}{Environment.NewLine}**Admin Module:**{Environment.NewLine}`;setup ...`{Environment.NewLine}`;admin ...`{Environment.NewLine}`;yellowlist ...`{Environment.NewLine}`;log ...`{Environment.NewLine}`;echo ...`{Environment.NewLine}{Environment.NewLine}**Info Module:**{Environment.NewLine}`;origin`{Environment.NewLine}{Environment.NewLine}Use `;help <command>` for more details on a particular command.");
                    break;
                case "pick":
                    await ReplyAsync(
                        $"`;pick <query>`{Environment.NewLine}Posts a random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "pickrecent":
                    await ReplyAsync(
                        $"`;pick <query>`{Environment.NewLine}Posts the most recently posted image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "id":
                    await ReplyAsync(
                        $"`;id <number>`{Environment.NewLine}Posts Image #<number> from Manebooru, if it is available. If the image includes spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "tags":
                    await ReplyAsync(
                        $"`;tags <number>`{Environment.NewLine}Posts the list of tags on Image <number> from Manebooru, if it is available, including identifying any tags that are spoilered.");
                    break;
                case "featured":
                    await ReplyAsync($"`;featured`{Environment.NewLine}Posts the current Featured Image from Manebooru.");
                    break;
                case "getspoilers":
                    await ReplyAsync($"`;getspoilers`{Environment.NewLine}Posts a list of currently spoilered tags.");
                    break;
                case "report":
                    await ReplyAsync($"`;report <id>`{Environment.NewLine}Alerts the admins about image#<id>. Only use this for images that violate the server rules!");
                    break;
                case "setup":
                    await ReplyAsync(
                        $"`;setup <filter Id> <admin channel> <admin role>`{Environment.NewLine}*Only a server administrator may use this command.*{Environment.NewLine}Initial bot setup. Sets <filter ID> as the public Manebooru filter to use, <admin channel> for important admin output messages, and <admin role> as users who are allowed to use admin module commands.");
                    break;
                case "admin":
                    switch (subCommand)
                    {
                        case "":
                            await ReplyAsync(
                                $"**__;admin Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands*{Environment.NewLine}`;admin adminchannel ...`{Environment.NewLine}`;admin adminrole ...`{Environment.NewLine}`;admin ignorechannel ...`{Environment.NewLine}`;admin ignorerole ...`{Environment.NewLine}`;admin alloweuser ...`{Environment.NewLine}{Environment.NewLine}Use `;help admin <command>` for more details on a particular command.");
                            break;
                        case "adminchannel":
                            await ReplyAsync(
                                $"__;admin adminchannel Commands:__{Environment.NewLine}*Manages the admin channel.*{Environment.NewLine}`;admin adminchannel get` Gets the current admin channel.{Environment.NewLine}`;admin adminchannel set <channel>` Sets the admin channel to <channel>. Accepts a channel ping or plain text.");
                            break;
                        case "adminrole":
                            await ReplyAsync(
                                $"__;admin adminrole Commands:__{Environment.NewLine}*Manages the admin role.*{Environment.NewLine}`;admin adminrole get` Gets the current admin role.{Environment.NewLine}`;admin adminrole set <role>` Sets the admin role to <role>. Accepts a role ping or plain text.");
                            break;
                        case "ignorechannel":
                            await ReplyAsync(
                                $"__;admin ignorechannel Commands:__{Environment.NewLine}*Manages the list of channels to ignore commands from.*{Environment.NewLine}`;admin ignorechannel get` Gets the current list of ignored channels.{Environment.NewLine}`;admin ignorechannel add <channel>` Adds <channel> to the list of ignored channels. Accepts a channel ping or plain text.{Environment.NewLine}`;admin ignorechannel remove <channel>` Removes <channel> from the list of ignored channels. Accepts a channel ping or plain text.{Environment.NewLine}`;admin ignorechannel clear` Clears the list of ignored channels.");
                            break;
                        case "ignorerole":
                            await ReplyAsync(
                                $"__;admin ignorerole Commands:__{Environment.NewLine}*Manages the list of roles to ignore commands from.*{Environment.NewLine}`;admin ignorerole get` Gets the current list of ignored roles.{Environment.NewLine}`;admin ignorerole add <role>` Adds <role> to the list of ignored roles. Accepts a role ping or plain text.{Environment.NewLine}`;admin ignorerole remove <role>` Removes <role> from the list of ignored roles. Accepts a role ping or plain text.{Environment.NewLine}`;admin ignorerole clear` Clears the list of ignored roles.");
                            break;
                        case "allowuser":
                            await ReplyAsync(
                                $"__;admin allowuser Commands:__{Environment.NewLine}*Manages the list of users to allow commands from.*{Environment.NewLine}`;admin allowuser get` Gets the current list of allowd users.{Environment.NewLine}`;admin allowuser add <user>` Adds <user> to the list of allowd users. Accepts a user ping or plain text.{Environment.NewLine}`;admin allowuser remove <user>` Removes <user> from the list of allowd users. Accepts a user ping or plain text.{Environment.NewLine}`;admin allowuser clear` Clears the list of allowd users.");
                            break;
                        default:
                            await ReplyAsync("Invalid subcommand. Use `;help admin` for a list of available subcommands.");
                            break;
                    }

                    break;
                case "yellowlist":
                    await ReplyAsync(
                        $"**__;yellowlist Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of terms users are unable to search for.{Environment.NewLine}`;yellowlist add <term>` Add <term> to the yellowlist.{Environment.NewLine}`;yellowlist remove <term>` Removes <term> from the yellowlist.{Environment.NewLine}`;yellowlist get` Gets the current list of yellowlisted terms.{Environment.NewLine}`;yellowlist clear` Clears the yellowlist of all terms.");
                    break;
                case "log":
                    await ReplyAsync(
                        $"`;log <channel> <date>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the log file from <channel> and <date> into the admin channel. Accepts a channel ping or plain text. <date> must be formatted as YYYY-MM-DD.");
                    break;
                case "echo":
                    await ReplyAsync(
                        $"`;echo <channel> <message>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts <message> to a valid <channel>. If <channel> is invalid, posts to the current channel instead. Accepts a channel ping or plain text.");
                    break;
                case "origin":
                    await ReplyAsync($"`;origin`{Environment.NewLine}Posts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.");
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
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await _logger.Log("origin", Context);
            await ReplyAsync("Here is where I came from, thanks to RavenSunArt! <https://www.deviantart.com/ravensunart> https://imgur.com/a/RB16usb");
        }
    }
}

namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Discord;
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
        public async Task HelpCommandAsync([Summary("First subcommand")] string command = "", [Remainder] [Summary("Second subcommand")] string subCommand = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await _logger.Log($"help {command} {subCommand}", Context);

            switch (command)
            {
                case "":
                    await ReplyAsync(
                        $"**__All Commands:__**{Environment.NewLine}**Booru Module:**{Environment.NewLine}`;pick ...`{Environment.NewLine}`;pickrecent ...`{Environment.NewLine}`;id ...`{Environment.NewLine}`;tags ...`{Environment.NewLine}`;featured`{Environment.NewLine}`;getspoilers`{Environment.NewLine}`;report ...`{Environment.NewLine}**Admin Module:**{Environment.NewLine}`;setup ...`{Environment.NewLine}`;admin ...`{Environment.NewLine}`;yellowlist ...`{Environment.NewLine}`;log ...`{Environment.NewLine}`;echo ...`{Environment.NewLine}`;setprefix ...`{Environment.NewLine}`;listentobots ...`{Environment.NewLine}`;alias ...`{Environment.NewLine}`;getsettings`{Environment.NewLine}`;refreshlists`{Environment.NewLine}**Info Module:**{Environment.NewLine}`;origin`{Environment.NewLine}`;about`{Environment.NewLine}{Environment.NewLine}Use `;help <command>` for more details on a particular command.{Environment.NewLine}Ping <@{Context.Client.CurrentUser.Id}> with any message if you forget the prefix. Yes, I know you needed to know the prefix to see this message, but try to remember in case someone else asks, ok?");
                    break;
                case "pick":
                    await ReplyAsync(
                        $"`;pick <query>`{Environment.NewLine}Posts a random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "pickrecent":
                    await ReplyAsync(
                        $"`;pickrecent <query>`{Environment.NewLine}Posts the most recently posted image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
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
                    await ReplyAsync(
                        $"`;report <id> <reason>`{Environment.NewLine}Alerts the admins about image #<id> with an optional <reason> for the admins to see. Only use this for images that violate the server rules!");
                    break;
                case "setup":
                    await ReplyAsync(
                        $"`;setup <filter ID> <admin channel> <admin role>`{Environment.NewLine}*Only a server administrator may use this command.*{Environment.NewLine}Initial bot setup. Sets <filter ID> as the public Manebooru filter to use, <admin channel> for important admin output messages, and <admin role> as users who are allowed to use admin module commands. Validates that <Filter ID> is useable and if not, uses Filter 175.");
                    break;
                case "admin":
                    switch (subCommand)
                    {
                        case "":
                            await ReplyAsync(
                                $"**__;admin Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands*{Environment.NewLine}`;admin filter ...`{Environment.NewLine}`;admin adminchannel ...`{Environment.NewLine}`;admin adminrole ...`{Environment.NewLine}`;admin ignorechannel ...`{Environment.NewLine}`;admin ignorerole ...`{Environment.NewLine}`;admin allowuser ...`{Environment.NewLine}`;admin yellowchannel ...`{Environment.NewLine}`;admin yellowrole ...`{Environment.NewLine}`;admin redchannel ...`{Environment.NewLine}`;admin redrole ...`{Environment.NewLine}`;admin reportchannel ...`{Environment.NewLine}`;admin reportrole ...`{Environment.NewLine}`;admin logchannel ...`{Environment.NewLine}{Environment.NewLine}Use `;help admin <command>` for more details on a particular command.");
                            break;
                        case "filter":
                            await ReplyAsync(
                                $"__;admin filter Commands:__{Environment.NewLine}*Manages the active filter.*{Environment.NewLine}`;admin filter get` Gets the current active filter.{Environment.NewLine}`;admin filter set <filter ID>` Sets the active filter to <Filter ID>. Validates that the filter is useable by the bot.");
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
                                $"__;admin allowuser Commands:__{Environment.NewLine}*Manages the list of users to allow commands from.*{Environment.NewLine}`;admin allowuser get` Gets the current list of allowd users.{Environment.NewLine}`;admin allowuser add <user>` Adds <user> to the list of allowd users. Accepts a user ping or plain text.{Environment.NewLine}`;admin allowuser remove <user>` Removes <user> from the list of allowed users. Accepts a user ping or plain text.{Environment.NewLine}`;admin allowuser clear` Clears the list of allowd users.");
                            break;
                        case "yellowchannel":
                            await ReplyAsync(
                                $"__;admin yellowchannel Commands:__{Environment.NewLine}*Manages the yellow alert channel.*{Environment.NewLine}`;admin yellowchannel get` Gets the current yellow alert channel.{Environment.NewLine}`;admin yellowchannel set <channel>` Sets the yellow alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`;admin yellowchannel clear` Resets the yellow alert channel to the current admin channel.");
                            break;
                        case "yellowrole":
                            await ReplyAsync(
                                $"__;admin yellowrole Commands:__{Environment.NewLine}*Manages the yellow alert role.*{Environment.NewLine}`;admin yellowrole get` Gets the current yellow alert role.{Environment.NewLine}`;admin yellowrole set <role>` Sets the yellow alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`;admin yellowrole clear` Resets the yellow alert role to no role and turns pinging off.");
                            break;
                        case "redchannel":
                            await ReplyAsync(
                                $"__;admin redchannel Commands:__{Environment.NewLine}*Manages the red alert channel.*{Environment.NewLine}`;admin redchannel get` Gets the current red alert channel.{Environment.NewLine}`;admin redchannel set <channel>` Sets the red alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`;admin redchannel clear` Resets the red alert channel to the current admin channel.");
                            break;
                        case "redrole":
                            await ReplyAsync(
                                $"__;admin redrole Commands:__{Environment.NewLine}*Manages the red alert role.*{Environment.NewLine}`;admin redrole get` Gets the current red alert role.{Environment.NewLine}`;admin redrole set <role>` Sets the red alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`;admin redrole clear` Resets the red alert channel to no role and turns pinging off.");
                            break;
                        case "reportchannel":
                            await ReplyAsync(
                                $"__;admin reportchannel Commands:__{Environment.NewLine}*Manages the report alert channel.*{Environment.NewLine}`;admin reportchannel get` Gets the current report alert channel.{Environment.NewLine}`;admin reportchannel set <channel>` Sets the report alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`;admin reportchannel clear` Resets the report alert channel to the current admin channel.");
                            break;
                        case "reportrole":
                            await ReplyAsync(
                                $"__;admin reportrole Commands:__{Environment.NewLine}*Manages the report alert role.*{Environment.NewLine}`;admin reportrole get` Gets the current report alert role.{Environment.NewLine}`;admin reportrole set <role>` Sets the report alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`;admin reportrole clear` Resets the report alert channel to no role and turns pinging off.");
                            break;
                        case "logchannel":
                            await ReplyAsync(
                                $"__;admin logchannel Commands:__{Environment.NewLine}*Manages the log post channel.*{Environment.NewLine}`;admin logchannel get` Gets the current log post channel.{Environment.NewLine}`;admin logchannel set <channel>` Sets the log post channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`;admin logchannel clear` Resets the log post channel to the current admin channel.");
                            break;
                        default:
                            await ReplyAsync("Invalid subcommand. Use `;help admin` for a list of available subcommands.");
                            break;
                    }

                    break;
                case "yellowlist":
                    await ReplyAsync(
                        $"**__;yellowlist Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of terms users are unable to search for.{Environment.NewLine}`;yellowlist add <term>` Add <term> to the yellowlist. <term> may be a comma-separated list.{Environment.NewLine}`;yellowlist remove <term>` Removes <term> from the yellowlist.{Environment.NewLine}`;yellowlist get` Gets the current list of yellowlisted terms.{Environment.NewLine}`;yellowlist clear` Clears the yellowlist of all terms.");
                    break;
                case "log":
                    await ReplyAsync(
                        $"`;log <channel> <date>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the log file from <channel> and <date> into the admin channel. Accepts a channel ping or plain text. <date> must be formatted as YYYY-MM-DD.");
                    break;
                case "echo":
                    await ReplyAsync(
                        $"`;echo <channel> <message>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts <message> to a valid <channel>. If <channel> is invalid, posts to the current channel instead. Accepts a channel ping or plain text.");
                    break;
                case "setprefix":
                    await ReplyAsync(
                        $"`;setprefix <prefix>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Sets the prefix in front of commands to listen for to <prefix>. Accepts a single character.");
                    break;
                case "listentobots":
                    await ReplyAsync(
                        $"`;listentobots <pos/neg>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Toggles whether or not to run commands posted by other bots. Accepts y/n, yes/no, on/off, or true/false.");
                    break;
                case "alias":
                    await ReplyAsync(
                        $"**__;alias Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of command aliases.{Environment.NewLine}`;alias add <short> <long>` Sets <short> as an alias of <long>. If a command starts with <short>, <short> is replaced with <long> and the command is then processed normally. Do not include prefixes in <short> or <long>. Example: `;alias cute pick cute` sets `;cute` to run `;pick cute` instead. To use an alias that includes spaces, surround the entire <short> term with \"\" quotes. If an alias for <short> already exists, it replaces the previous value of <long> with the new one.{Environment.NewLine}`;alias remove <short>` Removes <short> as an alias for anything.{Environment.NewLine}`;alias get` Gets the current list of aliases.{Environment.NewLine}`;alias clear` Clears all aliases.");
                    break;
                case "getsettings":
                    await ReplyAsync(
                        $"`;getsettings`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the settings file to the log channel. This includes the redlist.");
                    break;
                case "refreshlists":
                    await ReplyAsync(
                        $"`;refreshlists`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Rebuilds the spoiler list and redlist from the current active filter. This may take several minutes depending on how many tags are in there.");
                    break;
                case "origin":
                    await ReplyAsync($"`;origin`{Environment.NewLine}Posts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.");
                    break;
                case "about":
                    await ReplyAsync("`;about` Information about this bot.");
                    break;
                case "help":
                    await ReplyAsync("<:sweetiegrump:642466824696627200>");
                    break;
                default:
                    await ReplyAsync("Invalid command. Use `;help` for a list of available commands.");
                    break;
            }
        }

        [Command("origin")]
        [Summary("Displays the origin of Cloudy Canvas")]
        public async Task OriginCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await _logger.Log("origin", Context);
            await ReplyAsync($"Here is where I came from, thanks to ConfettiCakez!{Environment.NewLine}<https://www.deviantart.com/confetticakez>{Environment.NewLine}https://imgur.com/a/XUHhKz1");
        }

        [Command("about")]
        [Summary("Displays the origin of Cloudy Canvas")]
        public async Task AboutCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await _logger.Log("about", Context);
            await ReplyAsync(
                $"**__Cloudy Canvas__** <:ccwink:803340572383117372>{Environment.NewLine}Created April 5th, 2021{Environment.NewLine}A Discord bot for interfacing with the <:manebooru:803361798216482878> <https://manebooru.art/> imageboard.{Environment.NewLine}{Environment.NewLine}Written by Raymond Welch (<@221742476153716736>) in C# using Discord.net. Special thanks to Ember Heartshine for hosting and HenBasket for testing.{Environment.NewLine}{Environment.NewLine}**GitHub:** <https://github.com/romulus4444/Cloudy-Canvas>",
                allowedMentions: AllowedMentions.None);
        }
    }
}

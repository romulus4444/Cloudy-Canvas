# Cloudy Canvas

Created April 5th, 2021

A Discord bot for interfacing with the [Manebooru](https://manebooru.art/) imageboard. The discord server can be found [here](https://discord.gg/K4pq9AnN8F). If you enjoy this bot and want to help out with hosting and development of the Manebooru platform, please consider my [Patreon](https://www.patreon.com/cloudy_canvas)!

Written by ([@romulus4444](https://github.com/romulus4444) on Discord) in C# using Discord.net and hosted on the Manebooru server. Special thanks to Ember Heartshine and CULTPONY.

Interested in adding Cloudy to your own server? Click [here](https://discord.com/api/oauth2/authorize?client_id=828682017868218445&permissions=515396463680&scope=bot) to add her! Please make sure you run the setup command once you do!

Cloudy Canvas's profile picture was drawn by me and can be found [here](https://manebooru.art/images/4010266).

Important terms:

`query` Any query that could be entered into the search field on Manebooru. This is a list of comma-separated terms, and may include logical operators and image metadata flags. See [here](https://manebooru.art/pages/search_syntax) for more details.

`active filter` The Manebooru filter that your server uses to filter images in the search results. It is advisable to make your own account on Manebooru, add a new filter, tailor it to your server's needs, and make it public (at the bottom of the page under Advanced Options). You do not need to leave it as your account's active filter, as long as you know the ID (it's in the URL when viewing the filter: `https://manebooru.art/filters/<filter ID>`) and the filter is public.

`watchlist` A list of terms that the bot will not search for if included in the query. These terms are not prevented from appearing in search results. Use cases include terms like `breasts` that are inappropriate to search for in a SFW server, but is a perfectly okay tag to appear in search results. This list can include image IDs as well. A user searching for a watch term will generate an alert message sent to the current watch alert channel and ping the watch alert role, if either are set.

## Running your own copy

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download), or Nix: `nix build .#cloudy-canvas` builds the bot from the included flake, and `service.nix` is a NixOS module that runs it as a service. From the repository folder, `dotnet run --project Cloudy-Canvas` starts the bot and `dotnet test` runs the tests.

Create an application in the [Discord Developer Portal](https://discord.com/developers/applications), add a bot to it and turn on its **Message Content Intent**: it is a privileged intent, and Cloudy reads the text of messages to find commands. Cloudy does not need the Server Members or Presence intents. To invite your own copy, use the invite link above with your application's client ID in place of `828682017868218445` (its permissions value is `515396463680`).

Copy `Cloudy-Canvas/appsettings.sample.json` to `Cloudy-Canvas/appsettings.json` (that file is git-ignored; never commit it) and fill in your Discord bot `token` and, optionally, a Manebooru API `token`. Instead of a file you can use environment variables (`DiscordSettings__token`, `ManebooruSettings__token`) or, in development, `dotnet user-secrets`. On NixOS, point `services.cloudy-canvas.environmentFile` at a root-only file containing those variables.

Everything Cloudy stores (each server's settings and logs) lives in one folder, `botsettings` in the directory the bot is started from. Change it with `Storage:RootPath` (or `Storage__RootPath`); an absolute path works too.

### What Cloudy stores

Everything is in the storage folder described above. There is no database, and nothing is sent anywhere except to Discord and to Manebooru. Discord asks the people who run bots to be open about how they handle user data; this list describes what the code does, as a starting point for that, and is not a privacy policy itself.

- **Settings.** `preloadedsettings.conf` holds, for each server (and for each user who has sent Cloudy a direct message), its name or username, prefix, command aliases and whether to answer other bots, plus the admin channel of each server. `servers/<server id>/settings.conf` holds the server's filter, its admin, alert and log channels and roles, the ignored channel and role IDs, the allowed user IDs, the watchlist, safe mode and the spoiler tag list.
- **Console log.** Every command is written to the console (the journal, when run as a systemd service): the time, the server and channel names and IDs, the sender's username and ID, and the text of the command, including search queries. How long it is kept depends on where the console output goes. Line breaks in what people type are written out as `\n`, so a message cannot add lines to the log.
- **Log files.** `servers/<server id>/<channel id>/<date>.log` records only admin changes to settings, `;setup`, `;echo`, watchlist changes, `;report`, and searches the watchlist blocked, each with the sender's username and ID (and, for a blocked search, its query). Admins can fetch them with `;log`. They are kept forever by default; set `LogRetention:RetentionDays` (or the `LogRetention__RetentionDays` environment variable) to delete log files older than that many days.
- **Direct messages.** Settings and log files for direct messages are kept in `servers/_userdms/<user id>/`.
- **Manebooru.** The search terms, image numbers and filter IDs in a command are sent to Manebooru to carry it out, together with the API key if one is configured.
- **Everything else.** Cloudy only looks at a message to see whether it starts with the prefix or mentions her; no other message is kept.

Cloudy does not delete a server's data when she is removed from it. To delete it, stop the bot, remove the `servers/<server id>` folder and that server's entry in `preloadedsettings.conf`, and start the bot again.

To report a security problem, see [SECURITY.md](SECURITY.md).

### Running a development copy alongside the real one
A test copy of the bot should not share the real one's data or answer the same commands. Give it its own settings, for example in `appsettings.Development.json` (also git-ignored), user secrets, or environment variables:

```json
{
  "Storage": { "RootPath": "devsettings" },
  "DiscordSettings": { "token": "<the test bot's token>", "PrefixOverride": "?" }
}
```

`Storage:RootPath` keeps its servers' settings and logs in a separate folder. `DiscordSettings:PrefixOverride` is a single punctuation or symbol character that replaces every server's prefix, so the test bot answers `?pick` while the real one keeps answering `;pick`. Leave it unset in production. (These replace the old `DevSettings.cs`, which you had to edit and recompile.)

## Commands:
### Booru Module:
*All searches are subject to the current active filter*

*To go easy on Manebooru, each user can run one lookup command (`;pick`, `;pickrecent`, `;id`, `;tags`, `;featured` or `;report`) every two seconds. If Manebooru can't be reached, Cloudy says so rather than reporting "no results".*

---

<!-- BEGIN GENERATED: booru. Do not edit by hand: change Cloudy-Canvas/Helpers/HelpTopics.cs, then run the tests with UPDATE_SNAPSHOTS=1 to rewrite this part. -->

`;pick <query>` Posts a random image from a Manebooru `<query>`, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.

---

`;pickrecent <query>` Posts the most recently posted image from a Manebooru `<query>`, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.

---

`;id <number>` Posts Image #`<number>` from Manebooru, if it is available. If the image includes spoilered tags, the post is made in `||` spoiler bars.

---

`;tags <number>` Posts the list of tags on Image #`<number>` from Manebooru, if it is available, including identifying any tags that are spoilered.

---

`;featured` Posts the current Featured Image from Manebooru.

---

`;getspoilers` Posts a list of currently spoilered tags.

---

`;report <id> <reason>` Alerts the admins about image #`<id>` with an optional `<reason>` for the admins to see. Only use this for images that violate the server rules; this is not a report to Manebooru itself!

---

<!-- END GENERATED: booru -->

### Mixins:

Use mixins (one of the terms below wrapped in double curly braces with no spaces `{ { term } }`) in the `<query>` to introduce values that are calculated when the query is executed.
Example: If today was May 23rd, 2021, then `;pick created_at:{ { today } }, lyra` (with no spaces between { and }) would execute as `;pick created_at:2021-05-23, lyra`. All date values are generated using UTC ("Zulu") time. [More info](https://manebooru.art/pages/search_syntax#date-range).

| Term | Info |
| -------- | ---- |
| `today` | The current day, month, and year using the ISO 8601 standard. |
| `current_year` | The current year. |
| `current_month` | The current month. |
| `current_day` | The current day. |
| `current_hour` | The current hour. |
| `current_minute` | The current minute. |
| `current_second` | The current second. |

---

### Admin Module
*Only users with the specified admin role (and members with the Administrator permission) may use the commands in this module. Until an admin role is set, only members with the Administrator or Manage Server permission can use them.*

---

<!-- BEGIN GENERATED: admin. Do not edit by hand: change Cloudy-Canvas/Helpers/HelpTopics.cs, then run the tests with UPDATE_SNAPSHOTS=1 to rewrite this part. -->

`;setup <filter ID> <admin channel> <admin role>` *Only a server administrator may use this command.* Initial bot setup. Run this before doing anything else when adding Cloudy Canvas to your server! Sets `<filter ID>` as the public Manebooru filter to use, `<admin channel>` for important admin output messages, and `<admin role>` as users who are allowed to use admin module commands. Validates that `<Filter ID>` is useable and if not, uses Filter 175.

Filters are viewable at `https://manebooru.art/filters/<filter ID>`. All alert channels are defaulted to the admin channel, and all alert roles and pings are turned off. The spoiler list is then built, which can take several minutes, depending on how many tags in the filter are spoilered. Please wait until it is done being built before running more commands; Cloudy will tell you when she is ready. This is a one-time process, unless manually initiated later.

---

Manages the active filter.

`;admin filter get` Gets the current active filter.

`;admin filter set <filter ID>` Sets the active filter to `<Filter ID>`. Validates that the filter is useable by the bot. The spoiler list is rebuilt after the new filter is set.

---

Manages the admin channel.

`;admin adminchannel get` Gets the current admin channel.

`;admin adminchannel set <channel>` Sets the admin channel to `<channel>`. Accepts a channel ping or plain text.

---

Manages the admin role.

`;admin adminrole get` Gets the current admin role.

`;admin adminrole set <role>` Sets the admin role to `<role>`. Accepts a role ping or plain text.

---

Manages the list of channel-specific filters. NOTE: watchlist checks are disabled for any channels on this list! Moderators will need to keep an eye on searches performed here!

`;admin filterchannel get` Gets the current list of channel-specific filters.

`;admin filterchannel add <channel> <filterId>` Sets `<channel>` to use filter #`<filterId>`. Validates the filter first. Accepts a channel ping or plain text.

`;admin filterchannel remove <channel>` Removes `<channel>` from the list of channel-specific filters. This channel will now use the default server filter. Accepts a channel ping or plain text.

`;admin filterchannel clear` Clears the list of channel-specific filters. All channels will use the default server filter.

---

Manages the list of channels to ignore commands from. Cloudy will not respond in any of these channels.

`;admin ignorechannel get` Gets the current list of ignored channels.

`;admin ignorechannel add <channel>` Adds `<channel>` to the list of ignored channels. Accepts a channel ping or plain text.

`;admin ignorechannel remove <channel>` Removes `<channel>` from the list of ignored channels. Accepts a channel ping or plain text.

`;admin ignorechannel clear` Clears the list of ignored channels.

---

Manages the list of roles to ignore commands from.

Cloudy will not respond to users that have any of these roles, not even with an error message. Users with the admin role, and users on the allowed-user list, are never ignored. Role changes take effect within about 30 seconds.

`;admin ignorerole get` Gets the current list of ignored roles.

`;admin ignorerole add <role>` Adds `<role>` to the list of ignored roles. Accepts a role ping or plain text.

`;admin ignorerole remove <role>` Removes `<role>` from the list of ignored roles. Accepts a role ping or plain text.

`;admin ignorerole clear` Clears the list of ignored roles.

---

Manages the list of users to allow commands from. This overrides the ignorechannel and ignorerole restrictions!

Everywhere a command takes a user, channel or role you can give a ping, a name, or the ID (right-click > Copy ID with Developer Mode on), for example `;admin allowuser add 221742476153716736`.

`;admin allowuser get` Gets the current list of allowed users.

`;admin allowuser add <user>` Adds `<user>` to the list of allowed users. Accepts a user ping or plain text.

`;admin allowuser remove <user>` Removes `<user>` from the list of allowed users. Accepts a user ping or plain text.

`;admin allowuser clear` Clears the list of allowed users.

---

Manages the watch alert channel.

`;admin watchchannel get` Gets the current watch alert channel.

`;admin watchchannel set <channel>` Sets the watch alert channel to `<channel>`. Accepts a channel ping or plain text.

`;admin watchchannel clear` Resets the watch alert channel to the current admin channel.

---

Manages the watch alert role.

`;admin watchrole get` Gets the current watch alert role.

`;admin watchrole set <role>` Sets the watch alert role to `<role>` and turns pinging on. Accepts a role ping or plain text.

`;admin watchrole clear` Resets the watch alert role to no role and turns pinging off.

---

Manages the report alert channel.

`;admin reportchannel get` Gets the current report alert channel.

`;admin reportchannel set <channel>` Sets the report alert channel to `<channel>`. Accepts a channel ping or plain text.

`;admin reportchannel clear` Resets the report alert channel to the current admin channel.

---

Manages the report alert role.

`;admin reportrole get` Gets the current report alert role.

`;admin reportrole set <role>` Sets the report alert role to `<role>` and turns pinging on. Accepts a role ping or plain text.

`;admin reportrole clear` Resets the report alert role to no role and turns pinging off.

---

Manages the log post channel.

`;admin logchannel get` Gets the current log post channel.

`;admin logchannel set <channel>` Sets the log post channel to `<channel>`. Accepts a channel ping or plain text.

`;admin logchannel clear` Resets the log post channel to the current admin channel.

---

Manages the list of terms users are unable to search for.

`;watchlist add <term>` Add `<term>` to the watchlist. `<term>` may be a comma-separated list.

`;watchlist remove <term>` Removes `<term>` from the watchlist.

`;watchlist get` Gets the current list of watchlisted terms.

`;watchlist clear` Clears the watchlist of all terms.

---

`;log <channel> <date>` Posts the log file from `<channel>` and `<date>` into the admin channel. Accepts a channel ping or plain text. `<date>` must be formatted as `YYYY-MM-DD`. Logs are saved based on date in UTC.

---

`;echo <message>` Posts `<message>` to the current channel.

`;echo <channel> <message>` Posts `<message>` to a valid `<channel>`. If `<channel>` is invalid, posts to the current channel instead. Accepts a channel ping or plain text.

---

`;setprefix <prefix>` Sets the prefix in front of commands to listen for to `<prefix>`. Accepts a single punctuation or symbol character (not `@`, `#`, `<`, `>` or a backtick).

Cloudy stays silent when a message starts with the prefix but isn't one of her commands.

---

`;listentobots <pos/neg>` Toggles whether or not to run commands posted by other bots. Accepts `y/n`, `yes/no`, `on/off`, or `true/false`.

---

`;safemode <pos/neg>` Toggles whether or not to automatically append `safe` to all booru queries. This overrides any channel-specific filters! Accepts `y/n`, `yes/no`, `on/off`, or `true/false`.

---

Manages the list of command aliases.

`;alias add <short> <long>` Sets `<short>` as an alias of `<long>`. If a command starts with `<short>`, `<short>` is replaced with `<long>` and the command is then processed normally. Do not include prefixes in `<short>` or `<long>`. Example: `;alias add cute pick cute` sets `;cute` to run `;pick cute` instead. To use an alias that includes spaces, surround the entire `<short>` term with "" quotes. If an alias for `<short>` already exists, it replaces the previous value of `<long>` with the new one.

`;alias remove <short>` Removes `<short>` as an alias for anything.

`;alias get` Gets the current list of aliases.

`;alias clear` Clears all aliases.

---

`;getsettings` Posts the settings file to the log channel. This includes the watchlist.

---

`;refreshlists` Rebuilds the spoiler list from the current active filter. This may take several minutes depending on how many tags are in there.

---

<!-- END GENERATED: admin -->

### Info Module

<!-- BEGIN GENERATED: info. Do not edit by hand: change Cloudy-Canvas/Helpers/HelpTopics.cs, then run the tests with UPDATE_SNAPSHOTS=1 to rewrite this part. -->

`;origin` Posts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.

---

`;about` Information about this bot.

---

<!-- END GENERATED: info -->

`;help` A list of commands and descriptions, much like this page. The admin commands are only listed, and their help only shown, to users who can use them.

---

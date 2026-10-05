# Cloudy Canvas Privacy Policy

Last updated: 4 October 2026

Cloudy Canvas is a Discord bot that looks up images on [Manebooru](https://manebooru.art/) and gives server admins moderation tools for those lookups. It is run by Dr. Romulus ([@romulus4444](https://github.com/romulus4444) on Discord and GitHub), called "the operator" below. This policy explains what information Cloudy handles, why, how long it is kept, who else sees it, and how to have it removed.

## The short version

- Cloudy reads the text of a message only to find out whether it is a command: a message that starts with the server's prefix (`;` unless the server changed it) or that mentions Cloudy. Every other message is looked at for that one purpose, then dropped. It is not stored, logged, analysed or shared.
- When someone runs a command, Cloudy sends the search terms or image number to Manebooru to carry it out, and writes a line about the command to a log.
- Each server's settings, and some logs with usernames and user IDs, are stored on the operator's server.
- Nothing is sold, used for advertising, used to train any model, or sent anywhere except Discord and Manebooru.
- You can ask for your data to be deleted (see [Your choices](#your-choices)).

## What Cloudy receives from Discord, and why

| Information | Why Cloudy needs it |
| --- | --- |
| The text of messages (the Message Content intent) | To see whether a message is a command and, if it is, to read its arguments. Without this, Cloudy could not read `;pick twilight sparkle`. |
| The sender's user ID and username | To apply a server's rules (ignored roles and channels, allowed users, admin checks), to enforce a short cooldown between lookups, and to write the logs described below. |
| The sender's roles and permissions in the server | To decide whether a person is an admin of the bot, or is on a server's ignore list. These are fetched from Discord when needed and kept in memory for 30 seconds. |
| Server (guild) and channel names and IDs | To keep each server's settings apart, to send replies and alerts to the right channel, and to write the logs. |
| Role and channel IDs that admins choose | To remember the server's admin, alert and log channels and roles, and which channels and roles to ignore. |

Cloudy asks Discord for the permissions in its invite link and the following gateway intents: Guilds, Guild Messages, Direct Messages and Message Content. It does not request the Server Members or Presence intents and does not read member lists, presence, voice, or reactions.

### How the Message Content intent is used

Discord delivers the text of every message in the channels Cloudy can see. For each one Cloudy checks only whether it starts with the server's prefix or with a mention of the bot. If it does not, nothing more happens: the text is not written to disk, not written to any log, not kept in memory (Cloudy does not keep a message cache), not analysed and not shared. If it does, the text is treated as a command: it is run, and the log lines described below are written. A message that starts with the prefix but is not one of Cloudy's commands is ignored and is not logged. Cloudy has no feature that searches, summarises, profiles or learns from conversations.

## What is stored

Everything is kept in plain files in one folder on the operator's server. There is no other database, and no copy is sent to a third-party service.

1. **Server settings.** For each server: its name, the Manebooru filter it uses, its admin, alert and log channels and roles, the ignored channel and role IDs, the user IDs on its allow list, its watchlist terms, whether safe mode is on, any per-channel filters, and the list of spoilered tags from its filter. For each server (and for each person who has sent Cloudy a direct message) there is also a record with its name or username, prefix, command aliases and whether to answer other bots, and each server's admin channel is recorded so admin announcements can reach it.
2. **Console log.** Every command that is run is written to the console, which on the operator's server is the system log. Each entry has the time, the server and channel names and IDs, the sender's username and user ID, and the text of the command, including search terms. Line breaks and control characters in that text are escaped so a message cannot add fake lines.
3. **Log files.** In addition, a few kinds of events are written to a log file for the channel they happened in (`servers/<server id>/<channel id>/<date>.log`), each with the time and the sender's username and user ID: changes to a server's settings, the setup command, messages posted on an admin's behalf, changes to the watchlist, image reports (the image number only, not the reason), and searches that the watchlist blocked (including the search terms). Server admins can ask Cloudy to post one of these files with `;log`.
4. **Direct messages.** If you send Cloudy a direct message, its settings record and any log files for that conversation are kept under your user ID.

In memory only, and gone when Cloudy restarts: the time of your last lookup (for the cooldown), your roles for up to 30 seconds, and the servers, channels and roles Discord tells Cloudy about while it runs.

Image reports are different: `;report` posts a message in the server's own report channel with a mention of the person reporting and the reason they gave. That message lives in Discord, is seen by that server's staff, and is under that server's control.

## Who else receives information

- **Discord** receives everything you send Cloudy and everything Cloudy sends back, as it does for any bot. See [Discord's privacy policy](https://discord.com/privacy).
- **Manebooru** receives the search terms, image numbers and filter numbers in the commands people run, together with the bot's API key if one is set and a User-Agent that identifies Cloudy and its version. It does not receive Discord user IDs, usernames or server names. See Manebooru's own site for its policies.
- **The operator's hosting.** The files and logs above sit on the server the operator runs Cloudy on, and are accessible to the operator.

Nothing is sold, rented, used for advertising, or used to train or evaluate any model. Information is not shared with anyone else unless the law requires it.

## How long things are kept

- **Server settings** are kept while Cloudy is in the server. Cloudy does not delete a server's settings automatically when it is removed, so they remain until deleted on request.
- **Log files** are kept until they are deleted. The operator deletes them on request and may set Cloudy to delete log files automatically after a number of days.
- **The console log** is kept for as long as the system log on the operator's server keeps it, which is governed by that system's own log rotation.
- **Direct message records** are kept until deleted on request.

## Your choices

- **See or delete what is stored about you.** Contact the operator (below) with your Discord user ID. They will find the files and log entries that contain it and delete them. Removing your data does not stop you using Cloudy; new commands will be logged again.
- **Stop Cloudy handling your messages.** A server's admins can make Cloudy ignore a channel, a role or a person, and can remove Cloudy from the server. Cloudy only ever acts on a message that begins with the prefix or mentions it, so you can also simply not use those.
- **Server admins** can ask for their server's settings and logs to be deleted in the same way after removing the bot.

Deleting a message or leaving a server in Discord does not delete what Cloudy has already logged. Ask the operator to do that.

## Children

Discord does not allow people under 13 (or the higher minimum age in their country) to have an account, and Cloudy does not knowingly collect information from them. If you believe a child's information has been logged, contact the operator and it will be deleted.

## Security

Cloudy's access tokens are kept out of the code and supplied through the server's configuration. Its data folder is readable only by the account that runs the bot. Admin commands are restricted to people the server has made admins of the bot, and a person's logs and settings are kept apart from other servers'. No system is perfectly secure, and this does not promise that nothing can go wrong. To report a security problem, see [SECURITY.md](SECURITY.md).

## Changes to this policy

This policy lives in the Cloudy Canvas repository, and its full history of changes is visible there. The date at the top shows when it was last updated. If Cloudy starts handling information in a new way, this policy will be updated before or when that change is released.

## Contact

Questions, requests to see or delete data, or concerns: ask in the [Cloudy Canvas Discord server](https://discord.gg/K4pq9AnN8F) and say you would like to talk privately, or open an issue at [github.com/romulus4444/Cloudy-Canvas/issues](https://github.com/romulus4444/Cloudy-Canvas/issues) without including personal details.

## If you run your own copy of Cloudy

Cloudy is open source, and this policy describes how the code behaves and how the operator's copy is run. If you host your own copy you are its operator: you decide how long its files are kept, you are responsible for how your copy is run, and you should write a privacy policy for it. You are welcome to start from this one; the technical detail behind it is in the README's [What Cloudy stores](README.md#what-cloudy-stores).

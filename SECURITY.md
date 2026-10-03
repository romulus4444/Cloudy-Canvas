# Security Policy

## Reporting a vulnerability

Please do not open a public issue for a security problem. Use GitHub's private reporting instead: open the **Security** tab of this repository and choose **Report a vulnerability** ([direct link](../../security/advisories/new)).

If that option is not available, use the Discord server linked in the [README](README.md) and ask for a private way to share the details before you post them anywhere.

A useful report says what you did, what you expected, what happened instead, and which version (the commit on `mane`) you tried it on. A way to reproduce it in a test server helps most.

This is a small volunteer project. Reports are read and handled as time allows, with no promised response time. There are no numbered releases: the supported version is the latest commit on `mane`.

### What counts

Examples of things worth reporting:

- getting past an admin check (using an admin command without the admin role, or without the Administrator or Manage Server permission)
- reading or changing another server's settings or logs
- making the bot ping `@everyone`, `@here` or a role it should not
- getting a search past the watchlist or the server's safe mode
- writing false entries into the logs
- the Discord or Manebooru token showing up anywhere it should not

Not in scope: flooding the bot with commands (each user has a short cooldown, and Discord rate-limits the rest), what is on Manebooru itself, problems in Discord, and the security of the machine you run your own copy on.

## What protects the bot

For people running their own copy, and for reviewers:

- **Admin commands fail closed.** Only members with the server's admin role, or with the Administrator or Manage Server permission, can use them. Until an admin role is set, only the permissions count. Everyone else gets no reply at all, so the commands are not advertised to them.
- **Roles are checked fresh.** A member's roles are fetched from Discord (and cached for 30 seconds), so removing a role takes effect without restarting the bot, and no privileged "server members" intent is needed.
- **Mentions are off by default.** Replies cannot ping anyone unless a command asks to: `;echo` may mention users and roles but never `@everyone` or `@here`, and the report and watchlist alerts ping only the role the server chose.
- **Log lookups stay inside their server's folder.** The channel and date for `;log` may only contain letters, digits, `-` and `_`, and the path is built from IDs.
- **User text cannot forge log entries.** Line breaks and control characters in what people type are written out before it is logged.
- **Settings are written safely.** Files are replaced atomically and one that cannot be read is moved aside rather than overwritten.
- **Secrets stay out of the repository.** The Discord and Manebooru tokens are read from environment variables, user secrets or a git-ignored `appsettings.json`, never from tracked files, and the Manebooru key is never logged.
- **Dependencies are kept current.** Dependabot proposes updates weekly, and the CI build treats compiler and analyzer warnings as errors.

## Running your own copy safely

- Keep the bot token out of the repository. Prefer an environment variable (`DiscordSettings__token`) or, on NixOS, `services.cloudy-canvas.environmentFile` pointing at a file only root can read. If a token is ever exposed, reset it in the Discord Developer Portal straight away.
- Turn on only the intent Cloudy needs (Message Content) and invite her with the permissions value given in the [README](README.md), no more.
- The storage folder holds usernames and IDs in its logs, so keep it readable only by the account that runs the bot. See [What Cloudy stores](README.md#what-cloudy-stores), and consider setting `LogRetention:RetentionDays`.
- Pull updates from `mane` regularly.

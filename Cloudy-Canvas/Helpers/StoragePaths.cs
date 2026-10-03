namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.IO;

    /// <summary>
    /// Whose files a path is for: a server's, or (when <see cref="GuildId"/> is null) a user's direct messages with the bot.
    /// </summary>
    /// <param name="UserId">The user who sent the command.</param>
    /// <param name="GuildId">The server, or null in a direct message.</param>
    /// <param name="ChannelId">The channel the command was sent in.</param>
    public sealed record StorageScope(ulong UserId, ulong? GuildId, ulong ChannelId);

    /// <summary>
    /// Where each kind of file lives under the storage root. Kept apart from Discord so the layout, and in particular the guarantee that
    /// a log lookup can't leave its server's folder, can be tested. Building a path creates the folders it passes through.
    /// </summary>
    public static class StoragePaths
    {
        /// <summary>
        /// The path of a file under <paramref name="root"/>. <paramref name="scope"/> is null for files that belong to no server.
        /// For <see cref="FilePathType.LogRetrieval"/> the path is the channel's folder and date of the log to read; both are checked first,
        /// because they come from what an admin typed.
        /// </summary>
        /// <exception cref="ArgumentException">A log channel or date contains anything but letters, digits, '-' and '_'.</exception>
        public static string Build(string root, FilePathType type, string filename, string extension, StorageScope scope, string logChannel, string date, DateTime utcNow)
        {
            var filepath = root;
            CreateDirectoryIfNotExists(filepath);

            if (type != FilePathType.Root)
            {
                filepath = Path.Join(filepath, "servers");
                CreateDirectoryIfNotExists(filepath);

                if (scope is { GuildId: null })
                {
                    filepath = Path.Join(filepath, "_userdms");
                    CreateDirectoryIfNotExists(filepath);
                    filepath = Path.Join(filepath, $"{scope.UserId}");
                    CreateDirectoryIfNotExists(filepath);
                }
                else if (scope != null)
                {
                    filepath = Path.Join(filepath, $"{scope.GuildId}");
                    CreateDirectoryIfNotExists(filepath);

                    if (type == FilePathType.Channel)
                    {
                        filepath = Path.Join(filepath, $"{scope.ChannelId}");
                        CreateDirectoryIfNotExists(filepath);
                    }
                    else if (type != FilePathType.Server)
                    {
                        if (!FileHelper.IsSafePathSegment(logChannel) || !FileHelper.IsSafePathSegment(date))
                        {
                            throw new ArgumentException("Log channel and date may only contain letters, digits, '-' and '_'.");
                        }

                        filepath = Path.Join(filepath, $"{logChannel}");
                        CreateDirectoryIfNotExists(filepath);
                        return Path.Join(filepath, $"{date}.{extension}");
                    }
                }
            }

            return filename switch
            {
                "" => Path.Join(filepath, $"default.{extension}"),
                "<date>" => Path.Join(filepath, $"{FileHelper.DateStamp(utcNow)}.{extension}"),
                _ => Path.Join(filepath, $"{filename}.{extension}"),
            };
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }
    }
}

namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    public static class FileHelper
    {
        // Per-server settings are re-read if someone edits the file by hand. The presettings object is shared with the
        // dependency-injection container and other components, so it must stay the same instance.
        private static readonly JsonFileStore<ServerSettings> ServerSettingsStore = new(true);
        private static readonly JsonFileStore<AllPreloadedSettings> PresettingsStore = new(false);

        public static string SetUpFilepath(FilePathType type, string filename, string extension, SocketCommandContext context = null, string logChannel = "", string date = "")
        {
            //Root
            var filepath = DevSettings.RootPath;
            CreateDirectoryIfNotExists(filepath);

            //Server
            if (type != FilePathType.Root)
            {
                filepath = Path.Join(filepath, "servers");
                CreateDirectoryIfNotExists(filepath);

                if (context is { IsPrivate: true })
                {
                    filepath = Path.Join(filepath, "_userdms");
                    CreateDirectoryIfNotExists(filepath);
                    filepath = Path.Join(filepath, $"{context.User.Id}");
                    CreateDirectoryIfNotExists(filepath);
                }
                else
                {
                    if (context != null)
                    {
                        filepath = Path.Join(filepath, $"{context.Guild.Id}");
                        CreateDirectoryIfNotExists(filepath);

                        //channel
                        if (type != FilePathType.Server)
                        {
                            if (type == FilePathType.Channel)
                            {
                                filepath = Path.Join(filepath, $"{context.Channel.Id}");
                                CreateDirectoryIfNotExists(filepath);
                            }
                            else
                            {
                                if (!IsSafePathSegment(logChannel) || !IsSafePathSegment(date))
                                {
                                    throw new ArgumentException("Log channel and date may only contain letters, digits, '-' and '_'.");
                                }

                                filepath = Path.Join(filepath, $"{logChannel}");
                                CreateDirectoryIfNotExists(filepath);
                                filepath = Path.Join(filepath, $"{date}.{extension}");
                                return filepath;
                            }
                        }
                    }
                }
            }

            filepath = filename switch
            {
                "" => Path.Join(filepath, $"default.{extension}"),
                "<date>" => Path.Join(filepath, $"{DateStamp(DateTime.UtcNow)}.{extension}"),
                _ => Path.Join(filepath, $"{filename}.{extension}"),
            };
            return filepath;
        }

        public static async Task<ServerSettings> LoadServerSettingsAsync(SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "settings", "conf", context);
            return await ServerSettingsStore.LoadAsync(filepath);
        }

        public static async Task SaveServerSettingsAsync(ServerSettings settings, SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "settings", "conf", context);
            await ServerSettingsStore.SaveAsync(filepath, settings);
        }

        public static async Task<AllPreloadedSettings> LoadAllPresettingsAsync()
        {
            var filepath = SetUpFilepath(FilePathType.Root, "preloadedsettings", "conf");
            return await PresettingsStore.LoadAsync(filepath);
        }

        public static async Task<ServerPreloadedSettings> LoadServerPresettingsAsync(SocketCommandContext context, AllPreloadedSettings allPresettingsInput = null)
        {
            AllPreloadedSettings allPresettings;
            if (allPresettingsInput == null)
            {
                allPresettings = await LoadAllPresettingsAsync();
            }
            else
            {
                allPresettings = allPresettingsInput;
            }

            var settings = new ServerPreloadedSettings();
            var serverId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            var name = context.IsPrivate ? context.User.Username : context.Guild.Name;
            settings.Name = name;
            if (allPresettings.Settings.TryGetValue(serverId, out var existing))
            {
                settings = existing;
            }
            else
            {
                // Another command may have created the entry at the same time; keep whichever got there first.
                if (allPresettings.Settings.TryAdd(serverId, settings))
                {
                    await SaveAllPresettingsAsync(allPresettings);
                }
                else
                {
                    settings = allPresettings.Settings[serverId];
                }
            }

            return settings;
        }

        public static async Task SaveAllPresettingsAsync(AllPreloadedSettings settings)
        {
            var filepath = SetUpFilepath(FilePathType.Root, "preloadedsettings", "conf");
            await PresettingsStore.SaveAsync(filepath, settings);
        }

        /// <summary>The date part of a daily log file name.</summary>
        public static string DateStamp(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// True if the text is safe to use as a single path component: non-empty and only ASCII letters, digits, '-' or '_'.
        /// Rejects separators, dots (so no ".."), and anything else that could escape the intended directory.
        /// </summary>
        public static bool IsSafePathSegment(string segment)
        {
            return !string.IsNullOrEmpty(segment) && segment.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
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

    public enum FilePathType
    {
        Root,
        Server,
        Channel,
        LogRetrieval,
    }
}

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

        /// <summary>Where settings and logs are stored; set once at startup from the Storage:RootPath setting.</summary>
        public static string RootPath { get; private set; } = StorageSettings.DefaultRootPath;

        public static void UseRootPath(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Storage:RootPath must not be empty.", nameof(rootPath));
            }

            RootPath = rootPath;
        }

        public static string SetUpFilepath(FilePathType type, string filename, string extension, SocketCommandContext context = null, string logChannel = "", string date = "")
        {
            StorageScope scope = null;
            if (context != null)
            {
                scope = context.IsPrivate
                    ? new StorageScope(context.User.Id, null, context.Channel.Id)
                    : new StorageScope(context.User.Id, context.Guild.Id, context.Channel.Id);
            }

            return StoragePaths.Build(RootPath, type, filename, extension, scope, logChannel, date, DateTime.UtcNow);
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
    }

    public enum FilePathType
    {
        Root,
        Server,
        Channel,
        LogRetrieval,
    }
}

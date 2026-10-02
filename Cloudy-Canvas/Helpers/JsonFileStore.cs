namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Serilog;

    /// <summary>
    /// Reads and writes settings objects stored as JSON files.
    /// <list type="bullet">
    /// <item>Writes are atomic (temp file, then replace), so a crash can never leave a half-written file behind.</item>
    /// <item>All access to one file is serialised, so parallel commands can't interleave a read and a write.</item>
    /// <item>Each file has one shared, cached object. Every caller mutates the same instance, so one command's save can't
    /// overwrite another's changes with a stale copy.</item>
    /// <item>A file that is empty or isn't valid JSON is moved aside (<c>.corrupt-yyyyMMddHHmmss</c>) and replaced by defaults,
    /// instead of failing every later command with a null reference.</item>
    /// </list>
    /// </summary>
    public class JsonFileStore<T>
        where T : class, new()
    {
        private readonly bool _reloadWhenFileChanges;
        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        /// <param name="reloadWhenFileChanges">
        /// When true, a file edited outside the bot (detected through its write time) is re-read on the next load.
        /// Leave it false for an object that other components hold on to.
        /// </param>
        public JsonFileStore(bool reloadWhenFileChanges)
        {
            _reloadWhenFileChanges = reloadWhenFileChanges;
        }

        /// <summary>Returns the shared object for <paramref name="path"/>, creating the file with default values if it doesn't exist.</summary>
        public async Task<T> LoadAsync(string path)
        {
            var entry = GetEntry(path);
            await entry.Gate.WaitAsync();
            try
            {
                if (entry.Value != null)
                {
                    if (!_reloadWhenFileChanges)
                    {
                        return entry.Value;
                    }

                    if (File.Exists(path) && File.GetLastWriteTimeUtc(path) == entry.WriteTimeUtc)
                    {
                        return entry.Value;
                    }
                }

                entry.Value = await ReadOrCreateAsync(path);
                entry.WriteTimeUtc = File.GetLastWriteTimeUtc(path);
                return entry.Value;
            }
            finally
            {
                entry.Gate.Release();
            }
        }

        /// <summary>Atomically writes <paramref name="value"/> to <paramref name="path"/> and makes it the shared object for that file.</summary>
        public async Task SaveAsync(string path, T value)
        {
            var entry = GetEntry(path);
            await entry.Gate.WaitAsync();
            try
            {
                await WriteAtomicAsync(path, value);
                entry.Value = value;
                entry.WriteTimeUtc = File.GetLastWriteTimeUtc(path);
            }
            finally
            {
                entry.Gate.Release();
            }
        }

        private static async Task<T> ReadOrCreateAsync(string path)
        {
            if (File.Exists(path))
            {
                var contents = await File.ReadAllTextAsync(path);
                try
                {
                    var loaded = JsonConvert.DeserializeObject<T>(contents);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
                catch (JsonException)
                {
                    // Handled below together with an empty file.
                }

                Quarantine(path);
            }

            var defaults = new T();
            await WriteAtomicAsync(path, defaults);
            return defaults;
        }

        private static async Task WriteAtomicAsync(string path, T value)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var contents = JsonConvert.SerializeObject(value, Formatting.Indented);
            var temp = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllTextAsync(temp, contents);
                await ReplaceWithRetryAsync(temp, path);
            }
            catch
            {
                try
                {
                    File.Delete(temp);
                }
                catch (IOException)
                {
                    // Leave it; a stray temp file is harmless.
                }

                throw;
            }
        }

        /// <summary>
        /// Replacing a file fails on Windows while something else (antivirus, a backup tool, an editor) has it open.
        /// Such locks are normally gone within moments, so retry a few times before giving up.
        /// </summary>
        private static async Task ReplaceWithRetryAsync(string temp, string path)
        {
            const int attempts = 6;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    File.Move(temp, path, true);
                    return;
                }
                catch (Exception ex) when (attempt < attempts && (ex is IOException || ex is UnauthorizedAccessException))
                {
                    await Task.Delay(25 * attempt);
                }
            }
        }

        private static void Quarantine(string path)
        {
            var aside = $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            try
            {
                File.Move(path, aside, true);
                Log.Error("Settings file {Path} was empty or not valid JSON. It was moved to {Backup} and replaced with default settings", path, aside);
            }
            catch (IOException ex)
            {
                Log.Error(ex, "Settings file {Path} was empty or not valid JSON and could not be moved aside; replacing it with default settings", path);
            }
        }

        private Entry GetEntry(string path)
        {
            return _entries.GetOrAdd(Path.GetFullPath(path), _ => new Entry());
        }

        private sealed class Entry
        {
            public SemaphoreSlim Gate { get; } = new(1, 1);

            public T Value { get; set; }

            public DateTime WriteTimeUtc { get; set; }
        }
    }
}

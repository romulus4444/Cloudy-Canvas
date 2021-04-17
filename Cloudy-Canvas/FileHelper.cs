﻿namespace Cloudy_Canvas
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Discord.Commands;

    public static class FileHelper
    {
        public static string SetUpFilepath(FilePathType type, string filename, string extension, SocketCommandContext context = null)
        {
            //Root
            var filepath = "BotSettings";
            CreateDirectoryIfNotExists(filepath);

            //Server
            if (type != FilePathType.Root && (type == FilePathType.Server || type == FilePathType.Channel))
            {
                filepath = Path.Join(filepath, "Servers");
                CreateDirectoryIfNotExists(filepath);

                if (context != null && context.IsPrivate)
                {
                    filepath = Path.Join(filepath, "_UserDMs");
                    CreateDirectoryIfNotExists(filepath);
                    filepath = Path.Join(filepath, $"@{context.User.Username}");
                    CreateDirectoryIfNotExists(filepath);
                }
                else
                {
                    if (context != null)
                    {
                        filepath = Path.Join(filepath, $"{context.Guild.Name}");
                        CreateDirectoryIfNotExists(filepath);

                        //channel
                        if (type == FilePathType.Channel)
                        {
                            filepath = Path.Join(filepath, $"#{context.Channel.Name}");
                            CreateDirectoryIfNotExists(filepath);
                        }
                    }
                }
            }

            switch (filename)
            {
                case "":
                    filepath = Path.Join(filepath, $"Default.{extension}");
                    break;
                case "<date>":
                    filepath = Path.Join(filepath, $"{DateTime.Today.ToShortDateString()}.{extension}");
                    break;
                default:
                    filepath = Path.Join(filepath, $"{filename}.{extension}");
                    break;
            }

            return filepath;
        }

        public static async Task WriteSpoilerListToFileAsync(List<Tuple<long, string>> tagList)
        {
            var filepath = SetUpFilepath(FilePathType.Root, "Spoilers", "txt");
            await File.WriteAllTextAsync(filepath, "Spoilered Tags:\n");
            foreach (var (tagId, tagName) in tagList)
            {
                await File.AppendAllTextAsync(filepath, $"{tagId}, {tagName}\n");
            }
        }

        public static async Task<List<Tuple<long, string>>> GetSpoilerTagIdListFromFileAsync()
        {
            var filepath = SetUpFilepath(FilePathType.Root, "Spoilers", "txt");
            if (!File.Exists(filepath))
            {
                await File.WriteAllTextAsync(filepath, "Spoilered Tags:\n");
            }

            var fileContents = File.ReadAllLines(filepath);
            var spoilerList = new List<Tuple<long, string>>();
            foreach (var line in fileContents)
            {
                if (line == "Spoilered Tags:")
                {
                    continue;
                }

                var splitLine = line.Split(',', 2);
                var parts = new Tuple<long, string>(long.Parse(splitLine[0]), splitLine[1].Trim());
                spoilerList.Add(parts);
            }

            return spoilerList;
        }

        public static async Task<string> GetSetting(string settingName, SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "Settings", "txt", context);
            if (!File.Exists(filepath))
            {
                return "<ERROR> File not found";
            }

            var settings = await File.ReadAllLinesAsync(filepath);
            var retrievedSetting = "<ERROR> Setting not found";
            foreach (var setting in settings)
            {
                if (!setting.Contains($"{settingName}:"))
                {
                    continue;
                }

                var split = setting.Split(": ", 2);
                retrievedSetting = split[1];
            }

            return retrievedSetting;
        }

        public static async Task SetSetting(string settingName, string settingValue, SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "Settings", "txt", context);
            if (!File.Exists(filepath))
            {
                await File.WriteAllTextAsync(filepath, $"{settingName}: {settingValue}");
            }
            else
            {
                var settings = await File.ReadAllLinesAsync(filepath);
                var changed = false;
                for (var x = 0; x < settings.Length; x++)
                {
                    if (!settings[x].Contains($"{settingName}: "))
                    {
                        continue;
                    }

                    settings[x] = $"{settingName}: {settingValue}";
                    changed = true;
                }

                if (changed)
                {
                    await File.WriteAllLinesAsync(filepath, settings);
                }
                else
                {
                    await File.AppendAllTextAsync(filepath, $"{settingName}: {settingValue}");
                }
            }
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
    }
}

namespace Cloudy_Canvas
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
            var filepath = "BotSettings/";
            CreateDirectoryIfNotExists(filepath);

            //Server
            if (type != FilePathType.Root && (type == FilePathType.Server || type == FilePathType.Channel))
            {
                filepath += "Servers/";
                CreateDirectoryIfNotExists(filepath);

                if (context.IsPrivate)
                {
                    filepath += "_UserDMs/";
                    CreateDirectoryIfNotExists(filepath);
                    filepath += $"@{context.User.Username}/";
                    CreateDirectoryIfNotExists(filepath);
                }
                else
                {
                    filepath += $"{context.Guild.Name}/";
                    CreateDirectoryIfNotExists(filepath);

                    //channel
                    if (type == FilePathType.Channel)
                    {
                        filepath += $"#{context.Channel.Name}/";
                        CreateDirectoryIfNotExists(filepath);
                    }
                }
            }

            switch (filename)
            {
                case "":
                    filepath += $"Default.{extension}";
                    break;
                case "<date>":
                    filepath += $"{DateTime.Today.ToShortDateString()}.{extension}";
                    break;
                default:
                    filepath += $"{filename}.{extension}";
                    break;
            }

            return filepath;
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                directory.Create();
            }
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

                var splitLine = line.Split(',',2);
                var parts = new Tuple<long, string>(long.Parse(splitLine[0]), splitLine[1].Trim());
                spoilerList.Add(parts);
            }

            return spoilerList;
        }
    }

    public enum FilePathType
    {
        Root,
        Server,
        Channel,
    }
}

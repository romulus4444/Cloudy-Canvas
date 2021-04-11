namespace Cloudy_Canvas
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Discord.Commands;

    public static class FileHelper
    {
        public static string SetUpFilepath(SocketCommandContext context, bool log = false)
        {
            var filepath = "Logs/";
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
                if (log)
                {
                    filepath += $"#{context.Channel.Name}/";
                    CreateDirectoryIfNotExists(filepath);
                }
            }

            if (log)
            {
                filepath += $"{DateTime.Today.ToShortDateString()}.txt";
            }
            else
            {
                filepath += "blacklist.txt";
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

        public static void WriteSpoilerListToFile(List<Tuple<long, string>> tagList)
        {
            var filepath = "Logs/";
            CreateDirectoryIfNotExists(filepath);
            filepath += "spoilers.txt";
            File.WriteAllText(filepath, "Spoilered Tags:\n");
            foreach (var (tagId, tagName) in tagList)
            {
                File.AppendAllText(filepath, $"{tagId}, {tagName}\n");
            }
        }

        public static List<Tuple<long, string>> GetSpoilerTagIdListFromFile()
        {
            var filepath = "Logs/";
            CreateDirectoryIfNotExists(filepath);
            filepath += "spoilers.txt";
            if (!File.Exists(filepath))
            {
                File.WriteAllText(filepath, "Spoilered Tags:\n");
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
}

namespace Cloudy_Canvas
{
    using System;
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
    }
}

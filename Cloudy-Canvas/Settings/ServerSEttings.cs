namespace Cloudy_Canvas.Settings
{
    using System;
    using System.Collections.Generic;

    public class ServerSettings
    {
        public ServerSettings()
        {
            filterId = 175;
            adminChannel = 0;
            adminRole = 0;
            aliases = new List<Tuple<string, string>>();
            spoilerList = new List<Tuple<long, string>>();
            yellowList = new List<string>();
            yellowAlertChannel = 0;
            yellowAlertRole = 0;
            yellowPing = false;
            redList = new List<Tuple<long, string>>();
            redAlertChannel = 0;
            redAlertRole = 0;
            redPing = false;
            logPostChannel = 0;
            reportChannel = 0;
            reportRole = 0;
            reportPing = false;
            ignoredChannels = new List<ulong>();
            ignoredRoles = new List<ulong>();
            allowedUsers = new List<ulong>();
        }

        public int filterId { get; set; }
        public ulong adminChannel { get; set; }
        public ulong adminRole { get; set; }
        public List<Tuple<string, string>> aliases { get; set; }
        public List<Tuple<long, string>> spoilerList { get; set; }
        public List<string> yellowList { get; set; }
        public ulong yellowAlertChannel { get; set; }
        public ulong yellowAlertRole { get; set; }
        public bool yellowPing { get; set; }
        public List<Tuple<long, string>> redList { get; set; }
        public ulong redAlertChannel { get; set; }
        public ulong redAlertRole { get; set; }
        public bool redPing { get; set; }
        public ulong logPostChannel { get; set; }
        public ulong reportChannel { get; set; }
        public ulong reportRole { get; set; }
        public bool reportPing { get; set; }
        public List<ulong> ignoredChannels { get; set; }
        public List<ulong> ignoredRoles { get; set; }
        public List<ulong> allowedUsers { get; set; }
    }
}

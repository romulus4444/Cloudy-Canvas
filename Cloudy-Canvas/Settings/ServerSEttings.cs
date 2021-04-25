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
            yellowList = new ListSettings();
            redList = new ListSettings();
            logPostChannel = 0;
            reportChannel = 0;
            reportRole = 0;
            reportPing = false;
            ignoredChannels = new List<ulong>();
            ignoredRoles = new List<ulong>();
            ignoredUsers = new List<ulong>();
        }

        public int filterId { get; set; }
        public ulong adminChannel { get; set; }
        public ulong adminRole { get; set; }
        public List<Tuple<string, string>> aliases { get; set; }
        public List<Tuple<long, string>> spoilerList { get; set; }
        public ListSettings yellowList { get; set; }
        public ListSettings redList { get; set; }
        public ulong logPostChannel { get; set; }
        public ulong reportChannel { get; set; }
        public ulong reportRole { get; set; }
        public bool reportPing { get; set; }
        public List<ulong> ignoredChannels { get; set; }
        public List<ulong> ignoredRoles { get; set; }
        public List<ulong> ignoredUsers { get; set; }
    }
}

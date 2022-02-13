namespace Cloudy_Canvas.Settings
{
    using System;
    using System.Collections.Generic;

    public class ServerSettings
    {
        public ServerSettings()
        {
            DefaultFilterId = 175;
            AdminChannel = 0;
            AdminRole = 0;
            SpoilerList = new List<Tuple<long, string>>();
            YellowList = new List<string>();
            YellowAlertChannel = 0;
            YellowAlertRole = 0;
            YellowPing = false;
            LogPostChannel = 0;
            ReportChannel = 0;
            ReportRole = 0;
            ReportPing = false;
            SafeMode = true;
            IgnoredChannels = new List<ulong>();
            IgnoredRoles = new List<ulong>();
            AllowedUsers = new List<ulong>();
            FilteredChannels = new List<Tuple<ulong, int>>();
        }

        public int DefaultFilterId { get; set; }
        public ulong AdminChannel { get; set; }
        public ulong AdminRole { get; set; }
        public List<Tuple<long, string>> SpoilerList { get; set; }
        public List<string> YellowList { get; set; }
        public ulong YellowAlertChannel { get; set; }
        public ulong YellowAlertRole { get; set; }
        public bool YellowPing { get; set; }
        public ulong LogPostChannel { get; set; }
        public ulong ReportChannel { get; set; }
        public ulong ReportRole { get; set; }
        public bool ReportPing { get; set; }
        public bool SafeMode { get; set; }
        public List<ulong> IgnoredChannels { get; set; }
        public List<ulong> IgnoredRoles { get; set; }
        public List<ulong> AllowedUsers { get; set; }
        public List<Tuple<ulong, int>> FilteredChannels { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudy_Canvas.Settings
{
    public class ServerSettings
    {
        public string accountToken { get; set; }
        public int filterId { get; set; }
        public ulong adminChannel { get; set; }
        public ulong adminRole { get; set; }
        public Tuple<string, string> aliases { get; set; }
        public List<string> spoilerList { get; set; }
        public List<string> yellowList { get; set; }
        public ulong yellowAlertChannel { get; set; }
        public ulong yellowAlertRole { get; set; }
        public List<string> redList { get; set; }
        public ulong redAlertChannel { get; set; }
        public ulong redAlertRole { get; set; }
        public ulong logPostChannel { get; set; }
        public ulong logPostRole { get; set; }
        public List<ulong> ignoredChannels { get; set; }
        public List<ulong> ignoredRoles { get; set; }
        public List<ulong> ignoredUsers { get; set; }

    }
}

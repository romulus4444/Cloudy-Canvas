namespace Cloudy_Canvas.Settings
{
    using System;
    using System.Collections.Generic;

    public class ListSettings
    {
        public ListSettings()
        {
            list = new List<string>();
            alertChannel = 0;
            alertRole = 0;
            ping = false;
        }

        public List<string> list { get; set; }
        public ulong alertChannel { get; set; }
        public ulong alertRole { get; set; }
        public bool ping { get; set; }
    }
}

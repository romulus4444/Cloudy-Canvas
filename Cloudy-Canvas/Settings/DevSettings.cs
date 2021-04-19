namespace Cloudy_Canvas.Settings
{
    public static class DevSettings
    {
        ////-------------------------Production-------------------------
        //public static char prefix { get; } = ';'; //production prefix
        //public static ulong CloudyCanvasId { get; } = 828682017868218445; //Cloudy Canvas's Production Id
        //public static string RootPath { get; } = "BotSettings"; //Production folders
        ////------------------------------------------------------------

        //-------------------------Development------------------------
        public static char prefix { get; } = '?'; //development prefix
        public static ulong CloudyCanvasId { get; } = 833556029521657856; //Cloudy Canvas's Development Id
        public static string RootPath { get; } = "DevSettings"; //Development folders
        //------------------------------------------------------------
    }
}

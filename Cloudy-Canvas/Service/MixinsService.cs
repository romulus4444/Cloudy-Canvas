namespace Cloudy_Canvas.Service
{
    using System;

    public class MixinsService
    {
        private readonly IDateTimeService _dateTime;
        public MixinsService(IDateTimeService dateTime)
        {
            _dateTime = dateTime;
        }

        public string Transpile(string query)
        {
            return query.Replace("{{today}}",_dateTime.Now().ToString("yyyy-MM-dd"));
        }
    }
}

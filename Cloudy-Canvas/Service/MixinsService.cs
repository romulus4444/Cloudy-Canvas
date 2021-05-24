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
            return query.Replace("{{today}}", _dateTime.UtcNow().ToString("yyyy-MM-dd"))
                        .Replace("{{current_year}}", _dateTime.UtcNow().ToString("yyyy"))
                        .Replace("{{current_month}}", _dateTime.UtcNow().ToString("MM"))
                        .Replace("{{current_day}}", _dateTime.UtcNow().ToString("dd"))
                        .Replace("{{current_hour}}", _dateTime.UtcNow().ToString("HH"))
                        .Replace("{{current_minute}}", _dateTime.UtcNow().ToString("mm"))
                        .Replace("{{current_second}}", _dateTime.UtcNow().ToString("ss"))
                        ;
        }
    }
}

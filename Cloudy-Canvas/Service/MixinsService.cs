namespace Cloudy_Canvas.Service
{
    using System.Globalization;

    public class MixinsService
    {
        private readonly IDateTimeService _dateTime;

        public MixinsService(IDateTimeService dateTime)
        {
            _dateTime = dateTime;
        }

        public string Transpile(string query)
        {
            var now = _dateTime.UtcNow();
            return query.Replace("{{today}}", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .Replace("{{current_year}}", now.ToString("yyyy", CultureInfo.InvariantCulture))
                    .Replace("{{current_month}}", now.ToString("MM", CultureInfo.InvariantCulture))
                    .Replace("{{current_day}}", now.ToString("dd", CultureInfo.InvariantCulture))
                    .Replace("{{current_hour}}", now.ToString("HH", CultureInfo.InvariantCulture))
                    .Replace("{{current_minute}}", now.ToString("mm", CultureInfo.InvariantCulture))
                    .Replace("{{current_second}}", now.ToString("ss", CultureInfo.InvariantCulture))
                ;
        }
    }
}

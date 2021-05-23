using System;
public interface IDateTimeService
{
    DateTime Now();
}

public class DateTimeService : IDateTimeService
{
    public DateTime Now()
    {
        return DateTime.Now;
    }
}
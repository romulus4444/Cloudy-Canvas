using System;
using Xunit;
using Cloudy_Canvas.Service;

namespace Cloudy_Canvas.Tests
{
    public class MixinsServiceTests
    {
        [Fact]
        public void DailyLyra()
        {
            var helper = new MixinsService(new MockDateTimeService());
            var result = helper.Transpile("created_at:{{today}}, Lyra");
            Assert.Equal("created_at:2021-04-23, Lyra",result);
        }
    }

    public class MockDateTimeService : IDateTimeService
    {
        public DateTime Now()
        {
            return new DateTime(2021, 4, 23);
        }
    }
}

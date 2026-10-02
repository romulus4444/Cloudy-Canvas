namespace Cloudy_Canvas.Tests.Service
{
    using System;
    using Cloudy_Canvas.Service;
    using Xunit;

    public class CooldownServiceTests
    {
        private sealed class FakeClock : IDateTimeService
        {
            public DateTime Now { get; set; } = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

            public DateTime UtcNow() => Now;
        }

        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);

        [Fact]
        public void TheFirstCommandIsAllowed()
        {
            var result = new CooldownService(new FakeClock()).Check(1, Cooldown);

            Assert.True(result.Allowed);
            Assert.False(result.ShouldNotify);
            Assert.Equal(TimeSpan.Zero, result.RetryAfter);
        }

        [Fact]
        public void ARepeatWithinTheCooldownIsRefusedWithTheTimeLeft()
        {
            var clock = new FakeClock();
            var service = new CooldownService(clock);
            service.Check(1, Cooldown);

            clock.Now = clock.Now.AddMilliseconds(500);
            var result = service.Check(1, Cooldown);

            Assert.False(result.Allowed);
            Assert.Equal(TimeSpan.FromMilliseconds(1500), result.RetryAfter);
        }

        [Fact]
        public void TheCommandIsAllowedAgainOnceTheCooldownHasPassed()
        {
            var clock = new FakeClock();
            var service = new CooldownService(clock);
            service.Check(1, Cooldown);

            clock.Now = clock.Now.AddSeconds(2);

            Assert.True(service.Check(1, Cooldown).Allowed);
        }

        [Fact]
        public void OnlyTheFirstRefusalInAWindowAsksForAReply()
        {
            var clock = new FakeClock();
            var service = new CooldownService(clock);
            service.Check(1, Cooldown);

            clock.Now = clock.Now.AddMilliseconds(100);
            Assert.True(service.Check(1, Cooldown).ShouldNotify);
            clock.Now = clock.Now.AddMilliseconds(100);
            Assert.False(service.Check(1, Cooldown).ShouldNotify);
            Assert.False(service.Check(1, Cooldown).ShouldNotify);

            // A new window starts after the cooldown expires, and the first refusal in it is notified again.
            clock.Now = clock.Now.AddSeconds(5);
            Assert.True(service.Check(1, Cooldown).Allowed);
            clock.Now = clock.Now.AddMilliseconds(100);
            Assert.True(service.Check(1, Cooldown).ShouldNotify);
        }

        [Fact]
        public void RefusedAttemptsDoNotExtendTheCooldown()
        {
            var clock = new FakeClock();
            var service = new CooldownService(clock);
            service.Check(1, Cooldown);

            // Hammering the command for a second must not push the end of the window out.
            for (var i = 0; i < 10; i++)
            {
                clock.Now = clock.Now.AddMilliseconds(100);
                Assert.False(service.Check(1, Cooldown).Allowed);
            }

            clock.Now = clock.Now.AddMilliseconds(1000); // 2 s after the first command in total
            Assert.True(service.Check(1, Cooldown).Allowed);
        }

        [Fact]
        public void UsersAreIndependent()
        {
            var service = new CooldownService(new FakeClock());
            service.Check(1, Cooldown);

            Assert.True(service.Check(2, Cooldown).Allowed);
            Assert.False(service.Check(1, Cooldown).Allowed);
        }

        [Fact]
        public void ExpiredEntriesArePrunedSoMemoryDoesNotGrowForever()
        {
            var clock = new FakeClock();
            var service = new CooldownService(clock);
            for (ulong user = 0; user < 1500; user++)
            {
                service.Check(user, Cooldown);
            }

            clock.Now = clock.Now.AddMinutes(1);
            service.Check(99999, Cooldown); // goes over the threshold, which triggers the prune

            // Everyone from before is long past their cooldown and behaves like a fresh user either way;
            // what matters is that this keeps working with a big table.
            Assert.True(service.Check(0, Cooldown).Allowed);
            Assert.False(service.Check(99999, Cooldown).Allowed);
        }
    }
}

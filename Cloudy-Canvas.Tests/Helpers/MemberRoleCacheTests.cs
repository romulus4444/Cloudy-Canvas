namespace Cloudy_Canvas.Tests.Helpers
{
    using System;
    using Cloudy_Canvas.Helpers;
    using Discord;
    using Xunit;

    public class MemberRoleCacheTests
    {
        private static readonly DateTime Start = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

        private static GuildMember Member(params ulong[] roles) => new(roles, new GuildPermissions(0));

        [Fact]
        public void NothingIsKnownAtFirst()
        {
            Assert.False(new MemberRoleCache(Lifetime).TryGet(1, 2, Start, out var member));
            Assert.Null(member);
        }

        [Fact]
        public void AMemberIsRememberedForTheLifetime()
        {
            var cache = new MemberRoleCache(Lifetime);
            cache.Set(1, 2, Start, Member(7, 8));

            Assert.True(cache.TryGet(1, 2, Start.AddSeconds(29), out var member));
            Assert.Equal(new ulong[] { 7, 8 }, member.RoleIds);
        }

        [Fact]
        public void AMemberIsForgottenWhenTheLifetimeEnds()
        {
            var cache = new MemberRoleCache(Lifetime);
            cache.Set(1, 2, Start, Member(7));

            Assert.False(cache.TryGet(1, 2, Start.AddSeconds(30), out _));
            Assert.False(cache.TryGet(1, 2, Start.AddMinutes(5), out _));
        }

        [Fact]
        public void SettingAgainRefreshesTheEntryAndItsLifetime()
        {
            var cache = new MemberRoleCache(Lifetime);
            cache.Set(1, 2, Start, Member(7));
            cache.Set(1, 2, Start.AddSeconds(20), Member(9)); // roles changed, fetched again

            Assert.True(cache.TryGet(1, 2, Start.AddSeconds(45), out var member));
            Assert.Equal(new ulong[] { 9 }, member.RoleIds);
        }

        [Fact]
        public void EntriesAreKeptPerServerAndPerMember()
        {
            var cache = new MemberRoleCache(Lifetime);
            cache.Set(1, 2, Start, Member(7));

            Assert.False(cache.TryGet(1, 3, Start, out _)); // another member
            Assert.False(cache.TryGet(9, 2, Start, out _)); // the same member in another server
        }

        [Fact]
        public void PruningDropsExpiredEntriesButKeepsLiveOnes()
        {
            var cache = new MemberRoleCache(Lifetime);
            for (ulong user = 0; user < 5000; user++)
            {
                cache.Set(1, user, Start, Member(1));
            }

            var later = Start.AddMinutes(1);
            cache.Set(1, 99999, later, Member(2)); // crosses the threshold, so expired entries are pruned

            Assert.True(cache.TryGet(1, 99999, later, out var live));
            Assert.Equal(new ulong[] { 2 }, live.RoleIds);
            Assert.False(cache.TryGet(1, 0, later, out _));
        }
    }
}

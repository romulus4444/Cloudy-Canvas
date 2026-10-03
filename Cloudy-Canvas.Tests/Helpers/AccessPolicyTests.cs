namespace Cloudy_Canvas.Tests.Helpers
{
    using System.Linq;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Xunit;

    public class AccessPolicyTests
    {
        private const ulong User = 100;
        private const ulong Channel = 200;
        private const ulong AdminRole = 1;
        private const ulong IgnoredRole = 2;
        private const ulong OtherRole = 3;

        private static ServerSettings Settings(ulong adminRole = AdminRole, ulong[] ignoredRoles = null, ulong[] ignoredChannels = null, ulong[] allowedUsers = null)
        {
            var settings = new ServerSettings { AdminRole = adminRole };
            settings.IgnoredRoles.AddRange(ignoredRoles ?? new ulong[0]);
            settings.IgnoredChannels.AddRange(ignoredChannels ?? new ulong[0]);
            settings.AllowedUsers.AddRange(allowedUsers ?? new ulong[0]);
            return settings;
        }

        private static bool Can(ServerSettings settings, params ulong[] roles) => AccessPolicy.CanRunCommands(User, Channel, roles, settings);

        // ---- CanRunCommands ------------------------------------------------------------------------------------------

        [Fact]
        public void EveryoneIsAllowedByDefault()
        {
            Assert.True(Can(Settings(), OtherRole));
            Assert.True(Can(Settings()));
        }

        [Fact]
        public void AnIgnoredRoleIsNotAnswered()
        {
            Assert.False(Can(Settings(ignoredRoles: new[] { IgnoredRole }), IgnoredRole));
        }

        [Fact]
        public void HavingAnIgnoredRoleAmongOthersStillCounts()
        {
            Assert.False(Can(Settings(ignoredRoles: new[] { IgnoredRole }), OtherRole, IgnoredRole, 99));
        }

        [Fact]
        public void AnyOneOfSeveralIgnoredRolesIsEnough()
        {
            var settings = Settings(ignoredRoles: new ulong[] { 10, 11, 12 });

            Assert.False(Can(settings, 12));
            Assert.False(Can(settings, OtherRole, 10));
            Assert.True(Can(settings, OtherRole));
        }

        [Fact]
        public void RolesThatAreNotIgnoredAreAnswered()
        {
            Assert.True(Can(Settings(ignoredRoles: new[] { IgnoredRole }), OtherRole));
        }

        [Fact]
        public void AnIgnoredChannelIsNotAnswered()
        {
            Assert.False(Can(Settings(ignoredChannels: new[] { Channel }), OtherRole));
        }

        [Fact]
        public void AnotherIgnoredChannelDoesNotMatter()
        {
            Assert.True(Can(Settings(ignoredChannels: new ulong[] { 999 }), OtherRole));
        }

        // The bot's admin role and the allowed-user list are never ignored, whatever else is configured.
        [Fact]
        public void TheAdminRoleIsNeverIgnored()
        {
            var settings = Settings(ignoredRoles: new[] { IgnoredRole }, ignoredChannels: new[] { Channel });

            Assert.True(Can(settings, AdminRole));
            Assert.True(Can(settings, AdminRole, IgnoredRole)); // an admin who also holds an ignored role
        }

        [Fact]
        public void AnAllowedUserIsNeverIgnored()
        {
            var settings = Settings(ignoredRoles: new[] { IgnoredRole }, ignoredChannels: new[] { Channel }, allowedUsers: new[] { User });

            Assert.True(Can(settings, IgnoredRole));
        }

        [Fact]
        public void AnotherUsersAllowEntryDoesNotHelp()
        {
            Assert.False(Can(Settings(ignoredRoles: new[] { IgnoredRole }, allowedUsers: new ulong[] { 555 }), IgnoredRole));
        }

        [Fact]
        public void NoAdminRoleSetMeansNobodyMatchesIt()
        {
            // AdminRole 0 means "not configured"; it must not match a role (or member) whose id happens to be 0.
            var settings = Settings(adminRole: 0, ignoredRoles: new[] { IgnoredRole });

            Assert.False(Can(settings, 0, IgnoredRole));
        }

        // ---- IsBotAdmin ----------------------------------------------------------------------------------------------

        private static GuildPermissions None => new(0);

        private static GuildPermissions Admin => new(administrator: true);

        private static GuildPermissions ManageServer => new(manageGuild: true);

        [Fact]
        public void TheAdminRoleMakesAMemberABotAdmin()
        {
            Assert.True(AccessPolicy.IsBotAdmin(new[] { AdminRole }, None, Settings()));
            Assert.False(AccessPolicy.IsBotAdmin(new[] { OtherRole }, None, Settings()));
        }

        [Fact]
        public void ServerAdministratorsAreAlwaysBotAdmins()
        {
            Assert.True(AccessPolicy.IsBotAdmin(new ulong[0], Admin, Settings()));
            Assert.True(AccessPolicy.IsBotAdmin(new ulong[0], Admin, Settings(adminRole: 0)));
        }

        [Fact]
        public void BeforeAnAdminRoleIsSetOnlyThoseWhoCanManageTheServerAreAdmins()
        {
            var settings = Settings(adminRole: 0);

            Assert.True(AccessPolicy.IsBotAdmin(new[] { OtherRole }, ManageServer, settings));
            Assert.False(AccessPolicy.IsBotAdmin(new[] { OtherRole }, None, settings));
        }

        [Fact]
        public void OnceAnAdminRoleIsSetManageServerAloneIsNotEnough()
        {
            Assert.False(AccessPolicy.IsBotAdmin(new[] { OtherRole }, ManageServer, Settings()));
        }

        // ---- CombinePermissions --------------------------------------------------------------------------------------

        [Fact]
        public void PermissionsAreTheUnionOfEveryRole()
        {
            var everyone = new GuildPermissions(viewChannel: true).RawValue;
            var moderators = new GuildPermissions(manageGuild: true).RawValue;

            var combined = AccessPolicy.CombinePermissions(new[] { everyone, moderators });

            Assert.True(combined.ViewChannel);
            Assert.True(combined.ManageGuild);
            Assert.False(combined.Administrator);
        }

        [Fact]
        public void AnAdministratorRoleGrantsAdministrator()
        {
            var combined = AccessPolicy.CombinePermissions(new[] { new GuildPermissions(viewChannel: true).RawValue, new GuildPermissions(administrator: true).RawValue });

            Assert.True(combined.Administrator);
        }

        [Fact]
        public void NoRolesMeansNoPermissions()
        {
            Assert.Equal(0UL, AccessPolicy.CombinePermissions(Enumerable.Empty<ulong>()).RawValue);
        }
    }
}

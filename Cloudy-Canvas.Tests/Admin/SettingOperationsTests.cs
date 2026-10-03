namespace Cloudy_Canvas.Tests.Admin
{
    using System;
    using System.Linq;
    using Cloudy_Canvas.Admin;
    using Cloudy_Canvas.Settings;
    using Xunit;

    /// <summary>
    /// The wording and effects of the ;admin settings commands. The expected texts are written out in full on purpose: they are what the
    /// admin commands have always said, and a change here should be a deliberate one.
    /// </summary>
    public class SettingOperationsTests
    {
        private const ulong Admin = 111;
        private const ulong Other = 222;
        private static readonly string NL = Environment.NewLine;

        private static ServerSettings NewSettings() => new() { AdminChannel = Admin, DefaultFilterId = 175 };

        // ---- channels ------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData("adminchannel", "Admin channel not set yet.", "Admin channel is <#222>")]
        [InlineData("watchchannel", "Watch alert channel not set yet.", "Watch alerts are being posted in <#222>")]
        [InlineData("reportchannel", "Report alert channel not set yet.", "Report alerts are being posted in <#222>")]
        [InlineData("logchannel", "Log posting channel not set yet.", "Logs are being posted in <#222>")]
        public void GettingAChannelShowsItOrSaysItIsNotSet(string key, string notSet, string shown)
        {
            var setting = ChannelByKey(key);
            var settings = NewSettings();
            setting.Set(settings, 0);

            Assert.Equal(notSet, SettingOperations.GetChannel(setting, settings).Message);

            setting.Set(settings, Other);
            var outcome = SettingOperations.GetChannel(setting, settings);
            Assert.Equal(shown, outcome.Message);
            Assert.True(outcome.Success);
            Assert.False(outcome.Changed);
        }

        [Theory]
        [InlineData("adminchannel", "Admin channel set to <#222>")]
        [InlineData("watchchannel", "Watch alert channel set to <#222>")]
        [InlineData("reportchannel", "Report alert channel set to <#222>")]
        [InlineData("logchannel", "Retrieved logs will be sent to <#222>")]
        public void SettingAChannelStoresItAndSaysSo(string key, string message)
        {
            var setting = ChannelByKey(key);
            var settings = NewSettings();

            var outcome = SettingOperations.SetChannel(setting, settings, Other, "general");

            Assert.Equal(message, outcome.Message);
            Assert.True(outcome.Success);
            Assert.True(outcome.Changed);
            Assert.Equal(Other, setting.Get(settings));
        }

        [Theory]
        [InlineData("adminchannel")]
        [InlineData("watchchannel")]
        [InlineData("reportchannel")]
        [InlineData("logchannel")]
        public void AnUnknownChannelChangesNothing(string key)
        {
            var setting = ChannelByKey(key);
            var settings = NewSettings();
            setting.Set(settings, Other);

            var outcome = SettingOperations.SetChannel(setting, settings, 0, "no-such-channel");

            Assert.Equal("Invalid channel name #no-such-channel.", outcome.Message);
            Assert.False(outcome.Success);
            Assert.False(outcome.Changed);
            Assert.Equal(Other, setting.Get(settings));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ANeededChannelCannotBeLeftOut(string typed)
        {
            var settings = NewSettings();

            var outcome = SettingOperations.SetChannel(AdminSettings.WatchChannel, settings, 0, typed);

            Assert.Equal("You must specify a channel.", outcome.Message);
            Assert.False(outcome.Success);
            Assert.False(outcome.Changed);
        }

        [Theory]
        [InlineData("watchchannel", "Watch alert channel reset to the current admin channel, <#111>")]
        [InlineData("reportchannel", "Report alert channel reset to the current admin channel, <#111>")]
        [InlineData("logchannel", "Log post channel reset to the current admin channel, <#111>")]
        public void ClearingAnAlertChannelPutsItBackToTheAdminChannel(string key, string message)
        {
            var setting = ChannelByKey(key);
            var settings = NewSettings();
            setting.Set(settings, Other);

            var outcome = SettingOperations.ClearChannel(setting, settings);

            Assert.Equal(message, outcome.Message);
            Assert.True(outcome.Changed);
            Assert.Equal(Admin, setting.Get(settings));
        }

        [Fact]
        public void OnlyTheAdminChannelIsRecordedInTheListOfServers()
        {
            Assert.True(AdminSettings.AdminChannel.UpdatesGuildList);
            Assert.False(AdminSettings.WatchChannel.UpdatesGuildList);
            Assert.False(AdminSettings.ReportChannel.UpdatesGuildList);
            Assert.False(AdminSettings.LogChannel.UpdatesGuildList);
        }

        [Fact]
        public void OnlyTheAdminChannelCannotBeCleared()
        {
            Assert.Null(AdminSettings.AdminChannel.ClearFormat);
            Assert.NotNull(AdminSettings.WatchChannel.ClearFormat);
            Assert.NotNull(AdminSettings.ReportChannel.ClearFormat);
            Assert.NotNull(AdminSettings.LogChannel.ClearFormat);
        }

        // ---- roles ---------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData("adminrole", "Admin role not set yet.", "Admin role is <@&333>")]
        [InlineData("watchrole", "Watch alert role not set yet.", "Watch alerts will ping <@&333>")]
        [InlineData("reportrole", "Report alert role not set yet.", "Report alerts will ping <@&333>")]
        public void GettingARoleShowsItOrSaysItIsNotSet(string key, string notSet, string shown)
        {
            var setting = RoleByKey(key);
            var settings = NewSettings();

            Assert.Equal(notSet, SettingOperations.GetRole(setting, settings).Message);

            setting.Set(settings, 333);
            Assert.Equal(shown, SettingOperations.GetRole(setting, settings).Message);
        }

        [Theory]
        [InlineData("adminrole", "Admin role set to <@&333>")]
        [InlineData("watchrole", "Watch alerts will now ping <@&333>")]
        [InlineData("reportrole", "Report alerts will now ping <@&333>")]
        public void SettingARoleStoresItAndSaysSo(string key, string message)
        {
            var setting = RoleByKey(key);
            var settings = NewSettings();

            var outcome = SettingOperations.SetRole(setting, settings, 333, "Moderators");

            Assert.Equal(message, outcome.Message);
            Assert.True(outcome.Changed);
            Assert.Equal(333UL, setting.Get(settings));
        }

        [Theory]
        [InlineData("adminrole")]
        [InlineData("watchrole")]
        [InlineData("reportrole")]
        public void AnUnknownRoleChangesNothingAndAlwaysUsesTheRoleSigil(string key)
        {
            var setting = RoleByKey(key);
            var settings = NewSettings();
            setting.Set(settings, 333);

            var outcome = SettingOperations.SetRole(setting, settings, 0, "Nobody");

            Assert.Equal("Invalid role name @Nobody.", outcome.Message); // never "channel", never "#"
            Assert.False(outcome.Success);
            Assert.Equal(333UL, setting.Get(settings));
        }

        [Fact]
        public void ANeededRoleCannotBeLeftOut()
        {
            var outcome = SettingOperations.SetRole(AdminSettings.AdminRole, NewSettings(), 0, "");

            Assert.Equal("You must specify a role.", outcome.Message);
            Assert.False(outcome.Success);
        }

        [Fact]
        public void ClearingAnAlertRoleStopsThePingAndNamesTheChannelItNowGoesTo()
        {
            var settings = NewSettings();
            settings.WatchAlertChannel = 444;
            settings.WatchAlertRole = 333;
            settings.ReportChannel = 555;
            settings.ReportRole = 333;

            var watch = SettingOperations.ClearRole(AdminSettings.WatchRole, settings);
            var report = SettingOperations.ClearRole(AdminSettings.ReportRole, settings);

            Assert.Equal("Watch alerts will not ping anyone now in <#444>", watch.Message);
            Assert.Equal("Report alerts will not ping anyone now in <#555>", report.Message);
            Assert.Equal(0UL, settings.WatchAlertRole);
            Assert.Equal(0UL, settings.ReportRole);
            Assert.True(watch.Changed && report.Changed);
        }

        [Fact]
        public void OnlyTheAdminRoleCannotBeCleared()
        {
            Assert.Null(AdminSettings.AdminRole.ClearMessage);
            Assert.NotNull(AdminSettings.WatchRole.ClearMessage);
            Assert.NotNull(AdminSettings.ReportRole.ClearMessage);
        }

        // ---- lists ---------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData("ignorechannel", "No channels on ignore list.", "__Channel Ignore List:__", "<#1>", "<#2>")]
        [InlineData("ignorerole", "No roles on ignore list.", "__Role Ignore List:__", "<@&1>", "<@&2>")]
        [InlineData("allowuser", "No users on allow list.", "__Allowed User List:__", "<@1>", "<@2>")]
        public void GettingAListShowsEachEntryOnItsOwnLine(string key, string empty, string header, string first, string second)
        {
            var setting = ListByKey(key);
            var settings = NewSettings();

            Assert.Equal(empty, SettingOperations.GetList(setting, settings).Message);

            setting.List(settings).AddRange(new ulong[] { 1, 2 });
            Assert.Equal($"{header}{NL}{first}{NL}{second}{NL}", SettingOperations.GetList(setting, settings).Message);
        }

        [Theory]
        [InlineData("ignorechannel", "Added <#5> to ignore list.", "<#5> is already on the list.")]
        [InlineData("ignorerole", "Added <@&5> to ignore list.", "<@&5> is already on the list.")]
        [InlineData("allowuser", "Added <@5> to allow list.", "<@5> is already on the list.")]
        public void AddingToAListAddsOnceAndSaysIfItWasAlreadyThere(string key, string added, string already)
        {
            var setting = ListByKey(key);
            var settings = NewSettings();

            var first = SettingOperations.AddToList(setting, settings, 5, "x");
            var second = SettingOperations.AddToList(setting, settings, 5, "x");

            Assert.Equal(added, first.Message);
            Assert.True(first.Changed);
            Assert.Equal(already, second.Message);
            Assert.True(second.Success);
            Assert.False(second.Changed);
            Assert.Equal(new ulong[] { 5 }, setting.List(settings));
        }

        [Theory]
        [InlineData("ignorechannel", "Removed <#5> from ignore list.", "<#5> was not on the list.")]
        [InlineData("ignorerole", "Removed <@&5> from ignore list.", "<@&5> was not on the list.")]
        [InlineData("allowuser", "Removed <@5> from allow list.", "<@5> was not on the list.")]
        public void RemovingFromAListSaysIfItWasNotThere(string key, string removed, string notThere)
        {
            var setting = ListByKey(key);
            var settings = NewSettings();
            setting.List(settings).AddRange(new ulong[] { 5, 6 });

            var first = SettingOperations.RemoveFromList(setting, settings, 5, "x");
            var second = SettingOperations.RemoveFromList(setting, settings, 5, "x");

            Assert.Equal(removed, first.Message);
            Assert.True(first.Changed);
            Assert.Equal(notThere, second.Message);
            Assert.False(second.Changed);
            Assert.Equal(new ulong[] { 6 }, setting.List(settings));
        }

        [Theory]
        [InlineData("ignorechannel", "Invalid channel name #ghost.", "You must specify a channel.")]
        [InlineData("ignorerole", "Invalid role name @ghost.", "You must specify a role.")]
        [InlineData("allowuser", "Invalid user name @ghost.", "You must specify a user.")]
        public void AnUnknownOrMissingNameChangesNothing(string key, string invalid, string missing)
        {
            var setting = ListByKey(key);
            var settings = NewSettings();
            setting.List(settings).Add(9);

            foreach (var outcome in new[]
            {
                SettingOperations.AddToList(setting, settings, 0, "ghost"),
                SettingOperations.RemoveFromList(setting, settings, 0, "ghost"),
            })
            {
                Assert.Equal(invalid, outcome.Message);
                Assert.False(outcome.Success);
                Assert.False(outcome.Changed);
            }

            Assert.Equal(missing, SettingOperations.AddToList(setting, settings, 0, "").Message);
            Assert.Equal(missing, SettingOperations.RemoveFromList(setting, settings, 0, null).Message);
            Assert.Equal(new ulong[] { 9 }, setting.List(settings));
        }

        [Theory]
        [InlineData("ignorechannel", "Ignored channels list cleared.")]
        [InlineData("ignorerole", "Ignored roles list cleared.")]
        [InlineData("allowuser", "Allowed users list cleared.")]
        public void ClearingAListEmptiesIt(string key, string message)
        {
            var setting = ListByKey(key);
            var settings = NewSettings();
            setting.List(settings).AddRange(new ulong[] { 1, 2, 3 });

            var outcome = SettingOperations.ClearList(setting, settings);

            Assert.Equal(message, outcome.Message);
            Assert.True(outcome.Changed);
            Assert.Empty(setting.List(settings));
        }

        // ---- channel-specific filters --------------------------------------------------------------------------------

        [Fact]
        public void TheFilterListSaysWhenThereAreNoChannelFilters()
        {
            Assert.Equal(
                "No channel-specific filters are currently set. All channels use the server filter 175.",
                SettingOperations.GetFilterChannels(NewSettings()).Message);
        }

        [Fact]
        public void TheFilterListShowsEveryChannelWithItsFilter()
        {
            var settings = NewSettings();
            settings.FilteredChannels.Add(new ChannelFilter(7, 56027));
            settings.FilteredChannels.Add(new ChannelFilter(8, 12));

            Assert.Equal(
                $"__Channel-Specific Filter List:__{NL}(Any channel not listed here uses the server filter 175){NL}<#7>: Filter 56027{NL}<#8>: Filter 12{NL}",
                SettingOperations.GetFilterChannels(settings).Message);
        }

        [Fact]
        public void AddingAChannelFilterRecordsIt()
        {
            var settings = NewSettings();

            var outcome = SettingOperations.AddFilterChannel(settings, 7, 56027);

            Assert.Equal("Set <#7> to use filter 56027.", outcome.Message);
            Assert.True(outcome.Changed);
            var entry = Assert.Single(settings.FilteredChannels);
            Assert.Equal((7UL, 56027), (entry.ChannelId, entry.FilterId));
        }

        [Fact]
        public void AddingAFilterForAChannelThatHasOneReplacesIt()
        {
            var settings = NewSettings();
            settings.FilteredChannels.Add(new ChannelFilter(7, 12));

            var outcome = SettingOperations.AddFilterChannel(settings, 7, 99);

            Assert.Equal("Updated the filter for <#7> from 12 to 99.", outcome.Message);
            Assert.True(outcome.Changed);
            var entry = Assert.Single(settings.FilteredChannels);
            Assert.Equal(99, entry.FilterId);
        }

        [Fact]
        public void TheServerDefaultFilterIsNotRecordedPerChannel()
        {
            var settings = NewSettings();

            var outcome = SettingOperations.AddFilterChannel(settings, 7, 175);

            Assert.Equal("That's the server default filter already.", outcome.Message);
            Assert.False(outcome.Changed);
            Assert.Empty(settings.FilteredChannels);
        }

        [Fact]
        public void RemovingAChannelFilter()
        {
            var settings = NewSettings();
            settings.FilteredChannels.Add(new ChannelFilter(7, 12));
            settings.FilteredChannels.Add(new ChannelFilter(8, 13));

            var removed = SettingOperations.RemoveFilterChannel(settings, 7, "x");
            var again = SettingOperations.RemoveFilterChannel(settings, 7, "x");

            Assert.Equal("Removed <#7> from channel-specific filter list.", removed.Message);
            Assert.True(removed.Changed);
            Assert.Equal("<#7> was not on the list.", again.Message);
            Assert.False(again.Changed);
            Assert.Equal(new ulong[] { 8 }, settings.FilteredChannels.Select(c => c.ChannelId));
        }

        [Fact]
        public void RemovingAnUnknownOrMissingChannelFilterChangesNothing()
        {
            var settings = NewSettings();
            settings.FilteredChannels.Add(new ChannelFilter(7, 12));

            Assert.Equal("Invalid channel name #ghost.", SettingOperations.RemoveFilterChannel(settings, 0, "ghost").Message);
            Assert.Equal("You must specify a channel.", SettingOperations.RemoveFilterChannel(settings, 0, "").Message);
            Assert.Single(settings.FilteredChannels);
        }

        [Fact]
        public void ClearingTheChannelFilters()
        {
            var settings = NewSettings();
            settings.FilteredChannels.Add(new ChannelFilter(7, 12));

            var outcome = SettingOperations.ClearFilterChannels(settings);

            Assert.Equal("Channel-specific filters cleared.", outcome.Message);
            Assert.True(outcome.Changed);
            Assert.Empty(settings.FilteredChannels);
        }

        // ---- the registry --------------------------------------------------------------------------------------------

        [Fact]
        public void EverySettingHasItsOwnKeyAndTheKeysAreTheOnesInTheHelpText()
        {
            var keys = new[]
            {
                AdminSettings.AdminChannel.Key, AdminSettings.WatchChannel.Key, AdminSettings.ReportChannel.Key, AdminSettings.LogChannel.Key,
                AdminSettings.AdminRole.Key, AdminSettings.WatchRole.Key, AdminSettings.ReportRole.Key,
                AdminSettings.IgnoreChannel.Key, AdminSettings.IgnoreRole.Key, AdminSettings.AllowUser.Key,
            };

            Assert.Equal(keys.Length, keys.Distinct().Count());
            Assert.Equal(
                new[]
                {
                    "adminchannel", "adminrole", "allowuser", "ignorechannel", "ignorerole", "logchannel", "reportchannel", "reportrole",
                    "watchchannel", "watchrole",
                },
                keys.OrderBy(k => k));
        }

        private static ChannelSetting ChannelByKey(string key) =>
            new[] { AdminSettings.AdminChannel, AdminSettings.WatchChannel, AdminSettings.ReportChannel, AdminSettings.LogChannel }.Single(s => s.Key == key);

        private static RoleSetting RoleByKey(string key) =>
            new[] { AdminSettings.AdminRole, AdminSettings.WatchRole, AdminSettings.ReportRole }.Single(s => s.Key == key);

        private static ListSetting ListByKey(string key) =>
            new[] { AdminSettings.IgnoreChannel, AdminSettings.IgnoreRole, AdminSettings.AllowUser }.Single(s => s.Key == key);
    }
}

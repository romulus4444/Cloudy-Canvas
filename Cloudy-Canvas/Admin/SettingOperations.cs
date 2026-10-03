namespace Cloudy_Canvas.Admin
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Cloudy_Canvas.Settings;

    /// <summary>
    /// What the ";admin ..." commands do to a server's settings. These work on plain settings objects and already-resolved ids and return
    /// the reply to send, so they can be tested without Discord; the command module only resolves names, saves, replies and logs.
    /// </summary>
    public static class SettingOperations
    {
        // ---- a single channel -----------------------------------------------------------------------------------------

        public static AdminOutcome GetChannel(ChannelSetting setting, ServerSettings settings)
        {
            var id = setting.Get(settings);
            return AdminOutcome.Info(id > 0 ? Fill(setting.GetFormat, Mentions.Channel(id)) : setting.NotSet);
        }

        /// <param name="channelId">The channel the name resolved to, or 0 if it matched nothing.</param>
        /// <param name="typed">What the admin typed, for the error message.</param>
        public static AdminOutcome SetChannel(ChannelSetting setting, ServerSettings settings, ulong channelId, string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
            {
                return AdminOutcome.Failure("You must specify a channel.");
            }

            if (channelId == 0)
            {
                return AdminOutcome.Failure($"Invalid channel name #{typed}.");
            }

            setting.Set(settings, channelId);
            return AdminOutcome.Updated(Fill(setting.SetFormat, Mentions.Channel(channelId)));
        }

        /// <summary>Puts the setting back to the admin channel.</summary>
        public static AdminOutcome ClearChannel(ChannelSetting setting, ServerSettings settings)
        {
            setting.Set(settings, settings.AdminChannel);
            return AdminOutcome.Updated(Fill(setting.ClearFormat, Mentions.Channel(setting.Get(settings))));
        }

        // ---- a single role --------------------------------------------------------------------------------------------

        public static AdminOutcome GetRole(RoleSetting setting, ServerSettings settings)
        {
            var id = setting.Get(settings);
            return AdminOutcome.Info(id > 0 ? Fill(setting.GetFormat, Mentions.Role(id)) : setting.NotSet);
        }

        /// <param name="roleId">The role the name resolved to, or 0 if it matched nothing.</param>
        /// <param name="typed">What the admin typed, for the error message.</param>
        public static AdminOutcome SetRole(RoleSetting setting, ServerSettings settings, ulong roleId, string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
            {
                return AdminOutcome.Failure("You must specify a role.");
            }

            if (roleId == 0)
            {
                return AdminOutcome.Failure($"Invalid role name @{typed}.");
            }

            setting.Set(settings, roleId);
            return AdminOutcome.Updated(Fill(setting.SetFormat, Mentions.Role(roleId)));
        }

        /// <summary>Stops the setting pinging anyone.</summary>
        public static AdminOutcome ClearRole(RoleSetting setting, ServerSettings settings)
        {
            setting.Set(settings, 0);
            return AdminOutcome.Updated(setting.ClearMessage(settings));
        }

        // ---- a list of ids --------------------------------------------------------------------------------------------

        public static AdminOutcome GetList(ListSetting setting, ServerSettings settings)
        {
            var list = setting.List(settings);
            if (list.Count == 0)
            {
                return AdminOutcome.Info(setting.Empty);
            }

            var output = new StringBuilder(setting.Header).Append(Environment.NewLine);
            foreach (var id in list)
            {
                output.Append(setting.Mention(id)).Append(Environment.NewLine);
            }

            return AdminOutcome.Info(output.ToString());
        }

        /// <param name="id">The id the name resolved to, or 0 if it matched nothing.</param>
        public static AdminOutcome AddToList(ListSetting setting, ServerSettings settings, ulong id, string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
            {
                return AdminOutcome.Failure($"You must specify a {setting.Noun}.");
            }

            if (id == 0)
            {
                return AdminOutcome.Failure(Fill(setting.InvalidFormat, typed));
            }

            var list = setting.List(settings);
            if (list.Contains(id))
            {
                return AdminOutcome.Info($"{setting.Mention(id)} is already on the list.");
            }

            list.Add(id);
            return AdminOutcome.Updated(Fill(setting.AddedFormat, setting.Mention(id)));
        }

        /// <param name="id">The id the name resolved to, or 0 if it matched nothing.</param>
        public static AdminOutcome RemoveFromList(ListSetting setting, ServerSettings settings, ulong id, string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
            {
                return AdminOutcome.Failure($"You must specify a {setting.Noun}.");
            }

            if (id == 0)
            {
                return AdminOutcome.Failure(Fill(setting.InvalidFormat, typed));
            }

            return setting.List(settings).Remove(id)
                ? AdminOutcome.Updated(Fill(setting.RemovedFormat, setting.Mention(id)))
                : AdminOutcome.Info($"{setting.Mention(id)} was not on the list.");
        }

        public static AdminOutcome ClearList(ListSetting setting, ServerSettings settings)
        {
            setting.List(settings).Clear();
            return AdminOutcome.Updated(setting.Cleared);
        }

        // ---- channel-specific filters ---------------------------------------------------------------------------------

        public static AdminOutcome GetFilterChannels(ServerSettings settings)
        {
            if (settings.FilteredChannels.Count == 0)
            {
                return AdminOutcome.Info($"No channel-specific filters are currently set. All channels use the server filter {settings.DefaultFilterId}.");
            }

            var output = new StringBuilder($"__Channel-Specific Filter List:__{Environment.NewLine}")
                .Append(CultureInfo.InvariantCulture, $"(Any channel not listed here uses the server filter {settings.DefaultFilterId}){Environment.NewLine}");
            foreach (var (channel, filter) in settings.FilteredChannels)
            {
                output.Append(CultureInfo.InvariantCulture, $"<#{channel}>: Filter {filter}{Environment.NewLine}");
            }

            return AdminOutcome.Info(output.ToString());
        }

        /// <param name="filterId">A filter that has already been checked to exist and be public.</param>
        public static AdminOutcome AddFilterChannel(ServerSettings settings, ulong channelId, int filterId)
        {
            if (filterId == settings.DefaultFilterId)
            {
                return AdminOutcome.Info("That's the server default filter already.");
            }

            var existing = settings.FilteredChannels.FirstOrDefault(entry => entry.ChannelId == channelId);
            if (existing != null)
            {
                settings.FilteredChannels.Remove(existing);
                settings.FilteredChannels.Add(new ChannelFilter(channelId, filterId));
                return AdminOutcome.Updated($"Updated the filter for {Mentions.Channel(channelId)} from {existing.FilterId} to {filterId}.");
            }

            settings.FilteredChannels.Add(new ChannelFilter(channelId, filterId));
            return AdminOutcome.Updated($"Set {Mentions.Channel(channelId)} to use filter {filterId}.");
        }

        /// <param name="channelId">The channel the name resolved to, or 0 if it matched nothing.</param>
        public static AdminOutcome RemoveFilterChannel(ServerSettings settings, ulong channelId, string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
            {
                return AdminOutcome.Failure("You must specify a channel.");
            }

            if (channelId == 0)
            {
                return AdminOutcome.Failure($"Invalid channel name #{typed}.");
            }

            var existing = settings.FilteredChannels.FirstOrDefault(entry => entry.ChannelId == channelId);
            if (existing == null)
            {
                return AdminOutcome.Info($"{Mentions.Channel(channelId)} was not on the list.");
            }

            settings.FilteredChannels.Remove(existing);
            return AdminOutcome.Updated($"Removed {Mentions.Channel(channelId)} from channel-specific filter list.");
        }

        public static AdminOutcome ClearFilterChannels(ServerSettings settings)
        {
            settings.FilteredChannels.Clear();
            return AdminOutcome.Updated("Channel-specific filters cleared.");
        }

        private static string Fill(string format, object argument)
        {
            return string.Format(CultureInfo.InvariantCulture, format, argument);
        }
    }
}

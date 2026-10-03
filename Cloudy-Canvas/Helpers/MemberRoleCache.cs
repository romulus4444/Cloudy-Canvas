namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Remembers a member's roles for a short time so that a burst of commands costs one request to Discord, not one each.</summary>
    public sealed class MemberRoleCache
    {
        private const int PruneThreshold = 5000;

        private readonly TimeSpan _lifetime;
        private readonly object _gate = new();
        private readonly Dictionary<(ulong Guild, ulong User), (DateTime Expires, GuildMember Member)> _entries = new();

        public MemberRoleCache(TimeSpan lifetime)
        {
            _lifetime = lifetime;
        }

        public bool TryGet(ulong guildId, ulong userId, DateTime now, out GuildMember member)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue((guildId, userId), out var entry) && now < entry.Expires)
                {
                    member = entry.Member;
                    return true;
                }
            }

            member = null;
            return false;
        }

        public void Set(ulong guildId, ulong userId, DateTime now, GuildMember member)
        {
            lock (_gate)
            {
                _entries[(guildId, userId)] = (now + _lifetime, member);
                if (_entries.Count > PruneThreshold)
                {
                    foreach (var expired in _entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToList())
                    {
                        _entries.Remove(expired);
                    }
                }
            }
        }
    }
}

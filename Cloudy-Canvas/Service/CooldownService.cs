namespace Cloudy_Canvas.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>The outcome of a cooldown check.</summary>
    /// <param name="Allowed">Whether the command may run now.</param>
    /// <param name="RetryAfter">How long until the user may try again (zero when allowed).</param>
    /// <param name="ShouldNotify">True for the first refusal in a window, so the bot answers once instead of replying to every attempt.</param>
    public readonly record struct CooldownResult(bool Allowed, TimeSpan RetryAfter, bool ShouldNotify);

    /// <summary>Limits how often one user can run commands that cost a request to the booru.</summary>
    public class CooldownService
    {
        private const int PruneThreshold = 1000;

        private readonly IDateTimeService _clock;
        private readonly object _gate = new();
        private readonly Dictionary<ulong, Entry> _users = new();

        public CooldownService(IDateTimeService clock)
        {
            _clock = clock;
        }

        /// <summary>
        /// Starts the user's cooldown and returns Allowed if they were not already cooling down. A refused attempt does not extend it.
        /// </summary>
        public CooldownResult Check(ulong userId, TimeSpan cooldown)
        {
            var now = _clock.UtcNow();
            lock (_gate)
            {
                if (_users.TryGetValue(userId, out var entry) && now < entry.AllowedAt)
                {
                    var notify = !entry.Notified;
                    entry.Notified = true;
                    return new CooldownResult(false, entry.AllowedAt - now, notify);
                }

                _users[userId] = new Entry { AllowedAt = now + cooldown };
                if (_users.Count > PruneThreshold)
                {
                    foreach (var expired in _users.Where(pair => pair.Value.AllowedAt <= now).Select(pair => pair.Key).ToList())
                    {
                        _users.Remove(expired);
                    }
                }

                return new CooldownResult(true, TimeSpan.Zero, false);
            }
        }

        private sealed class Entry
        {
            public DateTime AllowedAt { get; init; }

            public bool Notified { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudy_Canvas.Modules
{
    using Cloudy_Canvas.Blacklist;
    using Discord.Commands;

    public class BlacklistModule : ModuleBase<SocketCommandContext>
    {
        private readonly Blacklist _blacklist;

        public BlacklistModule(Blacklist blacklist)
        {
            _blacklist = blacklist;
        }

        [Command("getblacklist")]
        [Summary("Gets the blacklist")]
        public async Task GetBlacklist()
        {
            var output = "The blacklist is currently empty.";
            var blacklist = _blacklist.GetList();
            foreach (var term in blacklist)
            {
                if (output == "The blacklist is currently empty.")
                {
                    output = term;
                }
                else
                {
                    output += $", {term}";
                }
            }

            await ReplyAsync($"__Blacklist Terms:__\n{output}");
        }
    }
}

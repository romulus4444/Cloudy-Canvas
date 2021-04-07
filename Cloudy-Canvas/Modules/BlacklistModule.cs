namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Discord.Commands;

    public class BlacklistModule : ModuleBase<SocketCommandContext>
    {
        private readonly Blacklist _blacklist;

        public BlacklistModule(Blacklist blacklist)
        {
            _blacklist = blacklist;
        }


        [Command("blacklist")]
        [Summary("Blacklist base command")]
        public async Task Blacklist(string arg = null, [Remainder]string term = null)
        {
            switch (arg)
            {
                case null:
                    await ReplyAsync("You must specify a subcommand.");
                    break;
                case "add":
                    var added = _blacklist.AddTerm(term);
                    if (added)
                    {
                        await ReplyAsync($"Added {term} to the blacklist.");
                    }
                    else
                    {
                        await ReplyAsync($"{term} is already on the blacklist.");
                    }
                    break;
                case "remove":
                    var removed = _blacklist.RemoveTerm(term);
                    if (removed)
                    {
                        await ReplyAsync($"Removed {term} from the blacklist.");
                    }
                    else
                    {
                        await ReplyAsync($"{term} was not on the blacklist.");
                    }
                    break;
                case "get":
                    var output = "The blacklist is currently empty.";
                    var blacklist = _blacklist.GetList();
                    foreach (var item in blacklist)
                    {
                        if (output == "The blacklist is currently empty.")
                        {
                            output = item;
                        }
                        else
                        {
                            output += $", {item}";
                        }
                    }

                    await ReplyAsync($"__Blacklist Terms:__\n{output}");
                    break;
                case "clear":
                    await ReplyAsync("Unimplemented clear command");
                    break;
                default:
                    await ReplyAsync("Invalid subcommand");
                    break;
            }
        }
    }
}

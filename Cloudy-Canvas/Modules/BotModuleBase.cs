namespace Cloudy_Canvas.Modules
{
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Base class for command modules. Replies never ping anyone unless the caller passes explicit allowed mentions,
    /// so user-supplied text echoed back (e.g. "@everyone") can't trigger notifications.
    /// </summary>
    public abstract class BotModuleBase : ModuleBase<SocketCommandContext>
    {
        protected override Task<IUserMessage> ReplyAsync(
            string message = null,
            bool isTTS = false,
            Embed embed = null,
            RequestOptions options = null,
            AllowedMentions allowedMentions = null,
            MessageReference messageReference = null,
            MessageComponent components = null,
            ISticker[] stickers = null,
            Embed[] embeds = null)
        {
            return base.ReplyAsync(message, isTTS, embed, options, allowedMentions ?? AllowedMentions.None, messageReference, components, stickers, embeds);
        }
    }
}

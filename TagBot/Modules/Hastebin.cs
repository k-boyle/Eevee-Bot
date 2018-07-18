using Discord.Commands;
using System.Threading.Tasks;
using TagBot.Handlers;
using TagBot.Services;

namespace TagBot.Modules
{
    public class Hastebin : ModuleBase<SocketCommandContext>
    {
        private readonly MessageService _message;
        private readonly HasteBinHandler _handler;

        public Hastebin(MessageService message, HasteBinHandler handler)
        {
            _message = message;
            _handler = handler;
        }

        [Command("haste", RunMode = RunMode.Async)]
        [Alias("h")]
        public async Task CreateHaste(ulong messageId)
        {
            var msg = Context.Channel.GetCachedMessage(messageId) ?? await Context.Channel.GetMessageAsync(messageId);
            if (msg is null)
            {
                await _message.SendMessageAsync(Context, "Message was not found");
                return;
            }

            var code = await _handler.GetCode(msg.Content);
            if (code is null)
            {
                await _message.SendMessageAsync(Context, "No code was not found :(");
                return;
            }

            await _message.SendMessageAsync(Context, await _handler.CreateHasteOrGist(code));
        }

        [Command("haste", RunMode = RunMode.Async)]
        [Alias("h")]
        public async Task CreateHaste([Remainder] string pastebin)
        {
            pastebin = pastebin.Length == 8 ? $"https://pastebin.com/{pastebin}" : pastebin;
            var code = await _handler.GetCode(pastebin);
            if (code is null)
            {
                await _message.SendMessageAsync(Context, "That wasn't a valid pastebin :(");
                return;
            }

            await _message.SendMessageAsync(Context, await _handler.CreateHasteOrGist(code));
        }
    }
}

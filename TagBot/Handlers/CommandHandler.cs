using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TagBot.Services;

namespace TagBot.Handlers
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly HasteBinHandler _handler;
        private readonly MessageService _message;
        private readonly CasinoQueue<ulong> _handledMessages = new CasinoQueue<ulong>(5);

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services, HasteBinHandler handler, MessageService message)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _handler = handler;
            _message = message;
        }

        public async Task InitiateAsync()
        {
            _client.MessageReceived += MessageReceived;
            _client.MessageUpdated += MessageUpdated;
            _client.ReactionAdded += ReactionAdded;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel.Id != 381889909113225237 && channel.Id != 443162366360682508) return;
            var emote = new Emoji("#⃣");
            if (emote.Name != reaction.Emote.Name) return;
            var msg = await message.GetOrDownloadAsync();
            if (msg is null) return;
            if (_handledMessages.Contains(msg.Id))
                return;
            var code = await _handler.GetCode(msg.Content);
            if (code is null) return;
            await channel.SendMessageAsync(await _handler.CreateHasteOrGist(code));
            _handledMessages.Enqueue(msg.Id);
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            await MessageReceived(arg2);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (arg is SocketUserMessage message)
            {
                if (message.Author.IsBot || message.Channel is SocketDMChannel) return;

                var context = new SocketCommandContext(_client, message);
                _message.SetCurrentMessage(message.Id);

                if (!context.Guild.CurrentUser.GetPermissions(context.Channel as SocketGuildChannel)
                    .SendMessages) return;

                var argPos = 0;
                if (context.Message.HasStringPrefix("ev?", ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                    {
                        switch (result.Error)
                        {
                            case CommandError.UnmetPrecondition:
                                await _message.SendMessageAsync(context, result.ErrorReason);
                                break;
                            default:
                                var channel = context.Client.GetChannel(443162366360682508) as SocketTextChannel;
                                await channel.SendMessageAsync($"{context.Guild.Name} | {context.Channel.Name} : {result.ErrorReason}");
                                break;
                        }
                    }
                }
            }
        }
    }
}

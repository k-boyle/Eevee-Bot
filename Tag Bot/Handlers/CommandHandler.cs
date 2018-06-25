using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
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

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitiateAsync()
        {
            _client.MessageReceived += MessageReceived;
            _client.MessageUpdated += MessageUpdated;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
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
                var messageService = _services.GetService<MessageService>();
                messageService.SetCurrentMessage(message.Id);

                if (!context.Guild.CurrentUser.GetPermissions(context.Channel as SocketGuildChannel)
                    .SendMessages) return;

                var argPos = 0;
                if (context.Message.HasStringPrefix("ev!", ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                    {
                        switch (result.Error)
                        {
                            case CommandError.UnmetPrecondition:
                                await messageService.SendMessageAsync(context, result.ErrorReason);
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

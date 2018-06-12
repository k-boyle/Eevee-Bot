using Discord.Commands;
using Discord.WebSocket;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using TagBot.Services;

namespace TagBot
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
                messageService.MessageReceived(message.Id);

                var argPos = 0;
                if (context.Message.HasStringPrefix("ev!", ref argPos))
                {
                    messageService.CommandReceived(message.Id);
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                    {
                        switch (result.Error)
                        {
                            case CommandError.UnmetPrecondition:
                                await context.Channel.SendMessageAsync(result.ErrorReason);
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

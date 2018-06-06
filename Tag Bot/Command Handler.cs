using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

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
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (arg is SocketUserMessage message)
            {
                if (message.Author.IsBot || message.Channel is SocketDMChannel) return;

                var context = new SocketCommandContext(_client, message);
                var argPos = 0;
                if (context.Message.HasStringPrefix("ev?", ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using TagBot.Handlers;
using TagBot.Services;

namespace TagBot
{
    internal class Program
    {
        private static IServiceProvider _services;

        private static async Task Main()
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 20
            });

            client.Log += LogMethod;
            client.JoinedGuild += NewGuild;

            var commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Verbose
            });

            commands.Log += LogMethod;

            _services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<ReliabilityService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<MessageService>()
                .AddSingleton<Func<LogMessage, Task>>(LogMethod)
                .BuildServiceProvider();

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("Tag Bot"));
            await client.StartAsync();

            client.Ready += () =>
            {
                _services.GetService<DatabaseService>().Initialise();
                return Task.CompletedTask;
            };

            var handler = new CommandHandler(client, commands, _services);
            await handler.InitiateAsync();

            await Task.Delay(-1);
        }
        private static async Task NewGuild(SocketGuild arg)
        {
            await _services.GetService<DatabaseService>().AddNewGuild(arg.Id);
        }

        private static Task LogMethod(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}

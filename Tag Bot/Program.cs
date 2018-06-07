using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using TagBot.Services;

namespace TagBot
{
    internal class Program
    {
        private static void Main()
            => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private async Task StartAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 20
            });

            _client.Log += LogMethod;
            _client.JoinedGuild += NewGuild;

            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Verbose
            });

            _commands.Log += LogMethod;

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<ReliabilityService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<MessageService>()
                .AddSingleton<Func<LogMessage, Task>>(LogMethod)
                .BuildServiceProvider();

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("Tag Bot"));
            await _client.StartAsync();

            _client.Ready += () => 
            {
                _services.GetService<DatabaseService>().Initialise();
                return Task.CompletedTask;
            };

            
            var handler = new CommandHandler(_client, _commands, _services);
            await handler.InitiateAsync();

            await Task.Delay(-1);
        }

        private async Task NewGuild(SocketGuild arg)
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

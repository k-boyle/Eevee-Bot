using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using TagBot.Handlers;
using TagBot.Services;
using Octokit;

namespace TagBot
{
    internal class Program
    {
        private static IServiceProvider _services;

        private static async Task Main()
        {
            var gitClient = new GitHubClient(new ProductHeaderValue("eevee-bot"));
            var tokenAuth = new Credentials(Environment.GetEnvironmentVariable("GitToken"));
            gitClient.Credentials = tokenAuth;

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
                .AddSingleton(gitClient)
                .AddSingleton<ReliabilityService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<MessageService>()
                .AddSingleton<RequestsService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<HasteBinHandler>()
                .AddSingleton<Func<LogMessage, Task>>(LogMethod)
                .BuildServiceProvider();

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TagBot"));
            await client.StartAsync();

            client.Ready += () =>
            {
                _services.GetService<DatabaseService>().Initialise();
                return Task.CompletedTask;
            };

            await _services.GetService<CommandHandler>().InitiateAsync();

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

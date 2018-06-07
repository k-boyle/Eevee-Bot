using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TagBot.Services
{
    public class ReliabilityService
    {
        // --- Begin Configuration Section ---
        // How long should we wait on the client to reconnect before resetting?
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        // Should we attempt to reset the client? Set this to false if your client is still locking up.
        private const bool AttemptReset = true;

        // Change log levels if desired:
        private const LogSeverity Debug = LogSeverity.Debug;
        private const LogSeverity Info = LogSeverity.Info;

        private const LogSeverity Critical = LogSeverity.Critical;

        // --- End Configuration Section ---

        private readonly DiscordSocketClient _discord;
        private readonly Func<LogMessage, Task> _logger;
        private CancellationTokenSource _cts;

        public ReliabilityService(DiscordSocketClient discord, Func<LogMessage, Task> logger = null)
        {
            _cts = new CancellationTokenSource();
            _discord = discord;
            _logger = logger ?? (_ => Task.CompletedTask);

            _discord.Connected += ConnectedAsync;
            _discord.Disconnected += DisconnectedAsync;
        }

        private Task ConnectedAsync()
        {
            // Cancel all previous state checks and reset the CancelToken - client is back online
            _ = DebugAsync("Client reconnected, resetting cancel tokens...");
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _ = DebugAsync("Client reconnected, cancel tokens reset.");

            return Task.CompletedTask;
        }

        private Task DisconnectedAsync(Exception e)
        {
            // Check the state after <timeout> to see if we reconnected
            _ = InfoAsync("Client disconnected, starting timeout task...");
            _ = Task.Delay(Timeout, _cts.Token).ContinueWith(async _ =>
            {
                await DebugAsync("Timeout expired, continuing to check client state...");
                await CheckStateAsync();
                await DebugAsync("State came back okay");
            });

            return Task.CompletedTask;
        }

        private async Task CheckStateAsync()
        {
            // Client reconnected, no need to reset
            if (_discord.ConnectionState == ConnectionState.Connected) return;
            if (AttemptReset)
            {
                await InfoAsync("Attempting to reset the client");

                var timeout = Task.Delay(Timeout);
                var connect = _discord.StartAsync();
                var task = await Task.WhenAny(timeout, connect);

                if (task == timeout)
                {
                    await CriticalAsync("Client reset timed out (task deadlocked?), killing process");
                    FailFast();
                }
                else if (connect.IsFaulted)
                {
                    await CriticalAsync("Client reset faulted, killing process", connect.Exception);
                    FailFast();
                }
                else if (connect.IsCompletedSuccessfully)
                    await InfoAsync("Client reset succesfully!");
                return;
            }

            await CriticalAsync("Client did not reconnect in time, killing process");
            FailFast();
        }

        private static void FailFast()
            => Environment.Exit(1);

        // Logging Helpers
        private const string LogSource = "Reliability";
        private Task DebugAsync(string message)
            => _logger.Invoke(new LogMessage(Debug, LogSource, message));
        private Task InfoAsync(string message)
            => _logger.Invoke(new LogMessage(Info, LogSource, message));
        private Task CriticalAsync(string message, Exception error = null)
            => _logger.Invoke(new LogMessage(Critical, LogSource, message, error));
    }
}

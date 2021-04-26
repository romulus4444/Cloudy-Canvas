namespace Cloudy_Canvas
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceCollection _services;
        private readonly DiscordSettings _settings;
        private readonly CommandService _commands;
        private DiscordSocketClient _client;

        public Worker(ILogger<Worker> logger, IServiceCollection services, IOptions<DiscordSettings> settings)
        {
            _logger = logger;
            _commands = new CommandService();
            _settings = settings.Value;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _client = new DiscordSocketClient();
                _client.Log += Log;
                await _client.LoginAsync(TokenType.Bot,
                    _settings.token);
                await _client.StartAsync();
                await _client.SetGameAsync("with my paintbrush");
                await InstallCommandsAsync();

                // Block this task until the program is closed.
                await Task.Delay(-1);


                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);
                }
            }
            finally
            {
                await _client.StopAsync();
            }
        }

        private async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services.BuildServiceProvider());
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            var argPos = 0;
            if (!(message.HasCharPrefix(DevSettings.prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)) /*|| message.Author.IsBot*/)
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(context, argPos, _services.BuildServiceProvider());
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogInformation(msg.Message);
            return Task.CompletedTask;
        }
    }
}

namespace Cloudy_Canvas
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Service;
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
        private readonly BooruService _booru;
        private DiscordSocketClient _client;
        private bool _ready;

        public Worker(ILogger<Worker> logger, IServiceCollection services, IOptions<DiscordSettings> settings, BooruService booru)
        {
            _logger = logger;
            _commands = new CommandService();
            _settings = settings.Value;
            _services = services;
            _booru = booru;
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
                await _booru.GetSpoilerTagsAsync();

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

        private Task ReadyAsync()
        {
            _ready = true;
            return Task.CompletedTask;
        }

        private async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services.BuildServiceProvider());
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;

            //// Only process commands in DMs
            //if (!(message?.Channel is SocketDMChannel))
            //{
            //    return;
            //}

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix(';', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot)
            {
                return;
            }

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(context, argPos, _services.BuildServiceProvider());
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogInformation(msg.Message);
            return Task.CompletedTask;
        }
    }
}

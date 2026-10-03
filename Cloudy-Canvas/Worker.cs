namespace Cloudy_Canvas
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _services;
        private readonly DiscordSettings _settings;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly AllPreloadedSettings _servers;

        public Worker(
            ILogger<Worker> logger,
            IHostApplicationLifetime lifetime,
            IServiceProvider services,
            IOptions<DiscordSettings> settings,
            DiscordSocketClient client,
            CommandService commands,
            AllPreloadedSettings servers)
        {
            _logger = logger;
            _lifetime = lifetime;
            _services = services;
            _settings = settings.Value;
            _client = client;
            _commands = commands;
            _servers = servers;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Disconnecting from Discord");
            await _client.StopAsync();
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The host only logs a failed background service and stops with exit code 0; record the failure so the process
                // exits non-zero and the service manager (systemd Restart=on-failure) can act on it.
                _logger.LogCritical(ex, "The Discord worker failed; shutting down");
                Environment.ExitCode = 1;
                _lifetime.StopApplication();
            }
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            // Discord.Net only warns about an empty token and then retries a doomed connection forever; fail fast instead
            // so the host exits with an error and the service manager can report/restart it.
            if (string.IsNullOrWhiteSpace(_settings.token))
            {
                throw new InvalidOperationException(
                    "No Discord bot token is configured. Set DiscordSettings:token (appsettings.json, user secrets or the DiscordSettings__token environment variable).");
            }

            _client.Log += Log;
            _commands.Log += Log;
            _commands.CommandExecuted += CommandExecutedAsync;
            _client.Ready += ReadyAsync;

            // Commands are installed before connecting so no message can arrive while the module list is still empty.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;

            await _client.LoginAsync(TokenType.Bot, _settings.token, true);
            await _client.StartAsync();
            await _client.SetGameAsync("https://cloudycanvas.art");

            // Run until the host asks us to stop.
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }

        // Commands run asynchronously (RunMode.Async), so ExecuteAsync returns before they finish. This is where their failures
        // arrive; CommandService has already logged any exception, so all that is left is to tell the user.
        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            var reply = CommandErrorReplies.For(result);
            if (reply == null)
            {
                return;
            }

            try
            {
                // A failure like a malformed command happens before the command's own checks run, so apply the ignore rules here too:
                // someone the server told the bot to ignore gets no reply at all, not even an error message.
                if (context is SocketCommandContext socketContext)
                {
                    var settings = await FileHelper.LoadServerSettingsAsync(socketContext);
                    if (!await DiscordHelper.CanUserRunCommandsAsync(socketContext, settings))
                    {
                        return;
                    }
                }

                await context.Channel.SendMessageAsync(reply, allowedMentions: AllowedMentions.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send the error reply for command {Command}", command.IsSpecified ? command.Value.Name : "(unknown)");
            }
        }

        private Task ReadyAsync()
        {
            _logger.LogInformation("Discord signals ready");
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (messageParam.Type == MessageType.ThreadCreated)
            {
                return;
            }

            if (messageParam is not SocketUserMessage message)
            {
                return;
            }

            var argPos = 0;
            var context = new SocketCommandContext(_client, message);

            // Look the server up without creating an entry: most messages are not commands and must not touch the settings file.
            // A server we haven't seen yet simply uses the defaults until it runs its first command.
            var serverId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            if (!_servers.Settings.TryGetValue(serverId, out var settings))
            {
                settings = new ServerPreloadedSettings();
            }

            var prefix = DiscordHelper.EffectivePrefix(settings.Prefix, _settings.PrefixOverride);
            if (message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                if (!(settings.ListenToBots) && message.Author.IsBot)
                {
                    return;
                }

                string parsedMessage;
                var checkCommands = true;
                if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    parsedMessage = "<mention>";
                    checkCommands = false;
                }
                else
                {
                    parsedMessage = DiscordHelper.ResolveAliases(message.Content, settings);
                }

                if (parsedMessage == "")
                {
                    parsedMessage = "<blank message>";
                    checkCommands = false;
                }

                if (checkCommands)
                {
                    var validCommand = false;
                    foreach (var command in _commands.Commands)
                    {
                        if (command.Name != parsedMessage.Split(" ")[0])
                        {
                            continue;
                        }

                        validCommand = true;
                        break;
                    }

                    if (!validCommand)
                    {
                        // Stay quiet on unknown commands so the bot doesn't answer every message that happens to start with the prefix
                        // (other bots on the server often share it).
                        return;
                    }
                }

                if (!_servers.Settings.ContainsKey(serverId))
                {
                    await FileHelper.LoadServerPresettingsAsync(context, _servers);
                }

                await _commands.ExecuteAsync(context, parsedMessage, _services);
            }
        }

        private Task Log(LogMessage msg)
        {
            var level = msg.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                _ => LogLevel.Trace,
            };

            _logger.Log(level, msg.Exception, "{Source}: {Message}", msg.Source, msg.Message);
            return Task.CompletedTask;
        }
    }
}

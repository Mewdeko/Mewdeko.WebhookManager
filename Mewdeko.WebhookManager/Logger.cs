
using Discord;
using Discord.Webhook;

namespace Mewdeko.WebhookManager;
internal class Logger : IDisposable
{
    public List<LogMessage> Messages { get; private set; }
    public string Source { get; private set; }
    public string? WebhookUrl
    {
        get => _webhookUrl;
        set
        {
            _webhookUrl = value;
            _client = new(value);
        }
    }
    public LogSeverity LogLevel { get; set; }
    public LogSeverity WebhookLevel { get; set; }
    public event Func<LogMessage, Task> LogFired;

    private string _webhookUrl { get; set; }
    private DiscordWebhookClient _client { get; set; }

    public Logger(string source, LogSeverity severity = LogSeverity.Info)
    {
        Source = source;
        Messages = new List<LogMessage>();
        LogLevel = severity;

        LogFired += FireWebhook;
        Log("Logger", "Started Logger", LogSeverity.Info);
    }

    public Logger(string source, string webhookUrl, LogSeverity severity = LogSeverity.Info,
        LogSeverity webhookSeverity = LogSeverity.Warning)
    {
        Source = source;
        WebhookUrl = webhookUrl;
        LogLevel = severity;
        WebhookLevel = webhookSeverity;
        Messages = new List<LogMessage>();

        LogFired += FireWebhook;
        Log("Logger", "Started Logger", LogSeverity.Info);
    }

    private async Task FireWebhook(LogMessage message)
    {
        if (_client is null)
            return;

        if (message.Severity > WebhookLevel)
            return;

        var builder = new EmbedBuilder()
            .WithTitle(message.Severity.ToString())
            .WithAuthor(message.Source)
            .WithDescription(message.Message)
            .WithCurrentTimestamp();
        if (message.Exception != null)
            builder.WithFooter(string.Concat(message.Exception?.ToString() ?? "[invalid exception]",
                "https://cdn.discordapp.com/emojis/842172192401915971.png?size=64"));

        await _client.SendMessageAsync(embeds: new [] { builder.Build() });
    }

    public void Log (LogMessage message)
    {
        Messages.Add(message);
        LogFired.Invoke(message);
    }

    public void Log (string source, string message, LogSeverity severity, Exception? exception = null)
    {
        LogMessage lMessage = new (severity, source, message, exception);
        Log(lMessage);
    }

    #region log methods
    public void LogDebug (string message, Exception? exception = null)
        => LogDebug(Source, message, exception);
    public void LogDebug (string source, string message, Exception? exception = null)
        => Log(source, message, LogSeverity.Debug, exception);

    public void LogVerbose (string message, Exception? exception = null)
        => LogVerbose(Source, message, exception);
    public void LogVerbose (string source, string message, Exception? exception = null)
        => Log(source, message, LogSeverity.Verbose, exception);

    public void LogInfo (string message, Exception? exception = null)
        => LogInfo(Source, message, exception);
    public void LogInfo (string source, string message, Exception? exception = null)
        => Log(source, message, LogSeverity.Info, exception);

    public void LogWarning (string message, Exception? exception = null)
        => LogWarning(Source, message, exception);
    public void LogWarning (string source, string message, Exception? exception = null)
        => Log(source, message, LogSeverity.Warning, exception);

    public void LogError (string message, Exception? exception = null)
        => LogError(Source, message, exception);
    public void LogError (string source, string message, Exception? exception = null)
        => Log(source, message, LogSeverity.Error, exception);

    public void LogCritical (string message, Exception? exception = null)
        => LogCritical(Source, message, exception);
    public void LogCritical (string source, string message, Exception? exception = null)
        => Log(source, message, LogSeverity.Critical, exception);
    #endregion

    public void Dispose()
    {
        Messages.Clear();
        _client.Dispose();
        LogFired = null;
        GC.SuppressFinalize(this);
    }
}

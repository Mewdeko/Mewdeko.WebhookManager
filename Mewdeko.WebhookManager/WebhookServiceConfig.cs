using Discord;

namespace Mewdeko.WebhookManager;

public class WebhookServiceConfig
{
    // Stuffs still cached, I just suck at naming.
    public WebhookCacheMode CacheMode { get; set; } = WebhookCacheMode.None;
    public LogSeverity LogSeverity { get; set; } =
#if DEBUG
        LogSeverity.Debug;
#else
        LogSeverity.Info;
#endif
    public bool CreateMissingWebhooks { get; set; } = true;
    public string AutomaticallyCreatedWebhookName { get; set; } = "a webhook??? PERRY THE WEBHOOK?????";
    public bool RequireApplicationOwnedWebhooks { get; set; } = false;
}

using Discord;
using Discord.Rest;
using Discord.Webhook;

namespace Mewdeko.WebhookManager;

/// <summary>
///     Represents data about a webhook that may have a stored client.
/// </summary>
public class WebhookClientData
{
    /// <summary>
    ///     Client is only set if the library has accessed it before.
    /// </summary>
    public DiscordWebhookClient? Client { get; set; }
    /// <summary>
    ///     Info should be stored for every webhook that cached before it was accessed.
    /// </summary>
    public IWebhook ClientInfo { get; set; }
}

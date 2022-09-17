namespace Mewdeko.WebhookManager;

[Flags]
public enum WebhookCacheMode
{
    None,
    CacheAllClients,
    CacheOnWebhookUpdate,
}

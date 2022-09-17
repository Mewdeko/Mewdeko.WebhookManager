using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using NonBlocking;

namespace Mewdeko.WebhookManager;

public class WebhookService
{
    /// <summary>
    ///     Initializes a new webhook service
    /// </summary>
    /// <param name="config">Config data used by the service</param>
    /// <param name="client">
    ///     the discord socket client to provide cache updates and serve as a base for some of the
    ///     libraries rest requests.
    /// </param>
    public WebhookService(WebhookServiceConfig config, BaseSocketClient client)
    {
        Config = config;
        _restClient = client.Rest;
        client.WebhooksUpdated += HandleWebhooksUpdated;
        _logger = new("WebhookService", Config.LogSeverity);
    }

    /// <summary>
    ///     Configures default properties for use in the webhook service.
    /// </summary>
    public WebhookServiceConfig Config { get; set; }

    /// <summary>
    ///     Basic logging for the library, actual messages to come soon™
    /// </summary>
    public event Func<LogMessage, Task> Log;
    private DiscordRestClient _restClient { get; set; }
    private Logger _logger { get; set; }
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, WebhookClientData>> _webhooks = new();

    /// <summary>
    ///     You can pass the webhooks updated events from additional socket clients to this method, make sure you are
    ///     not sending events twice as that increases the risk of your app being ratelimmited.
    /// </summary>
    /// <param name="guild">The guild triggering a webhook update</param>
    /// <param name="channel">The channel a webhook was updated in</param>
    public async Task HandleWebhooksUpdated(SocketGuild guild, SocketChannel channel)
    {
        _logger.LogDebug($"Received webhooksUpdate in {guild}[{guild.Id}]#{channel}[{channel.Id}]");
        await CacheGuild(guild);

        if (_webhooks!.GetValueOrDefault(guild.Id, null) is { } guildWebhooks)
        {
            if
            (
                guildWebhooks!.GetValueOrDefault(channel.Id, null) is {Client: null} channelWebhook &&
                Config.CacheMode.HasFlag(WebhookCacheMode.CacheOnWebhookUpdate)
            )
                channelWebhook.Client = new(channelWebhook.ClientInfo);
        }
    }

    private async Task CacheGuild(IGuild guild)
    {
        var hooks = (await guild.GetWebhooksAsync())
            .Where(x => !string.IsNullOrEmpty(x.Token))
            .DistinctBy(x => x.ChannelId);

        _webhooks.AddOrUpdate(guild.Id,
            _ => new(hooks.Select(x => new KeyValuePair<ulong, WebhookClientData>(x.ChannelId,
                new()
                {
                    ClientInfo = x,
                    Client = Config.CacheMode.HasFlag(WebhookCacheMode.CacheAllClients) ? new(x) : null
                }))),
            (_, _) => new(hooks.Select(x => new KeyValuePair<ulong, WebhookClientData>(x.ChannelId,
                new()
                {
                    ClientInfo = x,
                    Client = Config.CacheMode.HasFlag(WebhookCacheMode.CacheAllClients) ? new(x) : null
                }))));
    }

    private async Task<WebhookClientData> GetClientData(IGuildChannel channel, bool forceCacheClient)
    {
        WebhookClientData result = new();

        if (channel is not ITextChannel {Guild: var guild, Id: var channelId} tChannel)
            throw new NotSupportedException(
                "Webhook messages can only be sent in text channels, support for forum channels is coming in a future update.");

        ConcurrentDictionary<ulong, WebhookClientData> guildWebhooks;

        if (_webhooks!.GetValueOrDefault(guild.Id, null) is not { } tGuildWebhooks)
        {
            await CacheGuild(guild);
            guildWebhooks = _webhooks.GetValueOrDefault(guild.Id, new());
        }
        else
            guildWebhooks = tGuildWebhooks;

        if (guildWebhooks.ContainsKey(channelId))
            result = guildWebhooks[channelId];

        else if (!Config.CreateMissingWebhooks)
            throw new ArgumentException(
                $"No webhooks could be found in {channelId}, if you would like to create missing webhooks automatically enable {nameof(Config.CreateMissingWebhooks)} in your config.");
        else
        {
            await tChannel.CreateWebhookAsync(Config.AutomaticallyCreatedWebhookName);
            // TODO: this call should cause an automatic caching of the guilds, but we need to wait a half a second
            // TODO: to make sure completed. Would it be possible to broadcast an event after a cache update is successful
            // TODO: and wait for that???

            //⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
            //⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣾⣿⣿⣷⣦⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
            //⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⣿⣿⣿⣿⣿⣿⣦⡀⠒⢶⣄⠀⠀⠀⠀⠀⠀⠀
            //⠀⢰⣶⣷⣶⣶⣤⣄⠀⣠⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣾⣿⡆⠀⠀⠀⠀⠀⠀
            //⠀⢿⣿⣿⣿⣿⡟⢁⣄⠙⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠃⠀⠀⠀⠀⠀⠀
            //⠀⠘⣿⣿⣿⣿⣧⡈⠻⢷⣦⣄⡉⠛⠿⢿⣿⣿⣿⣿⣿⣿⣿⠀⠀⠀⠀⠀⠀⠀
            //⠀⠀⠈⠻⣿⣿⣿⣿⣶⣄⡈⠙⠻⢷⣶⣤⣄⣈⡉⠛⠛⠛⠃⢠⣀⣀⡀⠀⠀⠀
            //⠀⠀⠀⠀⠈⠙⠻⢿⣿⣿⣿⣿⣶⣦⣤⣍⣉⠙⠛⠛⠛⠿⠃⢸⣿⣿⣿⣷⡀⠀
            //⠀⠀⠀⠀⠀⠀⠀⠀⠈⠙⠻⠿⣿⣿⣿⣿⣿⣿⣿⣷⣶⣶⣾⣿⣿⣿⣿⣿⣧⠀
            //⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠙⠛⠻⠏⠀⠉⠻⢿⣿⣿⣿⣿⠿⠋⠀
            // Perry the dumbcodeapuss (emojicombos.com)
            Thread.Sleep(500);
            result = _webhooks.GetValueOrDefault(guild.Id)?.GetValueOrDefault(channelId) ??
                   throw new InvalidOperationException(
                       $"Failed to create a webhook in [{guild.Id}]#[{channelId}]. Make sure your bot has the correct permissions.");
        }

        if (forceCacheClient && result.Client is null)
            result.Client = new(result.ClientInfo);

        return result;
    }

    /// <summary>
    ///     Sends a message using a webhook of your choice
    /// </summary>
    /// <param name="channel">The channel to send the message to.</param>
    /// <param name="username">The webhooks username, defaults to 'Greetings from Mewdeko.Webhooks'</param>
    /// <param name="avatarUrl">The webhooks avatar, defaults to a photo of the Hexside School of Magic and Demonics</param>
    /// <param name="text">The text content of the message</param>
    /// <param name="isTTS">Should the message be forcibly read out using tts</param>
    /// <param name="embed">Appends a single embed to the embeds list</param>
    /// <param name="embeds">Controls what embeds, if any, should be on the message</param>
    /// <param name="mentions">Enables or disables mentions on specific pings within your message</param>
    /// <param name="components">Send components with your message, this is only supported on application webhooks</param>
    /// <param name="flags">Flags passed with the message</param>
    /// <param name="threadId">The id of a thread to post a message to</param>
    /// <param name="options">Discord.Net request options to the libraries webhook request</param>
    /// <returns>The id of the sent message</returns>
    public async Task<ulong> SendMessageAsync(IGuildChannel channel,
        string username = "Greetings from Mewdeko.Webhooks",
        string avatarUrl = "https://avatars.githubusercontent.com/u/93441824?s=200&v=4",
        string? text = null,
        bool isTTS = false,
        Embed? embed = null,
        IEnumerable<Embed>? embeds = null,
        AllowedMentions? mentions = null,
        MessageComponent? components = null,
        MessageFlags flags = MessageFlags.None,
        ulong? threadId = null,
        RequestOptions? options = null)
    {
        var client = (await GetClientData(channel, true)).Client;
        if (embed is not null)
            (embeds ?? new List<Embed>()).ToList().Add(embed);
        return await client!.SendMessageAsync(text, isTTS, embeds, username, avatarUrl, options, mentions, components, flags,
            threadId);
    }

    /// <summary>
    ///     Gets a webhook client
    /// </summary>
    /// <param name="channel">the channel to get the client from</param>
    /// <returns>the webhook client</returns>
    public async Task<DiscordWebhookClient> GetClient(IGuildChannel channel) =>
        (await GetClientData(channel, true)).Client!;
}

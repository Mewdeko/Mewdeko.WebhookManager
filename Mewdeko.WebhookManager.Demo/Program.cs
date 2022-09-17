using Discord;
using Discord.WebSocket;
using Mewdeko.WebhookManager;

DiscordSocketClient client = new(new() {LogLevel = LogSeverity.Debug, GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent});
await client.LoginAsync(TokenType.Bot, File.ReadAllText("token.txt"));
await client.StartAsync();
var service = new WebhookService(new(), client);
service.Log += x => Log(x, true);
client.Log += x => Log(x, false);

async Task Log(LogMessage msg, bool highlight)
{
    await Task.CompletedTask;
    if (highlight) Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine(msg);
    if(highlight) Console.ResetColor();
}

client.MessageReceived += async x =>
{
    switch (x.Content)
    {
        case "SECRET WORD IF YOU SEND THIS MESSAGE IT WILL MESS WITH MY DEBUGGING, REEEEEEEEEEEEEEEEE":
            Console.WriteLine($"too fast {service}");
            break;
        case "SAY HI YOU DUMBASS":
            for(var i = 0; i < 20; i++)
                await service.SendMessageAsync((x.Channel as IGuildChannel)!, text: "HI YOU DUMBASS");
            Console.WriteLine($"too fast {service}");
            break;
    }
};

await Task.Delay(-1);


using BlossomiShymae.Smolder;
using Microsoft.Extensions.Logging;

var client = new Client()
{
    Logger = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
        });
    }).CreateLogger<Client>(),
    MaxDepth = 2,
};

await client.DownloadDirectoryAsync("https://raw.communitydragon.org/13.9/game/data/characters/");

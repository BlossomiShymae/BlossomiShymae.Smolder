
using BlossomiShymae.Smolder;
using Microsoft.Extensions.Logging;

var client = new Client() { Logger = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
    });
}).CreateLogger<Client>() };

await client.DownloadDirectoryAsync("https://universe.communitydragon.org/events/2024/mgs-crepe/assets/");
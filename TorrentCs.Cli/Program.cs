using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TorrentCs;

var portOption = new Option<int>("--port", "-p")
{
    Description = "Port to listen on for incoming connections.",
    DefaultValueFactory = _ => 5000,
};

var outputOption = new Option<string>("--output", "-o")
{
    Description = "Directory to save downloaded files to.",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
};

var verboseOption = new Option<bool>("--verbose", "-v")
{
    Description = "Enable verbose (debug) logging.",
};

var upnpOption = new Option<bool>("--upnp")
{
    Description = "Forward the listen port on the router via UPnP / NAT-PMP.",
};

var inputArgument = new Argument<string>("input")
{
    Description = "Path to a .torrent file, or a magnet link.",
};

var rootCommand = new RootCommand("TorrentCs — a command-line BitTorrent client.");
rootCommand.Add(portOption);
rootCommand.Add(outputOption);
rootCommand.Add(verboseOption);
rootCommand.Add(upnpOption);
rootCommand.Add(inputArgument);

rootCommand.SetAction(async (parseResult, ct) =>
{
    int port = parseResult.GetValue(portOption);
    string output = parseResult.GetValue(outputOption)!;
    bool verbose = parseResult.GetValue(verboseOption);
    bool upnp = parseResult.GetValue(upnpOption);
    string input = parseResult.GetValue(inputArgument)!;

    return await RunAsync(input, output, port, verbose, upnp, ct);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task<int> RunAsync(
    string input, string output, int port, bool verbose, bool upnp, CancellationToken ct)
{
    MagnetLink? magnet = null;
    if (MagnetLink.IsMagnetLink(input))
    {
        if (!MagnetLink.TryParse(input, out magnet))
        {
            Console.Error.WriteLine($"Invalid magnet link: {input}");
            return 1;
        }
    }
    else if (!File.Exists(input))
    {
        Console.Error.WriteLine($"Torrent file not found: {input}");
        return 1;
    }

    Directory.CreateDirectory(output);

    var logLevel = verbose ? LogLevel.Debug : LogLevel.Information;

    var builder = TorrentClientBuilder.CreateDefaultBuilder()
        .UsePort(port)
        .ConfigureServices(services =>
            services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true)
                                      .SetMinimumLevel(logLevel)));
    if (upnp)
        builder.UsePortForwarding();

    using var client = builder.Build();

    var download = magnet is not null ? client.Add(magnet, output) : client.Add(input, output);

    Console.WriteLine($"Downloading '{download.Description.Name}' to '{output}'");
    Console.WriteLine($"Peer ID: {client.LocalPeerId}");

    download.Start();

    using var statusTimer = new Timer(_ => LogStatus(download), null,
        TimeSpan.Zero, TimeSpan.FromSeconds(1));

    try
    {
        await download.WaitForDownloadCompletionAsync();
        LogStatus(download);
        Console.WriteLine("Download completed.");
        return 0;
    }
    catch (OperationCanceledException)
    {
        download.Stop();
        Console.WriteLine("Cancelled.");
        return 130;
    }
}

static void LogStatus(TorrentDownload download)
{
    Console.WriteLine(
        $"[{download.State}] {download.Progress:P1} " +
        $"↓ {FormatRate(download.DownloadRate())} ↑ {FormatRate(download.UploadRate())}");
}

static string FormatRate(long bytesPerSecond) => bytesPerSecond switch
{
    >= 1024 * 1024 => $"{bytesPerSecond / (1024.0 * 1024.0):F1} MB/s",
    >= 1024 => $"{bytesPerSecond / 1024.0:F1} KB/s",
    _ => $"{bytesPerSecond} B/s",
};

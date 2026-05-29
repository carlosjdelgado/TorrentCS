# TorrentCs

A BitTorrent library for .NET, built on .NET 10 and compatible with .NET 6 and above.

TorrentCs is a modern revival of the excellent [torrentcore](https://github.com/SamuelFisher/torrentcore)
by Samuel Fisher. It keeps the same structure and design philosophy — a modular, dependency-injected
library where transport protocols, trackers, pipeline stages and piece-picking strategies are all
pluggable — while targeting current .NET runtimes.

> **Status:** work in progress. The core download/upload flow works against real trackers and peers
> (see the integration test), but several features are still missing — most notably DHT and peer
> exchange. See the [roadmap](#roadmap) below.

## Features

- Open single-file and multi-file `.torrent` files
- Download from and seed to peers over TCP
- Contact trackers over **HTTP/HTTPS** and **UDP**
- SHA-1 verification of every downloaded piece
- Choking / optimistic-unchoke with tit-for-tat upload slot management
- Fast resume — persists verified pieces to skip re-hashing on restart
- Optional UPnP / NAT-PMP port forwarding (via Mono.Nat)
- Periodic re-announce, `numwant` and `event` reporting
- Fully modular, dependency-injection based architecture

Implemented BitTorrent Enhancement Proposals (BEPs):

| BEP | Description |
| --- | --- |
| [BEP 3](https://www.bittorrent.org/beps/bep_0003.html) | The BitTorrent core protocol |
| [BEP 10](https://www.bittorrent.org/beps/bep_0010.html) | Extension protocol (extended handshake, extension registry) |
| [BEP 12](https://www.bittorrent.org/beps/bep_0012.html) | Multitracker metadata extension (`announce-list`) |
| [BEP 15](https://www.bittorrent.org/beps/bep_0015.html) | UDP tracker protocol |
| [BEP 20](https://www.bittorrent.org/beps/bep_0020.html) | Peer ID conventions (Azureus-style IDs, client identification) |
| [BEP 23](https://www.bittorrent.org/beps/bep_0023.html) | Compact peer lists in tracker responses |

## Getting started

Install the library (once published to NuGet) and download a torrent in a few lines:

```csharp
using TorrentCs;

using var client = TorrentClient.Create();

var download = client.Add("ubuntu.torrent", "/home/user/Downloads");
download.Start();

await download.WaitForDownloadCompletionAsync();
```

`TorrentClient.Create()` builds a client with sensible defaults: TCP transport, the BitTorrent
application protocol and the default download pipeline.

### Advanced configuration

For more control, use `TorrentClientBuilder`:

```csharp
using TorrentCs;

using var client = TorrentClientBuilder.CreateDefaultBuilder()
    .UsePort(6881)
    .Build();

var download = client.Add("ubuntu.torrent", "/home/user/Downloads");
download.Start();

// Inspect progress while downloading
Console.WriteLine($"{download.State} {download.Progress:P1} " +
                  $"↓ {download.DownloadRate()} B/s ↑ {download.UploadRate()} B/s");
```

## Extensibility

TorrentCs is designed to be extended. Most of its own behaviour is implemented through the same
extension points you can use:

- **Custom transport protocols** beyond TCP, via `ITransportProtocol`
- **Custom storage** (disk, memory, anything) via `IFileHandler`
- **Custom piece-picking** strategies via `IPiecePicker`
- **Pipeline stages** that run during a download, via `IPipelineStage`
- **Protocol modules** that hook into peer events and messages, via `IModule`

Components are wired together with `Microsoft.Extensions.DependencyInjection`, so you can register
your own implementations through `TorrentClientBuilder.ConfigureServices(...)`.

## Command-line client

The repository also includes `TorrentCs.Cli`, a small command-line client:

```
torrentcs <torrent-file> [options]

Arguments:
  <torrent-file>        Path to the .torrent file to download.

Options:
  -o, --output <dir>    Directory to save downloaded files to (default: current directory).
  -p, --port <port>     Port to listen on for incoming connections (default: 5000).
  -v, --verbose         Enable verbose (debug) logging.
      --upnp            Forward the listen port on the router via UPnP / NAT-PMP.
```

Example:

```bash
dotnet run --project TorrentCs.Cli -- ubuntu.torrent -o /home/user/Downloads -p 6881 -v
```

## Repository layout

```
TorrentCs/         The library (targets net6.0, net8.0 and net10.0)
TorrentCs.Cli/     Command-line client
TorrentCs.Tests/   xUnit test suite (unit + integration)
```

## Building and testing

Requires the .NET 10 SDK.

```bash
dotnet build TorrentCs.slnx
dotnet test  TorrentCs.Tests/TorrentCs.Tests.csproj
```

The test suite includes a deterministic end-to-end integration test that spins up a seeder and a
leecher on localhost, transfers a file over the real wire protocol, and verifies the downloaded
content matches the original by SHA-1 — no network access required.

## Roadmap

Planned features, in rough priority order:

1. [x] **Choking / optimistic-unchoke** algorithm and upload management
2. [x] **Resume support** — persist piece state to avoid re-hashing on restart
3. [x] **Route incoming connections** across multiple torrents by info-hash
4. [x] **UPnP port forwarding** — automatically open the listen port on the router
5. [x] **Extension protocol** ([BEP 10](https://www.bittorrent.org/beps/bep_0010.html)) — the foundation for most modern extensions
6. [ ] **Peer exchange / PEX** ([BEP 11](https://www.bittorrent.org/beps/bep_0011.html)) — discover peers from other peers
7. [ ] **Metadata exchange** ([BEP 9](https://www.bittorrent.org/beps/bep_0009.html)) — fetch the `info` dictionary from peers
8. [ ] **Magnet link support** — parse `magnet:` URIs (builds on BEP 9 + BEP 10)
9. [ ] **Fast extension** ([BEP 6](https://www.bittorrent.org/beps/bep_0006.html))
10. [ ] **DHT** for trackerless torrents ([BEP 5](https://www.bittorrent.org/beps/bep_0005.html))
11. [ ] **uTorrent Transport Protocol / uTP** ([BEP 29](https://www.bittorrent.org/beps/bep_0029.html))
12. [ ] **IPv6 trackers** ([BEP 7](https://www.bittorrent.org/beps/bep_0007.html))

## Credits

TorrentCs is based on [torrentcore](https://github.com/SamuelFisher/torrentcore) by Samuel Fisher,
and preserves its architecture and design.

## License

Licensed under the [GNU Lesser General Public License, version 3](LICENSE) (LGPL-3.0-only).

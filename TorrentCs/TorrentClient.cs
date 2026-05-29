using Microsoft.Extensions.Logging;
using TorrentCs.Application;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.Pipelines;
using TorrentCs.Data;
using TorrentCs.Engine;
using TorrentCs.Tracker;
using TorrentCs.TorrentParsers;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs;

public class TorrentClient : ITorrentClient
{
    private readonly ILogger<TorrentClient> _logger;
    private readonly IMainLoop _mainLoop;
    private readonly ITcpTransportProtocol _transport;
    private readonly ITrackerClientFactory _trackerClientFactory;
    private readonly IApplicationProtocolFactory _applicationProtocolFactory;
    private readonly IPipelineFactory _pipelineFactory;
    private readonly IResumeStore _resumeStore;
    private readonly Dictionary<Sha1Hash, TorrentDownload> _downloads = [];
    private readonly List<IApplicationProtocol> _protocols = [];
    private readonly List<ResumePersister> _resumePersisters = [];

    public TorrentClient(
        ILogger<TorrentClient> logger,
        IMainLoop mainLoop,
        ITcpTransportProtocol transport,
        ITrackerClientFactory trackerClientFactory,
        IApplicationProtocolFactory applicationProtocolFactory,
        PeerId localPeerId,
        IPipelineFactory pipelineFactory,
        IResumeStore resumeStore)
    {
        _logger = logger;
        _mainLoop = mainLoop;
        _transport = transport;
        _trackerClientFactory = trackerClientFactory;
        _applicationProtocolFactory = applicationProtocolFactory;
        LocalPeerId = localPeerId;
        _pipelineFactory = pipelineFactory;
        _resumeStore = resumeStore;

        _transport.AcceptConnectionHandler += OnIncomingConnection;
        _mainLoop.Start();
        _transport.Start();
    }

    public PeerId LocalPeerId { get; }

    public IReadOnlyCollection<TorrentDownload> Downloads => _downloads.Values;

    public static ITorrentClient Create() => TorrentClientBuilder.CreateDefaultBuilder().Build();

    public TorrentDownload Add(string torrentFilePath, string downloadDirectory)
    {
        using var fs = File.OpenRead(torrentFilePath);
        return Add(fs, downloadDirectory);
    }

    public TorrentDownload Add(Stream torrentStream, string downloadDirectory)
    {
        var metainfo = TorrentParser.ReadFromStream(torrentStream);
        return Add(metainfo, downloadDirectory);
    }

    public TorrentDownload Add(Metainfo metainfo, string downloadDirectory)
    {
        var fileHandler = new DiskFileHandler(downloadDirectory);
        var dataHandler = new BlockDataHandler(fileHandler, metainfo);

        var protocol = _applicationProtocolFactory.Create(metainfo, dataHandler);
        _protocols.Add(protocol);

        RestoreResume(metainfo, protocol, downloadDirectory);

        var tracker = new AggregatedTracker(_trackerClientFactory, metainfo.Trackers);
        var pipeline = _pipelineFactory.CreatePipeline(protocol);

        var runner = new PipelineRunner(protocol, tracker, pipeline, _mainLoop, LocalPeerId);
        var download = new TorrentDownload(runner, tracker);

        _downloads[metainfo.InfoHash] = download;
        _logger.LogInformation("Added torrent {Name} ({InfoHash})", metainfo.Name, metainfo.InfoHash);
        return download;
    }

    public void Dispose()
    {
        _transport.AcceptConnectionHandler -= OnIncomingConnection;
        foreach (var persister in _resumePersisters)
            persister.Dispose(); // flushes the final resume state to disk
        foreach (var download in _downloads.Values)
            download.Stop();
        _transport.Stop();
        _mainLoop.Stop();
        GC.SuppressFinalize(this);
    }

    private void RestoreResume(Metainfo metainfo, IApplicationProtocol protocol, string downloadDirectory)
    {
        var resume = _resumeStore.Load(downloadDirectory, metainfo.InfoHash, metainfo.Pieces.Count);
        if (resume is not null)
        {
            foreach (var index in resume.CompletedPieces)
                protocol.DataHandler.MarkPieceAsCompleted(metainfo.Pieces[index]);
            _logger.LogInformation("Restored {Count} pieces from resume data for {Name}",
                resume.CompletedPieces.Count, metainfo.Name);
        }

        // Subscribe after restoring so the restored pieces don't trigger a redundant first save.
        _resumePersisters.Add(new ResumePersister(_resumeStore, protocol.DataHandler, downloadDirectory));
    }

    private void OnIncomingConnection(AcceptConnectionEventArgs args)
    {
        args.Accept();

        // Route the incoming connection to a torrent. The application protocol completes the
        // server-side handshake and verifies the info-hash, dropping the peer on mismatch.
        // With a single active torrent this hands over directly; routing by info-hash across
        // multiple torrents is a future improvement.
        var protocol = _protocols.FirstOrDefault();
        protocol?.AcceptConnection(new AcceptPeerConnectionEventArgs(args.TransportStream));
    }
}

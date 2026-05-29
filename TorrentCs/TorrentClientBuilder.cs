using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TorrentCs.Application;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.BitTorrent.Pipelines;
using TorrentCs.Application.Pipelines;
using TorrentCs.Data;
using TorrentCs.Engine;
using TorrentCs.Extensions.ExtensionProtocol;
using TorrentCs.Extensions.PeerExchange;
using TorrentCs.Modularity;
using TorrentCs.Tracker;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs;

public class TorrentClientBuilder
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly PipelineBuilder _pipelineBuilder = new();
    private PeerId _peerId = PeerId.CreateNew();
    private int _port = 6881;

    public static TorrentClientBuilder CreateDefaultBuilder()
    {
        var builder = new TorrentClientBuilder();
        builder._services.AddLogging();
        builder._services.AddSingleton<IMainLoop, MainLoop>();
        builder.AddTcpTransportProtocol();
        builder.AddBitTorrentApplicationProtocol();
        builder.AddDefaultPipeline();
        return builder;
    }

    public TorrentClientBuilder UsePeerId(PeerId peerId)
    {
        _peerId = peerId;
        return this;
    }

    public TorrentClientBuilder UsePort(int port)
    {
        _port = port;
        return this;
    }

    public TorrentClientBuilder AddTcpTransportProtocol()
    {
        _services.AddSingleton<ITcpTransportProtocol, TcpTransportProtocol>();
        return this;
    }

    public TorrentClientBuilder AddBitTorrentApplicationProtocol()
    {
        _services.AddSingleton<IModule, CoreMessagingModule>();
        _services.AddSingleton<IModule, ExtensionProtocolModule>();
        _services.AddSingleton<IExtensionProtocolMessageHandler, PeerExchangeMessageHandler>();
        _services.AddSingleton<IPiecePicker, PiecePicker>();
        _services.AddSingleton<IApplicationProtocolFactory>(sp =>
            new ApplicationProtocolFactory(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetServices<IModule>(),
                _peerId));
        return this;
    }

    public TorrentClientBuilder AddDefaultPipeline()
    {
        _pipelineBuilder
            .AddStage<VerifyDownloadedPiecesStage>()
            .AddStage<DownloadPiecesStage>();
        return this;
    }

    /// <summary>
    /// Enables UPnP / NAT-PMP port forwarding so the listen port is opened on the home router.
    /// Disabled by default.
    /// </summary>
    public TorrentClientBuilder UsePortForwarding()
    {
        _services.AddSingleton<IPortForwarding, MonoNatPortForwarding>();
        return this;
    }

    public TorrentClientBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public ITorrentClient Build()
    {
        _services.AddSingleton(new LocalTcpConnectionOptions
        {
            Port = _port,
            PublicAddress = System.Net.IPAddress.Any,
            BindAddress = System.Net.IPAddress.Any,
        });
        // TryAdd so callers can override these via ConfigureServices (e.g. tests).
        _services.TryAddSingleton<ITrackerClientFactory>(sp =>
            new TrackerClientFactory(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<LocalTcpConnectionOptions>()));
        _services.TryAddSingleton<IResumeStore, FileResumeStore>();
        _services.TryAddSingleton<IPortForwarding, NullPortForwarding>(); // off unless UsePortForwarding()

        var serviceProvider = _services.BuildServiceProvider();
        var pipelineFactory = _pipelineBuilder.Build(serviceProvider);

        return ActivatorUtilities.CreateInstance<TorrentClient>(
            serviceProvider, _peerId, pipelineFactory);
    }
}

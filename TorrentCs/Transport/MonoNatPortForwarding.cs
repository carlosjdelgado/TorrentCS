using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace TorrentCs.Transport;

/// <summary>
/// Forwards the listen port using UPnP and NAT-PMP via Mono.Nat. Router discovery runs in the
/// background; mappings are created as devices are found and removed on dispose.
/// </summary>
public sealed class MonoNatPortForwarding : IPortForwarding
{
    private readonly ILogger<MonoNatPortForwarding> _logger;
    private readonly object _lock = new();
    private readonly List<(INatDevice Device, Mapping Mapping)> _mappings = [];
    private int? _port;
    private bool _discovering;

    public MonoNatPortForwarding(ILogger<MonoNatPortForwarding> logger) => _logger = logger;

    public void MapPort(int port)
    {
        lock (_lock)
        {
            _port = port;
            if (_discovering) return;
            _discovering = true;
            NatUtility.DeviceFound += OnDeviceFound;
            NatUtility.StartDiscovery();
        }
        _logger.LogDebug("Started UPnP/NAT-PMP discovery to forward port {Port}", port);
    }

    public void Dispose()
    {
        List<(INatDevice Device, Mapping Mapping)> toRemove;
        lock (_lock)
        {
            if (_discovering)
            {
                NatUtility.DeviceFound -= OnDeviceFound;
                NatUtility.StopDiscovery();
                _discovering = false;
            }
            toRemove = [.. _mappings];
            _mappings.Clear();
        }

        foreach (var (device, mapping) in toRemove)
            _ = SafeDeleteAsync(device, mapping);
    }

    private async void OnDeviceFound(object? sender, DeviceEventArgs e)
    {
        int port;
        lock (_lock)
        {
            if (_port is not int p) return;
            port = p;
        }

        try
        {
            var mapping = new Mapping(Protocol.Tcp, port, port);
            await e.Device.CreatePortMapAsync(mapping);
            lock (_lock) _mappings.Add((e.Device, mapping));
            _logger.LogInformation("Forwarded port {Port} via {Protocol}", port, e.Device.NatProtocol);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to forward port {Port} via {Protocol}", port, e.Device.NatProtocol);
        }
    }

    private async Task SafeDeleteAsync(INatDevice device, Mapping mapping)
    {
        try
        {
            await device.DeletePortMapAsync(mapping);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to remove port mapping");
        }
    }
}

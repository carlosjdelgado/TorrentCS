namespace TorrentCs.Tests.Client;

/// <summary>
/// Agrupa las pruebas que crean un TorrentClient real (enlazan puertos TCP) en una
/// única colección para que xUnit no las ejecute en paralelo y colisionen en el puerto.
/// </summary>
[CollectionDefinition("TorrentClient")]
public class ClientTestCollection { }

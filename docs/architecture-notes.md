# Architecture notes & future work

Technical notes, known limitations, and improvements worth making that are **not** user-facing
features (those live in the roadmap in the [README](../README.md)). This is the place for internal
design decisions and technical debt.

## Concurrency model

**Current state.** Each connected peer runs its own message-receive loop
(`BitTorrentPeer.ReceiveMessagesAsync`) on its own task/thread, and the application protocol is
registered as the message handler directly. That means message processing happens concurrently
across peers. The shared state touched during processing is guarded with locks:

- `BlockRequestManager` — `lock` around the requested/downloaded sets; getters return snapshots.
- `PieceCheckerHandler` — `lock` around the pending-blocks map and completed-pieces set; events are
  raised outside the lock.
- `BlockDataHandler` — `lock` around seek+read/write on the shared file streams.

This is correct, but the locking is spread across several types and is easy to get wrong as the code
grows.

**Proposed improvement — serialise processing on the main loop.** The original design (and the
cleaner approach) is to do *all* message processing on a single thread, the `MainLoop`. We already
have the pieces:

- `IMainLoop` / `MainLoop` — a single-threaded task queue.
- `QueueingMessageHandler` — wraps a message handler and posts the work onto the main loop.

The missing step is that `BitTorrentPeer.ReceiveMessagesAsync` currently hands the network
`BinaryReader` straight to the handler. To defer processing onto the main loop we must first read the
whole message payload into a buffer on the peer's thread, then queue processing of that buffer:

1. On the peer thread: read the 4-byte length, the 1-byte id, and the full payload into a `byte[]`.
2. Hand `(id, payload)` to the handler.
3. `QueueingMessageHandler` posts the processing onto the `MainLoop`.
4. Modules run on the single main-loop thread — no shared-state races, so the locks above can go.

This also fixes message framing (below) for free, and removes the per-type locking.

## Message framing (stream vs buffer)

Message processing reads directly from the peer's network stream via the shared `BinaryReader`. Each
incoming message id is consumed by exactly one module (`CoreMessagingModule` handles 0–8,
`ExtensionProtocolModule` handles id 20). If a peer sends a message id we don't handle (e.g. id 9,
DHT port, or a Fast-extension id), **no module consumes its payload and the stream desynchronises**,
corrupting every message after it.

Today this is safe in practice because we only advertise BEP 10, so peers don't send us those ids.
The robust fix is to read the full payload into a buffer (see the concurrency improvement above) and
parse from the buffer, so an unrecognised id simply leaves an unread buffer instead of desyncing the
connection.

## Known limitations

- **`FindAvailablePort` is not implemented.** `TorrentClientSettings` has the flag, but the TCP
  transport always binds the configured port. If it is already in use the client throws
  `SocketException(98)` from the constructor. The CLI should either pick a free port or fail
  gracefully; for now pass a free port with `-p`.
- **No endgame mode.** When a download is almost complete, requesting the last few blocks from
  multiple peers avoids stalling on a slow peer. Not implemented.
- **No selective/file-priority downloading.** All files in a multi-file torrent are downloaded; there
  is no way to pick a subset or set priorities.
- **Rate limiting is not exposed.** `RateLimiter` / `RateLimitedStream` exist and are wired into the
  transport, but there is no public API to configure upload/download speed caps.

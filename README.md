# SharpTorrent

**SharpTorrent** is a crossplatform torrent client written in **C#**, implementing the [BitTorrent Protocol Specification v1 (BEP 0003)](https://www.bittorrent.org/beps/bep_0003.html).

---

## Key Features

- ✅ **Supports both IPv4 and IPv6**
- ✅ **UDP tracker support**
- ✅ **Handles multiple peer connections**
- ✅ **Download state recovery** after interruptions
- ✅ Fully handles **Bitfield, Request, Piece, Choke/Unchoke** messages

---

## Architecture Overview

- P2P: Manages TCP peer connections, message parsing, bitfield exchange
- Tracker: Parses .torrent` files and communicates with UDP trackers
- Disk: Manages file writing, piece verification, and download state

---

## Requirements

- .NET 9.0+
- Compatible with Windows, Linux, and macOS

---

## 🧪 Features in Progress

- ⏳ Support for **Magnet Links**
- ⏳ **DHT (Distributed Hash Table)** support
- ⏳ Message encryption (BEP 6 / BEP 10)

---

## ▶️ Quick Start

```bash
git clone https://github.com/LeonardoKaftal/SharpTorrent.git
cd SharpTorrent
cd SharpTorrent
dotnet build
dotnet run -- path/to/file.torrent path/to/download/folder
```


## ▶️ VIDEO
https://file.garden/aHvuZ7j9PThoD4Wy/video%20torrent.webm

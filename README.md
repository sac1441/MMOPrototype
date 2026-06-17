# MMO Prototype — Unity 3D (C#)

A server-authoritative multiplayer prototype built in **Unity 2022.3 LTS** using **Netcode for GameObjects**, demonstrating core MMO engineering concepts relevant to production online games.

> Built as a portfolio project to showcase understanding of real-time multiplayer architecture, server authority, and MMO systems design.

---

## 🏗 Architecture

\\\
Client                              Server
──────                              ──────
GatherInput()
    │
    ▼
SimulateLocally()  ──ServerRpc──▶  ValidateInput()
(client prediction)                 AuthoritativeSimulate()
    │                                   │
stateBuffer[]      ◀──ClientRpc──  BroadcastState()
    │
Reconcile() — correct mispredictions silently
\\\

### Why server-authoritative?
In a real MMO, clients are never trusted. The server owns all game state — positions, health, combat results. Clients predict locally to feel responsive, but the server always wins. This prevents cheating and keeps all clients in sync.

---

## ✅ Systems Implemented

| System | Details |
|---|---|
| **Network Transport** | Unity Transport (UDP) via Netcode for GameObjects |
| **Player Spawning** | Server spawns a NetworkObject prefab per connecting client |
| **Movement Sync** | NetworkTransform for remote players; CharacterController for local |
| **Server Authority** | Server validates all input via ServerRpc |
| **Client Prediction** | Local player simulates movement instantly, reconciles on server correction |
| **NetworkVariables** | Health, Mana, Level synced server to all clients automatically |
| **Combat System** | Attack via ServerRpc, damage applied server-side, broadcast via ClientRpc |
| **Death and Respawn** | Player death triggers respawn at random spawn point |
| **Player Interpolation** | Remote players lerp between network updates — no jitter |
| **Nameplates** | World-space UI above each player showing name and live HP bar |
| **Kill Feed** | Top-right overlay logs kills in real time |
| **HUD** | Bottom-left HP/MP/Level overlay for local player |
| **Tick Rate** | 20Hz server tick loop |

---

## 📁 Project Structure

\\\
Assets/
├── Scripts/
│   ├── Network/
│   │   ├── MMONetworkManager.cs   — Bootstrap: host/client startup, transport config
│   │   └── PlayerSpawner.cs       — Spawns/despawns players on connect/disconnect
│   ├── Player/
│   │   └── PlayerController.cs    — WASD input, client prediction, reconciliation
│   ├── Stats/
│   │   └── PlayerStats.cs         — NetworkVariables for HP, MP, Level
│   └── Combat/
│       └── CombatSystem.cs        — ServerRpc attack validation, damage, death, respawn
├── Prefabs/
│   └── Player.prefab              — NetworkObject + NetworkTransform + all scripts
└── Scenes/
    └── GameScene.unity            — Ground plane, NetworkManager, lighting
\\\

---

## 🔑 Key Engineering Concepts

### Client-Side Prediction
The local player's CharacterController moves immediately on input — no waiting for a server round-trip. This gives instant, responsive feel even with latency. Each input is stamped and stored in a buffer.

### Server Reconciliation
When the server sends back its authoritative position, the client checks it against the buffered prediction. If they diverge beyond a threshold, the client silently snaps to the server position and replays inputs.

### NetworkVariables
PlayerStats.cs uses NGO's NetworkVariable<float> for health, mana, and level. These are server-write, broadcast-to-all — the server is always the source of truth, and any client that joins late gets the current value immediately.

### ServerRpc / ClientRpc
All game-changing actions go through [ServerRpc] — only the server can modify state. Results are broadcast back via [ClientRpc] so all clients update simultaneously.

---

## 🚀 How to Run Locally

### Prerequisites
- Unity 2022.3 LTS
- ParrelSync (https://github.com/VeriorPies/ParrelSync)

### Packages Required
Install via Window → Package Manager → Add by name:
\\\
com.unity.netcode.gameobjects
com.unity.multiplayer.tools
\\\

### Steps
1. Clone this repo and open in Unity 2022.3 LTS
2. Install the packages above
3. Open Assets/Scenes/GameScene.unity
4. Install ParrelSync and open a clone window
5. Press Play in the main window → click Start Host
6. Press Play in the clone window → click Start Client
7. WASD to move, F to attack

---

## 🛠 Tech Stack

| Tool | Version | Role |
|---|---|---|
| Unity | 2022.3 LTS | Game engine |
| Netcode for GameObjects | 1.x | Multiplayer framework |
| Unity Transport | 2.x | UDP transport layer |
| C# | 9.0 | All gameplay and network code |
| ParrelSync | latest | Local multi-client testing |

---

## 🗺 Roadmap

- [ ] Phase 3 — Full input buffer + server reconciliation loop
- [ ] Phase 4 — Zone/area-of-interest culling (only sync nearby players)
- [ ] Phase 5 — Inventory system with NetworkList
- [ ] Phase 6 — Dedicated headless server build
- [ ] Phase 7 — Docker-containerised server deployment

---

## 👤 Author

Built by @sac1441 as an MMO engineering portfolio project.

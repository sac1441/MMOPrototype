# MMO Prototype — Unity 3D (C#)

A server-authoritative multiplayer prototype built in Unity 2022.3 LTS using Netcode for GameObjects.

## What it does
- Multiple players connect and see each other move in real time
- Server-authoritative movement with client-side prediction
- Synced player stats (HP, MP, Level) using NetworkVariables
- Combat system with death and respawn
- Player nameplates and kill feed HUD

## Tech Stack
- Unity 2022.3 LTS
- Netcode for GameObjects
- Unity Transport (UDP)
- C#

## How to Run
1. Open project in Unity 2022.3 LTS
2. Install packages: com.unity.netcode.gameobjects
3. Open Assets/Scenes/GameScene.unity
4. Install ParrelSync for two-client local testing
5. Press Play → Start Host in main window
6. Press Play → Start Client in clone window
7. WASD to move, F to attack

## Project Structure
- Assets/Scripts/Network/ — connection, player spawning
- Assets/Scripts/Player/ — movement, client prediction
- Assets/Scripts/Stats/  — HP, MP, Level sync
- Assets/Scripts/Combat/ — attack, damage, respawn

## Author
[@sac1441](https://github.com/sac1441)

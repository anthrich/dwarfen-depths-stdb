# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DwarfenDepths is a multiplayer networked game built with **Unity 6000.1.13f1** and **SpacetimeDB** as the backend. The game uses client-side prediction with server reconciliation for real-time multiplayer gameplay.

## Build & Test Commands

### Server
```bash
# Build the server
dotnet build DwarfenDepthsServer/DwarfenDepthsServer.sln

# Run unit tests (SharedPhysics tests via xUnit)
dotnet test DwarfenDepthsServer/UnitTests/UnitTests.csproj

# Run a single test
dotnet test DwarfenDepthsServer/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~TestMethodName"
```

### Client
The client is built through the Unity Editor (open the project in Unity 6000.1.13f1). No CLI build pipeline exists.

## Architecture

### Client-Server Model
The game uses a **server-authoritative** model with **client-side prediction**. Both client and server run the same deterministic physics engine (`SharedPhysics/`), so the client can predict movement locally and reconcile when the server state arrives.

### SharedPhysics (shared between client and server)
`Assets/Scripts/SharedPhysics/` is the deterministic physics engine compiled for both Unity (.NET 4.7.1) and the server (.NET 8.0 wasi-wasm). The server project links to this same source code at `DwarfenDepthsServer/SharedPhysics/`. Changes here affect both client and server — always run `dotnet test` after modifying.

Key classes: `Engine.cs` (simulation + collision), `Entity.cs`, `Line.cs` (collision walls), `Vector2.cs`.

### Client Prediction & Reconciliation (`Simulation.cs`)
- Runs physics at the server tick rate, accumulating delta time
- Caches simulation states in a 1024-entry ring buffer
- On server update, compares cached state to server state; if mismatch detected (distance > 0.001), rewinds to server state and replays all subsequent inputs
- Caps input queue to 12 ahead-of-simulation inputs

### SpacetimeDB Integration
- Server logic lives in `DwarfenDepthsServer/StdbModule/` with **Reducers** (server RPCs) and **Tables** (database schema)
- Auto-generated client bindings are in `Assets/server-types/` — do not edit these manually
- `GameManager.cs` orchestrates the connection, subscribes to table changes via SQL queries, and routes entity/player events to the appropriate systems
- Server endpoints: Local (`http://127.0.0.1:3000`), Maincloud (`https://maincloud.spacetimedb.com`)

### Key Patterns
- **Pub/Sub**: `IPublisher<T>` / `ISubscriber<T>` for loose coupling (e.g., `Simulation` publishes entity updates)
- **Singletons**: `GameManager.Instance`, `Simulation.Instance`, `PrefabManager.Instance`
- **Event-driven networking**: SpacetimeDB table callbacks (`OnInsert`, `OnUpdate`, `OnDelete`) drive entity lifecycle

### Naming Conventions
- Private fields: `_camelCase` with underscore prefix
- Public properties/methods: `PascalCase`
- Events: `On` prefix (`OnConnected`, `OnEntityUpdated`)

### Server Tick Loop (`MoveAllEntitiesReducer.cs`)
A scheduled reducer processes all entity movement each tick: reads queued `PlayerInput` rows for the current sequence ID, applies them to entities, runs `Engine.Simulate`, updates entity rows, and increments the sequence ID.

### Adaptive Time Scaling (`NetworkTime.cs`)
The client adjusts `Time.timeScale` based on how far ahead/behind it is relative to the server's simulation offset, keeping client prediction synchronized.

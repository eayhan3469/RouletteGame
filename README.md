# RouletteGame

Deterministic Roulette case project built in Unity 6000 with a modular, SOLID-oriented architecture.

## Current Architecture

The project is organized to keep responsibilities separated and to avoid a `GameContext` or any manager becoming a God Object.

```text
Assets/Scripts
├── Core
│   ├── Persistence
│   │   ├── PlayerData.cs
│   │   └── SaveLoadManager.cs
│   └── StateMachine
│       ├── GameContext.cs
│       ├── GameStateBase.cs
│       ├── IGameState.cs
│       ├── StateMachine.cs
│       └── States
│           ├── InitializeState.cs
│           ├── BettingState.cs
│           ├── SpinningState.cs
│           ├── ResolutionState.cs
│           └── ResultState.cs
├── Gameplay
│   └── RoundFlow
├── Betting
├── Roulette
├── UI
├── Presentation
├── Persistence
├── Data
│   ├── Runtime
│   └── SaveData
└── Infrastructure
```

## Folder Responsibilities

- `Core`: Current working foundation. The standalone `StateMachine`, scene `GameContext`, active states, and current persistence classes live here.
- `Gameplay`: Round and game flow orchestration. As the project grows, roulette round states can be promoted here from `Core`.
- `Betting`: Bet definitions, bet placement, chip selection, bet validation, and payout input models.
- `Roulette`: Wheel numbers, table rules, deterministic spin requests, and roulette-mode specific rule logic.
- `UI`: Unity UI presenters, views, panels, buttons, and screen bindings.
- `Presentation`: VFX, SFX triggers, animations, highlights, and other player-facing feedback layers.
- `Persistence`: Planned home for save/load services, serializers, and future resume-session logic.
- `Data/SaveData`: Planned home for serializable persisted models such as player statistics and settings.
- `Data/Runtime`: Non-persistent runtime session models such as active bets, current round snapshot, and pending spin requests.
- `Infrastructure`: Bootstrap, installers, scene loading, input adapters, and other framework-facing utilities if needed later.

## Why This Fits The Case

- The case naturally maps to a round-based flow: `Initialize -> Betting -> Spinning -> Result -> Betting`.
- Deterministic roulette needs a clean separation between round flow, betting rules, and wheel outcome selection.
- Save/load is optional in the brief, but player statistics persistence is required, so `Persistence` and `Data/SaveData` are already split out.
- Visual polish is important in the case, so `Presentation` stays separate from core gameplay logic.

## Next Recommended Steps

1. Add `RoundSessionData` under `Data/Runtime` for active bets, selected deterministic outcome, and latest spin result.
2. Create `BetDefinition`, `BetSlip`, and `BetManager` under `Betting`.
3. Create roulette rule models under `Roulette` for European/American layout support.
4. Add UI panels for deterministic outcome selection, betting controls, and statistics display under `UI`.
5. Move persistence and round state scripts into their planned folders after Unity regenerates project files cleanly.
6. Expand persistence to support optional resume state if the scope allows it.

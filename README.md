# RouletteGame

Deterministic Roulette prototype built with Unity 6000 and C# for the Joker Games Unity case.

## Gameplay

- Start from the main menu and choose European or American mode.
- Drag 3D chips from the tray onto the roulette table betting spots.
- Right-click a placed chip to return it to the tray.
- Open the deterministic number panel and select the next result, or choose Random.
- Press Spin to play the round.
- The wheel and ball animate toward the selected result, then payouts are calculated and the next betting round begins.

## Implemented Case Features

- Deterministic outcome selection with a UI number pad.
- Random outcome fallback when no number is selected.
- 3D roulette wheel, table, ball, chips, and drag-and-drop chip placement.
- Inside bets: Straight, Split, Street, Corner, Six Line.
- Outside bets: Red/Black, Even/Odd, High/Low, Dozens, Columns.
- Payout table via `RoulettePayoutSO`.
- Multi-round flow: Initialize -> Betting -> Spinning -> Result -> Betting.
- Player statistics: total spins, total wins, total wagered, total won, and net profit.
- JSON persistence for balance, statistics, roulette type, active bets, and pending spin target.
- Audio feedback for wheel, ball, chip placement, and round result.
- Visual highlighting for covered numbers while hovering bet spots.

## Current Limitations

- American roulette UI mode exists, but the table/wheel content is still effectively European; double-zero needs full table and wheel integration.
- There is no celebratory particle/VFX pass yet.
- A demo video link still needs to be added before final delivery.
- Automated gameplay tests are not included; verification is currently build/manual play based.

## Architecture

The project is organized by gameplay responsibility:

```text
Assets/Scripts
|-- Betting
|   |-- BetManager.cs
|   |-- RoulettePayoutSO.cs
|   |-- Chips
|   `-- Spots
|-- Core
|   `-- StateMachine
|-- Data
|   `-- SaveData
|-- Gameplay
|   `-- RoundFlow/States
|-- Persistence
|-- Presentation
|-- Roulette
`-- UI
```

## Design Patterns

- State: `StateMachine`, `InitializeState`, `BettingState`, `SpinningState`, and `ResultState` isolate round phases.
- Observer/Event: UI controllers, wheel events, and bet total changes communicate through C# events.
- ScriptableObject data: payout rules and roulette number color data are configured outside gameplay code.
- Composition root: `GameContext` owns scene references and wires the active flow without embedding betting or wheel logic.
- Model/View separation: `PlayerData` stores persisted state while UI controllers only present and trigger actions.

## Build Notes

- Unity version target: 6000.0.x.
- No third-party gameplay plugins or tween libraries are used.
- Main scene: `Assets/Scenes/SCE_Game.unity`.

## Verification

Last checked locally:

```powershell
dotnet build RouletteGame.slnx
```

Result: build succeeded with 0 warnings and 0 errors.

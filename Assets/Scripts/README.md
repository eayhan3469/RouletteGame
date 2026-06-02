## Scripts Structure

This folder is split by gameplay responsibility so the deterministic roulette case stays modular.

### Active Areas

- `Betting`: Bet spots, active bet tracking, chip behaviour, tray distribution, and payout configuration.
- `Core/StateMachine`: Shared state machine infrastructure and the scene-level `GameContext`.
- `Data/SaveData`: Serializable player save model.
- `Gameplay/RoundFlow/States`: Concrete round phases.
- `Persistence`: JSON save/load utility.
- `Presentation`: Audio and player-facing feedback controllers.
- `Roulette`: Wheel animation, wheel slot data, and number color catalog.
- `UI`: Main menu, betting panel, deterministic number selection, result display, and statistics UI.

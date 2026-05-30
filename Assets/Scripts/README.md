## Scripts Structure

This folder is intentionally split by responsibility so the deterministic roulette case can grow without centralizing too much behavior in one place.

### Active Core

- `Core/StateMachine`: Contains the standalone state machine, `GameContext`, and current round lifecycle states.
- `Core/Persistence`: Contains the active save/load utility and the current serializable player save model.

### Planned Expansion

- `Gameplay/RoundFlow`: Future home for round orchestration classes when the project grows beyond the current bootstrap state.
- `Betting`: Bet models, chip values, bet validation, and wager coordination.
- `Roulette`: Table layout data, roulette numbers, and deterministic outcome selection models.
- `UI`: Unity UI views and presenters.
- `Presentation`: Wheel animation hooks, highlights, sounds, and feedback orchestration.
- `Data/Runtime`: Transient round/session data.
- `Data/SaveData`: Future home for persisted data models if persistence is separated out from `Core`.
- `Persistence`: Future home for persistence services if the bootstrap layer gets split further.
- `Infrastructure`: Future bootstrapping and adapters if needed.

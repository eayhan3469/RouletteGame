using UnityEngine;

/// <summary>
/// Shared base class for concrete roulette states.
/// Keeps context and state machine references together while avoiding duplicated logging code.
/// </summary>
public abstract class GameStateBase : IGameState
{
    protected GameContext Context { get; }
    protected StateMachine StateMachine { get; }

    protected GameStateBase(GameContext context, StateMachine stateMachine)
    {
        Context = context;
        StateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Tick();
    public abstract void Exit();

    protected void LogLifecycle(string phase)
    {
#if UNITY_EDITOR
        // In the editor, include the full context path for easier debugging.
        Debug.Log($"{GetType().Name} {phase} on {Context.name}");
#endif
    }
}

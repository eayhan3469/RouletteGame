using UnityEngine;

/// <summary>
/// Standalone state machine responsible only for holding the current state,
/// updating it, and handling state transitions.
/// </summary>
public sealed class StateMachine
{
    private IGameState _currentState;

    public IGameState CurrentState => _currentState;

    public void Tick()
    {
        _currentState?.Tick();
    }

    public void ChangeState(IGameState newState)
    {
        if (newState == null)
        {
            Debug.LogError("Cannot change to a null game state.");
            return;
        }

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
}

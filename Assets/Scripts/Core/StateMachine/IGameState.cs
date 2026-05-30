/// <summary>
/// Defines the lifecycle contract for all roulette game states.
/// </summary>
public interface IGameState
{
    void Enter();
    void Tick();
    void Exit();
}

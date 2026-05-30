/// <summary>
/// Entry state for initializing the roulette flow and future shared systems.
/// </summary>
public sealed class InitializeState : GameStateBase
{
    public InitializeState(GameContext context, StateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override void Enter()
    {
        LogLifecycle("Enter");
    }

    public override void Tick()
    {
        LogLifecycle("Tick");
    }

    public override void Exit()
    {
        LogLifecycle("Exit");
    }
}

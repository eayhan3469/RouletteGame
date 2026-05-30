/// <summary>
/// Placeholder state for resolving results and payouts.
/// </summary>
public sealed class ResolutionState : GameStateBase
{
    public ResolutionState(GameContext context, StateMachine stateMachine)
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

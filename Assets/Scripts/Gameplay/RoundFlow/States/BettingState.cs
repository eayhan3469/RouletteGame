/// <summary>
/// Placeholder state for the future betting phase.
/// </summary>
public sealed class BettingState : GameStateBase
{
    public BettingState(GameContext context, StateMachine stateMachine)
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

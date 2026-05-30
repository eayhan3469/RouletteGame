/// <summary>
/// Placeholder state for the future spinning phase.
/// </summary>
public sealed class SpinningState : GameStateBase
{
    public SpinningState(GameContext context, StateMachine stateMachine)
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

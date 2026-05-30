/// <summary>
/// Placeholder state for presenting the round result.
/// </summary>
public sealed class ResultState : GameStateBase
{
    public ResultState(GameContext context, StateMachine stateMachine)
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

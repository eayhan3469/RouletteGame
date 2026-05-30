/// <summary>
/// Placeholder state for resolving results and payouts.
/// </summary>
public sealed class ResolutionState : GameStateBase
{
    private readonly int _winningNumber;

    public ResolutionState(GameContext context, StateMachine stateMachine)
        : this(context, stateMachine, -1)
    {
    }

    public ResolutionState(GameContext context, StateMachine stateMachine, int winningNumber)
        : base(context, stateMachine)
    {
        _winningNumber = winningNumber;
    }

    public override void Enter()
    {
        LogLifecycle($"Enter - Winning Number: {_winningNumber}");
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        LogLifecycle("Exit");
    }
}

/// <summary>
/// Drives the deterministic wheel spin and advances once the ball lands.
/// </summary>
public sealed class SpinningState : GameStateBase
{
    private readonly int _targetNumber;
    private bool _isActive;

    public SpinningState(GameContext context, StateMachine stateMachine)
        : this(context, stateMachine, -1)
    {
    }

    public SpinningState(GameContext context, StateMachine stateMachine, int targetNumber)
        : base(context, stateMachine)
    {
        _targetNumber = targetNumber;
    }

    public override void Enter()
    {
        LogLifecycle($"Enter - Target Number: {_targetNumber}");

        _isActive = true;

        if (Context.WheelController == null)
        {
            UnityEngine.Debug.LogWarning("SpinningState could not spin because WheelController is missing.");
            StateMachine.ChangeState(new ResolutionState(Context, StateMachine, _targetNumber));
            return;
        }

        Context.WheelController.SpinToNumber(_targetNumber, HandleSpinCompleted);
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        _isActive = false;
        LogLifecycle("Exit");
    }

    private void HandleSpinCompleted()
    {
        if (!_isActive)
        {
            return;
        }

        StateMachine.ChangeState(new ResolutionState(Context, StateMachine, _targetNumber));
    }
}

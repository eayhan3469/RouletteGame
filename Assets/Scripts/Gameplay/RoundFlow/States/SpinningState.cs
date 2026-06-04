/// <summary>
/// Drives the deterministic wheel spin and advances once the ball lands.
/// </summary>
public sealed class SpinningState : GameStateBase
{
    private readonly int _targetNumber;
    private WheelController _wheelController;
    private bool _isActive;

    public SpinningState(GameContext context, StateMachine stateMachine, int targetNumber)
        : base(context, stateMachine)
    {
        _targetNumber = targetNumber;
    }

    public override void Enter()
    {
        LogLifecycle($"Enter - Target Number: {_targetNumber}");

        Context.SetChipInteractionEnabled(false);
        _isActive = true;
        Context.SavePendingSpinBets(_targetNumber);
        _wheelController = Context.WheelController;

        if (_wheelController == null)
        {
            UnityEngine.Debug.LogWarning("SpinningState could not spin because WheelController is missing.");
            StateMachine.ChangeState(new ResultState(Context, StateMachine, _targetNumber));
            return;
        }

        _wheelController.BallReleased -= HandleBallReleased;
        _wheelController.BallReleased += HandleBallReleased;
        _wheelController.BallPocketEntryStarted -= HandleBallPocketEntryStarted;
        _wheelController.BallPocketEntryStarted += HandleBallPocketEntryStarted;
        _wheelController.BallPocketBounced -= HandleBallPocketBounced;
        _wheelController.BallPocketBounced += HandleBallPocketBounced;
        _wheelController.BallPocketLanded -= HandleBallPocketLanded;
        _wheelController.BallPocketLanded += HandleBallPocketLanded;
        _wheelController.SpinToNumber(_targetNumber, HandleSpinCompleted);
    }

    public override void Tick()
    {
        UpdateBallLoopAudio();
    }

    public override void Exit()
    {
        if (_wheelController != null)
        {
            _wheelController.BallReleased -= HandleBallReleased;
            _wheelController.BallPocketEntryStarted -= HandleBallPocketEntryStarted;
            _wheelController.BallPocketBounced -= HandleBallPocketBounced;
            _wheelController.BallPocketLanded -= HandleBallPocketLanded;
        }

        Context.AudioFeedbackController?.StopBallTravelLoop();
        _wheelController = null;
        _isActive = false;
        LogLifecycle("Exit");
    }

    private void HandleSpinCompleted()
    {
        if (!_isActive)
        {
            return;
        }

        StateMachine.ChangeState(new ResultState(Context, StateMachine, _targetNumber));
    }

    private void HandleBallReleased()
    {
        Context.AudioFeedbackController?.PlayBallReleaseThenTravelLoop();
    }

    private void HandleBallPocketEntryStarted()
    {
        Context.AudioFeedbackController?.StopBallTravelLoop();
    }

    private void HandleBallPocketBounced(float bounceIntensity)
    {
        Context.AudioFeedbackController?.PlayBallPocketBounce(bounceIntensity);
    }

    private void HandleBallPocketLanded()
    {
        Context.AudioFeedbackController?.PlayBallPocketLand();
    }

    private void UpdateBallLoopAudio()
    {
        if (_wheelController == null || Context.AudioFeedbackController == null)
        {
            return;
        }

        float maxBallSpeed = _wheelController.MaxBallDegreesPerSecond;

        if (maxBallSpeed <= 0f)
        {
            Context.AudioFeedbackController.SetBallTravelIntensity(0f);
            return;
        }

        float normalizedIntensity = UnityEngine.Mathf.Clamp01(
            _wheelController.CurrentBallDegreesPerSecond / maxBallSpeed);
        Context.AudioFeedbackController.SetBallTravelIntensity(normalizedIntensity);
    }
}

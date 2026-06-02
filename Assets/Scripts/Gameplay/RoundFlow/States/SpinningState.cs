/// <summary>
/// Drives the deterministic wheel spin and advances once the ball lands.
/// </summary>
public sealed class SpinningState : GameStateBase
{
    private readonly int _targetNumber;
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

        if (Context.WheelController == null)
        {
            UnityEngine.Debug.LogWarning("SpinningState could not spin because WheelController is missing.");
            StateMachine.ChangeState(new ResultState(Context, StateMachine, _targetNumber));
            return;
        }

        Context.WheelController.BallReleased -= HandleBallReleased;
        Context.WheelController.BallReleased += HandleBallReleased;
        Context.WheelController.BallPocketEntryStarted -= HandleBallPocketEntryStarted;
        Context.WheelController.BallPocketEntryStarted += HandleBallPocketEntryStarted;
        Context.WheelController.BallPocketBounced -= HandleBallPocketBounced;
        Context.WheelController.BallPocketBounced += HandleBallPocketBounced;
        Context.WheelController.BallPocketLanded -= HandleBallPocketLanded;
        Context.WheelController.BallPocketLanded += HandleBallPocketLanded;
        Context.WheelController.SpinToNumber(_targetNumber, HandleSpinCompleted);
    }

    public override void Tick()
    {
        UpdateBallLoopAudio();
    }

    public override void Exit()
    {
        if (Context.WheelController != null)
        {
            Context.WheelController.BallReleased -= HandleBallReleased;
            Context.WheelController.BallPocketEntryStarted -= HandleBallPocketEntryStarted;
            Context.WheelController.BallPocketBounced -= HandleBallPocketBounced;
            Context.WheelController.BallPocketLanded -= HandleBallPocketLanded;
        }

        Context.AudioFeedbackController?.StopBallTravelLoop();
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
        //Context.AudioFeedbackController?.StopBallTravelLoop();
        Context.AudioFeedbackController?.PlayBallPocketLand();
    }

    private void UpdateBallLoopAudio()
    {
        if (Context.WheelController == null || Context.AudioFeedbackController == null)
        {
            return;
        }

        float maxBallSpeed = Context.WheelController.MaxBallDegreesPerSecond;

        if (maxBallSpeed <= 0f)
        {
            Context.AudioFeedbackController.SetBallTravelIntensity(0f);
            return;
        }

        float normalizedIntensity = UnityEngine.Mathf.Clamp01(
            Context.WheelController.CurrentBallDegreesPerSecond / maxBallSpeed);
        Context.AudioFeedbackController.SetBallTravelIntensity(normalizedIntensity);
    }
}

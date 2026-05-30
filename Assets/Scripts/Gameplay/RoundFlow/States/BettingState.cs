/// <summary>
/// Betting phase setup that shows the betting UI, spawns player chips,
/// and waits for the deterministic spin trigger.
/// </summary>
public sealed class BettingState : GameStateBase
{
    private const float InitialTotalBet = 0f;

    public BettingState(GameContext context, StateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override void Enter()
    {
        LogLifecycle("Enter");
        SubscribeToBetManager();
        ShowBettingUi();
        SpawnPlayerChips();
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        UnsubscribeFromBettingUi();
        UnsubscribeFromBetManager();
        Context.SetBettingUiVisible(false);
        LogLifecycle("Exit");
    }

    private void ShowBettingUi()
    {
        if (Context.BettingUIController == null)
        {
            UnityEngine.Debug.LogWarning("BettingState could not show the betting UI because BettingUIController is missing.");
            return;
        }

        Context.SetBettingUiVisible(true);
        Context.BettingUIController.Initialize(
            Context.PlayerData != null && Context.PlayerData.IsEuropeanRoulette,
            Context.PlayerData != null ? Context.PlayerData.Balance : 0f,
            Context.BetManager != null ? Context.BetManager.TotalBet : InitialTotalBet);
        Context.BettingUIController.OnSpinTriggered -= HandleSpinTriggered;
        Context.BettingUIController.OnSpinTriggered += HandleSpinTriggered;
    }

    private void UnsubscribeFromBettingUi()
    {
        if (Context.BettingUIController != null)
        {
            Context.BettingUIController.OnSpinTriggered -= HandleSpinTriggered;
        }
    }

    private void SubscribeToBetManager()
    {
        if (Context.BetManager == null)
        {
            UnityEngine.Debug.LogWarning("BettingState could not subscribe to total bet changes because BetManager is missing.");
            return;
        }

        Context.BetManager.TotalBetChanged -= HandleTotalBetChanged;
        Context.BetManager.TotalBetChanged += HandleTotalBetChanged;
    }

    private void UnsubscribeFromBetManager()
    {
        if (Context.BetManager != null)
        {
            Context.BetManager.TotalBetChanged -= HandleTotalBetChanged;
        }
    }

    private void HandleTotalBetChanged(float totalBet)
    {
        if (Context.BettingUIController == null)
        {
            return;
        }

        Context.BettingUIController.UpdateTotalBetText(totalBet);
    }

    private void SpawnPlayerChips()
    {
        if (Context.PlayerData == null)
        {
            UnityEngine.Debug.LogError("BettingState could not spawn chips because PlayerData is missing.");
            return;
        }

        if (Context.ChipManager == null)
        {
            UnityEngine.Debug.LogError("BettingState could not spawn chips because ChipManager is missing.");
            return;
        }

        if (!Context.PlayerData.IsEuropeanRoulette)
        {
            UnityEngine.Debug.LogWarning("American roulette is not implemented yet. Using the current European tray setup.");
        }

        int chipBalance = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.FloorToInt(Context.PlayerData.Balance));
        Context.ChipManager.DistributeBalanceToChips(chipBalance);
    }

    private void HandleSpinTriggered(int targetNumber)
    {
        if (Context.PlayerData == null)
        {
            UnityEngine.Debug.LogError("BettingState could not start spinning because PlayerData is missing.");
            return;
        }

        int resolvedTargetNumber = ResolveTargetNumber(targetNumber, Context.PlayerData.IsEuropeanRoulette);
        StateMachine.ChangeState(new SpinningState(Context, StateMachine, resolvedTargetNumber));
    }

    private int ResolveTargetNumber(int targetNumber, bool isEuropeanRoulette)
    {
        if (targetNumber >= 0)
        {
            return targetNumber;
        }

        int exclusiveMax = isEuropeanRoulette ? 37 : 38;
        return UnityEngine.Random.Range(0, exclusiveMax);
    }
}

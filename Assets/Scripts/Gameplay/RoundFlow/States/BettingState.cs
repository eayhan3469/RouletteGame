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
        Context.SetChipInteractionEnabled(false);
        ResetTrayChipsFromBalance();
        RestoreSavedBetsIfNeeded();
        LogLifecycle("Enter");

        if (TryResumePendingSpin())
        {
            return;
        }

        SubscribeToBetManager();
        ShowBettingUi();
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        Context.SetChipInteractionEnabled(false);
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
        Context.SetChipInteractionEnabled(true);
        Context.BettingUIController.Initialize(
            Context.PlayerData != null && Context.PlayerData.IsEuropeanRoulette,
            Context.PlayerData != null ? Context.PlayerData.Balance : 0f,
            Context.BetManager != null ? Context.BetManager.TotalBet : InitialTotalBet);
        Context.StatisticsUIController?.RefreshStats(Context.PlayerData);
        Context.BettingUIController.OnSpinTriggered -= HandleSpinTriggered;
        Context.BettingUIController.OnSpinTriggered += HandleSpinTriggered;
        Context.BettingUIController.OnMenuTriggered -= HandleMenuTriggered;
        Context.BettingUIController.OnMenuTriggered += HandleMenuTriggered;
    }

    private void UnsubscribeFromBettingUi()
    {
        if (Context.BettingUIController != null)
        {
            Context.BettingUIController.OnSpinTriggered -= HandleSpinTriggered;
            Context.BettingUIController.OnMenuTriggered -= HandleMenuTriggered;
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

    private void ResetTrayChipsFromBalance()
    {
        if (Context.PlayerData == null)
        {
            UnityEngine.Debug.LogError("BettingState could not reset tray chips because PlayerData is missing.");
            return;
        }

        if (Context.ChipManager == null)
        {
            UnityEngine.Debug.LogError("BettingState could not reset tray chips because ChipManager is missing.");
            return;
        }

        Context.ChipManager.ClearTrayChips();

        if (!Context.PlayerData.IsEuropeanRoulette)
        {
            UnityEngine.Debug.LogWarning("American roulette is not implemented yet. Using the current European tray setup.");
        }

        int chipBalance = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.FloorToInt(Context.PlayerData.Balance));
        Context.ChipManager.DistributeBalanceToChips(chipBalance);
    }

    private void RestoreSavedBetsIfNeeded()
    {
        if (Context.PlayerData == null || Context.BetManager == null || Context.ChipManager == null)
        {
            return;
        }

        if (Context.BetManager.ActiveBets.Count > 0)
        {
            return;
        }

        if (Context.PlayerData.SavedBets == null || Context.PlayerData.SavedBets.Count == 0)
        {
            return;
        }

        Context.BetManager.RestoreSavedBets(Context.PlayerData.SavedBets, Context.ChipManager);
    }

    private bool TryResumePendingSpin()
    {
        if (Context.PlayerData == null)
        {
            return false;
        }

        if (Context.PlayerData.SavedRoundPhase != PlayerData.RoundPhase.Spinning)
        {
            return false;
        }

        int pendingTargetNumber = Context.PlayerData.PendingSpinTargetNumber;

        if (!IsValidTargetNumber(pendingTargetNumber, Context.PlayerData.IsEuropeanRoulette))
        {
            Context.ClearPendingSpinBets();
            SaveLoadManager.Save(Context.PlayerData);
            return false;
        }

        StateMachine.ChangeState(new SpinningState(Context, StateMachine, pendingTargetNumber));
        return true;
    }

    private void HandleSpinTriggered(int targetNumber)
    {
        if (Chip3D.HasActiveDrag)
        {
            return;
        }

        if (Context.PlayerData == null)
        {
            UnityEngine.Debug.LogError("BettingState could not start spinning because PlayerData is missing.");
            return;
        }

        int resolvedTargetNumber = ResolveTargetNumber(targetNumber, Context.PlayerData.IsEuropeanRoulette);
        StateMachine.ChangeState(new SpinningState(Context, StateMachine, resolvedTargetNumber));
    }

    private void HandleMenuTriggered()
    {
        Context.SaveCurrentBettingState();
        StateMachine.ChangeState(new InitializeState(Context, StateMachine));
    }

    private int ResolveTargetNumber(int targetNumber, bool isEuropeanRoulette)
    {
        if (IsValidTargetNumber(targetNumber, isEuropeanRoulette))
        {
            return targetNumber;
        }

        int exclusiveMax = isEuropeanRoulette ? 37 : 38;
        return UnityEngine.Random.Range(0, exclusiveMax);
    }

    private bool IsValidTargetNumber(int targetNumber, bool isEuropeanRoulette)
    {
        int exclusiveMax = isEuropeanRoulette ? 37 : 38;
        return targetNumber >= 0 && targetNumber < exclusiveMax;
    }
}

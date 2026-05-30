/// <summary>
/// First playable betting phase setup.
/// For now it prepares the European table tray by spawning the player's balance as stacked chips.
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
        SpawnPlayerChips();
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        LogLifecycle("Exit");
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
}

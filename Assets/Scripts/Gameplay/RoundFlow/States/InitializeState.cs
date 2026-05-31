using System.IO;

/// <summary>
/// Loads persisted player data, opens the main menu, and transitions into the
/// betting flow once the player confirms their roulette type selection.
/// </summary>
public sealed class InitializeState : GameStateBase
{
    private const float FallbackStartingBalance = 1000f;

    public InitializeState(GameContext context, StateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override void Enter()
    {
        LogLifecycle("Enter");

        bool hasExistingSave = File.Exists(SaveLoadManager.SavePath);
        PlayerData playerData = SaveLoadManager.Load();

        if (!hasExistingSave)
        {
            playerData = CreateDefaultPlayerData();
        }

        Context.SetPlayerData(playerData);

        if (TryResumeSavedRound())
        {
            return;
        }

        if (Context.MainMenuController != null)
        {
            Context.MainMenuController.SetSelectedRouletteType(Context.PlayerData.IsEuropeanRoulette);
            Context.MainMenuController.PlayButtonClicked += HandlePlayButtonClicked;
        }
        else
        {
            UnityEngine.Debug.LogWarning("InitializeState could not subscribe because MainMenuController is missing.");
        }

        Context.SetMainMenuVisible(true);
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        if (Context.MainMenuController != null)
        {
            Context.MainMenuController.PlayButtonClicked -= HandlePlayButtonClicked;
        }

        Context.SetMainMenuVisible(false);
        LogLifecycle("Exit");
    }

    private void HandlePlayButtonClicked(bool isEuropeanRoulette)
    {
        if (Context.PlayerData == null)
        {
            Context.SetPlayerData(CreateDefaultPlayerData());
        }

        Context.PlayerData.IsEuropeanRoulette = isEuropeanRoulette;
        SaveLoadManager.Save(Context.PlayerData);
        StateMachine.ChangeState(new BettingState(Context, StateMachine));
    }

    private PlayerData CreateDefaultPlayerData()
    {
        PlayerData playerData = new PlayerData();
        float startingBalance = Context != null ? Context.DefaultStartingBalance : FallbackStartingBalance;
        playerData.Balance = startingBalance;
        return playerData;
    }

    private bool TryResumeSavedRound()
    {
        if (Context.PlayerData == null)
        {
            return false;
        }

        if (Context.PlayerData.SavedBets != null && Context.PlayerData.SavedBets.Count > 0)
        {
            StateMachine.ChangeState(new BettingState(Context, StateMachine));
            return true;
        }

        return false;
    }
}

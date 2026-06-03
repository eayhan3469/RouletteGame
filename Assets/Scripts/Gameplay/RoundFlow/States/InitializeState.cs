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
        Context.SetChipInteractionEnabled(false);

        bool hasExistingSave = File.Exists(SaveLoadManager.SavePath);
        GameSaveData saveData = SaveLoadManager.Load();

        if (!hasExistingSave)
        {
            saveData = CreateDefaultSaveData();
        }

        Context.SetSaveData(saveData);
        Context.SetActiveProfile(Context.SaveData.LastSelectedVariant);

        if (Context.MainMenuController != null)
        {
            Context.MainMenuController.SetSelectedRouletteType(Context.SaveData.LastSelectedVariant);
            Context.MainMenuController.PlayButtonClicked += HandlePlayButtonClicked;
            Context.MainMenuController.ClearSaveButtonClicked += HandleClearSaveButtonClicked;
            Context.MainMenuController.SetClearSaveButtonInteractable(hasExistingSave);
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
            Context.MainMenuController.ClearSaveButtonClicked -= HandleClearSaveButtonClicked;
        }

        Context.SetMainMenuVisible(false);
        LogLifecycle("Exit");
    }

    private void HandlePlayButtonClicked(RouletteVariant rouletteVariant)
    {
        if (Context.SaveData == null)
        {
            Context.SetSaveData(CreateDefaultSaveData());
        }

        bool changedVariant = Context.SaveData.LastSelectedVariant != rouletteVariant;
        Context.SetActiveProfile(rouletteVariant);

        if (changedVariant && Context.PlayerData != null)
        {
            Context.PlayerData.ClearRoundState();
        }

        if (!Context.LoadTableVariant(rouletteVariant))
        {
            UnityEngine.Debug.LogWarning($"InitializeState could not load a table for {rouletteVariant}. Continuing with current scene references.");
        }

        Context.SaveActiveGameData();
        StateMachine.ChangeState(new BettingState(Context, StateMachine));
    }

    private void HandleClearSaveButtonClicked()
    {
        if (!SaveLoadManager.DeleteSave())
        {
            return;
        }

        Context.SetSaveData(CreateDefaultSaveData());
        Context.SetActiveProfile(Context.SaveData.LastSelectedVariant);

        if (Context.MainMenuController != null)
        {
            Context.MainMenuController.SetSelectedRouletteType(Context.SaveData.LastSelectedVariant);
            Context.MainMenuController.SetClearSaveButtonInteractable(false);
        }
    }

    private GameSaveData CreateDefaultSaveData()
    {
        float startingBalance = Context != null ? Context.DefaultStartingBalance : FallbackStartingBalance;
        GameSaveData saveData = new GameSaveData();
        saveData.LastSelectedVariant = RouletteVariant.European;
        saveData.EuropeanProfile.Balance = startingBalance;
        saveData.AmericanProfile.Balance = startingBalance;
        return saveData;
    }
}

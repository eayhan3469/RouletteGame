using System.Collections;
using UnityEngine;

/// <summary>
/// Resolves the round payout, persists the updated balance, shows the result UI,
/// and then loops back into the next betting phase.
/// </summary>
public sealed class ResultState : GameStateBase
{
    private const float ResultDisplayDuration = 3f;

    private readonly int _winningNumber;
    private Coroutine _returnToBettingCoroutine;

    public ResultState(GameContext context, StateMachine stateMachine, int winningNumber)
        : base(context, stateMachine)
    {
        _winningNumber = winningNumber;
    }

    public override void Enter()
    {
        LogLifecycle($"Enter - Winning Number: {_winningNumber}");

        float totalBet = Context.BetManager != null
            ? Context.BetManager.TotalBet
            : 0f;
        float amountWon = Context.BetManager != null
            ? Context.BetManager.CalculateWinnings(_winningNumber)
            : 0f;
        float roundResult = amountWon - totalBet;

        if (Context.PlayerData != null)
        {
            Context.PlayerData.Balance += roundResult;
            SaveLoadManager.Save(Context.PlayerData);
        }
        else
        {
            Debug.LogError("ResultState could not persist winnings because PlayerData is missing.");
        }

        if (Context.ResultUIController != null)
        {
            Context.ResultUIController.ShowResult(roundResult);
        }
        else
        {
            Debug.LogWarning("ResultState is missing the ResultUIController reference.");
        }

        _returnToBettingCoroutine = Context.StartCoroutine(ReturnToBettingAfterDelay());
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        if (_returnToBettingCoroutine != null)
        {
            Context.StopCoroutine(_returnToBettingCoroutine);
            _returnToBettingCoroutine = null;
        }

        Context.BetManager?.ClearTableBets();
        Context.ResultUIController?.Hide();
        LogLifecycle("Exit");
    }

    private IEnumerator ReturnToBettingAfterDelay()
    {
        yield return new WaitForSeconds(ResultDisplayDuration);

        _returnToBettingCoroutine = null;
        StateMachine.ChangeState(new BettingState(Context, StateMachine));
    }
}

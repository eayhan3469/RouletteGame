using System.Collections;
using UnityEngine;

/// <summary>
/// Resolves the round payout, persists the updated balance, shows the result UI,
/// and then loops back into the next betting phase.
/// </summary>
public sealed class ResultState : GameStateBase
{
    private const float ResultRevealDelay = 1f;
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
            Context.PlayerData.TotalSpins++;
            Context.PlayerData.TotalWagered += totalBet;
            Context.PlayerData.TotalWon += amountWon;

            if (roundResult > 0f)
            {
                Context.PlayerData.TotalWins++;
            }

            Context.PlayerData.Balance += amountWon;
            Context.ClearPendingSpinBets();
            SaveLoadManager.Save(Context.PlayerData);
            Context.StatisticsUIController?.RefreshStats(Context.PlayerData);
        }
        else
        {
            Debug.LogError("ResultState could not persist winnings because PlayerData is missing.");
        }

        _returnToBettingCoroutine = Context.StartCoroutine(ReturnToBettingAfterDelay(roundResult));
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

    private IEnumerator ReturnToBettingAfterDelay(float roundResult)
    {
        yield return new WaitForSeconds(ResultRevealDelay);

        if (Context.ResultUIController != null)
        {
            Context.AudioFeedbackController?.PlayRoundResult(roundResult);
            Context.ResultUIController.ShowResult(roundResult, _winningNumber);
        }
        else
        {
            Debug.LogWarning("ResultState is missing the ResultUIController reference.");
        }

        yield return new WaitForSeconds(ResultDisplayDuration);

        _returnToBettingCoroutine = null;
        StateMachine.ChangeState(new BettingState(Context, StateMachine));
    }
}

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
    private const float ResultHoldAfterSettlement = 1f;

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
        Context.SetChipInteractionEnabled(false);
        Context.WinVfxController?.StopAndClear();
        Context.SettlementVfxController?.StopAndClear();

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

        _returnToBettingCoroutine = Context.StartCoroutine(ReturnToBettingAfterDelay(roundResult, amountWon));
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
        Context.WinVfxController?.StopAndClear();
        Context.SettlementVfxController?.StopAndClear();
        LogLifecycle("Exit");
    }

    private IEnumerator ReturnToBettingAfterDelay(float roundResult, float amountWon)
    {
        yield return new WaitForSeconds(ResultRevealDelay);

        if (Context.ResultUIController != null)
        {
            Context.AudioFeedbackController?.PlayRoundResult(roundResult);
            Context.ResultUIController.ShowResult(roundResult, _winningNumber);

            if (roundResult > 0f)
            {
                Context.WinVfxController?.PlayWinSequence();
            }
        }
        else
        {
            Debug.LogWarning("ResultState is missing the ResultUIController reference.");

            if (roundResult > 0f)
            {
                Context.WinVfxController?.PlayWinSequence();
            }
        }

        if (Context.ResultUIController != null)
        {
            yield return new WaitForSeconds(ResultDisplayDuration);
            Context.ResultUIController.Hide();
            Context.WinVfxController?.StopAndClear();
        }

        if (Context.SettlementVfxController != null)
        {
            yield return Context.SettlementVfxController.PlaySettlement(
                roundResult,
                amountWon,
                Context.BetManager != null ? Context.BetManager.ActiveBets : null,
                Context.ChipManager,
                Context.AudioFeedbackController);
        }

        yield return new WaitForSeconds(ResultHoldAfterSettlement);

        _returnToBettingCoroutine = null;
        StateMachine.ChangeState(new BettingState(Context, StateMachine));
    }
}

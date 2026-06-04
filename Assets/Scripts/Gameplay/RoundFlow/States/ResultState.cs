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
    private BetManager _betManager;
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
        Context.VfxManager?.StopAndClearAll();
        _betManager = Context.BetManager;

        float totalBet = _betManager != null
            ? _betManager.TotalBet
            : 0f;
        float amountWon = _betManager != null
            ? _betManager.CalculateWinnings(_winningNumber)
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
            Context.SaveActiveGameData();
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

        _betManager?.ClearTableBets();
        _betManager = null;
        Context.ResultUIController?.Hide();
        Context.VfxManager?.StopAndClearAll();
        LogLifecycle("Exit");
    }

    private IEnumerator ReturnToBettingAfterDelay(float roundResult, float amountWon)
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

        if (roundResult > 0f)
        {
            Context.VfxManager?.PlayWinSequence();
        }

        if (Context.ResultUIController != null)
        {
            yield return new WaitForSeconds(ResultDisplayDuration);
            Context.ResultUIController.Hide();
            Context.VfxManager?.StopAndClearAll();
        }

        if (Context.VfxManager != null && Context.VfxManager.HasSettlementController)
        {
            yield return Context.VfxManager.PlaySettlement(
                roundResult,
                amountWon,
                _betManager != null ? _betManager.ActiveBets : null,
                Context.ChipManager,
                Context.AudioFeedbackController);
        }

        yield return new WaitForSeconds(ResultHoldAfterSettlement);

        _returnToBettingCoroutine = null;
        StateMachine.ChangeState(new BettingState(Context, StateMachine));
    }
}

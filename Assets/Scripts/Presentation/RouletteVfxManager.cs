using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinates roulette presentation effects without making GameContext know every effect controller.
/// </summary>
[DisallowMultipleComponent]
public sealed class RouletteVfxManager : MonoBehaviour
{
    [SerializeField]
    private RouletteWinVfxController _winVfxController;

    [SerializeField]
    private RouletteSettlementVfxController _settlementVfxController;

    public bool HasSettlementController => _settlementVfxController != null;

    private void Awake()
    {
        StopAndClearAll();
    }

    public void PlayWinSequence()
    {
        _winVfxController?.PlayWinSequence();
    }

    public IEnumerator PlaySettlement(
        float roundResult,
        float amountWon,
        IReadOnlyList<BetManager.PlacedBet> activeBets,
        ChipManager chipManager,
        RouletteAudioFeedbackController audioFeedbackController)
    {
        if (_settlementVfxController == null)
        {
            yield break;
        }

        yield return _settlementVfxController.PlaySettlement(
            roundResult,
            amountWon,
            activeBets,
            chipManager,
            audioFeedbackController);
    }

    public void StopAndClearAll()
    {
        _winVfxController?.StopAndClear();
        _settlementVfxController?.StopAndClear();
    }
}

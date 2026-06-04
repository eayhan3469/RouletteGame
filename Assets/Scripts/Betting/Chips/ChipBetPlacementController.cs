using UnityEngine;

/// <summary>
/// Handles the betting side effects of moving a chip between the tray and table spots.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChipBetPlacementController : MonoBehaviour
{
    private GameContext _gameContext;
    private ChipManager _chipManager;
    private Transform _traySourceSlot;
    private BetSpot _assignedBetSpot;
    private BetSpot _dragOriginBetSpot;
    private bool _isTrayChip;

    public bool HasPlacedBet => _assignedBetSpot != null;

    public void AssignTraySource(ChipManager chipManager, Transform traySourceSlot)
    {
        _chipManager = chipManager;
        _traySourceSlot = traySourceSlot;
        _isTrayChip = chipManager != null && traySourceSlot != null;
    }

    public void MarkPlacedOnBetSpot(BetSpot betSpot)
    {
        _assignedBetSpot = betSpot;
        _dragOriginBetSpot = null;
        _traySourceSlot = null;
        _isTrayChip = false;
    }

    public void ClearForSettlement()
    {
        _assignedBetSpot = null;
        _dragOriginBetSpot = null;
        _traySourceSlot = null;
        _isTrayChip = false;
    }

    public void ReleaseAssignedBetForDrag(Chip3D chip)
    {
        _dragOriginBetSpot = _assignedBetSpot;

        if (_dragOriginBetSpot == null)
        {
            return;
        }

        UnregisterBet(chip, _dragOriginBetSpot);
        _assignedBetSpot = null;
    }

    public void RestoreDragOrigin(Chip3D chip)
    {
        if (_dragOriginBetSpot == null)
        {
            return;
        }

        RegisterBet(chip, _dragOriginBetSpot);
        _assignedBetSpot = _dragOriginBetSpot;
        _dragOriginBetSpot = null;
        SaveAndPlayDropFeedback();
    }

    public void CommitDropToBetSpot(Chip3D chip, BetSpot betSpot, Transform dragStartParent)
    {
        if (chip == null || betSpot == null)
        {
            return;
        }

        bool wasTrayChip = _isTrayChip && _traySourceSlot != null;

        RegisterBet(chip, betSpot);
        MarkPlacedOnBetSpot(betSpot);

        if (wasTrayChip)
        {
            _chipManager?.ConsumeTrayChip(chip, dragStartParent);
            AdjustBalance(-chip.Value);
        }

        SaveAndPlayDropFeedback();
    }

    public bool TryBeginReturnPlacedBet(Chip3D chip, out Vector3 returnTargetPosition, out bool hasReturnTarget)
    {
        returnTargetPosition = default;
        hasReturnTarget = false;

        if (chip == null || _assignedBetSpot == null)
        {
            return false;
        }

        UnregisterBet(chip, _assignedBetSpot);
        _assignedBetSpot = null;
        _dragOriginBetSpot = null;

        AdjustBalance(chip.Value);
        SaveBettingState();
        hasReturnTarget = TryResolveReturnTarget(chip.Value, out returnTargetPosition);

        if (!hasReturnTarget)
        {
            CompleteReturnToTray(chip);
        }

        return true;
    }

    public void CompleteReturnToTray(Chip3D chip)
    {
        if (chip != null)
        {
            _chipManager?.ReturnChipToTray(chip.Value);
        }

        PlayDropFeedback();
    }

    private void RegisterBet(Chip3D chip, BetSpot betSpot)
    {
        betSpot.RegisterChip(chip);
        ResolveBetManager()?.RegisterBet(chip, betSpot);
    }

    private void UnregisterBet(Chip3D chip, BetSpot betSpot)
    {
        BetManager betManager = ResolveBetManager();

        if (betManager != null)
        {
            betManager.UnregisterBet(chip);
            return;
        }

        betSpot.UnregisterChip(chip);
    }

    private void AdjustBalance(float delta)
    {
        GameContext gameContext = ResolveGameContext();

        if (gameContext == null || gameContext.PlayerData == null)
        {
            return;
        }

        gameContext.PlayerData.Balance += delta;
        gameContext.BettingUIController?.UpdateBalanceText(gameContext.PlayerData.Balance);
    }

    private void SaveAndPlayDropFeedback()
    {
        SaveBettingState();
        PlayDropFeedback();
    }

    private void SaveBettingState()
    {
        GameContext gameContext = ResolveGameContext();

        gameContext?.SaveCurrentBettingState();
    }

    private void PlayDropFeedback()
    {
        GameContext gameContext = ResolveGameContext();

        gameContext?.AudioFeedbackController?.PlayChipDrop();
    }

    private bool TryResolveReturnTarget(int chipValue, out Vector3 returnTargetPosition)
    {
        returnTargetPosition = default;

        if (_chipManager == null)
        {
            return false;
        }

        if (_chipManager.TryGetChipStackTarget(chipValue, out returnTargetPosition, out _))
        {
            return true;
        }

        return _chipManager.TryGetChipStackTarget(out returnTargetPosition);
    }

    private BetManager ResolveBetManager()
    {
        return ResolveGameContext()?.BetManager;
    }

    private GameContext ResolveGameContext()
    {
        if (_gameContext == null)
        {
            _gameContext = FindFirstObjectByType<GameContext>();
        }

        return _gameContext;
    }
}

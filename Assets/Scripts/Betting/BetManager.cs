using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the chips currently committed to betting spots.
/// This is intentionally lightweight and only records placement data.
/// </summary>
[DisallowMultipleComponent]
public sealed class BetManager : MonoBehaviour
{
    [Serializable]
    public sealed class BetEntry
    {
        public Chip3D Chip;
        public BetSpot Spot;
        public int ChipValue;
        public BetType BetType;
    }

    [SerializeField]
    private List<BetEntry> _activeBets = new List<BetEntry>();

    public event Action<float> TotalBetChanged;

    public IReadOnlyList<BetEntry> ActiveBets => _activeBets;
    public float TotalBet => CalculateTotalBet();

    public void RegisterBet(Chip3D chip, BetSpot spot)
    {
        if (chip == null || spot == null)
        {
            return;
        }

        UnregisterBet(chip);

        _activeBets.Add(new BetEntry
        {
            Chip = chip,
            Spot = spot,
            ChipValue = chip.Value,
            BetType = spot.Type
        });

        NotifyTotalBetChanged();
    }

    public void UnregisterBet(Chip3D chip)
    {
        if (chip == null)
        {
            return;
        }

        for (int i = _activeBets.Count - 1; i >= 0; i--)
        {
            if (_activeBets[i].Chip == chip)
            {
                _activeBets.RemoveAt(i);
            }
        }

        NotifyTotalBetChanged();
    }

    private float CalculateTotalBet()
    {
        float totalBet = 0f;

        for (int i = 0; i < _activeBets.Count; i++)
        {
            totalBet += _activeBets[i].ChipValue;
        }

        return totalBet;
    }

    private void NotifyTotalBetChanged()
    {
        TotalBetChanged?.Invoke(TotalBet);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the chips currently committed to betting spots.
/// It also resolves roulette payouts and clears the table between rounds.
/// </summary>
[DisallowMultipleComponent]
public sealed class BetManager : MonoBehaviour
{
    [Serializable]
    public sealed class PlacedBet
    {
        public Chip3D Chip;
        public BetSpot Spot;
    }

    [SerializeField]
    private RoulettePayoutSO _roulettePayout;

    [SerializeField]
    private List<PlacedBet> _activeBets = new List<PlacedBet>();

    public event Action<float> TotalBetChanged;

    public IReadOnlyList<PlacedBet> ActiveBets => _activeBets;
    public float TotalBet => CalculateTotalBet();

    public void RegisterBet(Chip3D chip, BetSpot spot)
    {
        if (chip == null || spot == null)
        {
            return;
        }

        UnregisterBet(chip);

        _activeBets.Add(new PlacedBet
        {
            Chip = chip,
            Spot = spot
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
                if (_activeBets[i].Spot != null)
                {
                    _activeBets[i].Spot.UnregisterChip(chip);
                }

                _activeBets.RemoveAt(i);
            }
        }

        NotifyTotalBetChanged();
    }

    public float CalculateWinnings(int winningNumber)
    {
        float totalPayout = 0f;

        for (int i = 0; i < _activeBets.Count; i++)
        {
            PlacedBet placedBet = _activeBets[i];

            if (placedBet == null || placedBet.Chip == null || placedBet.Spot == null)
            {
                continue;
            }

            int[] coveredNumbers = placedBet.Spot.CoveredNumbers;

            if (coveredNumbers == null || Array.IndexOf(coveredNumbers, winningNumber) < 0)
            {
                continue;
            }

            float chipValue = placedBet.Chip.Value;
            int multiplier = GetPayoutMultiplier(placedBet.Spot.Type);
            totalPayout += chipValue + (chipValue * multiplier);
        }

        return totalPayout;
    }

    public void ClearTableBets()
    {
        for (int i = _activeBets.Count - 1; i >= 0; i--)
        {
            PlacedBet placedBet = _activeBets[i];

            if (placedBet == null)
            {
                continue;
            }

            if (placedBet.Spot != null && placedBet.Chip != null)
            {
                placedBet.Spot.UnregisterChip(placedBet.Chip);
            }

            if (placedBet.Chip == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(placedBet.Chip.gameObject);
            }
            else
            {
                DestroyImmediate(placedBet.Chip.gameObject);
            }
        }

        _activeBets.Clear();
        NotifyTotalBetChanged();
    }

    private float CalculateTotalBet()
    {
        float totalBet = 0f;

        for (int i = 0; i < _activeBets.Count; i++)
        {
            if (_activeBets[i] == null || _activeBets[i].Chip == null)
            {
                continue;
            }

            totalBet += _activeBets[i].Chip.Value;
        }

        return totalBet;
    }

    private int GetPayoutMultiplier(BetType betType)
    {
        if (_roulettePayout != null)
        {
            return _roulettePayout.GetMultiplier(betType);
        }

        Debug.LogWarning($"BetManager is missing a RoulettePayoutSO reference. Using default multiplier for {betType}.");
        return RoulettePayoutSO.GetDefaultMultiplier(betType);
    }

    private void NotifyTotalBetChanged()
    {
        TotalBetChanged?.Invoke(TotalBet);
    }
}

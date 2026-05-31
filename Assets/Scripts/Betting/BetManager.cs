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

    private readonly Dictionary<string, BetSpot> _betSpotLookup = new Dictionary<string, BetSpot>();

    public event Action<float> TotalBetChanged;

    public IReadOnlyList<PlacedBet> ActiveBets => _activeBets;
    public float TotalBet => CalculateTotalBet();

    private void Awake()
    {
        CacheBetSpots();
    }

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

    public List<PlayerData.SavedBetData> CreateSavedBetsSnapshot()
    {
        List<PlayerData.SavedBetData> snapshot = new List<PlayerData.SavedBetData>();

        for (int i = 0; i < _activeBets.Count; i++)
        {
            PlacedBet placedBet = _activeBets[i];

            if (placedBet == null || placedBet.Chip == null || placedBet.Spot == null)
            {
                continue;
            }

            snapshot.Add(new PlayerData.SavedBetData
            {
                BetSpotId = placedBet.Spot.SaveId,
                ChipValue = placedBet.Chip.Value
            });
        }

        return snapshot;
    }

    public void RestoreSavedBets(IReadOnlyList<PlayerData.SavedBetData> savedBets, ChipManager chipManager)
    {
        if (savedBets == null || savedBets.Count == 0)
        {
            return;
        }

        if (chipManager == null)
        {
            Debug.LogError("BetManager could not restore saved bets because ChipManager is missing.");
            return;
        }

        CacheBetSpots();
        ClearTableBets();

        for (int i = 0; i < savedBets.Count; i++)
        {
            PlayerData.SavedBetData savedBet = savedBets[i];

            if (savedBet == null || string.IsNullOrWhiteSpace(savedBet.BetSpotId))
            {
                continue;
            }

            if (!_betSpotLookup.TryGetValue(savedBet.BetSpotId, out BetSpot betSpot) || betSpot == null)
            {
                Debug.LogWarning($"BetManager could not restore bet because no BetSpot matched save id '{savedBet.BetSpotId}'.");
                continue;
            }

            Chip3D spawnedChip = chipManager.SpawnTableChip(savedBet.ChipValue, betSpot);

            if (spawnedChip == null)
            {
                continue;
            }

            RegisterBet(spawnedChip, betSpot);
        }
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

    private void CacheBetSpots()
    {
        _betSpotLookup.Clear();

        BetSpot[] betSpots = FindObjectsByType<BetSpot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < betSpots.Length; i++)
        {
            BetSpot betSpot = betSpots[i];

            if (betSpot == null || string.IsNullOrWhiteSpace(betSpot.SaveId))
            {
                continue;
            }

            if (_betSpotLookup.ContainsKey(betSpot.SaveId))
            {
                Debug.LogWarning($"Duplicate BetSpot save id detected: {betSpot.SaveId}");
                continue;
            }

            _betSpotLookup.Add(betSpot.SaveId, betSpot);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configurable roulette payout table keyed by bet type.
/// </summary>
[CreateAssetMenu(fileName = "SO_RoulettePayout_Default", menuName = "Roulette/Roulette Payout Table")]
public sealed class RoulettePayoutSO : ScriptableObject
{
    [Serializable]
    private sealed class PayoutEntry
    {
        public BetType BetType = BetType.Straight;

        [Min(0)]
        public int Multiplier = 35;
    }

    [SerializeField]
    private List<PayoutEntry> _payoutEntries = new List<PayoutEntry>
    {
        new PayoutEntry { BetType = BetType.Straight, Multiplier = 35 },
        new PayoutEntry { BetType = BetType.Split, Multiplier = 17 },
        new PayoutEntry { BetType = BetType.Street, Multiplier = 11 },
        new PayoutEntry { BetType = BetType.Corner, Multiplier = 8 },
        new PayoutEntry { BetType = BetType.SixLine, Multiplier = 5 },
        new PayoutEntry { BetType = BetType.Dozen, Multiplier = 2 },
        new PayoutEntry { BetType = BetType.Column, Multiplier = 2 },
        new PayoutEntry { BetType = BetType.Red, Multiplier = 1 },
        new PayoutEntry { BetType = BetType.Black, Multiplier = 1 },
        new PayoutEntry { BetType = BetType.Even, Multiplier = 1 },
        new PayoutEntry { BetType = BetType.Odd, Multiplier = 1 },
        new PayoutEntry { BetType = BetType.Low, Multiplier = 1 },
        new PayoutEntry { BetType = BetType.High, Multiplier = 1 }
    };

    public int GetMultiplier(BetType betType)
    {
        for (int i = 0; i < _payoutEntries.Count; i++)
        {
            PayoutEntry payoutEntry = _payoutEntries[i];

            if (payoutEntry != null && payoutEntry.BetType == betType)
            {
                return payoutEntry.Multiplier;
            }
        }

        Debug.LogWarning($"RoulettePayoutSO does not define a multiplier for {betType}. Using the default value.");
        return GetDefaultMultiplier(betType);
    }

    public static int GetDefaultMultiplier(BetType betType)
    {
        switch (betType)
        {
            case BetType.Straight:
                return 35;

            case BetType.Split:
                return 17;

            case BetType.Street:
                return 11;

            case BetType.Corner:
                return 8;

            case BetType.SixLine:
                return 5;

            case BetType.Dozen:
            case BetType.Column:
                return 2;

            case BetType.Red:
            case BetType.Black:
            case BetType.Even:
            case BetType.Odd:
            case BetType.Low:
            case BetType.High:
                return 1;

            default:
                return 0;
        }
    }
}

using System.Collections.Generic;

/// <summary>
/// Serializable player save model containing long-lived progression
/// and resumable round state.
/// </summary>
[System.Serializable]
public sealed class PlayerData
{
    [System.Serializable]
    public sealed class SavedBetData
    {
        public string BetSpotId;
        public int ChipValue;
    }

    public enum RoundPhase
    {
        None,
        Betting,
        Spinning
    }

    public int TotalSpins;
    public int TotalWins;
    public float TotalWagered;
    public float TotalWon;
    public float Balance;
    public RoundPhase SavedRoundPhase;
    public int PendingSpinTargetNumber = -1;
    public List<SavedBetData> SavedBets = new List<SavedBetData>();

    public PlayerData()
    {
        TotalSpins = 0;
        TotalWins = 0;
        TotalWagered = 0f;
        TotalWon = 0f;
        Balance = 0f;
        SavedRoundPhase = RoundPhase.None;
        PendingSpinTargetNumber = -1;
        SavedBets = new List<SavedBetData>();
    }

    public void ClearRoundState()
    {
        SavedRoundPhase = RoundPhase.None;
        PendingSpinTargetNumber = -1;
        SavedBets = new List<SavedBetData>();
    }
}

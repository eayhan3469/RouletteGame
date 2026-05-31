/// <summary>
/// Serializable player save model containing long-lived progression and table settings.
/// </summary>
[System.Serializable]
public sealed class PlayerData
{
    public int TotalSpins;
    public int TotalWins;
    public float TotalWagered;
    public float TotalWon;
    public float Balance;
    public bool IsEuropeanRoulette = true;

    public PlayerData()
    {
        TotalSpins = 0;
        TotalWins = 0;
        TotalWagered = 0f;
        TotalWon = 0f;
        Balance = 0f;
        IsEuropeanRoulette = true;
    }
}

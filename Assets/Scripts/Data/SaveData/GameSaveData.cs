/// <summary>
/// Root save model. Each roulette variant owns an independent player profile,
/// while the root remembers the last menu selection.
/// </summary>
[System.Serializable]
public sealed class GameSaveData
{
    public RouletteVariant LastSelectedVariant = RouletteVariant.European;
    public PlayerData EuropeanProfile = new PlayerData();
    public PlayerData AmericanProfile = new PlayerData();

    public PlayerData GetProfile(RouletteVariant variant)
    {
        EnsureProfiles();
        return variant == RouletteVariant.American ? AmericanProfile : EuropeanProfile;
    }

    public void EnsureProfiles()
    {
        if (EuropeanProfile == null)
        {
            EuropeanProfile = new PlayerData();
        }

        if (AmericanProfile == null)
        {
            AmericanProfile = new PlayerData();
        }
    }
}

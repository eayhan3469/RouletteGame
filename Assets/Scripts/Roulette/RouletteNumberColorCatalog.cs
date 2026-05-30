using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized roulette number color data source.
/// UI and gameplay systems can query this asset to learn whether
/// a roulette pocket is red, black, or green.
/// </summary>
[CreateAssetMenu(
    fileName = "RouletteNumberColorCatalog",
    menuName = "Deterministic Roulette/Roulette Number Color Catalog")]
public sealed class RouletteNumberColorCatalog : ScriptableObject
{
    [SerializeField]
    private List<int> _redNumbers = new List<int>();

    [SerializeField]
    private List<int> _blackNumbers = new List<int>();

    private readonly HashSet<int> _redLookup = new HashSet<int>();
    private readonly HashSet<int> _blackLookup = new HashSet<int>();

    private void OnEnable()
    {
        RebuildLookups();
    }

    private void OnValidate()
    {
        RebuildLookups();
    }

    /// <summary>
    /// Returns the standard roulette pocket color for the given number.
    /// Zero and double-zero are always treated as green.
    /// </summary>
    public bool TryGetPocketColor(int number, out RoulettePocketColor pocketColor)
    {
        if (number == 0 || number == 37)
        {
            pocketColor = RoulettePocketColor.Green;
            return true;
        }

        if (_redLookup.Contains(number))
        {
            pocketColor = RoulettePocketColor.Red;
            return true;
        }

        if (_blackLookup.Contains(number))
        {
            pocketColor = RoulettePocketColor.Black;
            return true;
        }

        pocketColor = RoulettePocketColor.Unknown;
        return false;
    }

    [ContextMenu("Populate Standard Roulette Colors")]
    private void PopulateStandardRouletteColors()
    {
        _redNumbers = new List<int>
        {
            1, 3, 5, 7, 9,
            12, 14, 16, 18,
            19, 21, 23, 25, 27,
            30, 32, 34, 36
        };

        _blackNumbers = new List<int>
        {
            2, 4, 6, 8, 10, 11,
            13, 15, 17, 20, 22, 24,
            26, 28, 29, 31, 33, 35
        };

        RebuildLookups();
    }

    private void RebuildLookups()
    {
        _redLookup.Clear();
        _blackLookup.Clear();

        for (int i = 0; i < _redNumbers.Count; i++)
        {
            _redLookup.Add(_redNumbers[i]);
        }

        for (int i = 0; i < _blackNumbers.Count; i++)
        {
            _blackLookup.Add(_blackNumbers[i]);
        }
    }
}

public enum RoulettePocketColor
{
    Unknown = 0,
    Red = 1,
    Black = 2,
    Green = 3
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toggles the dedicated highlight object already assigned under each bet spot.
/// </summary>
[DisallowMultipleComponent]
public sealed class BetSpotHighlighter : MonoBehaviour
{
    private readonly Dictionary<int, BetSpot> _straightNumberSpots = new Dictionary<int, BetSpot>();
    private readonly List<BetSpot> _activeSpots = new List<BetSpot>();

    [SerializeField]
    private Color _highlightColor = new Color(1f, 0.9f, 0.2f, 0.35f);

    [SerializeField]
    private int _numberHighlightSortingOrder = 55;

    public void ShowFor(BetSpot betSpot)
    {
        if (betSpot == null)
        {
            Hide();
            return;
        }

        Hide();
        RebuildStraightNumberSpotCache();
        TryActivateSpot(betSpot);
        TryActivateCoveredNumberSpots(betSpot);
    }

    public void Hide()
    {
        for (int i = 0; i < _activeSpots.Count; i++)
        {
            if (_activeSpots[i] != null)
            {
                _activeSpots[i].SetNumberHighlightVisible(false, _highlightColor, _numberHighlightSortingOrder);
            }
        }

        _activeSpots.Clear();
    }

    private void Awake()
    {
        Hide();
    }

    private void RebuildStraightNumberSpotCache()
    {
        _straightNumberSpots.Clear();
        BetSpot[] betSpots = FindObjectsByType<BetSpot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < betSpots.Length; i++)
        {
            BetSpot betSpot = betSpots[i];

            if (betSpot == null || !betSpot.IsStraightNumberSpot)
            {
                continue;
            }

            int straightNumber = betSpot.StraightNumber;

            if (_straightNumberSpots.ContainsKey(straightNumber))
            {
                continue;
            }

            _straightNumberSpots.Add(straightNumber, betSpot);
        }
    }

    private void TryActivateCoveredNumberSpots(BetSpot sourceSpot)
    {
        int[] coveredNumbers = sourceSpot.CoveredNumbers;

        if (coveredNumbers == null || coveredNumbers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < coveredNumbers.Length; i++)
        {
            if (_straightNumberSpots.TryGetValue(coveredNumbers[i], out BetSpot straightNumberSpot))
            {
                TryActivateSpot(straightNumberSpot);
            }
        }
    }

    private void TryActivateSpot(BetSpot betSpot)
    {
        if (betSpot == null || !betSpot.HasNumberHighlightRenderer || _activeSpots.Contains(betSpot))
        {
            return;
        }

        betSpot.SetNumberHighlightVisible(true, _highlightColor, _numberHighlightSortingOrder);
        _activeSpots.Add(betSpot);
    }
}

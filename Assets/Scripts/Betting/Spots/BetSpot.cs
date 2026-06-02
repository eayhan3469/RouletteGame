using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a physical betting area on the roulette table.
/// Chips can be dropped onto this invisible collider to represent a bet selection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class BetSpot : MonoBehaviour
{
    [SerializeField]
    private BetType _betType = BetType.Straight;

    [SerializeField]
    private int[] _coveredNumbers = Array.Empty<int>();

    [SerializeField]
    private Vector3 _snapOffset = Vector3.zero;

    [SerializeField]
    [Min(0)]
    private int _currentChipCount;

    [SerializeField]
    [Min(0.001f)]
    private float _chipThickness = 0.05f;

    [Header("Persistence")]
    [SerializeField]
    private string _saveId = string.Empty;

    [Header("Highlight")]
    [SerializeField]
    private SpriteRenderer _numberHighlightRenderer;

    private Collider _cachedCollider;
    private readonly List<Chip3D> _placedChips = new List<Chip3D>();

    public BetType Type => _betType;
    public int[] CoveredNumbers => _coveredNumbers;
    public int CurrentChipCount => _currentChipCount;
    public string SaveId => string.IsNullOrWhiteSpace(_saveId) ? GetHierarchyPath() : _saveId;
    public bool IsStraightNumberSpot => _betType == BetType.Straight && _coveredNumbers != null && _coveredNumbers.Length == 1;
    public int StraightNumber => IsStraightNumberSpot ? _coveredNumbers[0] : -1;
    public bool HasNumberHighlightRenderer => _numberHighlightRenderer != null;

    private void Awake()
    {
        CacheHighlightRendererIfNeeded();
        _cachedCollider = GetComponent<Collider>();
        SetNumberHighlightVisible(false, Color.white, 0);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheHighlightRendererIfNeeded();
    }
#endif

    /// <summary>
    /// Returns the collider bounds used both for hover highlighting
    /// and for calculating chip placement positions.
    /// </summary>
    public Bounds GetWorldBounds()
    {
        Collider targetCollider = _cachedCollider != null ? _cachedCollider : GetComponent<Collider>();
        return targetCollider.bounds;
    }

    /// <summary>
    /// Returns the next stack position for a chip placed on this bet spot.
    /// </summary>
    public Vector3 GetNextDropPosition()
    {
        Bounds bounds = GetWorldBounds();
        Vector3 basePosition = bounds.center;

        basePosition.y = bounds.max.y + (_chipThickness * 0.5f);
        basePosition += _snapOffset;

        return basePosition + (Vector3.up * (_currentChipCount * _chipThickness));
    }

    public void RegisterChip(Chip3D chip)
    {
        if (chip == null || _placedChips.Contains(chip))
        {
            return;
        }

        _placedChips.Add(chip);
        _currentChipCount = _placedChips.Count;
    }

    public void UnregisterChip(Chip3D chip)
    {
        if (chip == null)
        {
            return;
        }

        _placedChips.Remove(chip);
        _currentChipCount = _placedChips.Count;
    }

    public void SetNumberHighlightVisible(bool isVisible, Color color, int sortingOrder)
    {
        if (_numberHighlightRenderer == null)
        {
            return;
        }

        _numberHighlightRenderer.color = color;
        _numberHighlightRenderer.sortingOrder = sortingOrder;
        _numberHighlightRenderer.enabled = isVisible;
    }

    private void CacheHighlightRendererIfNeeded()
    {
        if (_numberHighlightRenderer != null)
        {
            return;
        }

        Transform highlightTransform = transform.Find("Highlight");

        if (highlightTransform != null)
        {
            _numberHighlightRenderer = highlightTransform.GetComponent<SpriteRenderer>();
        }

        if (_numberHighlightRenderer == null)
        {
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i] != null && childRenderers[i].transform != transform)
                {
                    _numberHighlightRenderer = childRenderers[i];
                    break;
                }
            }
        }
    }

    private string GetHierarchyPath()
    {
        string path = name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }
}

/// <summary>
/// Identifies the roulette bet category represented by a bet spot.
/// </summary>
public enum BetType
{
    Straight,
    Split,
    Street,
    Corner,
    SixLine,
    Dozen,
    Column,
    Red,
    Black,
    Even,
    Odd,
    Low,
    High
}

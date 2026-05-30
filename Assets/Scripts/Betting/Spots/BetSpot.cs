using System;
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
    private bool _snapToCenter = true;

    [SerializeField]
    private Vector3 _snapOffset = Vector3.zero;

    private Collider _cachedCollider;

    public BetType Type => _betType;
    public int[] CoveredNumbers => _coveredNumbers;

    private void Awake()
    {
        _cachedCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Resolves the final placement position for a chip dropped on this bet spot.
    /// </summary>
    public Vector3 GetSnapPosition(RaycastHit hit, float chipHeight)
    {
        Collider targetCollider = _cachedCollider != null ? _cachedCollider : hit.collider;
        Vector3 snapPosition = _snapToCenter ? targetCollider.bounds.center : hit.point;

        snapPosition.y = targetCollider.bounds.max.y + (chipHeight * 0.5f);
        snapPosition += _snapOffset;

        return snapPosition;
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

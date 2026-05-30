using UnityEngine;

/// <summary>
/// Marks a single roulette pocket transform with its associated roulette number.
/// These slot transforms are used as deterministic landing targets for the ball.
/// </summary>
[DisallowMultipleComponent]
public sealed class WheelSlot : MonoBehaviour
{
    [SerializeField]
    [Min(0)]
    private int _number;

    public int Number => _number;

    /// <summary>
    /// Allows editor tooling or runtime fallback generation to stamp a number onto the slot.
    /// </summary>
    public void SetNumber(int number)
    {
        _number = Mathf.Max(0, number);
    }
}

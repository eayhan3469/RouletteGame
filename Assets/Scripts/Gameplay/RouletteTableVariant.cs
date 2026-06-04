using UnityEngine;

/// <summary>
/// Runtime-facing contract for a roulette table prefab.
/// </summary>
[DisallowMultipleComponent]
public sealed class RouletteTableVariant : MonoBehaviour
{
    [SerializeField]
    private RouletteVariant _variant = RouletteVariant.European;

    [SerializeField]
    private BetManager _betManager;

    [SerializeField]
    private WheelController _wheelController;

    [SerializeField]
    private Transform _tableRoot;

    [SerializeField]
    private Transform _wheelRoot;

    [SerializeField]
    private Transform _betSpotsRoot;

    public RouletteVariant Variant => _variant;
    public BetManager BetManager => _betManager;
    public WheelController WheelController => _wheelController;
    public Transform TableRoot => _tableRoot;
    public Transform WheelRoot => _wheelRoot;
    public Transform BetSpotsRoot => _betSpotsRoot;
}

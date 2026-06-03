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

    private void Awake()
    {
        CacheReferencesIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferencesIfNeeded();
    }
#endif

    private void CacheReferencesIfNeeded()
    {
        if (_betManager == null)
        {
            _betManager = GetComponentInChildren<BetManager>(true);
        }

        if (_wheelController == null)
        {
            _wheelController = GetComponentInChildren<WheelController>(true);
        }

        if (_tableRoot == null)
        {
            Transform tableRoot = transform.Find("TableRoot");
            _tableRoot = tableRoot != null ? tableRoot : transform;
        }

        if (_wheelRoot == null)
        {
            Transform wheelRoot = transform.Find("WheelRoot");
            _wheelRoot = wheelRoot != null ? wheelRoot : transform;
        }

        if (_betSpotsRoot == null)
        {
            Transform betSpotsRoot = transform.Find("BetSpotsRoot_European");
            if (betSpotsRoot == null)
            {
                betSpotsRoot = transform.Find("BetSpotsRoot_American");
            }

            _betSpotsRoot = betSpotsRoot;
        }
    }
}

using UnityEngine;

/// <summary>
/// Owns the active table instance and swaps it based on the selected roulette variant.
/// </summary>
[DisallowMultipleComponent]
public sealed class TableVariantLoader : MonoBehaviour
{
    [SerializeField]
    private RouletteTableVariant _europeanTablePrefab;

    [SerializeField]
    private RouletteTableVariant _americanTablePrefab;

    [SerializeField]
    private Transform _spawnParent;

    [SerializeField]
    private RouletteTableVariant _sceneFallbackTable;

    private RouletteTableVariant _activeTable;

    public RouletteTableVariant ActiveTable => _activeTable != null ? _activeTable : _sceneFallbackTable;

    public RouletteTableVariant LoadVariant(RouletteVariant variant)
    {
        RouletteTableVariant prefab = GetPrefab(variant);

        if (prefab == null)
        {
            if (_sceneFallbackTable != null)
            {
                _sceneFallbackTable.gameObject.SetActive(true);
                _activeTable = _sceneFallbackTable;
                return _activeTable;
            }

            Debug.LogWarning($"TableVariantLoader has no prefab or fallback table for {variant}.");
            return null;
        }

        DestroyActiveRuntimeInstance();

        Transform parent = _spawnParent != null ? _spawnParent : transform;
        _activeTable = Instantiate(prefab, parent);
        _activeTable.transform.localPosition = Vector3.zero;
        _activeTable.transform.localRotation = Quaternion.identity;
        _activeTable.transform.localScale = Vector3.one;
        _activeTable.gameObject.SetActive(true);
        return _activeTable;
    }

    public void ClearActiveTable()
    {
        DestroyActiveRuntimeInstance();

        if (_sceneFallbackTable != null)
        {
            _sceneFallbackTable.gameObject.SetActive(false);
        }
    }

    private RouletteTableVariant GetPrefab(RouletteVariant variant)
    {
        return variant == RouletteVariant.American ? _americanTablePrefab : _europeanTablePrefab;
    }

    private void DestroyActiveRuntimeInstance()
    {
        if (_activeTable == null || _activeTable == _sceneFallbackTable)
        {
            _activeTable = null;
            return;
        }

        _activeTable.gameObject.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(_activeTable.gameObject);
        }
        else
        {
            DestroyImmediate(_activeTable.gameObject);
        }

        _activeTable = null;
    }
}

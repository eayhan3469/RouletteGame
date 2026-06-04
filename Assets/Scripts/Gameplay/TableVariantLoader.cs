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

    private RouletteTableVariant _activeTable;

    public RouletteTableVariant ActiveTable => _activeTable;

    public RouletteTableVariant LoadVariant(RouletteVariant variant)
    {
        RouletteTableVariant prefab = GetPrefab(variant);

        if (prefab == null)
        {
            Debug.LogError($"TableVariantLoader cannot load {variant} because the table prefab is missing.");
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
    }

    private RouletteTableVariant GetPrefab(RouletteVariant variant)
    {
        return variant == RouletteVariant.American ? _americanTablePrefab : _europeanTablePrefab;
    }

    private void DestroyActiveRuntimeInstance()
    {
        if (_activeTable == null)
        {
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

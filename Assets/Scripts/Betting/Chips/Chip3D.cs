using TMPro;
using UnityEngine;

/// <summary>
/// Represents a physical 3D roulette chip in the scene.
/// It stores the denomination value and updates its visual presentation.
/// </summary>
public sealed class Chip3D : MonoBehaviour
{
    private static readonly int BaseColorShaderProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorShaderProperty = Shader.PropertyToID("_Color");

    [SerializeField]
    private int _value;

    [Header("Visual References")]
    [SerializeField]
    private Renderer _chipRenderer;

    [SerializeField]
    [Min(0)]
    private int _bodyMaterialIndex;

    [SerializeField]
    [Min(0)]
    private int _stripeMaterialIndex = 1;

    [SerializeField]
    private TextMeshPro _valueText;

    private MaterialPropertyBlock _materialPropertyBlock;

    public int Value => _value;

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

    public void Initialize(int value, Color bodyColor, Color stripeColor, Color textColor)
    {
        _value = value;
        ApplyVisuals(value, bodyColor, stripeColor, textColor);
    }

    private void ApplyVisuals(int value, Color bodyColor, Color stripeColor, Color textColor)
    {
        ApplyValueText(value);
        ApplyMaterialColor(_bodyMaterialIndex, bodyColor);
        ApplyMaterialColor(_stripeMaterialIndex, stripeColor);
        ApplyTextColor(textColor);
    }

    private void ApplyValueText(int value)
    {
        if (_valueText == null)
        {
            return;
        }

        _valueText.text = value.ToString();
    }

    private void ApplyTextColor(Color textColor)
    {
        if (_valueText == null)
        {
            return;
        }

        _valueText.color = textColor;
    }

    private void CacheReferencesIfNeeded()
    {
        if (_chipRenderer == null)
        {
            _chipRenderer = GetComponentInChildren<Renderer>();
        }

        if (_valueText == null)
        {
            _valueText = GetComponentInChildren<TextMeshPro>();
        }
    }

    private void ApplyMaterialColor(int materialIndex, Color color)
    {
        if (_chipRenderer == null)
        {
            return;
        }

        if (_materialPropertyBlock == null)
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        _chipRenderer.GetPropertyBlock(_materialPropertyBlock, materialIndex);

        Material sharedMaterial = GetSharedMaterial(materialIndex);

        if (sharedMaterial != null)
        {
            if (sharedMaterial.HasProperty(BaseColorShaderProperty))
            {
                _materialPropertyBlock.SetColor(BaseColorShaderProperty, color);
            }

            if (sharedMaterial.HasProperty(ColorShaderProperty))
            {
                _materialPropertyBlock.SetColor(ColorShaderProperty, color);
            }
        }

        _chipRenderer.SetPropertyBlock(_materialPropertyBlock, materialIndex);
    }

    private Material GetSharedMaterial(int materialIndex)
    {
        if (_chipRenderer == null)
        {
            return null;
        }

        Material[] sharedMaterials = _chipRenderer.sharedMaterials;

        if (sharedMaterials == null || materialIndex < 0 || materialIndex >= sharedMaterials.Length)
        {
            return null;
        }

        return sharedMaterials[materialIndex];
    }
}

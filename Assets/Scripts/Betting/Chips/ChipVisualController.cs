using TMPro;
using UnityEngine;

/// <summary>
/// Applies denomination text and material colors for a chip.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChipVisualController : MonoBehaviour
{
    private static readonly int BaseColorShaderProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorShaderProperty = Shader.PropertyToID("_Color");

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

    public void ApplyVisuals(int value, Color bodyColor, Color stripeColor, Color textColor)
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

using UnityEngine;

/// <summary>
/// Represents a physical 3D roulette chip in the scene.
/// For now it only stores the denomination value assigned by the chip manager.
/// </summary>
public sealed class Chip3D : MonoBehaviour
{
    [SerializeField]
    private int _value;

    public int Value => _value;

    public void Initialize(int value)
    {
        _value = value;
    }
}

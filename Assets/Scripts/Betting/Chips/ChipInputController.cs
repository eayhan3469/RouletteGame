using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Polls pointer input for a chip and delegates physical movement to Chip3D.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Chip3D))]
public sealed class ChipInputController : MonoBehaviour
{
    private static ChipInputController _activeInputController;

    [SerializeField]
    private Chip3D _chip;

    private GameContext _gameContext;

    public static bool HasActiveDrag => _activeInputController != null;

    private void Awake()
    {
        CacheReferencesIfNeeded();
        _gameContext = FindFirstObjectByType<GameContext>();
    }

    private void Update()
    {
        if (_chip == null || !_chip.enabled)
        {
            ClearActiveDragIfOwned();
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        if (!CanAcceptChipInput())
        {
            CancelActiveDragIfOwned();
            return;
        }

        Vector2 pointerPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame && _activeInputController == null)
        {
            if (_chip.TryBeginDrag(pointerPosition))
            {
                _activeInputController = this;
            }
        }

        if (mouse.rightButton.wasPressedThisFrame && _activeInputController == null)
        {
            _chip.TryReturnPlacedBet(pointerPosition);
        }

        if (_activeInputController != this)
        {
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            _chip.DragToPointer(pointerPosition);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            _chip.EndDrag(pointerPosition);
            ClearActiveDragIfOwned();
        }
    }

    private void OnDestroy()
    {
        ClearActiveDragIfOwned();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferencesIfNeeded();
    }
#endif

    public static void ClearActiveDrag(Chip3D chip)
    {
        if (_activeInputController == null || _activeInputController._chip != chip)
        {
            return;
        }

        _activeInputController = null;
    }

    private bool CanAcceptChipInput()
    {
        if (_gameContext == null)
        {
            _gameContext = FindFirstObjectByType<GameContext>();
        }

        return _gameContext != null && _gameContext.IsChipInteractionEnabled;
    }

    private void CancelActiveDragIfOwned()
    {
        if (_activeInputController != this)
        {
            return;
        }

        _chip.CancelDrag();
        _activeInputController = null;
    }

    private void ClearActiveDragIfOwned()
    {
        if (_activeInputController == this)
        {
            _activeInputController = null;
        }
    }

    private void CacheReferencesIfNeeded()
    {
        if (_chip == null)
        {
            _chip = GetComponent<Chip3D>();
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents a physical 3D roulette chip in the scene.
/// It stores the denomination value, updates its visual presentation,
/// and handles physics-based drag-and-drop onto table bet spots.
/// </summary>
[DisallowMultipleComponent]
public sealed class Chip3D : MonoBehaviour
{
    private static readonly int BaseColorShaderProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorShaderProperty = Shader.PropertyToID("_Color");
    private static Chip3D _activeDraggedChip;

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

    [Header("Drag And Drop")]
    [SerializeField]
    [Min(0f)]
    private float _dragPlaneHeight = 1f;

    [SerializeField]
    [Min(0f)]
    private float _dragHoverOffset = 0.35f;

    [SerializeField]
    [Min(0.01f)]
    private float _returnDuration = 0.2f;

    [SerializeField]
    [Min(0f)]
    private float _dragFollowSpeed = 20f;

    [SerializeField]
    [Min(1f)]
    private float _dragScaleMultiplier = 1.08f;

    [SerializeField]
    [Range(0f, 20f)]
    private float _maxTiltAngle = 8f;

    [SerializeField]
    [Min(0f)]
    private float _tiltSensitivity = 10f;

    [SerializeField]
    [Min(0f)]
    private float _visualFollowSpeed = 14f;

    [SerializeField]
    [Min(0.01f)]
    private float _dropSnapDuration = 0.12f;

    private MaterialPropertyBlock _materialPropertyBlock;
    private readonly RaycastHit[] _raycastHits = new RaycastHit[16];
    private Collider[] _colliders;
    private Coroutine _returnToOriginCoroutine;
    private Coroutine _settleCoroutine;
    private Plane _dragPlane;
    private BetManager _betManager;
    private BetSpotHighlighter _betSpotHighlighter;
    private ChipManager _chipManager;
    private GameContext _gameContext;
    private BetSpot _currentHoveredSpot;
    private BetSpot _assignedBetSpot;
    private BetSpot _dragOriginBetSpot;
    private Transform _dragStartParent;
    private Transform _traySourceSlot;
    private Vector3 _dragStartLocalPosition;
    private Vector3 _dragStartLocalScale;
    private Quaternion _dragStartLocalRotation;
    private Quaternion _dragStartWorldRotation;
    private Vector3 _lastDragTargetPosition;
    private bool _isDragging;
    private bool _isTrayChip;

    public int Value => _value;

    private void Awake()
    {
        CacheReferencesIfNeeded();
        _colliders = GetComponentsInChildren<Collider>();
        _betManager = FindFirstObjectByType<BetManager>();
        _betSpotHighlighter = FindFirstObjectByType<BetSpotHighlighter>();
        _gameContext = FindFirstObjectByType<GameContext>();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryBeginDrag(mouse.position.ReadValue());
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            TryReturnPlacedBet(mouse.position.ReadValue());
        }

        if (_activeDraggedChip != this)
        {
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            DragToPointer(mouse.position.ReadValue());
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
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

    public void AssignTraySource(ChipManager chipManager, Transform traySourceSlot)
    {
        _chipManager = chipManager;
        _traySourceSlot = traySourceSlot;
        _isTrayChip = chipManager != null && traySourceSlot != null;
    }

    public void MarkPlacedOnBetSpot(BetSpot betSpot)
    {
        _assignedBetSpot = betSpot;
        _dragOriginBetSpot = null;
        _traySourceSlot = null;
        _isTrayChip = false;
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        if (_activeDraggedChip != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        Ray selectionRay = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(selectionRay, out RaycastHit hit, Mathf.Infinity) || !OwnsCollider(hit.collider))
        {
            return;
        }

        _gameContext?.AudioFeedbackController?.PlayChipPickup();

        if (_returnToOriginCoroutine != null)
        {
            StopCoroutine(_returnToOriginCoroutine);
            _returnToOriginCoroutine = null;
            SetCollidersEnabled(true);
        }

        if (_settleCoroutine != null)
        {
            StopCoroutine(_settleCoroutine);
            _settleCoroutine = null;
            SetCollidersEnabled(true);
        }

        ClearHoveredSpot();

        _dragOriginBetSpot = _assignedBetSpot;

        if (_dragOriginBetSpot != null)
        {
            _dragOriginBetSpot.UnregisterChip(this);
            _betManager?.UnregisterBet(this);
            _assignedBetSpot = null;
        }

        _dragStartParent = transform.parent;
        _dragStartLocalPosition = transform.localPosition;
        _dragStartLocalScale = transform.localScale;
        _dragStartLocalRotation = transform.localRotation;
        _dragStartWorldRotation = transform.rotation;
        _lastDragTargetPosition = transform.position;
        float dragPlaneWorldHeight = Mathf.Max(_dragPlaneHeight, transform.position.y + _dragHoverOffset);
        _dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneWorldHeight, 0f));
        _isDragging = true;
        _activeDraggedChip = this;

        transform.SetParent(null, true);
    }

    private void TryReturnPlacedBet(Vector2 screenPosition)
    {
        if (_isDragging || _assignedBetSpot == null || _activeDraggedChip != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        Ray selectionRay = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(selectionRay, out RaycastHit hit, Mathf.Infinity) || !OwnsCollider(hit.collider))
        {
            return;
        }

        _betManager?.UnregisterBet(this);
        _assignedBetSpot = null;
        _dragOriginBetSpot = null;
        ClearHoveredSpot();

        if (_gameContext != null && _gameContext.PlayerData != null)
        {
            _gameContext.PlayerData.Balance += Value;
            _gameContext.BettingUIController?.UpdateBalanceText(_gameContext.PlayerData.Balance);
        }

        _gameContext?.SaveCurrentBettingState();
        _gameContext?.AudioFeedbackController?.PlayChipDrop();

        _chipManager?.ReturnChipToTray(Value);

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    private void DragToPointer(Vector2 screenPosition)
    {
        if (!_isDragging)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        Ray dragRay = mainCamera.ScreenPointToRay(screenPosition);

        if (!_dragPlane.Raycast(dragRay, out float enterDistance))
        {
            return;
        }

        Vector3 targetPosition = dragRay.GetPoint(enterDistance);

        if (TryGetBetSpotFromPointer(screenPosition, out BetSpot hoveredBetSpot, out _))
        {
            SetHoveredSpot(hoveredBetSpot);
        }
        else
        {
            ClearHoveredSpot();
        }

        Vector3 movementDelta = targetPosition - _lastDragTargetPosition;
        _lastDragTargetPosition = targetPosition;

        float interpolationFactor = Mathf.Clamp01(_dragFollowSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationFactor);

        float tiltAroundX = Mathf.Clamp(-movementDelta.z * _tiltSensitivity, -_maxTiltAngle, _maxTiltAngle);
        float tiltAroundZ = Mathf.Clamp(movementDelta.x * _tiltSensitivity, -_maxTiltAngle, _maxTiltAngle);
        Quaternion targetRotation = _dragStartWorldRotation * Quaternion.Euler(tiltAroundX, 0f, tiltAroundZ);
        float visualInterpolation = Mathf.Clamp01(_visualFollowSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, visualInterpolation);
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            _dragStartLocalScale * _dragScaleMultiplier,
            visualInterpolation);
    }

    private void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        _activeDraggedChip = null;

        BetSpot betSpot = _currentHoveredSpot;

        if (betSpot == null && Mouse.current != null)
        {
            TryGetBetSpotFromPointer(Mouse.current.position.ReadValue(), out betSpot, out _);
        }

        if (betSpot != null)
        {
            ClearHoveredSpot();

            Vector3 snapPosition = betSpot.GetNextDropPosition();
            _settleCoroutine = StartCoroutine(AnimateDropToBetSpotRoutine(betSpot, snapPosition));
            return;
        }

        ClearHoveredSpot();
        _returnToOriginCoroutine = StartCoroutine(ReturnToOriginRoutine());
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

    private bool TryGetBetSpotFromPointer(Vector2 screenPosition, out BetSpot betSpot, out Vector3 previewPosition)
    {
        betSpot = null;
        previewPosition = default;

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return false;
        }

        Ray pointerRay = mainCamera.ScreenPointToRay(screenPosition);
        int hitCount = Physics.RaycastNonAlloc(pointerRay, _raycastHits, Mathf.Infinity);
        float closestDistance = float.MaxValue;
        BetSpot closestBetSpot = null;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastHits[i];

            if (OwnsCollider(hit.collider))
            {
                continue;
            }

            BetSpot candidateBetSpot = hit.collider.GetComponentInParent<BetSpot>();

            if (candidateBetSpot != null && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestBetSpot = candidateBetSpot;
            }
        }

        if (closestBetSpot == null)
        {
            return false;
        }

        betSpot = closestBetSpot;
        previewPosition = closestBetSpot.GetNextDropPosition() + (Vector3.up * _dragHoverOffset);
        return true;
    }

    private bool OwnsCollider(Collider targetCollider)
    {
        if (targetCollider == null || _colliders == null)
        {
            return false;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == targetCollider)
            {
                return true;
            }
        }

        return false;
    }

    private float GetChipHeight()
    {
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    return _colliders[i].bounds.size.y;
                }
            }
        }

        if (_chipRenderer != null)
        {
            return _chipRenderer.bounds.size.y;
        }

        return 0.1f;
    }

    private IEnumerator ReturnToOriginRoutine()
    {
        SetCollidersEnabled(false);

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;
        Quaternion targetRotation = _dragStartParent != null
            ? _dragStartParent.rotation * _dragStartLocalRotation
            : _dragStartLocalRotation;

        float elapsedTime = 0f;

        while (elapsedTime < _returnDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = EaseOut(Mathf.Clamp01(elapsedTime / _returnDuration));

            transform.position = Vector3.Lerp(startPosition, GetDragStartWorldPosition(), normalizedTime);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, normalizedTime);
            transform.localScale = Vector3.Lerp(startScale, _dragStartLocalScale, normalizedTime);

            yield return null;
        }

        if (_dragStartParent != null)
        {
            transform.SetParent(_dragStartParent, false);
            transform.localPosition = _dragStartLocalPosition;
            transform.localRotation = _dragStartLocalRotation;
            transform.localScale = _dragStartLocalScale;
        }
        else
        {
            transform.position = GetDragStartWorldPosition();
            transform.rotation = targetRotation;
            transform.localScale = _dragStartLocalScale;
        }

        SetCollidersEnabled(true);

        if (_dragOriginBetSpot != null)
        {
            _dragOriginBetSpot.RegisterChip(this);
            _betManager?.RegisterBet(this, _dragOriginBetSpot);
            _assignedBetSpot = _dragOriginBetSpot;
            _dragOriginBetSpot = null;
            _gameContext?.SaveCurrentBettingState();
            _gameContext?.AudioFeedbackController?.PlayChipDrop();
        }

        _returnToOriginCoroutine = null;
    }

    private IEnumerator AnimateDropToBetSpotRoutine(BetSpot betSpot, Vector3 snapPosition)
    {
        SetCollidersEnabled(false);

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;
        Quaternion targetRotation = _dragStartParent != null
            ? _dragStartParent.rotation * _dragStartLocalRotation
            : _dragStartLocalRotation;

        float elapsedTime = 0f;

        while (elapsedTime < _dropSnapDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = EaseOut(Mathf.Clamp01(elapsedTime / _dropSnapDuration));

            transform.position = Vector3.Lerp(startPosition, snapPosition, normalizedTime);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, normalizedTime);
            transform.localScale = Vector3.Lerp(startScale, _dragStartLocalScale, normalizedTime);

            yield return null;
        }

        transform.position = snapPosition;
        transform.rotation = targetRotation;
        transform.localScale = _dragStartLocalScale;
        transform.SetParent(betSpot.transform, true);
        betSpot.RegisterChip(this);
        _betManager?.RegisterBet(this, betSpot);
        bool wasTrayChip = _isTrayChip && _traySourceSlot != null;
        MarkPlacedOnBetSpot(betSpot);

        if (wasTrayChip)
        {
            _chipManager?.ConsumeTrayChip(this, _dragStartParent);

            if (_gameContext != null && _gameContext.PlayerData != null)
            {
                _gameContext.PlayerData.Balance -= Value;
                _gameContext.BettingUIController?.UpdateBalanceText(_gameContext.PlayerData.Balance);
            }
        }

        _gameContext?.SaveCurrentBettingState();
        _gameContext?.AudioFeedbackController?.PlayChipDrop();

        SetCollidersEnabled(true);
        _settleCoroutine = null;
    }

    private Vector3 GetDragStartWorldPosition()
    {
        if (_dragStartParent == null)
        {
            return _dragStartLocalPosition;
        }

        return _dragStartParent.TransformPoint(_dragStartLocalPosition);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (_colliders == null)
        {
            return;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
            {
                _colliders[i].enabled = isEnabled;
            }
        }
    }

    private void SetHoveredSpot(BetSpot hoveredSpot)
    {
        if (hoveredSpot == null)
        {
            ClearHoveredSpot();
            return;
        }

        if (hoveredSpot == _currentHoveredSpot)
        {
            return;
        }

        _betSpotHighlighter?.Hide();
        _currentHoveredSpot = hoveredSpot;
        _betSpotHighlighter?.ShowFor(_currentHoveredSpot);
    }

    private void ClearHoveredSpot()
    {
        if (_currentHoveredSpot == null)
        {
            _betSpotHighlighter?.Hide();
            return;
        }

        _betSpotHighlighter?.Hide();
        _currentHoveredSpot = null;
    }

    private float EaseOut(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }
}

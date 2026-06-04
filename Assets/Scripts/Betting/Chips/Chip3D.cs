using System.Collections;
using UnityEngine;

/// <summary>
/// Represents a physical 3D roulette chip in the scene.
/// It stores the denomination value, updates its visual presentation,
/// and handles physics-based drag-and-drop onto table bet spots.
/// </summary>
[DisallowMultipleComponent]
public sealed class Chip3D : MonoBehaviour
{
    [SerializeField]
    private int _value;

    [Header("Visual References")]
    [SerializeField]
    private ChipVisualController _visualController;

    [Header("Placement References")]
    [SerializeField]
    private ChipBetPlacementController _placementController;

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

    [SerializeField]
    [Min(0.01f)]
    private float _trayReturnDuration = 0.3f;

    [SerializeField]
    [Min(0f)]
    private float _trayReturnArcHeight = 0.35f;

    private readonly RaycastHit[] _raycastHits = new RaycastHit[16];
    private Collider[] _colliders;
    private Coroutine _returnToOriginCoroutine;
    private Coroutine _settleCoroutine;
    private Coroutine _returnToTrayCoroutine;
    private Plane _dragPlane;
    private BetSpotHighlighter _betSpotHighlighter;
    private GameContext _gameContext;
    private BetSpot _currentHoveredSpot;
    private Transform _dragStartParent;
    private Vector3 _dragStartLocalPosition;
    private Vector3 _dragStartLocalScale;
    private Quaternion _dragStartLocalRotation;
    private Quaternion _dragStartWorldRotation;
    private Vector3 _lastDragTargetPosition;
    private bool _isDragging;

    public int Value => _value;
    public static bool HasActiveDrag => ChipInputController.HasActiveDrag;

    private void Awake()
    {
        CacheReferencesIfNeeded();
        _colliders = GetComponentsInChildren<Collider>();
        _gameContext = FindFirstObjectByType<GameContext>();
        _betSpotHighlighter = FindFirstObjectByType<BetSpotHighlighter>();
    }

    private void OnDestroy()
    {
        ChipInputController.ClearActiveDrag(this);
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
        _visualController?.ApplyVisuals(value, bodyColor, stripeColor, textColor);
    }

    public void AssignTraySource(ChipManager chipManager, Transform traySourceSlot)
    {
        _placementController?.AssignTraySource(chipManager, traySourceSlot);
    }

    public void MarkPlacedOnBetSpot(BetSpot betSpot)
    {
        _placementController?.MarkPlacedOnBetSpot(betSpot);
    }

    public void PrepareForSettlement()
    {
        if (_returnToOriginCoroutine != null)
        {
            StopCoroutine(_returnToOriginCoroutine);
            _returnToOriginCoroutine = null;
        }

        if (_settleCoroutine != null)
        {
            StopCoroutine(_settleCoroutine);
            _settleCoroutine = null;
        }

        if (_returnToTrayCoroutine != null)
        {
            StopCoroutine(_returnToTrayCoroutine);
            _returnToTrayCoroutine = null;
        }

        ChipInputController.ClearActiveDrag(this);

        _isDragging = false;
        _placementController?.ClearForSettlement();
        ClearHoveredSpot();
        SetCollidersEnabled(false);
        enabled = false;
    }

    internal bool TryBeginDrag(Vector2 screenPosition)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return false;
        }

        Ray selectionRay = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(selectionRay, out RaycastHit hit, Mathf.Infinity) || !OwnsCollider(hit.collider))
        {
            return false;
        }

        if (_gameContext == null)
        {
            _gameContext = FindFirstObjectByType<GameContext>();
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

        _placementController?.ReleaseAssignedBetForDrag(this);

        _dragStartParent = transform.parent;
        _dragStartLocalPosition = transform.localPosition;
        _dragStartLocalScale = transform.localScale;
        _dragStartLocalRotation = transform.localRotation;
        _dragStartWorldRotation = transform.rotation;
        _lastDragTargetPosition = transform.position;
        float dragPlaneWorldHeight = Mathf.Max(_dragPlaneHeight, transform.position.y + _dragHoverOffset);
        _dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneWorldHeight, 0f));
        _isDragging = true;

        transform.SetParent(null, true);
        return true;
    }

    internal bool TryReturnPlacedBet(Vector2 screenPosition)
    {
        if (_isDragging || _returnToTrayCoroutine != null || _placementController == null || !_placementController.HasPlacedBet)
        {
            return false;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return false;
        }

        Ray selectionRay = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(selectionRay, out RaycastHit hit, Mathf.Infinity) || !OwnsCollider(hit.collider))
        {
            return false;
        }

        if (!_placementController.TryBeginReturnPlacedBet(this, out Vector3 returnTargetPosition, out bool hasReturnTarget))
        {
            return false;
        }

        ClearHoveredSpot();

        if (hasReturnTarget)
        {
            _returnToTrayCoroutine = StartCoroutine(AnimateReturnToTrayRoutine(returnTargetPosition));
            return true;
        }

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }

        return true;
    }

    internal void DragToPointer(Vector2 screenPosition)
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

    internal void EndDrag(Vector2 screenPosition)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;

        BetSpot betSpot = _currentHoveredSpot;

        if (betSpot == null)
        {
            TryGetBetSpotFromPointer(screenPosition, out betSpot, out _);
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

    internal void CancelDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        ClearHoveredSpot();

        if (_returnToOriginCoroutine == null)
        {
            _returnToOriginCoroutine = StartCoroutine(ReturnToOriginRoutine());
        }
    }

    private void CacheReferencesIfNeeded()
    {
        if (_visualController == null)
        {
            _visualController = GetComponent<ChipVisualController>();
        }

        if (_placementController == null)
        {
            _placementController = GetComponent<ChipBetPlacementController>();
        }
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

        _placementController?.RestoreDragOrigin(this);

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
        _placementController?.CommitDropToBetSpot(this, betSpot, _dragStartParent);

        SetCollidersEnabled(true);
        _settleCoroutine = null;
    }

    private IEnumerator AnimateReturnToTrayRoutine(Vector3 targetPosition)
    {
        SetCollidersEnabled(false);
        transform.SetParent(null, true);

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;
        Quaternion targetRotation = Quaternion.identity;
        float elapsedTime = 0f;

        while (elapsedTime < _trayReturnDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = EaseOut(Mathf.Clamp01(elapsedTime / _trayReturnDuration));
            float arcOffset = Mathf.Sin(normalizedTime * Mathf.PI) * _trayReturnArcHeight;

            transform.position = Vector3.Lerp(startPosition, targetPosition, normalizedTime) + (Vector3.up * arcOffset);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, normalizedTime);
            transform.localScale = Vector3.Lerp(startScale, Vector3.one, normalizedTime);

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        transform.localScale = Vector3.one;

        _placementController?.CompleteReturnToTray(this);
        _returnToTrayCoroutine = null;

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
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

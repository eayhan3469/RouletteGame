using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates roulette wheel spinning and ball dropping using pure transform math.
/// The ball is animated in a single continuous orbit model from rim travel to pocket entry,
/// which avoids visible jumps caused by switching between multiple parenting or motion systems.
/// </summary>
[DisallowMultipleComponent]
public sealed class WheelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform _wheelTransform;

    [SerializeField]
    private Transform _ballTransform;

    [SerializeField]
    private Transform _ballRimPivot;

    [SerializeField]
    private List<WheelSlot> _allSlots = new List<WheelSlot>();

    [Header("Curves")]
    [SerializeField]
    private AnimationCurve _ballBounceCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.2f, 0.9f),
        new Keyframe(0.55f, 0.28f),
        new Keyframe(0.82f, 0.06f),
        new Keyframe(1f, 0f));

    [SerializeField]
    private AnimationCurve _dropInwardCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.45f, 0.02f),
        new Keyframe(0.72f, 0.22f),
        new Keyframe(0.9f, 0.72f),
        new Keyframe(1f, 1f));

    [Header("Timing")]
    [SerializeField]
    [Min(0.1f)]
    private float _ballAppearDelay = 1.2f;

    [SerializeField]
    [Min(0.1f)]
    private float _rimTravelDuration = 1.35f;

    [SerializeField]
    [Min(0.1f)]
    private float _dropDuration = 3.3f;

    [Header("Wheel Speeds")]
    [SerializeField]
    [Min(0f)]
    private float _wheelStartupDegreesPerSecond = 720f;

    [SerializeField]
    [Min(0f)]
    private float _wheelDropDegreesPerSecond = 210f;

    [SerializeField]
    [Min(0f)]
    private float _wheelStopDegreesPerSecond = 0f;

    [Header("Ball Speeds")]
    [SerializeField]
    [Min(0f)]
    private float _ballRimStartDegreesPerSecond = 1000f;

    [SerializeField]
    [Min(0f)]
    private float _ballRimEndDegreesPerSecond = 260f;

    [Header("Ball Motion")]
    [SerializeField]
    private Vector3 _ballRimLocalOffset = new Vector3(0.42f, 0.16f, 0f);

    [SerializeField]
    [Min(0f)]
    private float _dropBounceHeight = 0.14f;

    [SerializeField]
    [Min(0f)]
    private float _ballTumbleDegreesPerSecond = 720f;

    [SerializeField]
    [Min(0f)]
    private float _ballTumbleFalloff = 0.4f;

    [SerializeField]
    [Range(0f, 180f)]
    private float _minimumLeadAngleDegrees = 95f;

    [SerializeField]
    [Min(0f)]
    private float _extraFullTurns = 1f;

    private readonly Dictionary<int, WheelSlot> _slotLookup = new Dictionary<int, WheelSlot>();
    private Renderer[] _ballRenderers = Array.Empty<Renderer>();
    private Coroutine _spinRoutine;
    private bool _isSpinning;
    private float _currentWheelDegreesPerSecond;
    private float _currentBallDegreesPerSecond;

    public bool IsSpinning => _isSpinning;
    public float CurrentWheelDegreesPerSecond => _currentWheelDegreesPerSecond;
    public float MaxWheelDegreesPerSecond => Mathf.Max(_wheelStartupDegreesPerSecond, _wheelDropDegreesPerSecond, _wheelStopDegreesPerSecond);
    public float CurrentBallDegreesPerSecond => _currentBallDegreesPerSecond;
    public float MaxBallDegreesPerSecond => Mathf.Max(_ballRimStartDegreesPerSecond, _ballRimEndDegreesPerSecond);
    public event Action BallReleased;
    public event Action BallPocketLanded;

    private void Awake()
    {
        CacheReferencesIfNeeded();
        BuildSlotLookup();
        CacheBallRimOffsetFromScene();
        CacheBallVisuals();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferencesIfNeeded();
        CacheBallRimOffsetFromScene();

        if (_ballBounceCurve == null || _ballBounceCurve.length == 0)
        {
            _ballBounceCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, 0.9f),
                new Keyframe(0.55f, 0.28f),
                new Keyframe(0.82f, 0.06f),
                new Keyframe(1f, 0f));
        }

        if (_dropInwardCurve == null || _dropInwardCurve.length == 0)
        {
            _dropInwardCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.45f, 0.02f),
                new Keyframe(0.72f, 0.22f),
                new Keyframe(0.9f, 0.72f),
                new Keyframe(1f, 1f));
        }
    }
#endif

    /// <summary>
    /// Starts a deterministic wheel spin that lands the ball on the requested number.
    /// </summary>
    public void SpinToNumber(int targetNumber, Action onSpinComplete)
    {
        CacheReferencesIfNeeded();
        BuildSlotLookup();

        if (!ValidateSetup())
        {
            onSpinComplete?.Invoke();
            return;
        }

        if (!_slotLookup.TryGetValue(targetNumber, out WheelSlot targetSlot) || targetSlot == null)
        {
            Debug.LogWarning($"WheelController could not find a slot for number {targetNumber}.");
            onSpinComplete?.Invoke();
            return;
        }

        if (_spinRoutine != null)
        {
            StopCoroutine(_spinRoutine);
            _spinRoutine = null;
        }

        _spinRoutine = StartCoroutine(SpinRoutine(targetSlot, onSpinComplete));
    }

    private IEnumerator SpinRoutine(WheelSlot targetSlot, Action onSpinComplete)
    {
        _isSpinning = true;

        PrepareBallForSpin();
        SetBallVisible(false);
        _currentBallDegreesPerSecond = 0f;

        Vector3 orbitCenterWorldPosition = _ballRimPivot.position;

        float elapsedTime = 0f;

        while (elapsedTime < _ballAppearDelay)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / _ballAppearDelay);
            float wheelSpeed = Mathf.Lerp(
                _wheelStartupDegreesPerSecond * 0.82f,
                _wheelStartupDegreesPerSecond,
                EaseOutCubic(normalizedTime));

            _currentWheelDegreesPerSecond = wheelSpeed;
            RotateWheelClockwise(wheelSpeed * Time.deltaTime);
            yield return null;
        }

        SetBallVisible(true);
        BallReleased?.Invoke();

        Vector3 visibleBallOffset = _ballTransform.position - orbitCenterWorldPosition;
        float rimRadius = new Vector2(visibleBallOffset.x, visibleBallOffset.z).magnitude;
        float rimHeight = visibleBallOffset.y;
        float currentBallWorldAngle = Mathf.Atan2(visibleBallOffset.z, visibleBallOffset.x) * Mathf.Rad2Deg;
        float totalVisibleDuration = _rimTravelDuration + _dropDuration;
        Vector3 targetLocalPosition = _wheelTransform.InverseTransformPoint(targetSlot.transform.position);
        float projectedWheelRotationDegrees = EstimateWheelRotationDuringVisiblePhase(totalVisibleDuration);
        Vector3 projectedTargetWorldPosition = EvaluateProjectedSlotWorldPosition(targetLocalPosition, projectedWheelRotationDegrees);
        Vector3 projectedTargetOffset = projectedTargetWorldPosition - orbitCenterWorldPosition;
        float targetRadius = new Vector2(projectedTargetOffset.x, projectedTargetOffset.z).magnitude;
        float targetHeight = projectedTargetOffset.y;
        float targetWorldAngle = Mathf.Atan2(projectedTargetOffset.z, projectedTargetOffset.x) * Mathf.Rad2Deg;

        // We drive the visible ball angle by integrating a decelerating counter-rotation speed.
        // To preserve deterministic landing, we precompute how much total angular travel we need,
        // then scale the deceleration curve so its time integral matches that exact travel.
        float clockwiseTravelToTarget = Mathf.Repeat(currentBallWorldAngle - targetWorldAngle, 360f);
        int extraFullTurns = Mathf.Max(0, Mathf.RoundToInt(_extraFullTurns));

        while (clockwiseTravelToTarget < _minimumLeadAngleDegrees)
        {
            clockwiseTravelToTarget += 360f;
        }

        float requiredAngularTravel = clockwiseTravelToTarget + (extraFullTurns * 360f);
        float nominalAngularTravel = EstimateBallAngularTravel(totalVisibleDuration);
        float angularSpeedScale = nominalAngularTravel > 0.001f
            ? requiredAngularTravel / nominalAngularTravel
            : 1f;
        Vector3 tumbleAxisA = UnityEngine.Random.onUnitSphere.normalized;
        Vector3 tumbleAxisB = UnityEngine.Random.onUnitSphere.normalized;

        if (tumbleAxisA == Vector3.zero)
        {
            tumbleAxisA = Vector3.up;
        }

        if (tumbleAxisB == Vector3.zero)
        {
            tumbleAxisB = Vector3.right;
        }

        elapsedTime = 0f;

        while (elapsedTime < totalVisibleDuration)
        {
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            float overallNormalizedTime = Mathf.Clamp01(elapsedTime / totalVisibleDuration);
            float ballSpeedBlend = EaseOutCubic(overallNormalizedTime);
            float visibleCounterSpeed = Mathf.Lerp(
                _ballRimStartDegreesPerSecond,
                _ballRimEndDegreesPerSecond,
                ballSpeedBlend) * angularSpeedScale;
            float wheelSpeed = GetWheelSpeedAtElapsedTime(elapsedTime);

            _currentWheelDegreesPerSecond = wheelSpeed;
            _currentBallDegreesPerSecond = visibleCounterSpeed;
            RotateWheelClockwise(wheelSpeed * deltaTime);
            currentBallWorldAngle -= visibleCounterSpeed * deltaTime;

            float inwardNormalizedTime = elapsedTime <= _rimTravelDuration
                ? 0f
                : Mathf.Clamp01((elapsedTime - _rimTravelDuration) / _dropDuration);
            float inwardBlend = Mathf.Clamp01(_dropInwardCurve.Evaluate(inwardNormalizedTime));
            float currentRadius = Mathf.Lerp(rimRadius, targetRadius, inwardBlend);
            float currentHeight = Mathf.Lerp(rimHeight, targetHeight, inwardBlend);
            float bounceHeight = _dropBounceHeight * Mathf.Max(0f, _ballBounceCurve.Evaluate(inwardNormalizedTime));

            Vector3 worldPosition = EvaluateOrbitWorldPosition(
                orbitCenterWorldPosition,
                currentBallWorldAngle,
                currentRadius,
                currentHeight + bounceHeight);

            _ballTransform.position = worldPosition;

            float tumbleSpeed = Mathf.Lerp(
                _ballTumbleDegreesPerSecond,
                _ballTumbleDegreesPerSecond * _ballTumbleFalloff,
                overallNormalizedTime);

            _ballTransform.Rotate(tumbleAxisA, tumbleSpeed * deltaTime, Space.World);
            _ballTransform.Rotate(tumbleAxisB, tumbleSpeed * 0.55f * deltaTime, Space.Self);

            yield return null;
        }

        _ballTransform.position = targetSlot.transform.position;
        _ballTransform.rotation = Quaternion.identity;
        _currentWheelDegreesPerSecond = 0f;
        _currentBallDegreesPerSecond = 0f;
        BallPocketLanded?.Invoke();

        _isSpinning = false;
        _spinRoutine = null;
        onSpinComplete?.Invoke();
    }

    private Vector3 EvaluateOrbitWorldPosition(
        Vector3 orbitCenterWorldPosition,
        float worldAngleDegrees,
        float radius,
        float height)
    {
        float angleInRadians = worldAngleDegrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleInRadians) * radius,
            height,
            Mathf.Sin(angleInRadians) * radius);

        return orbitCenterWorldPosition + offset;
    }

    private float EstimateWheelRotationDuringVisiblePhase(float duration)
    {
        const int sampleCount = 256;

        if (duration <= 0f)
        {
            return 0f;
        }

        float totalRotationDegrees = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float sampleTimeA = (i / (float)sampleCount) * duration;
            float sampleTimeB = ((i + 1) / (float)sampleCount) * duration;
            float speedA = GetWheelSpeedAtElapsedTime(sampleTimeA);
            float speedB = GetWheelSpeedAtElapsedTime(sampleTimeB);
            float averageSpeed = (speedA + speedB) * 0.5f;

            totalRotationDegrees += averageSpeed * (duration / sampleCount);
        }

        return totalRotationDegrees;
    }

    private float EstimateBallAngularTravel(float duration)
    {
        const int sampleCount = 24;

        if (duration <= 0f)
        {
            return 0f;
        }

        float totalAngularTravel = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float sampleTimeA = i / (float)sampleCount;
            float sampleTimeB = (i + 1) / (float)sampleCount;
            float speedA = Mathf.Lerp(_ballRimStartDegreesPerSecond, _ballRimEndDegreesPerSecond, EaseOutCubic(sampleTimeA));
            float speedB = Mathf.Lerp(_ballRimStartDegreesPerSecond, _ballRimEndDegreesPerSecond, EaseOutCubic(sampleTimeB));
            float averageSpeed = (speedA + speedB) * 0.5f;

            totalAngularTravel += averageSpeed * (duration / sampleCount);
        }

        return totalAngularTravel;
    }

    private float GetWheelSpeedAtElapsedTime(float elapsedTime)
    {
        if (elapsedTime <= _rimTravelDuration)
        {
            return Mathf.Lerp(
                _wheelStartupDegreesPerSecond,
                _wheelDropDegreesPerSecond,
                EaseOutCubic(Mathf.Clamp01(elapsedTime / _rimTravelDuration)));
        }

        return Mathf.Lerp(
            _wheelDropDegreesPerSecond,
            _wheelStopDegreesPerSecond,
            EaseInCubic(Mathf.Clamp01((elapsedTime - _rimTravelDuration) / _dropDuration)));
    }

    private Vector3 EvaluateProjectedSlotWorldPosition(Vector3 targetLocalPosition, float additionalWheelRotationDegrees)
    {
        Quaternion projectedWheelRotation =
            _wheelTransform.rotation * Quaternion.AngleAxis(-additionalWheelRotationDegrees, Vector3.up);

        return _wheelTransform.position + (projectedWheelRotation * targetLocalPosition);
    }

    private void PrepareBallForSpin()
    {
        _ballRimPivot.localRotation = Quaternion.identity;
        _ballTransform.position = _ballRimPivot.TransformPoint(_ballRimLocalOffset);
        _ballTransform.rotation = Quaternion.identity;
    }

    private void RotateWheelClockwise(float degrees)
    {
        if (_wheelTransform != null)
        {
            _wheelTransform.Rotate(Vector3.up, -degrees, Space.Self);
        }
    }

    private bool ValidateSetup()
    {
        if (_wheelTransform == null)
        {
            Debug.LogWarning("WheelController is missing the wheel transform reference.");
            return false;
        }

        if (_ballTransform == null)
        {
            Debug.LogWarning("WheelController is missing the ball transform reference.");
            return false;
        }

        if (_ballRimPivot == null)
        {
            Debug.LogWarning("WheelController is missing the ball rim pivot reference.");
            return false;
        }

        return true;
    }

    private void CacheReferencesIfNeeded()
    {
        if (_wheelTransform == null)
        {
            _wheelTransform = transform;
        }

        if ((_ballTransform == null || !HasVisualRenderer(_ballTransform)) && _ballRimPivot != null)
        {
            Renderer ballRenderer = _ballRimPivot.GetComponentInChildren<Renderer>(true);

            if (ballRenderer != null)
            {
                _ballTransform = ballRenderer.transform;
            }
        }
    }

    private void CacheBallVisuals()
    {
        if (_ballTransform == null)
        {
            _ballRenderers = Array.Empty<Renderer>();
            return;
        }

        _ballRenderers = _ballTransform.GetComponentsInChildren<Renderer>(true);
    }

    private void CacheBallRimOffsetFromScene()
    {
        if (_ballTransform == null || _ballRimPivot == null)
        {
            return;
        }

        Vector3 localOffset = _ballRimPivot.InverseTransformPoint(_ballTransform.position);

        if (localOffset.sqrMagnitude > 0.0001f)
        {
            _ballRimLocalOffset = localOffset;
        }
    }

    private void BuildSlotLookup()
    {
        if ((_allSlots == null || _allSlots.Count == 0) && _wheelTransform != null)
        {
            WheelSlot[] hierarchySlots = _wheelTransform.GetComponentsInChildren<WheelSlot>(true);

            if (hierarchySlots.Length > 0)
            {
                _allSlots = new List<WheelSlot>(hierarchySlots);
            }
        }

        _slotLookup.Clear();

        if (_allSlots == null)
        {
            return;
        }

        for (int i = 0; i < _allSlots.Count; i++)
        {
            WheelSlot slot = _allSlots[i];

            if (slot == null)
            {
                continue;
            }

            _slotLookup[slot.Number] = slot;
        }
    }

    private float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private void SetBallVisible(bool isVisible)
    {
        if (_ballRenderers == null || _ballRenderers.Length == 0)
        {
            CacheBallVisuals();
        }

        for (int i = 0; i < _ballRenderers.Length; i++)
        {
            if (_ballRenderers[i] != null)
            {
                _ballRenderers[i].enabled = isVisible;
            }
        }
    }

    private bool HasVisualRenderer(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return false;
        }

        return targetTransform.GetComponent<Renderer>() != null;
    }
}

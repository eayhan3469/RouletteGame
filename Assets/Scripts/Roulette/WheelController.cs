using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    private float _rimTravelDuration = 1.35f;

    [SerializeField]
    [Min(0.1f)]
    private float _dropDuration = 3.6f;

    [Header("Wheel Motion")]
    [SerializeField]
    [Min(0f)]
    private float _continuousWheelDegreesPerSecond = 36f;

    [Header("Ball Speeds")]
    [SerializeField]
    [Min(0f)]
    private float _ballRimStartDegreesPerSecond = 1600f;

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
    private float _extraFullTurns = 6f;

    [SerializeField]
    [FormerlySerializedAs("_wheelSettleDuration")]
    [Min(0.1f)]
    private float _pocketSettleDuration = 1.05f;

    [SerializeField]
    [Min(0f)]
    private float _landedBallVisibleDuration = 0.65f;

    [SerializeField]
    [Min(0)]
    private int _pocketBounceCount = 4;

    [SerializeField]
    [Range(0f, 1f)]
    private float _pocketRattleStartNormalized = 0.8f;

    [SerializeField]
    [Min(0f)]
    private float _pocketBounceHeight = 0.14f;

    [SerializeField]
    [Min(0f)]
    private float _pocketBounceRadialAmplitude = 0.22f;

    [SerializeField]
    [Min(0f)]
    private float _pocketBounceAngularAmplitudeDegrees = 13f;

    private readonly Dictionary<int, WheelSlot> _slotLookup = new Dictionary<int, WheelSlot>();
    private Renderer[] _ballRenderers = Array.Empty<Renderer>();
    private Coroutine _spinRoutine;
    private Coroutine _hideLandedBallRoutine;
    private Transform _ballRestParent;
    private bool _hasCachedBallRestParent;
    private bool _isSpinning;
    private float _currentWheelDegreesPerSecond;
    private float _currentBallDegreesPerSecond;

    public bool IsSpinning => _isSpinning;
    public float CurrentWheelDegreesPerSecond => _currentWheelDegreesPerSecond;
    public float MaxWheelDegreesPerSecond => _continuousWheelDegreesPerSecond;
    public float CurrentBallDegreesPerSecond => _currentBallDegreesPerSecond;
    public float MaxBallDegreesPerSecond => Mathf.Max(_ballRimStartDegreesPerSecond, _ballRimEndDegreesPerSecond);
    public event Action BallReleased;
    public event Action BallPocketEntryStarted;
    public event Action<float> BallPocketBounced;
    public event Action BallPocketLanded;

    private void Awake()
    {
        CacheReferencesIfNeeded();
        BuildSlotLookup();
        CacheBallRimOffsetFromScene();
        CacheBallVisuals();
        CacheBallRestParent();
        SetBallVisible(false);
        _currentWheelDegreesPerSecond = _continuousWheelDegreesPerSecond;
    }

    private void Update()
    {
        if (_isSpinning)
        {
            return;
        }

        RotateWheelAtContinuousSpeed(Time.deltaTime);
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
    public bool SpinToNumber(int targetNumber, Action onSpinComplete)
    {
        CacheReferencesIfNeeded();
        BuildSlotLookup();

        if (!ValidateSetup())
        {
            return false;
        }

        if (!_slotLookup.TryGetValue(targetNumber, out WheelSlot targetSlot) || targetSlot == null)
        {
            Debug.LogError(
                $"WheelController on '{name}' could not start spin because no WheelSlot exists for target number {FormatRouletteNumber(targetNumber)} ({targetNumber}).");
            return false;
        }

        if (_spinRoutine != null)
        {
            StopCoroutine(_spinRoutine);
            _spinRoutine = null;
        }

        StopHideLandedBallRoutine();
        _spinRoutine = StartCoroutine(SpinRoutine(targetSlot, onSpinComplete));
        return true;
    }

    private IEnumerator SpinRoutine(WheelSlot targetSlot, Action onSpinComplete)
    {
        _isSpinning = true;

        PrepareBallForSpin();
        SetBallVisible(true);
        _currentBallDegreesPerSecond = 0f;

        Vector3 orbitCenterWorldPosition = _ballRimPivot.position;
        BallReleased?.Invoke();

        Vector3 visibleBallOffset = _ballTransform.position - orbitCenterWorldPosition;
        float rimRadius = new Vector2(visibleBallOffset.x, visibleBallOffset.z).magnitude;
        float rimHeight = visibleBallOffset.y;
        float currentBallWorldAngle = Mathf.Atan2(visibleBallOffset.z, visibleBallOffset.x) * Mathf.Rad2Deg;
        float pocketEntryDropDuration = _dropDuration * Mathf.Clamp01(_pocketRattleStartNormalized);
        float pocketRattleDuration = (_dropDuration - pocketEntryDropDuration) + _pocketSettleDuration;
        float travelDuration = _rimTravelDuration + pocketEntryDropDuration;
        Vector3 targetLocalPosition = _wheelTransform.InverseTransformPoint(targetSlot.transform.position);
        // Calculate exactly where the target slot will be after the travel duration
        // to ensure the ball correctly lands on the deterministically chosen number.
        float projectedWheelRotationDegrees = EstimateWheelRotationDuringVisiblePhase(travelDuration);
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
        float nominalAngularTravel = EstimateBallAngularTravel(travelDuration);
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

        float elapsedTime = 0f;

        // Execute the rim travel phase. We decelerate the ball speed along a quadratic curve
        // and physically blend its position inwards to simulate dropping into the wheel center.
        while (elapsedTime < travelDuration)
        {
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            float clampedElapsedTime = Mathf.Min(elapsedTime, travelDuration);
            float overallNormalizedTime = Mathf.Clamp01(clampedElapsedTime / travelDuration);
            float ballSpeedBlend = EaseOutQuadratic(overallNormalizedTime);
            float visibleCounterSpeed = Mathf.Lerp(
                _ballRimStartDegreesPerSecond,
                _ballRimEndDegreesPerSecond,
                ballSpeedBlend) * angularSpeedScale;
            float wheelSpeed = GetContinuousWheelSpeed();

            _currentWheelDegreesPerSecond = wheelSpeed;
            _currentBallDegreesPerSecond = visibleCounterSpeed;
            RotateWheelClockwise(wheelSpeed * deltaTime);
            currentBallWorldAngle -= visibleCounterSpeed * deltaTime;

            float inwardNormalizedTime = elapsedTime <= _rimTravelDuration
                ? 0f
                : Mathf.Clamp01((clampedElapsedTime - _rimTravelDuration) / Mathf.Max(0.001f, pocketEntryDropDuration));
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

        _currentBallDegreesPerSecond = 0f;
        BallPocketEntryStarted?.Invoke();

        Vector3 pocketTargetOffset = targetSlot.transform.position - orbitCenterWorldPosition;
        float pocketTargetRadius = new Vector2(pocketTargetOffset.x, pocketTargetOffset.z).magnitude;
        float pocketTargetAngle = Mathf.Atan2(pocketTargetOffset.z, pocketTargetOffset.x) * Mathf.Rad2Deg;
        InitializePocketBouncePath(
            Mathf.DeltaAngle(pocketTargetAngle, currentBallWorldAngle),
            0f,
            out float[] pocketAngularOffsets,
            out float[] pocketRadialOffsets,
            out float[] pocketArcHeights);

        float pocketSettleElapsedTime = 0f;
        int nextPocketBounceEventIndex = 0;

        // Execute the pocket rattle phase. The ball is now trapped in the target slot, 
        // procedural bouncing back and forth is applied before it comes to a complete rest.
        while (pocketSettleElapsedTime < pocketRattleDuration)
        {
            float deltaTime = Time.deltaTime;
            pocketSettleElapsedTime += deltaTime;
            float settleNormalizedTime = Mathf.Clamp01(pocketSettleElapsedTime / pocketRattleDuration);

            while (nextPocketBounceEventIndex < _pocketBounceCount &&
                   settleNormalizedTime >= GetPocketBouncePeakProgress(nextPocketBounceEventIndex))
            {
                float bounceIntensity = 1f - (nextPocketBounceEventIndex / (float)Mathf.Max(1, _pocketBounceCount));
                BallPocketBounced?.Invoke(bounceIntensity);
                nextPocketBounceEventIndex++;
            }

            float wheelSpeed = GetContinuousWheelSpeed();
            _currentWheelDegreesPerSecond = wheelSpeed;
            RotateWheelClockwise(wheelSpeed * deltaTime);

            Vector3 liveTargetOffset = targetSlot.transform.position - orbitCenterWorldPosition;
            float liveTargetRadius = new Vector2(liveTargetOffset.x, liveTargetOffset.z).magnitude;
            float liveTargetHeight = liveTargetOffset.y;
            float liveTargetAngle = Mathf.Atan2(liveTargetOffset.z, liveTargetOffset.x) * Mathf.Rad2Deg;

            EvaluatePocketRattleOffsets(
                settleNormalizedTime,
                pocketAngularOffsets,
                pocketRadialOffsets,
                pocketArcHeights,
                out float angularOffsetDegrees,
                out float radialOffset,
                out float verticalOffset);

            _ballTransform.position = EvaluateOrbitWorldPosition(
                orbitCenterWorldPosition,
                liveTargetAngle + angularOffsetDegrees,
                liveTargetRadius + radialOffset,
                liveTargetHeight + verticalOffset);

            float tumbleSpeed = Mathf.Lerp(
                _ballTumbleDegreesPerSecond * _ballTumbleFalloff,
                _ballTumbleDegreesPerSecond * 0.18f,
                settleNormalizedTime);

            _ballTransform.Rotate(tumbleAxisA, tumbleSpeed * deltaTime, Space.World);
            _ballTransform.Rotate(tumbleAxisB, tumbleSpeed * 0.35f * deltaTime, Space.Self);

            yield return null;
        }

        _ballTransform.position = targetSlot.transform.position;
        _ballTransform.rotation = Quaternion.identity;
        _ballTransform.SetParent(targetSlot.transform, true);
        _currentWheelDegreesPerSecond = _continuousWheelDegreesPerSecond;
        BallPocketLanded?.Invoke();
        _hideLandedBallRoutine = StartCoroutine(HideLandedBallAfterDelay());

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
        if (duration <= 0f)
        {
            return 0f;
        }

        return _continuousWheelDegreesPerSecond * duration;
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
            float speedA = Mathf.Lerp(_ballRimStartDegreesPerSecond, _ballRimEndDegreesPerSecond, EaseOutQuadratic(sampleTimeA));
            float speedB = Mathf.Lerp(_ballRimStartDegreesPerSecond, _ballRimEndDegreesPerSecond, EaseOutQuadratic(sampleTimeB));
            float averageSpeed = (speedA + speedB) * 0.5f;

            totalAngularTravel += averageSpeed * (duration / sampleCount);
        }

        return totalAngularTravel;
    }

    private float GetContinuousWheelSpeed()
    {
        return _continuousWheelDegreesPerSecond;
    }

    private void InitializePocketBouncePath(
        float initialAngularOffsetDegrees,
        float initialRadialOffset,
        out float[] angularOffsets,
        out float[] radialOffsets,
        out float[] arcHeights)
    {
        int nodeCount = Mathf.Max(2, _pocketBounceCount + 2);
        angularOffsets = new float[nodeCount];
        radialOffsets = new float[nodeCount];
        arcHeights = new float[nodeCount - 1];

        angularOffsets[0] = initialAngularOffsetDegrees;
        radialOffsets[0] = initialRadialOffset;

        for (int i = 1; i < nodeCount - 1; i++)
        {
            float decay = Mathf.Pow(0.55f, i - 1);
            angularOffsets[i] = UnityEngine.Random.Range(
                -_pocketBounceAngularAmplitudeDegrees,
                _pocketBounceAngularAmplitudeDegrees) * decay;
            radialOffsets[i] = UnityEngine.Random.Range(
                -_pocketBounceRadialAmplitude,
                _pocketBounceRadialAmplitude) * decay;
            arcHeights[i - 1] = UnityEngine.Random.Range(
                _pocketBounceHeight * 0.45f,
                _pocketBounceHeight) * decay;
        }

        angularOffsets[nodeCount - 1] = 0f;
        radialOffsets[nodeCount - 1] = 0f;
        arcHeights[nodeCount - 2] = _pocketBounceHeight * 0.18f;
    }

    private void EvaluatePocketRattleOffsets(
        float normalizedTime,
        float[] angularOffsets,
        float[] radialOffsets,
        float[] arcHeights,
        out float angularOffsetDegrees,
        out float radialOffset,
        out float verticalOffset)
    {
        if (angularOffsets == null || radialOffsets == null || arcHeights == null ||
            angularOffsets.Length < 2 || radialOffsets.Length < 2 || arcHeights.Length == 0)
        {
            angularOffsetDegrees = 0f;
            radialOffset = 0f;
            verticalOffset = 0f;
            return;
        }

        float segmentCount = angularOffsets.Length - 1;
        float segmentedTime = Mathf.Clamp01(normalizedTime) * segmentCount;
        int segmentIndex = Mathf.Min(arcHeights.Length - 1, Mathf.FloorToInt(segmentedTime));
        float localTime = Mathf.Clamp01(segmentedTime - segmentIndex);
        float smoothTime = Mathf.SmoothStep(0f, 1f, localTime);
        float arc = Mathf.Sin(localTime * Mathf.PI);

        angularOffsetDegrees = Mathf.Lerp(
            angularOffsets[segmentIndex],
            angularOffsets[segmentIndex + 1],
            smoothTime);
        radialOffset = Mathf.Lerp(
            radialOffsets[segmentIndex],
            radialOffsets[segmentIndex + 1],
            smoothTime);
        verticalOffset = arcHeights[segmentIndex] * arc;
    }

    private float GetPocketBouncePeakProgress(int bounceIndex)
    {
        if (_pocketBounceCount <= 0)
        {
            return 1f;
        }

        float normalizedIndex = (bounceIndex + 0.5f) / _pocketBounceCount;
        return Mathf.Clamp01(normalizedIndex);
    }

    private Vector3 EvaluateProjectedSlotWorldPosition(Vector3 targetLocalPosition, float additionalWheelRotationDegrees)
    {
        Quaternion projectedWheelRotation =
            _wheelTransform.rotation * Quaternion.AngleAxis(-additionalWheelRotationDegrees, Vector3.up);

        return _wheelTransform.position + (projectedWheelRotation * targetLocalPosition);
    }

    private void PrepareBallForSpin()
    {
        if (_ballTransform.parent != _ballRestParent)
        {
            _ballTransform.SetParent(_ballRestParent, true);
        }

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

    private void RotateWheelAtContinuousSpeed(float deltaTime)
    {
        float wheelSpeed = GetContinuousWheelSpeed();
        _currentWheelDegreesPerSecond = wheelSpeed;
        RotateWheelClockwise(wheelSpeed * deltaTime);
    }

    private IEnumerator HideLandedBallAfterDelay()
    {
        if (_landedBallVisibleDuration > 0f)
        {
            yield return new WaitForSeconds(_landedBallVisibleDuration);
        }

        SetBallVisible(false);
        _hideLandedBallRoutine = null;
    }

    private void StopHideLandedBallRoutine()
    {
        if (_hideLandedBallRoutine == null)
        {
            return;
        }

        StopCoroutine(_hideLandedBallRoutine);
        _hideLandedBallRoutine = null;
    }

    private bool ValidateSetup()
    {
        if (_wheelTransform == null)
        {
            Debug.LogError($"WheelController on '{name}' is missing the wheel transform reference.");
            return false;
        }

        if (_ballTransform == null)
        {
            Debug.LogError($"WheelController on '{name}' is missing the ball transform reference.");
            return false;
        }

        if (_ballRimPivot == null)
        {
            Debug.LogError($"WheelController on '{name}' is missing the ball rim pivot reference.");
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

        CacheBallRestParent();
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

    private void CacheBallRestParent()
    {
        if (_ballTransform == null || _hasCachedBallRestParent)
        {
            return;
        }

        _ballRestParent = _ballTransform.parent;
        _hasCachedBallRestParent = true;
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
        List<WheelSlot> sourceSlots = _allSlots;

        if (_wheelTransform != null)
        {
            WheelSlot[] hierarchySlots = _wheelTransform.GetComponentsInChildren<WheelSlot>(true);

            if (hierarchySlots.Length > 0)
            {
                _allSlots = new List<WheelSlot>(hierarchySlots);
                sourceSlots = _allSlots;
            }
        }

        _slotLookup.Clear();

        if (sourceSlots == null || sourceSlots.Count == 0)
        {
            Debug.LogError($"WheelController on '{name}' could not find any WheelSlot references.");
            return;
        }

        for (int i = 0; i < sourceSlots.Count; i++)
        {
            WheelSlot slot = sourceSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (_slotLookup.ContainsKey(slot.Number))
            {
                Debug.LogWarning(
                    $"WheelController on '{name}' found duplicate WheelSlot number {FormatRouletteNumber(slot.Number)} ({slot.Number}). Keeping the first slot and ignoring '{slot.name}'.");
                continue;
            }

            _slotLookup.Add(slot.Number, slot);
        }
    }

    private float EaseOutQuadratic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse);
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

    private string FormatRouletteNumber(int number)
    {
        return number == 37 ? "00" : number.ToString();
    }
}

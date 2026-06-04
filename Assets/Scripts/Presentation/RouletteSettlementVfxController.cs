using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates chip settlement after a round so chips visually move before table state is reset.
/// </summary>
[DisallowMultipleComponent]
public sealed class RouletteSettlementVfxController : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField]
    private Transform _winChipSpawnPoint;

    [SerializeField]
    private Transform _playerChipStackTarget;

    [SerializeField]
    private Transform _dealerChipCollectTarget;

    [Header("Win Chips")]
    [SerializeField]
    [Min(0f)]
    private float _winSettlementStartDelay = 1.2f;

    [SerializeField]
    [Min(1)]
    private int _minRewardChipCount = 6;

    [SerializeField]
    [Min(1)]
    private int _maxRewardChipCount = 18;

    [SerializeField]
    [Min(1f)]
    private float _winAmountForMaxRewardChips = 1500f;

    [SerializeField]
    [Min(0f)]
    private float _rewardChipStagger = 0.055f;

    [SerializeField]
    [Min(0.05f)]
    private float _rewardChipTravelDuration = 0.68f;

    [SerializeField]
    [Min(0f)]
    private float _rewardChipArcHeight = 2.2f;

    [SerializeField]
    [Min(1f)]
    private float _rewardStackPulseScale = 1.08f;

    [SerializeField]
    [Min(0.01f)]
    private float _rewardStackPulseDuration = 0.18f;

    [Header("Losing Chips")]
    [SerializeField]
    [Min(0f)]
    private float _loseChipStagger = 0.08f;

    [SerializeField]
    [Min(0.05f)]
    private float _loseChipTravelDuration = 0.62f;

    [SerializeField]
    [Min(0f)]
    private float _loseChipArcHeight = 1.25f;

    [SerializeField]
    [Min(0f)]
    private float _dealerTargetScatterRadius = 0.35f;

    [Header("Chip Rotation")]
    [SerializeField]
    [Min(0f)]
    private float _minChipSpinDegrees = 180f;

    [SerializeField]
    [Min(0f)]
    private float _maxChipSpinDegrees = 540f;

    private readonly List<Chip3D> _temporaryRewardChips = new List<Chip3D>();
    private readonly Dictionary<Transform, Coroutine> _stackPulseRoutines = new Dictionary<Transform, Coroutine>();
    private readonly Dictionary<Transform, Vector3> _stackBaseScales = new Dictionary<Transform, Vector3>();

    public IEnumerator PlaySettlement(
        float roundResult,
        float amountWon,
        IReadOnlyList<BetManager.PlacedBet> activeBets,
        ChipManager chipManager,
        RouletteAudioFeedbackController audioFeedbackController)
    {
        StopAndClear();

        if (roundResult > 0f)
        {
            yield return PlayWinSettlement(amountWon, chipManager, audioFeedbackController);
            yield break;
        }

        yield return PlayLoseSettlement(activeBets, audioFeedbackController);
    }

    public void StopAndClear()
    {
        StopAllCoroutines();

        for (int i = _temporaryRewardChips.Count - 1; i >= 0; i--)
        {
            Chip3D temporaryChip = _temporaryRewardChips[i];

            if (temporaryChip == null)
            {
                continue;
            }

            Destroy(temporaryChip.gameObject);
        }

        _temporaryRewardChips.Clear();

        foreach (KeyValuePair<Transform, Coroutine> stackPulseRoutine in _stackPulseRoutines)
        {
            if (stackPulseRoutine.Value != null)
            {
                StopCoroutine(stackPulseRoutine.Value);
            }

            if (stackPulseRoutine.Key != null && _stackBaseScales.TryGetValue(stackPulseRoutine.Key, out Vector3 baseScale))
            {
                stackPulseRoutine.Key.localScale = baseScale;
            }
        }

        _stackPulseRoutines.Clear();
        _stackBaseScales.Clear();
    }

    private IEnumerator PlayWinSettlement(
        float amountWon,
        ChipManager chipManager,
        RouletteAudioFeedbackController audioFeedbackController)
    {
        if (chipManager == null || amountWon <= 0f)
        {
            yield break;
        }

        int maxRewardChipCount = GetRewardChipCount(amountWon);
        List<int> rewardChipValues = chipManager.CreateRewardChipValueDistribution(amountWon, maxRewardChipCount);
        Vector3 spawnPosition = ResolveWinChipSpawnPosition(chipManager);

        if (rewardChipValues.Count == 0)
        {
            yield break;
        }

        if (_winSettlementStartDelay > 0f)
        {
            yield return new WaitForSeconds(_winSettlementStartDelay);
        }

        for (int i = 0; i < rewardChipValues.Count; i++)
        {
            int chipValue = rewardChipValues[i];
            Vector3 targetPosition = ResolvePlayerChipStackTarget(chipManager, chipValue, out Transform stackPulseTarget);
            Vector3 startPosition = spawnPosition + GetRandomHorizontalOffset(0.35f);
            Chip3D rewardChip = chipManager.SpawnVisualChip(chipValue, startPosition, transform);

            if (rewardChip != null)
            {
                _temporaryRewardChips.Add(rewardChip);
                audioFeedbackController?.PlaySettlementChipMove();
                StartCoroutine(AnimateChipRoutine(
                    rewardChip.transform,
                    startPosition,
                    targetPosition,
                    _rewardChipArcHeight,
                    _rewardChipTravelDuration,
                    true,
                    stackPulseTarget));
            }

            if (_rewardChipStagger > 0f)
            {
                yield return new WaitForSeconds(_rewardChipStagger);
            }
        }

        yield return new WaitForSeconds(_rewardChipTravelDuration);
    }

    private IEnumerator PlayLoseSettlement(
        IReadOnlyList<BetManager.PlacedBet> activeBets,
        RouletteAudioFeedbackController audioFeedbackController)
    {
        List<BetManager.PlacedBet> settlementBets = CreateUniqueSettlementBetList(activeBets);

        if (settlementBets.Count == 0)
        {
            yield break;
        }

        Vector3 dealerTargetPosition = ResolveDealerChipCollectTarget(settlementBets);

        for (int i = 0; i < settlementBets.Count; i++)
        {
            BetManager.PlacedBet placedBet = settlementBets[i];

            if (placedBet == null || placedBet.Chip == null)
            {
                continue;
            }

            Vector3 scatteredTarget = dealerTargetPosition + GetRandomHorizontalOffset(_dealerTargetScatterRadius);
            audioFeedbackController?.PlaySettlementChipMove();
            StartCoroutine(AnimateLosingChipRoutine(
                placedBet.Chip,
                placedBet.Spot,
                scatteredTarget));

            if (_loseChipStagger > 0f)
            {
                yield return new WaitForSeconds(_loseChipStagger);
            }
        }

        yield return new WaitForSeconds(_loseChipTravelDuration);
    }

    private IEnumerator AnimateLosingChipRoutine(Chip3D chip, BetSpot spot, Vector3 targetPosition)
    {
        if (chip == null)
        {
            yield break;
        }

        spot?.UnregisterChip(chip);
        chip.PrepareForSettlement();
        chip.transform.SetParent(transform, true);

        yield return AnimateChipRoutine(
            chip.transform,
            chip.transform.position,
            targetPosition,
            _loseChipArcHeight,
            _loseChipTravelDuration,
            true);
    }

    private IEnumerator AnimateChipRoutine(
        Transform chipTransform,
        Vector3 startPosition,
        Vector3 targetPosition,
        float arcHeight,
        float duration,
        bool destroyOnArrival,
        Transform arrivalPulseTarget = null)
    {
        if (chipTransform == null)
        {
            yield break;
        }

        float startYaw = chipTransform.rotation.eulerAngles.y;
        float targetYaw = startYaw + GetRandomFlatSpinDegrees();
        float elapsedTime = 0f;

        chipTransform.rotation = Quaternion.Euler(0f, startYaw, 0f);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float easedTime = EaseOutCubic(normalizedTime);
            Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, easedTime);
            float arc = Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;

            chipTransform.position = linearPosition + (Vector3.up * arc);
            chipTransform.rotation = Quaternion.Euler(0f, Mathf.Lerp(startYaw, targetYaw, easedTime), 0f);
            yield return null;
        }

        chipTransform.position = targetPosition;
        chipTransform.rotation = Quaternion.Euler(0f, targetYaw, 0f);

        if (destroyOnArrival && chipTransform != null)
        {
            Destroy(chipTransform.gameObject);
        }

        PulseStack(arrivalPulseTarget);
    }

    private int GetRewardChipCount(float amountWon)
    {
        float normalizedAmount = Mathf.Clamp01(amountWon / _winAmountForMaxRewardChips);
        return Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Lerp(_minRewardChipCount, _maxRewardChipCount, normalizedAmount)),
            _minRewardChipCount,
            _maxRewardChipCount);
    }

    private List<BetManager.PlacedBet> CreateUniqueSettlementBetList(IReadOnlyList<BetManager.PlacedBet> activeBets)
    {
        List<BetManager.PlacedBet> settlementBets = new List<BetManager.PlacedBet>();

        if (activeBets == null)
        {
            return settlementBets;
        }

        for (int i = 0; i < activeBets.Count; i++)
        {
            BetManager.PlacedBet placedBet = activeBets[i];

            if (placedBet == null || placedBet.Chip == null)
            {
                continue;
            }

            if (ContainsChip(settlementBets, placedBet.Chip))
            {
                continue;
            }

            settlementBets.Add(placedBet);
        }

        return settlementBets;
    }

    private bool ContainsChip(List<BetManager.PlacedBet> settlementBets, Chip3D chip)
    {
        for (int i = 0; i < settlementBets.Count; i++)
        {
            if (settlementBets[i] != null && settlementBets[i].Chip == chip)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 ResolveWinChipSpawnPosition(ChipManager chipManager)
    {
        if (_winChipSpawnPoint != null)
        {
            return _winChipSpawnPoint.position;
        }

        return ResolvePlayerChipStackTarget(chipManager) + new Vector3(0f, 4f, 2f);
    }

    private Vector3 ResolvePlayerChipStackTarget(ChipManager chipManager, int chipValue, out Transform stackPulseTarget)
    {
        if (chipManager != null && chipManager.TryGetChipStackTarget(chipValue, out Vector3 chipStackTargetPosition, out stackPulseTarget))
        {
            return chipStackTargetPosition;
        }

        if (_playerChipStackTarget != null)
        {
            stackPulseTarget = _playerChipStackTarget;
            return _playerChipStackTarget.position;
        }

        if (chipManager != null && chipManager.TryGetChipStackTarget(out Vector3 targetPosition))
        {
            stackPulseTarget = null;
            return targetPosition;
        }

        stackPulseTarget = null;
        return new Vector3(21f, 1.8f, -7.5f);
    }

    private Vector3 ResolvePlayerChipStackTarget(ChipManager chipManager)
    {
        return ResolvePlayerChipStackTarget(chipManager, 0, out _);
    }

    private Vector3 ResolveDealerChipCollectTarget(List<BetManager.PlacedBet> settlementBets)
    {
        if (_dealerChipCollectTarget != null)
        {
            return _dealerChipCollectTarget.position;
        }

        Vector3 averageBetPosition = Vector3.zero;
        int betCount = 0;

        for (int i = 0; i < settlementBets.Count; i++)
        {
            BetManager.PlacedBet placedBet = settlementBets[i];

            if (placedBet == null || placedBet.Chip == null)
            {
                continue;
            }

            averageBetPosition += placedBet.Chip.transform.position;
            betCount++;
        }

        if (betCount <= 0)
        {
            return new Vector3(21f, 2f, 7.5f);
        }

        averageBetPosition /= betCount;
        return averageBetPosition + new Vector3(0f, 0.9f, 5.5f);
    }

    private Vector3 GetRandomHorizontalOffset(float radius)
    {
        if (radius <= 0f)
        {
            return Vector3.zero;
        }

        Vector2 offset = Random.insideUnitCircle * radius;
        return new Vector3(offset.x, 0f, offset.y);
    }

    private float GetRandomFlatSpinDegrees()
    {
        float maxSpinDegrees = Mathf.Max(_minChipSpinDegrees, _maxChipSpinDegrees);
        float spinDirection = Random.value < 0.5f ? -1f : 1f;
        return Random.Range(_minChipSpinDegrees, maxSpinDegrees) * spinDirection;
    }

    private void PulseStack(Transform stackTransform)
    {
        if (stackTransform == null)
        {
            return;
        }

        if (!_stackBaseScales.TryGetValue(stackTransform, out Vector3 baseScale))
        {
            baseScale = stackTransform.localScale;
            _stackBaseScales[stackTransform] = baseScale;
        }

        if (_stackPulseRoutines.TryGetValue(stackTransform, out Coroutine activeRoutine) && activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            stackTransform.localScale = baseScale;
        }

        _stackPulseRoutines[stackTransform] = StartCoroutine(PulseStackRoutine(stackTransform, baseScale));
    }

    private IEnumerator PulseStackRoutine(Transform stackTransform, Vector3 baseScale)
    {
        Vector3 pulseScale = baseScale * _rewardStackPulseScale;
        float halfDuration = _rewardStackPulseDuration * 0.5f;

        yield return ScaleTransformRoutine(stackTransform, baseScale, pulseScale, halfDuration);
        yield return ScaleTransformRoutine(stackTransform, pulseScale, baseScale, halfDuration);

        if (stackTransform != null)
        {
            stackTransform.localScale = baseScale;
            _stackPulseRoutines.Remove(stackTransform);
        }
    }

    private IEnumerator ScaleTransformRoutine(Transform targetTransform, Vector3 startScale, Vector3 targetScale, float duration)
    {
        if (targetTransform == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            targetTransform.localScale = targetScale;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float easedTime = EaseOutCubic(normalizedTime);
            targetTransform.localScale = Vector3.Lerp(startScale, targetScale, easedTime);
            yield return null;
        }

        targetTransform.localScale = targetScale;
    }

    private float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }
}

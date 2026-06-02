using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculates how a balance should be represented as physical chips and
/// spawns stacked chip piles into the tray slots assigned in the inspector.
/// </summary>
public sealed class ChipManager : MonoBehaviour
{
    [Serializable]
    private sealed class ChipTraySlot
    {
        [Min(1)]
        public int ChipValue = 5;

        [Range(0f, 1f)]
        public float DistributionWeight = 0.2f;

        [Min(1)]
        public int MaxVisibleChipCount = 10;

        public Transform TraySlot = null;
    }

    [Serializable]
    private sealed class ChipVisualDefinition
    {
        [Min(1)]
        public int ChipValue = 5;

        public Color BodyColor = Color.white;

        public Color StripeColor = Color.black;

        public Color TextColor = Color.black;
    }

    private readonly struct ChipStackInstruction
    {
        public ChipStackInstruction(int chipValue, int chipCount, int maxVisibleChipCount, Transform traySlot)
        {
            ChipValue = chipValue;
            ChipCount = chipCount;
            MaxVisibleChipCount = maxVisibleChipCount;
            TraySlot = traySlot;
        }

        public int ChipValue { get; }
        public int ChipCount { get; }
        public int MaxVisibleChipCount { get; }
        public Transform TraySlot { get; }
    }

    private sealed class TrayStackState
    {
        public int ChipValue;
        public int HiddenReserveCount;
        public int MaxVisibleChipCount;
        public Transform TraySlot;
        public ChipVisualDefinition VisualDefinition;
        public readonly List<Chip3D> VisibleChips = new List<Chip3D>();
    }

    [Header("Prefab")]
    [SerializeField]
    private Chip3D _chipPrefab;

    [Header("Tray Setup")]
    [SerializeField]
    private List<ChipTraySlot> _chipTraySlots = new List<ChipTraySlot>();

    [Header("Visual Setup")]
    [SerializeField]
    private List<ChipVisualDefinition> _chipVisualDefinitions = new List<ChipVisualDefinition>();

    [Header("Stacking")]
    [SerializeField]
    [Min(0.001f)]
    private float _chipThickness = 0.02f;

    [SerializeField]
    private bool _clearExistingStacksBeforeSpawn = true;

    private readonly List<Chip3D> _spawnedChips = new List<Chip3D>();
    private readonly Dictionary<Transform, TrayStackState> _trayStackStates = new Dictionary<Transform, TrayStackState>();

    /// <summary>
    /// Breaks the balance into chip denominations starting from the highest value
    /// and then spawns the resulting stacks into the configured tray slots.
    /// </summary>
    public void DistributeBalanceToChips(int balance)
    {
        if (balance < 0)
        {
            Debug.LogError("Chip distribution failed because balance cannot be negative.");
            return;
        }

        if (_chipPrefab == null)
        {
            Debug.LogError("Chip distribution failed because no chip prefab is assigned.");
            return;
        }

        List<ChipStackInstruction> instructions = CalculateChipDistribution(balance);
        SpawnAndStackChips(instructions);
    }

    public void ClearTrayChips()
    {
        ClearSpawnedChips();
    }

    public void ConsumeTrayChip(Chip3D chip, Transform traySlot)
    {
        if (chip == null || traySlot == null)
        {
            return;
        }

        if (!_trayStackStates.TryGetValue(traySlot, out TrayStackState trayStackState))
        {
            return;
        }

        trayStackState.VisibleChips.Remove(chip);
        _spawnedChips.Remove(chip);

        if (trayStackState.HiddenReserveCount > 0 && trayStackState.VisibleChips.Count < trayStackState.MaxVisibleChipCount)
        {
            SpawnVisibleChip(trayStackState);
            trayStackState.HiddenReserveCount--;
        }

        RestackVisibleChips(trayStackState);
    }

    public void ReturnChipToTray(int chipValue)
    {
        TrayStackState trayStackState = GetTrayStackStateForChipValue(chipValue);

        if (trayStackState == null)
        {
            Debug.LogWarning($"ChipManager could not return chip value {chipValue} because no tray stack state was found.");
            return;
        }

        if (trayStackState.VisibleChips.Count < trayStackState.MaxVisibleChipCount)
        {
            SpawnVisibleChip(trayStackState);
            RestackVisibleChips(trayStackState);
            return;
        }

        trayStackState.HiddenReserveCount++;
    }

    public Chip3D SpawnTableChip(int chipValue, BetSpot betSpot)
    {
        if (_chipPrefab == null)
        {
            Debug.LogError("ChipManager could not spawn a table chip because no chip prefab is assigned.");
            return null;
        }

        if (betSpot == null)
        {
            Debug.LogError("ChipManager could not spawn a table chip because the target BetSpot is missing.");
            return null;
        }

        ChipVisualDefinition visualDefinition = GetChipVisualDefinition(chipValue);
        Chip3D spawnedChip = Instantiate(_chipPrefab, betSpot.transform);
        spawnedChip.Initialize(
            chipValue,
            visualDefinition.BodyColor,
            visualDefinition.StripeColor,
            visualDefinition.TextColor);
        spawnedChip.AssignTraySource(this, null);
        spawnedChip.transform.position = betSpot.GetNextDropPosition();
        spawnedChip.transform.rotation = Quaternion.identity;
        spawnedChip.transform.localScale = Vector3.one;
        spawnedChip.MarkPlacedOnBetSpot(betSpot);
        betSpot.RegisterChip(spawnedChip);
        return spawnedChip;
    }

    public Chip3D SpawnVisualChip(int chipValue, Vector3 worldPosition, Transform parent)
    {
        if (_chipPrefab == null)
        {
            Debug.LogError("ChipManager could not spawn a visual chip because no chip prefab is assigned.");
            return null;
        }

        ChipVisualDefinition visualDefinition = GetChipVisualDefinition(chipValue);
        Chip3D spawnedChip = Instantiate(_chipPrefab, worldPosition, Quaternion.identity, parent);
        spawnedChip.Initialize(
            chipValue,
            visualDefinition.BodyColor,
            visualDefinition.StripeColor,
            visualDefinition.TextColor);
        spawnedChip.AssignTraySource(null, null);
        spawnedChip.PrepareForSettlement();
        return spawnedChip;
    }

    public int GetRepresentativeChipValue(float amount)
    {
        List<ChipTraySlot> orderedSlots = GetOrderedTraySlots();

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot != null && amount >= chipTraySlot.ChipValue)
            {
                return chipTraySlot.ChipValue;
            }
        }

        return orderedSlots.Count > 0 && orderedSlots[orderedSlots.Count - 1] != null
            ? orderedSlots[orderedSlots.Count - 1].ChipValue
            : 5;
    }

    public List<int> CreateRewardChipValueDistribution(float amount, int maxVisualChipCount)
    {
        int remainingAmount = Mathf.RoundToInt(amount);
        List<int> rewardChipValues = new List<int>();

        if (remainingAmount <= 0)
        {
            return rewardChipValues;
        }

        List<ChipTraySlot> orderedSlots = GetOrderedTraySlots();

        if (orderedSlots.Count == 0)
        {
            rewardChipValues.Add(5);
            return rewardChipValues;
        }

        int maxChipCount = Mathf.Max(1, maxVisualChipCount);
        int primaryMaxChipValue = GetHighestAffordableChipValue(orderedSlots, Mathf.Max(1, remainingAmount / 2));

        if (primaryMaxChipValue <= 0)
        {
            primaryMaxChipValue = GetHighestAffordableChipValue(orderedSlots, remainingAmount);
        }

        AddLimitedRewardChips(orderedSlots, primaryMaxChipValue, maxChipCount, rewardChipValues, ref remainingAmount);
        AddGreedyRewardChips(orderedSlots, maxChipCount, rewardChipValues, ref remainingAmount);

        if (remainingAmount > 0 && rewardChipValues.Count == 0)
        {
            rewardChipValues.Add(GetRepresentativeChipValue(remainingAmount));
        }

        ShuffleRewardChipValues(rewardChipValues);
        return rewardChipValues;
    }

    public bool TryGetChipStackTarget(out Vector3 targetPosition)
    {
        Vector3 totalPosition = Vector3.zero;
        int validSlotCount = 0;

        for (int i = 0; i < _chipTraySlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = _chipTraySlots[i];

            if (chipTraySlot == null || chipTraySlot.TraySlot == null)
            {
                continue;
            }

            totalPosition += chipTraySlot.TraySlot.position;
            validSlotCount++;
        }

        if (validSlotCount <= 0)
        {
            targetPosition = default;
            return false;
        }

        targetPosition = (totalPosition / validSlotCount) + (Vector3.up * 0.5f);
        return true;
    }

    public bool TryGetChipStackTarget(int chipValue, out Vector3 targetPosition, out Transform stackTransform)
    {
        TrayStackState trayStackState = GetTrayStackStateForChipValue(chipValue);

        if (trayStackState != null && trayStackState.TraySlot != null)
        {
            stackTransform = trayStackState.TraySlot;
            targetPosition = GetTraySlotTargetPosition(trayStackState.TraySlot);
            return true;
        }

        for (int i = 0; i < _chipTraySlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = _chipTraySlots[i];

            if (chipTraySlot == null || chipTraySlot.TraySlot == null || chipTraySlot.ChipValue != chipValue)
            {
                continue;
            }

            stackTransform = chipTraySlot.TraySlot;
            targetPosition = GetTraySlotTargetPosition(chipTraySlot.TraySlot);
            return true;
        }

        stackTransform = null;
        targetPosition = default;
        return false;
    }

    private List<ChipStackInstruction> CalculateChipDistribution(int balance)
    {
        List<ChipStackInstruction> instructions = new List<ChipStackInstruction>();
        List<ChipTraySlot> orderedSlots = GetOrderedTraySlots();

        if (orderedSlots.Count == 0)
        {
            Debug.LogWarning("Chip distribution failed because no chip tray slots are configured.");
            return instructions;
        }

        Dictionary<int, ChipStackInstruction> instructionMap = CreateWeightedDistribution(balance, orderedSlots);
        int distributedAmount = GetDistributedAmount(instructionMap);
        int remainingBalance = balance - distributedAmount;

        AddGreedyRemainder(remainingBalance, orderedSlots, instructionMap);

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot.TraySlot == null)
            {
                Debug.LogWarning($"Chip value {chipTraySlot.ChipValue} does not have a tray slot assigned.");
                continue;
            }

            if (!instructionMap.TryGetValue(chipTraySlot.ChipValue, out ChipStackInstruction instruction) || instruction.ChipCount <= 0)
            {
                continue;
            }

            instructions.Add(instruction);
        }

        return instructions;
    }

    private void AddLimitedRewardChips(
        List<ChipTraySlot> orderedSlots,
        int primaryMaxChipValue,
        int maxChipCount,
        List<int> rewardChipValues,
        ref int remainingAmount)
    {
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot == null ||
                chipTraySlot.ChipValue <= 0 ||
                chipTraySlot.ChipValue > primaryMaxChipValue)
            {
                continue;
            }

            int addedForValue = 0;

            while (remainingAmount >= chipTraySlot.ChipValue &&
                   addedForValue < 2 &&
                   rewardChipValues.Count < maxChipCount)
            {
                rewardChipValues.Add(chipTraySlot.ChipValue);
                remainingAmount -= chipTraySlot.ChipValue;
                addedForValue++;
            }
        }
    }

    private void AddGreedyRewardChips(
        List<ChipTraySlot> orderedSlots,
        int maxChipCount,
        List<int> rewardChipValues,
        ref int remainingAmount)
    {
        while (remainingAmount > 0 && rewardChipValues.Count < maxChipCount)
        {
            int chipValue = GetHighestAffordableChipValue(orderedSlots, remainingAmount);

            if (chipValue <= 0)
            {
                break;
            }

            rewardChipValues.Add(chipValue);
            remainingAmount -= chipValue;
        }
    }

    private int GetHighestAffordableChipValue(List<ChipTraySlot> orderedSlots, int amount)
    {
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot != null && chipTraySlot.ChipValue > 0 && chipTraySlot.ChipValue <= amount)
            {
                return chipTraySlot.ChipValue;
            }
        }

        return 0;
    }

    private void ShuffleRewardChipValues(List<int> rewardChipValues)
    {
        for (int i = rewardChipValues.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            int value = rewardChipValues[i];
            rewardChipValues[i] = rewardChipValues[swapIndex];
            rewardChipValues[swapIndex] = value;
        }
    }

    private void SpawnAndStackChips(List<ChipStackInstruction> instructions)
    {
        if (_clearExistingStacksBeforeSpawn)
        {
            ClearSpawnedChips();
        }

        for (int i = 0; i < instructions.Count; i++)
        {
            ChipStackInstruction instruction = instructions[i];
            SpawnStack(instruction);
        }
    }

    private void SpawnStack(ChipStackInstruction instruction)
    {
        int visibleChipCount = Mathf.Min(instruction.ChipCount, instruction.MaxVisibleChipCount);

        if (visibleChipCount <= 0)
        {
            return;
        }

        TrayStackState trayStackState = new TrayStackState
        {
            ChipValue = instruction.ChipValue,
            HiddenReserveCount = Mathf.Max(0, instruction.ChipCount - visibleChipCount),
            MaxVisibleChipCount = Mathf.Max(1, instruction.MaxVisibleChipCount),
            TraySlot = instruction.TraySlot,
            VisualDefinition = GetChipVisualDefinition(instruction.ChipValue)
        };

        _trayStackStates[instruction.TraySlot] = trayStackState;

        for (int chipIndex = 0; chipIndex < visibleChipCount; chipIndex++)
        {
            SpawnVisibleChip(trayStackState);
        }

        RestackVisibleChips(trayStackState);
    }

    private void ClearSpawnedChips()
    {
        _trayStackStates.Clear();

        for (int i = _spawnedChips.Count - 1; i >= 0; i--)
        {
            if (_spawnedChips[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(_spawnedChips[i].gameObject);
            }
            else
            {
                DestroyImmediate(_spawnedChips[i].gameObject);
            }
        }

        _spawnedChips.Clear();
    }

    private List<ChipTraySlot> GetOrderedTraySlots()
    {
        List<ChipTraySlot> orderedSlots = new List<ChipTraySlot>(_chipTraySlots);

        orderedSlots.Sort((left, right) =>
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return right.ChipValue.CompareTo(left.ChipValue);
        });

        return orderedSlots;
    }

    private Dictionary<int, ChipStackInstruction> CreateWeightedDistribution(int balance, List<ChipTraySlot> orderedSlots)
    {
        Dictionary<int, ChipStackInstruction> instructionMap = new Dictionary<int, ChipStackInstruction>();
        float totalWeight = 0f;

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot != null && chipTraySlot.DistributionWeight > 0f)
            {
                totalWeight += chipTraySlot.DistributionWeight;
            }
        }

        if (totalWeight <= 0f)
        {
            return instructionMap;
        }

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot == null || chipTraySlot.TraySlot == null || chipTraySlot.DistributionWeight <= 0f)
            {
                continue;
            }

            float share = chipTraySlot.DistributionWeight / totalWeight;
            int targetAmount = Mathf.FloorToInt(balance * share);
            int chipCount = targetAmount / chipTraySlot.ChipValue;

            if (chipCount <= 0)
            {
                continue;
            }

            instructionMap[chipTraySlot.ChipValue] = new ChipStackInstruction(
                chipTraySlot.ChipValue,
                chipCount,
                chipTraySlot.MaxVisibleChipCount,
                chipTraySlot.TraySlot);
        }

        return instructionMap;
    }

    private int GetDistributedAmount(Dictionary<int, ChipStackInstruction> instructionMap)
    {
        int distributedAmount = 0;

        foreach (ChipStackInstruction instruction in instructionMap.Values)
        {
            distributedAmount += instruction.ChipValue * instruction.ChipCount;
        }

        return distributedAmount;
    }

    private void AddGreedyRemainder(int remainingBalance, List<ChipTraySlot> orderedSlots, Dictionary<int, ChipStackInstruction> instructionMap)
    {
        int unresolvedBalance = remainingBalance;

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot == null || chipTraySlot.TraySlot == null || chipTraySlot.ChipValue <= 0)
            {
                continue;
            }

            int additionalChipCount = unresolvedBalance / chipTraySlot.ChipValue;

            if (additionalChipCount <= 0)
            {
                continue;
            }

            if (instructionMap.TryGetValue(chipTraySlot.ChipValue, out ChipStackInstruction existingInstruction))
            {
                instructionMap[chipTraySlot.ChipValue] = new ChipStackInstruction(
                    chipTraySlot.ChipValue,
                    existingInstruction.ChipCount + additionalChipCount,
                    chipTraySlot.MaxVisibleChipCount,
                    chipTraySlot.TraySlot);
            }
            else
            {
                instructionMap[chipTraySlot.ChipValue] = new ChipStackInstruction(
                    chipTraySlot.ChipValue,
                    additionalChipCount,
                    chipTraySlot.MaxVisibleChipCount,
                    chipTraySlot.TraySlot);
            }

            unresolvedBalance -= additionalChipCount * chipTraySlot.ChipValue;
        }

        if (unresolvedBalance > 0)
        {
            Debug.LogWarning($"Balance could not be represented exactly. Remaining amount: {unresolvedBalance}");
        }
    }

    private ChipVisualDefinition GetChipVisualDefinition(int chipValue)
    {
        for (int i = 0; i < _chipVisualDefinitions.Count; i++)
        {
            ChipVisualDefinition chipVisualDefinition = _chipVisualDefinitions[i];

            if (chipVisualDefinition != null && chipVisualDefinition.ChipValue == chipValue)
            {
                return chipVisualDefinition;
            }
        }

        Debug.LogWarning($"No chip visual definition found for chip value {chipValue}. Using fallback colors.");
        return new ChipVisualDefinition
        {
            ChipValue = chipValue,
            BodyColor = Color.white,
            StripeColor = Color.black,
            TextColor = Color.black
        };
    }

    private TrayStackState GetTrayStackStateForChipValue(int chipValue)
    {
        foreach (TrayStackState trayStackState in _trayStackStates.Values)
        {
            if (trayStackState != null && trayStackState.ChipValue == chipValue)
            {
                return trayStackState;
            }
        }

        return null;
    }

    private Vector3 GetTraySlotTargetPosition(Transform traySlot)
    {
        return traySlot.position + (Vector3.up * 0.5f);
    }

    private void SpawnVisibleChip(TrayStackState trayStackState)
    {
        if (trayStackState == null || trayStackState.TraySlot == null)
        {
            return;
        }

        Chip3D spawnedChip = Instantiate(_chipPrefab, trayStackState.TraySlot);
        spawnedChip.Initialize(
            trayStackState.ChipValue,
            trayStackState.VisualDefinition.BodyColor,
            trayStackState.VisualDefinition.StripeColor,
            trayStackState.VisualDefinition.TextColor);
        spawnedChip.AssignTraySource(this, trayStackState.TraySlot);

        _spawnedChips.Add(spawnedChip);
        trayStackState.VisibleChips.Add(spawnedChip);
    }

    private void RestackVisibleChips(TrayStackState trayStackState)
    {
        if (trayStackState == null)
        {
            return;
        }

        for (int i = trayStackState.VisibleChips.Count - 1; i >= 0; i--)
        {
            if (trayStackState.VisibleChips[i] != null)
            {
                continue;
            }

            trayStackState.VisibleChips.RemoveAt(i);
        }

        for (int chipIndex = 0; chipIndex < trayStackState.VisibleChips.Count; chipIndex++)
        {
            Chip3D visibleChip = trayStackState.VisibleChips[chipIndex];

            if (visibleChip == null)
            {
                continue;
            }

            Transform chipTransform = visibleChip.transform;
            chipTransform.SetParent(trayStackState.TraySlot, false);
            chipTransform.localPosition = Vector3.up * (_chipThickness * chipIndex);
            chipTransform.localRotation = Quaternion.identity;
        }
    }
}

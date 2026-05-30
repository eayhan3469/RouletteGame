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
        public ChipStackInstruction(int chipValue, int chipCount, Transform traySlot)
        {
            ChipValue = chipValue;
            ChipCount = chipCount;
            TraySlot = traySlot;
        }

        public int ChipValue { get; }
        public int ChipCount { get; }
        public Transform TraySlot { get; }
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
        ChipVisualDefinition chipVisual = GetChipVisualDefinition(instruction.ChipValue);

        for (int chipIndex = 0; chipIndex < instruction.ChipCount; chipIndex++)
        {
            Chip3D spawnedChip = Instantiate(_chipPrefab, instruction.TraySlot);
            spawnedChip.Initialize(
                instruction.ChipValue,
                chipVisual.BodyColor,
                chipVisual.StripeColor,
                chipVisual.TextColor);

            Transform chipTransform = spawnedChip.transform;
            chipTransform.localPosition = Vector3.up * (_chipThickness * chipIndex);
            chipTransform.localRotation = Quaternion.identity;

            _spawnedChips.Add(spawnedChip);
        }
    }

    private void ClearSpawnedChips()
    {
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

            instructionMap[chipTraySlot.ChipValue] = new ChipStackInstruction(chipTraySlot.ChipValue, chipCount, chipTraySlot.TraySlot);
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
                    chipTraySlot.TraySlot);
            }
            else
            {
                instructionMap[chipTraySlot.ChipValue] = new ChipStackInstruction(
                    chipTraySlot.ChipValue,
                    additionalChipCount,
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
}

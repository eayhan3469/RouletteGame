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

        public Transform TraySlot = null;
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
        int remainingBalance = balance;

        if (orderedSlots.Count == 0)
        {
            Debug.LogWarning("Chip distribution failed because no chip tray slots are configured.");
            return instructions;
        }

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            ChipTraySlot chipTraySlot = orderedSlots[i];

            if (chipTraySlot.TraySlot == null)
            {
                Debug.LogWarning($"Chip value {chipTraySlot.ChipValue} does not have a tray slot assigned.");
                continue;
            }

            int chipCount = remainingBalance / chipTraySlot.ChipValue;

            if (chipCount <= 0)
            {
                continue;
            }

            instructions.Add(new ChipStackInstruction(chipTraySlot.ChipValue, chipCount, chipTraySlot.TraySlot));
            remainingBalance -= chipCount * chipTraySlot.ChipValue;
        }

        if (remainingBalance > 0)
        {
            Debug.LogWarning($"Balance could not be represented exactly. Remaining amount: {remainingBalance}");
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
        for (int chipIndex = 0; chipIndex < instruction.ChipCount; chipIndex++)
        {
            Chip3D spawnedChip = Instantiate(_chipPrefab, instruction.TraySlot);
            spawnedChip.Initialize(instruction.ChipValue);

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
}

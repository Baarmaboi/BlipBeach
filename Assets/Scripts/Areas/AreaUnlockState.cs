using System;
using System.Collections.Generic;

/// <summary>
/// Remembers which star gates are open during the current 3-day cycle.
/// Cleared only on Day 3 → Day 1 wrap (not on Day 1→2 or 2→3).
/// Does not spend Stars — unlocks only mean "already opened this cycle".
/// </summary>
public static class AreaUnlockState
{
    private static readonly HashSet<string> unlockedGateIds = new HashSet<string>(StringComparer.Ordinal);

    public static event Action OnChanged;

    public static bool IsUnlocked(string gateId)
    {
        return !string.IsNullOrEmpty(gateId) && unlockedGateIds.Contains(gateId);
    }

    public static void Unlock(string gateId)
    {
        if (string.IsNullOrEmpty(gateId))
        {
            return;
        }

        if (!unlockedGateIds.Add(gateId))
        {
            return;
        }

        OnChanged?.Invoke();
    }

    public static void ClearAll()
    {
        if (unlockedGateIds.Count == 0)
        {
            return;
        }

        unlockedGateIds.Clear();
        OnChanged?.Invoke();
    }
}

using System;
using UnityEngine;

/// <summary>
/// Detector depth modules and active scan band. Persists across days (same as cash).
/// Put on GameSystems next to PlayerInventory.
/// </summary>
public class DetectorProgress : MonoBehaviour
{
    public static DetectorProgress Instance { get; private set; }

    [Header("Shop prices")]
    [SerializeField] private int depthModule1Price = 50;
    [SerializeField] private int depthModule2Price = 120;

    private int maxUnlockedDepth = 1;
    private DetectorScanMode activeMode = DetectorScanMode.Band1;

    public int MaxUnlockedDepth => maxUnlockedDepth;
    public DetectorScanMode ActiveMode => activeMode;
    public bool HasDepth2Module => maxUnlockedDepth >= 2;
    public bool HasDepth3Module => maxUnlockedDepth >= 3;
    public int DepthModule1Price => depthModule1Price;
    public int DepthModule2Price => depthModule2Price;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        maxUnlockedDepth = 1;
        activeMode = DetectorScanMode.Band1;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool OwnsBand(int depth)
    {
        return depth >= 1 && depth <= maxUnlockedDepth;
    }

    public bool OwnsBand(DepthLevel depth)
    {
        return OwnsBand((int)depth);
    }

    public bool CanSelect(DetectorScanMode mode)
    {
        switch (mode)
        {
            case DetectorScanMode.Band1:
                return OwnsBand(1);
            case DetectorScanMode.Band2:
                return OwnsBand(2);
            case DetectorScanMode.Band3:
                return OwnsBand(3);
            case DetectorScanMode.Full:
                return true;
            default:
                return false;
        }
    }

    public bool CanDetectDepth(DepthLevel depth)
    {
        int value = (int)depth;
        if (value < 1 || value > 3)
        {
            return false;
        }

        if (activeMode == DetectorScanMode.Full)
        {
            return value <= maxUnlockedDepth;
        }

        int band = activeMode == DetectorScanMode.Band2 ? 2
            : activeMode == DetectorScanMode.Band3 ? 3
            : 1;

        return OwnsBand(band) && value == band;
    }

    public void SetMode(DetectorScanMode mode)
    {
        if (!CanSelect(mode) || mode == activeMode)
        {
            return;
        }

        activeMode = mode;
        OnChanged?.Invoke();
        Debug.Log($"Detector: scan mode = {activeMode} (unlocked 1-{maxUnlockedDepth}).");
    }

    public void CycleMode(int direction)
    {
        int step = direction >= 0 ? 1 : -1;
        int current = (int)activeMode;
        for (int i = 0; i < 4; i++)
        {
            current = (current + step + 4) % 4;
            DetectorScanMode candidate = (DetectorScanMode)current;
            if (CanSelect(candidate))
            {
                SetMode(candidate);
                return;
            }
        }
    }

    public bool TryBuyDepthModule1(PlayerInventory inventory)
    {
        if (HasDepth2Module)
        {
            return false;
        }

        if (inventory == null || !inventory.TrySpend(depthModule1Price))
        {
            return false;
        }

        maxUnlockedDepth = 2;
        OnChanged?.Invoke();
        Debug.Log("Detector: unlocked depth 2 (15-30cm).");
        return true;
    }

    public bool TryBuyDepthModule2(PlayerInventory inventory)
    {
        if (HasDepth3Module || !HasDepth2Module)
        {
            return false;
        }

        if (inventory == null || !inventory.TrySpend(depthModule2Price))
        {
            return false;
        }

        maxUnlockedDepth = 3;
        OnChanged?.Invoke();
        Debug.Log("Detector: unlocked depth 3 (30-50cm).");
        return true;
    }
}

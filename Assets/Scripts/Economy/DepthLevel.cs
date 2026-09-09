using UnityEngine;

/// <summary>
/// Simulated burial depth. Treasures stay on the surface; this is flavor + detector gating only.
/// Lost Items do not use depth.
/// </summary>
public enum DepthLevel
{
    One = 1,
    Two = 2,
    Three = 3
}

public enum DetectorScanMode
{
    Band1 = 0,
    Band2 = 1,
    Band3 = 2,
    Full = 3
}

public static class DepthLevels
{
    public static string RangeLabel(DepthLevel depth)
    {
        switch (depth)
        {
            case DepthLevel.Two:
                return "15-30cm";
            case DepthLevel.Three:
                return "30-50cm";
            default:
                return "0-15cm";
        }
    }

    public static string FullRangeLabel(int maxUnlockedDepth)
    {
        if (maxUnlockedDepth >= 3)
        {
            return "FULL (0-50)";
        }

        if (maxUnlockedDepth >= 2)
        {
            return "FULL (0-30)";
        }

        return "FULL (0-15)";
    }

    public static string ModeLabel(DetectorScanMode mode, int maxUnlockedDepth)
    {
        switch (mode)
        {
            case DetectorScanMode.Band2:
                return "DEPTH: 15-30";
            case DetectorScanMode.Band3:
                return "DEPTH: 30-50";
            case DetectorScanMode.Full:
                return $"DEPTH: {FullRangeLabel(maxUnlockedDepth)}";
            default:
                return "DEPTH: 0-15";
        }
    }

    public static DepthLevel RollWeighted(float weight1, float weight2, float weight3)
    {
        float w1 = Mathf.Max(0f, weight1);
        float w2 = Mathf.Max(0f, weight2);
        float w3 = Mathf.Max(0f, weight3);
        float total = w1 + w2 + w3;
        if (total <= 0f)
        {
            return DepthLevel.One;
        }

        float roll = Random.Range(0f, total);
        if (roll < w1)
        {
            return DepthLevel.One;
        }

        if (roll < w1 + w2)
        {
            return DepthLevel.Two;
        }

        return DepthLevel.Three;
    }

    /// <summary>Fallback weights when a TreasureDefinition has no depth weights set.</summary>
    public static void DefaultWeights(MetalType metal, out float depth1, out float depth2, out float depth3)
    {
        switch (metal)
        {
            case MetalType.Aluminum:
                depth1 = 80f;
                depth2 = 18f;
                depth3 = 2f;
                return;
            case MetalType.Iron:
                depth1 = 70f;
                depth2 = 25f;
                depth3 = 5f;
                return;
            case MetalType.Copper:
                depth1 = 50f;
                depth2 = 35f;
                depth3 = 15f;
                return;
            case MetalType.Brass:
                depth1 = 40f;
                depth2 = 40f;
                depth3 = 20f;
                return;
            case MetalType.Lead:
                depth1 = 30f;
                depth2 = 45f;
                depth3 = 25f;
                return;
            case MetalType.Silver:
                depth1 = 15f;
                depth2 = 35f;
                depth3 = 50f;
                return;
            case MetalType.Gold:
                depth1 = 10f;
                depth2 = 25f;
                depth3 = 65f;
                return;
            default:
                depth1 = 60f;
                depth2 = 30f;
                depth3 = 10f;
                return;
        }
    }
}

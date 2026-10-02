using System;
using UnityEngine;

/// <summary>
/// Daily metal prices. Common metals fluctuate; silver/gold stay at 100%.
/// Put on GameSystems (same object as DayManager / QuestManager).
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [Header("Fluctuation (common metals only)")]
    [SerializeField] private float minMultiplier = 0.6f;
    [SerializeField] private float maxMultiplier = 1.5f;
    [SerializeField] private int priceSeed = 42;

    private readonly float[] multipliers = new float[7];
    private readonly float[] previousMultipliers = new float[7];
    private int pricedDay = -1;
    private bool hasPreviousDay;

    public event Action OnPricesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        for (int i = 0; i < previousMultipliers.Length; i++)
        {
            previousMultipliers[i] = 1f;
        }

        RollForCurrentDay();
    }

    private void OnEnable()
    {
        SubscribeDay();
        RollForCurrentDay();
    }

    private void Start()
    {
        SubscribeDay();
        RollForCurrentDay();
    }

    private void OnDisable()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= OnDayStarted;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SubscribeDay()
    {
        if (DayManager.Instance == null)
        {
            return;
        }

        DayManager.Instance.OnDayStarted -= OnDayStarted;
        DayManager.Instance.OnDayStarted += OnDayStarted;
    }

    private void OnDayStarted(int day)
    {
        RollForDay(day);
    }

    public void RollForCurrentDay()
    {
        int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        RollForDay(day);
    }

    public void RollForDay(int day)
    {
        day = Mathf.Max(1, day);
        if (pricedDay == day)
        {
            return;
        }

        if (pricedDay > 0)
        {
            Array.Copy(multipliers, previousMultipliers, multipliers.Length);
            hasPreviousDay = true;
        }

        UnityEngine.Random.State previous = UnityEngine.Random.state;
        UnityEngine.Random.InitState(priceSeed + day * 9176);

        for (int i = 0; i < MetalTypes.All.Length; i++)
        {
            MetalType metal = MetalTypes.All[i];
            if (MetalTypes.UsesDailyMarket(metal))
            {
                multipliers[(int)metal] = UnityEngine.Random.Range(minMultiplier, maxMultiplier);
            }
            else
            {
                multipliers[(int)metal] = 1f;
            }
        }

        UnityEngine.Random.state = previous;
        pricedDay = day;
        OnPricesChanged?.Invoke();
        Debug.Log($"Market: prices rolled for Day {day}.");
    }

    public float GetMultiplier(MetalType metal)
    {
        int index = (int)metal;
        if (index < 0 || index >= multipliers.Length)
        {
            return 1f;
        }

        float value = multipliers[index];
        return value > 0f ? value : 1f;
    }

    public float GetPreviousMultiplier(MetalType metal)
    {
        int index = (int)metal;
        if (index < 0 || index >= previousMultipliers.Length)
        {
            return 1f;
        }

        return previousMultipliers[index];
    }

    public int GetSellPrice(TreasureDefinition treasure)
    {
        if (treasure == null)
        {
            return 0;
        }

        float multiplier = GetMultiplier(treasure.metalType);
        return Mathf.Max(1, Mathf.RoundToInt(treasure.baseValue * multiplier));
    }

    public string FormatMultiplier(MetalType metal)
    {
        if (!MetalTypes.UsesDailyMarket(metal))
        {
            return "STABLE";
        }

        return $"{Mathf.RoundToInt(GetMultiplier(metal) * 100f)}%";
    }

    /// <summary>Change vs yesterday (or vs 100% on day 1), e.g. "▲ 12" / "▼ 8" / "—".</summary>
    public string FormatChange(MetalType metal)
    {
        if (!MetalTypes.UsesDailyMarket(metal))
        {
            return "—";
        }

        float now = GetMultiplier(metal);
        float baseline = hasPreviousDay ? GetPreviousMultiplier(metal) : 1f;
        int delta = Mathf.RoundToInt((now - baseline) * 100f);
        if (Mathf.Abs(delta) < 2)
        {
            return "—";
        }

        return delta > 0 ? $"▲ {delta}" : $"▼ {Mathf.Abs(delta)}";
    }

    public string FormatBoardLine(MetalType metal)
    {
        string name = BoardAbbrev(metal);
        if (!MetalTypes.UsesDailyMarket(metal))
        {
            return $"| {name} STABLE";
        }

        return $"| {name} {FormatMultiplier(metal)} {FormatChange(metal)}";
    }

    /// <summary>Four-letter board code: IRON, COPP, BRAS, ALUM, LEAD, SILV, GOLD.</summary>
    private static string BoardAbbrev(MetalType metal)
    {
        string upper = MetalTypes.UpperName(metal);
        if (upper.Length <= 4)
        {
            return upper;
        }

        return upper.Substring(0, 4);
    }

    public string BuildBoardText()
    {
        int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"BLIP BEACH MARKET");
        builder.AppendLine($"DAY {day}");
        builder.AppendLine();
        for (int i = 0; i < MetalTypes.All.Length; i++)
        {
            builder.AppendLine(FormatBoardLine(MetalTypes.All[i]));
        }

        return builder.ToString();
    }
}

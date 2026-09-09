using TMPro;
using UnityEngine;

/// <summary>
/// In-world electronic market board. Assign a world-space TMP (or per-metal texts) in the Inspector.
/// </summary>
public class MarketPriceBoard : MonoBehaviour
{
    [Header("Single readout (optional)")]
    [SerializeField] private TMP_Text boardText;

    [Header("Optional split layout")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text ironText;
    [SerializeField] private TMP_Text copperText;
    [SerializeField] private TMP_Text brassText;
    [SerializeField] private TMP_Text aluminumText;
    [SerializeField] private TMP_Text leadText;
    [SerializeField] private TMP_Text silverText;
    [SerializeField] private TMP_Text goldText;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (MarketManager.Instance != null)
        {
            MarketManager.Instance.OnPricesChanged -= Refresh;
        }

        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= OnDayStarted;
        }
    }

    private void Subscribe()
    {
        if (MarketManager.Instance != null)
        {
            MarketManager.Instance.OnPricesChanged -= Refresh;
            MarketManager.Instance.OnPricesChanged += Refresh;
        }

        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= OnDayStarted;
            DayManager.Instance.OnDayStarted += OnDayStarted;
        }
    }

    private void OnDayStarted(int day)
    {
        Refresh();
    }

    private void Refresh()
    {
        MarketManager market = MarketManager.Instance;
        if (market == null)
        {
            return;
        }

        if (boardText != null)
        {
            boardText.text = market.BuildBoardText();
        }

        if (headerText != null)
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            headerText.text = $"MARKET  DAY {day}";
        }

        SetLine(ironText, market, MetalType.Iron);
        SetLine(copperText, market, MetalType.Copper);
        SetLine(brassText, market, MetalType.Brass);
        SetLine(aluminumText, market, MetalType.Aluminum);
        SetLine(leadText, market, MetalType.Lead);
        SetLine(silverText, market, MetalType.Silver);
        SetLine(goldText, market, MetalType.Gold);
    }

    private static void SetLine(TMP_Text text, MarketManager market, MetalType metal)
    {
        if (text == null)
        {
            return;
        }

        text.text = market.FormatBoardLine(metal);
    }
}

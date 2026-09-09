using TMPro;
using UnityEngine;

/// <summary>
/// HUD clock: shows current day and wall-clock time (12:00 → 20:00, 24h).
/// </summary>
public class DayClockUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;

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
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= OnDayStarted;
            DayManager.Instance.OnTimeUpdated -= Refresh;
        }
    }

    private void Subscribe()
    {
        if (DayManager.Instance == null)
        {
            return;
        }

        DayManager.Instance.OnDayStarted -= OnDayStarted;
        DayManager.Instance.OnDayStarted += OnDayStarted;
        DayManager.Instance.OnTimeUpdated -= Refresh;
        DayManager.Instance.OnTimeUpdated += Refresh;
    }

    private void OnDayStarted(int day)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (DayManager.Instance == null)
        {
            return;
        }

        if (dayText != null)
        {
            dayText.text = $"Day {DayManager.Instance.CurrentDay}";
        }

        if (timeText != null)
        {
            timeText.text = DayManager.Instance.CurrentClockTime;
        }
    }
}

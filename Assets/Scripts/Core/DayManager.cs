using System;
using UnityEngine;

/// <summary>
/// Tracks the 3-day loop timer. Quest progress is NOT reset here — only the clock and day number change.
/// Day transitions are orchestrated by DayTransitionDirector.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day loop")]
    [SerializeField] private float dayLengthSeconds = 720f;
    [SerializeField] private int totalDays = 3;

    [Header("Clock display (12:00 → 20:00, 24h)")]
    [SerializeField] private float dayStartHour = 12f;
    [SerializeField] private float dayEndHour = 20f;

    [Header("Debug")]
    [SerializeField] private KeyCode debugEndDayKey = KeyCode.F7;

    public int CurrentDay { get; private set; } = 1;
    public float TimeRemainingSeconds { get; private set; }
    /// <summary>Wall-clock hour for the current moment (12 at day start, 20 at day end).</summary>
    public float CurrentClockHour => Mathf.Lerp(dayStartHour, dayEndHour, DayProgress);
    /// <summary>Digital-watch style time, e.g. "17:00".</summary>
    public string CurrentClockTime => FormatClockTime(CurrentClockHour);
    public float NormalizedTimeRemaining => dayLengthSeconds > 0f
        ? Mathf.Clamp01(TimeRemainingSeconds / dayLengthSeconds)
        : 0f;
    /// <summary>0 at sunrise, 1 at end of day.</summary>
    public float DayProgress => 1f - NormalizedTimeRemaining;
    public bool IsDayTransitioning { get; private set; }

    private bool gameplayClockActive;

    public event Action<int> OnDayStarted;
    /// <summary>Fires once the morning fade-in finishes and gameplay is unlocked.</summary>
    public event Action<int> OnMorningReady;
    public event Action<int> OnDayEnded;
    public event Action OnTimeUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Full timer from frame 0 — Update runs before DayTransitionDirector.Start otherwise.
        TimeRemainingSeconds = dayLengthSeconds;
        IsDayTransitioning = true;
    }

    private void Start()
    {
        // No director in scene: skip intro pause and start the clock.
        if (FindFirstObjectByType<DayTransitionDirector>(FindObjectsInactive.Include) == null)
        {
            SetTransitioning(false);
        }
    }

    private void Update()
    {
        if (!gameplayClockActive || IsDayTransitioning)
        {
            return;
        }

        if (Input.GetKeyDown(debugEndDayKey))
        {
            EndDayEarly();
            return;
        }

        TimeRemainingSeconds -= Time.deltaTime;
        if (TimeRemainingSeconds <= 0f)
        {
            TimeRemainingSeconds = 0f;
            OnTimeUpdated?.Invoke();
            EndDay();
            return;
        }

        OnTimeUpdated?.Invoke();
    }

    /// <summary>Ends the current day immediately (debug key or future "go to sleep" interactable).</summary>
    public void EndDayEarly()
    {
        if (IsDayTransitioning)
        {
            return;
        }

        TimeRemainingSeconds = 0f;
        EndDay();
    }

    /// <summary>Sets up day 1 at game start. Does not fire OnDayStarted — director does that after spawn.</summary>
    public void InitializeFirstDay()
    {
        CurrentDay = 1;
        TimeRemainingSeconds = dayLengthSeconds;
        OnTimeUpdated?.Invoke();
    }

    /// <summary>Advances to the next day and resets the timer. Does not fire OnDayStarted yet.</summary>
    public int AdvanceToNextDay()
    {
        int nextDay = CurrentDay + 1;
        if (nextDay > totalDays)
        {
            nextDay = 1;
        }

        CurrentDay = nextDay;
        TimeRemainingSeconds = dayLengthSeconds;
        OnTimeUpdated?.Invoke();
        return CurrentDay;
    }

    /// <summary>Fires OnDayStarted after spawn/title setup (NPCSchedule listens here).</summary>
    public void NotifyDayStarted()
    {
        OnDayStarted?.Invoke(CurrentDay);
        OnTimeUpdated?.Invoke();
    }

    public void SetTransitioning(bool transitioning)
    {
        bool wasTransitioning = IsDayTransitioning;
        IsDayTransitioning = transitioning;
        if (!transitioning)
        {
            gameplayClockActive = true;
            OnTimeUpdated?.Invoke();
            if (wasTransitioning)
            {
                OnMorningReady?.Invoke(CurrentDay);
            }
        }
    }

    private static string FormatClockTime(float hour24)
    {
        int hours = Mathf.FloorToInt(hour24);
        int minutes = Mathf.FloorToInt((hour24 - hours) * 60f);
        hours = Mathf.Clamp(hours, 0, 23);
        minutes = Mathf.Clamp(minutes, 0, 59);

        return $"{hours:00}:{minutes:00}";
    }

    private void EndDay()
    {
        if (IsDayTransitioning)
        {
            return;
        }

        IsDayTransitioning = true;
        TimeRemainingSeconds = 0f;
        OnTimeUpdated?.Invoke();

        int endedDay = CurrentDay;
        OnDayEnded?.Invoke(endedDay);

        DayTransitionDirector director = DayTransitionDirector.Instance;
        if (director == null)
        {
            director = FindFirstObjectByType<DayTransitionDirector>(FindObjectsInactive.Include);
        }

        if (director != null)
        {
            director.PlayEndOfDaySequence(endedDay);
            return;
        }

        // Fallback if director is missing from the scene.
        Debug.LogWarning("DayManager: no DayTransitionDirector — using simple summary fallback.");
        ShowSummaryFallback(endedDay);
    }

    private void ShowSummaryFallback(int endedDay)
    {
        DaySummaryUI summary = DaySummaryUI.Instance
            ?? FindFirstObjectByType<DaySummaryUI>(FindObjectsInactive.Include);

        if (summary != null)
        {
            summary.Show(endedDay, () =>
            {
                AdvanceToNextDay();
                NotifyDayStarted();
                IsDayTransitioning = false;
            });
        }
        else
        {
            AdvanceToNextDay();
            NotifyDayStarted();
            IsDayTransitioning = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

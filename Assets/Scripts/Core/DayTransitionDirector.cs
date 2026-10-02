using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the full day start / day end pipelines (fade, title, summary, spawn).
/// </summary>
public class DayTransitionDirector : MonoBehaviour
{
    public static DayTransitionDirector Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private ScreenFadeUI screenFade;
    [SerializeField] private DayTitleUI dayTitle;
    [SerializeField] private DayEndSequenceUI dayEndSequence;
    [SerializeField] private DaySummaryUI daySummary;
    [SerializeField] private PlayerDaySpawn playerSpawn;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [Header("Timings")]
    [SerializeField] private float endFadeOutDuration = 1f;
    [SerializeField] private float morningFadeInDuration = 1.25f;
    [SerializeField] private bool runIntroOnStart = true;

    private bool sequenceRunning;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        if (runIntroOnStart)
        {
            StartCoroutine(GameStartSequence());
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayEndOfDaySequence(int endedDay)
    {
        if (sequenceRunning)
        {
            return;
        }

        StartCoroutine(EndOfDaySequence(endedDay));
    }

    private IEnumerator GameStartSequence()
    {
        sequenceRunning = true;
        SetGameplayPaused(true);

        if (dayManager != null)
        {
            dayManager.SetTransitioning(true);
            dayManager.InitializeFirstDay();
        }

        if (screenFade != null)
        {
            screenFade.SetImmediate(1f);
        }

        EnsurePlayerSpawn();
        if (playerSpawn != null)
        {
            playerSpawn.TeleportToSpawn();
        }

        if (dayManager != null)
        {
            dayManager.NotifyDayStarted();
        }

        if (dayTitle != null)
        {
            yield return dayTitle.PlayShowDay(dayManager != null ? dayManager.CurrentDay : 1);
        }

        if (screenFade != null)
        {
            yield return screenFade.FadeInRoutine(morningFadeInDuration);
        }

        // Unpause before ending transition so MorningReady listeners can start dialogue.
        SetGameplayPaused(false);

        if (dayManager != null)
        {
            dayManager.SetTransitioning(false);
        }

        sequenceRunning = false;
    }

    private IEnumerator EndOfDaySequence(int endedDay)
    {
        sequenceRunning = true;
        SetGameplayPaused(true);

        if (dayEndSequence != null)
        {
            yield return dayEndSequence.PlayEndSequence();
        }

        if (screenFade != null)
        {
            yield return screenFade.FadeOutRoutine(endFadeOutDuration);
        }

        if (daySummary != null)
        {
            bool summaryDone = false;
            daySummary.Show(endedDay, () => summaryDone = true);
            while (!summaryDone)
            {
                yield return null;
            }
        }

        yield return MorningSequence();

        sequenceRunning = false;
    }

    private IEnumerator MorningSequence()
    {
        SetGameplayPaused(true);

        int newDay = dayManager != null ? dayManager.AdvanceToNextDay() : 1;

        EnsurePlayerSpawn();
        if (playerSpawn != null)
        {
            playerSpawn.TeleportToSpawn();
        }

        if (dayManager != null)
        {
            dayManager.NotifyDayStarted();
        }

        if (dayTitle != null)
        {
            yield return dayTitle.PlayShowDay(newDay);
        }

        if (screenFade != null)
        {
            yield return screenFade.FadeInRoutine(morningFadeInDuration);
        }

        // Unpause before ending transition so MorningReady listeners can start dialogue.
        SetGameplayPaused(false);

        if (dayManager != null)
        {
            dayManager.SetTransitioning(false);
        }
    }

    private void ResolveReferences()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance;
        }

        if (screenFade == null)
        {
            screenFade = ScreenFadeUI.Instance
                ?? FindFirstObjectByType<ScreenFadeUI>(FindObjectsInactive.Include);
        }

        if (dayTitle == null)
        {
            dayTitle = DayTitleUI.Instance
                ?? FindFirstObjectByType<DayTitleUI>(FindObjectsInactive.Include);
        }

        if (dayEndSequence == null)
        {
            dayEndSequence = DayEndSequenceUI.Instance
                ?? FindFirstObjectByType<DayEndSequenceUI>(FindObjectsInactive.Include);
        }

        if (daySummary == null)
        {
            daySummary = DaySummaryUI.Instance
                ?? FindFirstObjectByType<DaySummaryUI>(FindObjectsInactive.Include);
        }

        if (playerSpawn == null)
        {
            playerSpawn = PlayerDaySpawn.Instance
                ?? FindFirstObjectByType<PlayerDaySpawn>(FindObjectsInactive.Include);
        }

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }
    }

    private void EnsurePlayerSpawn()
    {
        if (playerSpawn == null)
        {
            playerSpawn = PlayerDaySpawn.Instance
                ?? FindFirstObjectByType<PlayerDaySpawn>(FindObjectsInactive.Include);
        }
    }

    private void SetGameplayPaused(bool paused)
    {
        if (playerMovement != null)
        {
            playerMovement.SetInputEnabled(!paused);
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.LookEnabled = !paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }
    }
}

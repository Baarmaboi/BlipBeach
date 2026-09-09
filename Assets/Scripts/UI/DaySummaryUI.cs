using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-day summary panel. Player must dismiss it before the next day starts.
/// </summary>
public class DaySummaryUI : MonoBehaviour
{
    public static DaySummaryUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text continueHintText;
    [SerializeField] private Button continueButton;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private Action onContinue;
    private bool suppressContinueUntilKeyUp;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    private void OnEnable()
    {
        Instance = this;

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Continue);
            continueButton.onClick.AddListener(Continue);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        // Wait for E to be released so the key that opened another UI doesn't instantly continue.
        if (suppressContinueUntilKeyUp)
        {
            if (!Input.GetKey(KeyCode.E))
            {
                suppressContinueUntilKeyUp = false;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Continue();
        }
    }

    public void Show(int completedDay, Action continueCallback)
    {
        onContinue = continueCallback;
        IsOpen = true;

        // Ensure this component is active even if it lived on a disabled panel hierarchy.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (summaryText != null)
        {
            summaryText.text = BuildSummaryText(completedDay);
        }

        if (continueHintText != null)
        {
            continueHintText.text = "E — continue";
        }

        suppressContinueUntilKeyUp = Input.GetKey(KeyCode.E);
        SetGameplayPaused(true);
    }

    private string BuildSummaryText(int completedDay)
    {
        int stars = QuestManager.Instance != null ? QuestManager.Instance.Stars : 0;
        int activeQuests = QuestManager.Instance != null ? QuestManager.Instance.ActiveQuests.Count : 0;
        int cash = PlayerInventory.Instance != null ? PlayerInventory.Instance.Cash : 0;
        int treasures = PlayerInventory.Instance != null ? PlayerInventory.Instance.Treasures.Count : 0;
        int maxDepth = DetectorProgress.Instance != null ? DetectorProgress.Instance.MaxUnlockedDepth : 1;
        string detectorRange = maxDepth >= 3 ? "0-50cm" : maxDepth >= 2 ? "0-30cm" : "0-15cm";

        return $"Day {completedDay} complete\n\nStars: {stars}\nActive quests: {activeQuests}\nCash: ${cash}\nTreasures: {treasures}\nDetector: {detectorRange}";
    }

    public void Continue()
    {
        if (!IsOpen)
        {
            return;
        }

        Action callback = onContinue;
        HideImmediate();
        callback?.Invoke();
    }

    private void HideImmediate()
    {
        IsOpen = false;
        onContinue = null;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (!DialogueUI.IsOpen && !QuestLogUI.IsOpen && !EconomyMenus.IsAnyOpen
            && (DayManager.Instance == null || !DayManager.Instance.IsDayTransitioning))
        {
            SetGameplayPaused(false);
        }
    }

    private void OnDisable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Continue);
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

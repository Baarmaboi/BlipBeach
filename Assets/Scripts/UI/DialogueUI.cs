using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    // Stops the same E that closed dialogue from immediately opening another talk.
    public static bool SuppressNpcInteract { get; private set; }

    /// <summary>Fires when a conversation panel closes. Safe no-op if nobody is listening.</summary>
    public static event System.Action Closed;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text continueHintText;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private string[] lines;
    private int lineIndex;
    private System.Action onFinished;
    private string finalHint = "E — continue";
    private bool suppressAdvanceUntilKeyUp;

    private void Awake()
    {
        Instance = this;
        Hide();
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

        // The same E press that opened dialogue must not skip the first line.
        if (suppressAdvanceUntilKeyUp)
        {
            if (!Input.GetKey(KeyCode.E))
            {
                suppressAdvanceUntilKeyUp = false;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Advance();
        }
    }

    public void ShowOffer(QuestDefinition quest, string[] linesOverride = null, Transform talkLookAt = null)
    {
        if (quest == null)
        {
            return;
        }

        string[] offerLines = linesOverride != null && linesOverride.Length > 0
            ? linesOverride
            : quest.dialogueLines != null && quest.dialogueLines.Length > 0
                ? quest.dialogueLines
                : new[] { "Can you help me find something?" };

        ShowLines(quest.npcName, offerLines, () =>
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.AcceptQuest(quest);
            }
        }, "E — accept quest", talkLookAt);
    }

    public void ShowLines(
        string speakerName,
        string[] dialogueLines,
        System.Action finishedCallback,
        string lastLineHint = "E — continue",
        Transform talkLookAt = null)
    {
        lines = dialogueLines != null && dialogueLines.Length > 0
            ? dialogueLines
            : new[] { "..." };
        lineIndex = 0;
        onFinished = finishedCallback;
        finalHint = lastLineHint;
        IsOpen = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (speakerText != null)
        {
            speakerText.text = speakerName;
        }

        suppressAdvanceUntilKeyUp = Input.GetKey(KeyCode.E);
        ShowCurrentLine();
        SetGameplayPaused(true);
        thirdPersonCamera?.EnterTalkMode(talkLookAt);
    }

    private void Advance()
    {
        lineIndex++;
        if (lineIndex >= lines.Length)
        {
            Finish();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (bodyText != null)
        {
            bodyText.text = lines[lineIndex];
        }

        if (continueHintText != null)
        {
            bool lastLine = lineIndex >= lines.Length - 1;
            continueHintText.text = lastLine ? finalHint : "E — continue";
        }
    }

    private void Finish()
    {
        System.Action callback = onFinished;
        Hide();
        callback?.Invoke();
    }

    public void Hide()
    {
        bool wasOpen = IsOpen;
        IsOpen = false;
        lines = null;
        onFinished = null;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (wasOpen)
        {
            thirdPersonCamera?.ExitTalkMode();
            SuppressNpcInteract = true;
            Closed?.Invoke();
        }

        if (!QuestLogUI.IsOpen && !DaySummaryUI.IsOpen && !EconomyMenus.IsAnyOpen)
        {
            SetGameplayPaused(false);
        }
    }

    public static void UpdateNpcInteractGate()
    {
        if (SuppressNpcInteract && !Input.GetKey(KeyCode.E))
        {
            SuppressNpcInteract = false;
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

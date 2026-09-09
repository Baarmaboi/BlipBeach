using System.Text;
using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text questListText;
    [SerializeField] private KeyCode toggleKey = KeyCode.J;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestsChanged -= Refresh;
            QuestManager.Instance.OnStarsChanged -= Refresh;
        }
    }

    private void Subscribe()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.OnQuestsChanged -= Refresh;
        QuestManager.Instance.OnStarsChanged -= Refresh;
        QuestManager.Instance.OnQuestsChanged += Refresh;
        QuestManager.Instance.OnStarsChanged += Refresh;
    }

    private void Update()
    {
        if (DialogueUI.IsOpen || DaySummaryUI.IsOpen || EconomyMenus.IsAnyOpen)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (panel == null)
        {
            return;
        }

        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(true);
        IsOpen = true;
        Refresh();
        SetGameplayPaused(true);
    }

    public void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;

        if (!DialogueUI.IsOpen && !DaySummaryUI.IsOpen && !EconomyMenus.IsAnyOpen)
        {
            SetGameplayPaused(false);
        }
    }

    public void Refresh()
    {
        if (questListText == null)
        {
            return;
        }

        if (QuestManager.Instance == null || QuestManager.Instance.ActiveQuests.Count == 0)
        {
            questListText.text = "No active quests.\nTalk to beach goers (E).";
            return;
        }

        StringBuilder builder = new StringBuilder();
        var quests = QuestManager.Instance.ActiveQuests;
        for (int i = 0; i < quests.Count; i++)
        {
            ActiveQuest entry = quests[i];
            if (entry == null || entry.Definition == null)
            {
                continue;
            }

            QuestDefinition quest = entry.Definition;
            string statusLabel = entry.Status == QuestStatus.ReadyToTurnIn
                ? "Ready to return"
                : "Active";

            builder.AppendLine($"• {quest.npcName}  [{statusLabel}]");
            builder.AppendLine($"  Lost: {quest.itemDisplayName}");
            builder.AppendLine($"  Where: {quest.locationHint}");
            builder.AppendLine();
        }

        questListText.text = builder.ToString();
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

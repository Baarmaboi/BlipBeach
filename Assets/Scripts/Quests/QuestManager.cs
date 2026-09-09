using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Story beats")]
    [Tooltip("Checked after each quest completion; sets storyFlagIdToSet when satisfied.")]
    [SerializeField] private StoryBeatDefinition[] storyBeats;

    private readonly List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    private readonly HashSet<string> completedQuestIds = new HashSet<string>();
    private readonly Dictionary<string, int> questCompletedOnDay = new Dictionary<string, int>();

    public event Action OnQuestsChanged;
    public event Action OnStarsChanged;

    public int Stars { get; private set; }
    public IReadOnlyList<ActiveQuest> ActiveQuests => activeQuests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool HasQuest(string questId)
    {
        return FindActive(questId) != null;
    }

    public bool IsCompleted(string questId)
    {
        return !string.IsNullOrEmpty(questId) && completedQuestIds.Contains(questId);
    }

    /// <summary>Day number when the quest was turned in, or 0 if never completed.</summary>
    public int GetQuestCompletedOnDay(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            return 0;
        }

        return questCompletedOnDay.TryGetValue(questId, out int day) ? day : 0;
    }

    public ActiveQuest FindActive(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            return null;
        }

        for (int i = 0; i < activeQuests.Count; i++)
        {
            if (activeQuests[i].Definition != null && activeQuests[i].Definition.questId == questId)
            {
                return activeQuests[i];
            }
        }

        return null;
    }

    /// <summary>Checks offer windows (or legacy day gates + prerequisites) before offering a quest.</summary>
    public bool CanOfferQuest(QuestDefinition quest)
    {
        if (quest == null || string.IsNullOrEmpty(quest.questId))
        {
            return false;
        }

        if (IsCompleted(quest.questId) || HasQuest(quest.questId))
        {
            return false;
        }

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        if (quest.offerWindows != null && quest.offerWindows.Length > 0)
        {
            for (int i = 0; i < quest.offerWindows.Length; i++)
            {
                if (quest.offerWindows[i].Matches(currentDay, this))
                {
                    return true;
                }
            }

            return false;
        }

        // Legacy fallback when no offer windows are configured.
        if (currentDay < Mathf.Max(1, quest.availableFromDay))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(quest.requiredCompletedQuestId)
            && !IsCompleted(quest.requiredCompletedQuestId))
        {
            return false;
        }

        return true;
    }

    public bool AcceptQuest(QuestDefinition quest)
    {
        if (quest == null || string.IsNullOrEmpty(quest.questId))
        {
            Debug.LogWarning("QuestManager: quest is null or missing questId.");
            return false;
        }

        if (!CanOfferQuest(quest))
        {
            Debug.Log($"QuestManager: cannot offer '{quest.questId}' right now.");
            return false;
        }

        activeQuests.Add(new ActiveQuest(quest));
        UnlockBuriedItems(quest.itemIdToUnlock);
        OnQuestsChanged?.Invoke();
        Debug.Log($"QuestManager: accepted '{quest.questId}' — unlocking itemId '{quest.itemIdToUnlock}'.");
        return true;
    }

    public void NotifyItemDug(BuriedItem item)
    {
        if (item == null || !item.RequiresQuest || string.IsNullOrEmpty(item.ItemId))
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < activeQuests.Count; i++)
        {
            ActiveQuest entry = activeQuests[i];
            if (entry.Status != QuestStatus.Active || entry.Definition == null)
            {
                continue;
            }

            if (entry.Definition.itemIdToUnlock == item.ItemId)
            {
                entry.Status = QuestStatus.ReadyToTurnIn;
                changed = true;
                Debug.Log($"QuestManager: '{entry.Definition.questId}' ready to turn in.");
            }
        }

        if (changed)
        {
            OnQuestsChanged?.Invoke();
        }
    }

    public bool CompleteQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || IsCompleted(questId))
        {
            return false;
        }

        ActiveQuest entry = FindActive(questId);
        if (entry == null || entry.Status != QuestStatus.ReadyToTurnIn)
        {
            return false;
        }

        entry.Status = QuestStatus.Completed;
        QuestDefinition definition = entry.Definition;
        activeQuests.Remove(entry);
        completedQuestIds.Add(questId);

        int completedDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        questCompletedOnDay[questId] = completedDay;

        if (definition != null && !string.IsNullOrEmpty(definition.storyFlagIdOnComplete))
        {
            StoryFlags.Instance?.SetFlag(definition.storyFlagIdOnComplete);
        }

        EvaluateStoryBeats();

        Stars += 1;
        OnStarsChanged?.Invoke();
        OnQuestsChanged?.Invoke();
        Debug.Log($"QuestManager: completed '{questId}' on Day {completedDay}. Stars = {Stars}");
        return true;
    }

    private void EvaluateStoryBeats()
    {
        if (storyBeats == null || storyBeats.Length == 0)
        {
            return;
        }

        StoryFlags flags = StoryFlags.Instance;
        for (int i = 0; i < storyBeats.Length; i++)
        {
            StoryBeatDefinition beat = storyBeats[i];
            if (beat == null || string.IsNullOrEmpty(beat.storyFlagIdToSet))
            {
                continue;
            }

            if (beat.IsSatisfied(this, flags))
            {
                flags?.SetFlag(beat.storyFlagIdToSet);
            }
        }
    }

    private void UnlockBuriedItems(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        BuriedItem[] items = FindObjectsByType<BuriedItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].ItemId == itemId && items[i].RequiresQuest)
            {
                items[i].UnlockForQuest();
            }
        }
    }
}

using System;
using UnityEngine;

[Serializable]
public class QuestOfferWindow
{
    public int minDay = 1;
    [Tooltip("0 = no upper limit.")]
    public int maxDay;
    public string prerequisiteQuestId;
    [Tooltip("0 = ignore exact day.")]
    public int prerequisiteCompletedOnExactDay;
    [Tooltip("0 = ignore.")]
    public int prerequisiteCompletedOnMinDay;
    [Tooltip("0 = ignore.")]
    public int prerequisiteCompletedOnMaxDay;

    public bool Matches(int currentDay, QuestManager qm)
    {
        if (!QuestConditionEvaluator.MatchesDay(currentDay, minDay, maxDay))
        {
            return false;
        }

        return QuestConditionEvaluator.MatchesQuestCompletion(
            qm,
            prerequisiteQuestId,
            prerequisiteCompletedOnExactDay,
            prerequisiteCompletedOnMinDay,
            prerequisiteCompletedOnMaxDay,
            requireCompleted: true);
    }
}

[Serializable]
public class ConditionalDialogue
{
    public int minDay = 1;
    [Tooltip("0 = no upper limit.")]
    public int maxDay;
    public StoryBeatDefinition requiredStoryBeat;
    public string requiredStoryFlag;
    public int priority;
    [TextArea(2, 4)]
    public string[] lines;

    public bool Matches(int day, QuestManager qm, StoryFlags flags)
    {
        if (!QuestConditionEvaluator.MatchesDay(day, minDay, maxDay))
        {
            return false;
        }

        if (requiredStoryBeat != null && !requiredStoryBeat.IsSatisfied(qm, flags))
        {
            return false;
        }

        if (!QuestConditionEvaluator.MatchesStoryFlag(flags, requiredStoryFlag))
        {
            return false;
        }

        return lines != null && lines.Length > 0;
    }
}

[CreateAssetMenu(fileName = "Quest_", menuName = "BongoBeach/Quest Definition")]
public class QuestDefinition : ScriptableObject
{
    public string questId;
    public string npcName;
    public string itemDisplayName;
    public string locationHint;
    [Tooltip("Must match BuriedItem.itemId on the quest prefab.")]
    public string itemIdToUnlock;

    [Header("Story")]
    [Tooltip("Optional flag set when this quest is turned in.")]
    public string storyFlagIdOnComplete;

    [Header("Offer gates (legacy)")]
    [Tooltip("First day this quest can be offered (1–3). Used when Offer Windows is empty.")]
    public int availableFromDay = 1;
    [Tooltip("If set, this quest must already be completed before offering.")]
    public string requiredCompletedQuestId;

    [Header("Offer gates (data-driven)")]
    [Tooltip("If set, ANY matching window allows the offer. Put specific windows first.")]
    public QuestOfferWindow[] offerWindows;

    [Header("Dialogue — offer & quest progress")]
    [TextArea(2, 4)]
    public string[] dialogueLines;
    [TextArea(2, 4)]
    public string[] searchingDialogueLines;
    [TextArea(2, 4)]
    public string[] turnInDialogueLines;
    [TextArea(2, 4)]
    public string[] completedDialogueLines;

    [Header("Dialogue — Day 2+ (legacy fallback)")]
    [TextArea(2, 4)]
    public string[] day2CompletedDialogueLines;
    [TextArea(2, 4)]
    public string[] day2IncompleteDialogueLines;

    [Header("Dialogue — Day 3 celebration (legacy fallback)")]
    [TextArea(2, 4)]
    public string[] day3PerfectDialogueLines;
    [TextArea(2, 4)]
    public string[] day3LateDialogueLines;

    [Header("Dialogue — completed variants (story beats, higher priority wins)")]
    public ConditionalDialogue[] completedDialogueVariants;

    [Header("Dialogue — when quest cannot be offered yet")]
    [Tooltip("Shown on Day 1 when CanOfferQuest is false (e.g. Tom before ring is available).")]
    [TextArea(2, 4)]
    public string[] day1BlockedDialogueLines;
    [Tooltip("Shown on Day 2 when CanOfferQuest is false.")]
    [TextArea(2, 4)]
    public string[] day2BlockedDialogueLines;
    [Tooltip("Shown on Day 3+ when CanOfferQuest is false.")]
    [TextArea(2, 4)]
    public string[] day3BlockedDialogueLines;

    /// <summary>Earliest day this quest can enter active/searching/turn-in talk.</summary>
    public int GetEarliestOfferDay()
    {
        if (offerWindows != null && offerWindows.Length > 0)
        {
            int earliest = int.MaxValue;
            for (int i = 0; i < offerWindows.Length; i++)
            {
                earliest = Mathf.Min(earliest, offerWindows[i].minDay);
            }

            return earliest == int.MaxValue ? availableFromDay : earliest;
        }

        return availableFromDay;
    }

    /// <summary>Blocked lines for the given day when this NPC cannot offer their quest yet.</summary>
    public string[] GetBlockedDialogueForDay(int day)
    {
        if (day <= 1)
        {
            return day1BlockedDialogueLines;
        }

        if (day == 2)
        {
            return day2BlockedDialogueLines;
        }

        return day3BlockedDialogueLines;
    }

    /// <summary>Highest-priority completed dialogue variant for this day, or null.</summary>
    public string[] GetBestCompletedDialogue(int currentDay, QuestManager qm, StoryFlags flags)
    {
        if (completedDialogueVariants == null || completedDialogueVariants.Length == 0)
        {
            return null;
        }

        ConditionalDialogue best = null;
        for (int i = 0; i < completedDialogueVariants.Length; i++)
        {
            ConditionalDialogue variant = completedDialogueVariants[i];
            if (!variant.Matches(currentDay, qm, flags))
            {
                continue;
            }

            if (best == null || variant.priority > best.priority)
            {
                best = variant;
            }
        }

        return best?.lines;
    }
}

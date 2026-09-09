using System;
using UnityEngine;

[Serializable]
public class FlavorDialogueEntry
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

/// <summary>
/// Optional profile for an NPC: display name, linked quest, and flavor-only dialogue lines.
/// </summary>
[CreateAssetMenu(fileName = "NPC_", menuName = "BongoBeach/NPC Definition")]
public class NPCDefinition : ScriptableObject
{
    public string npcId;
    public string displayName;
    public QuestDefinition quest;
    public FlavorDialogueEntry[] flavorDialogue;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        if (quest != null && !string.IsNullOrEmpty(quest.npcName))
        {
            return quest.npcName;
        }

        return npcId;
    }

    public string[] GetBestFlavorDialogue(int day, QuestManager qm, StoryFlags flags)
    {
        return FlavorDialogueHelper.GetBestEntry(flavorDialogue, day, qm, flags);
    }
}

/// <summary>Shared picker for flavor lines on NPCDefinition and NPCInteract.</summary>
public static class FlavorDialogueHelper
{
    public static string[] GetBestEntry(FlavorDialogueEntry[] entries, int day, QuestManager qm, StoryFlags flags)
    {
        if (entries == null || entries.Length == 0)
        {
            return null;
        }

        FlavorDialogueEntry best = null;
        for (int i = 0; i < entries.Length; i++)
        {
            FlavorDialogueEntry entry = entries[i];
            if (!entry.Matches(day, qm, flags))
            {
                continue;
            }

            if (best == null || entry.priority > best.priority)
            {
                best = entry;
            }
        }

        return best?.lines;
    }

    public static bool HasAnyEntries(FlavorDialogueEntry[] entries)
    {
        if (entries == null || entries.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].lines != null && entries[i].lines.Length > 0)
            {
                return true;
            }
        }

        return false;
    }
}

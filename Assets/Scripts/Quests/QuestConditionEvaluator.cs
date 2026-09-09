using UnityEngine;

/// <summary>
/// Shared day / quest / flag checks used by offer windows, schedule rules, and story beats.
/// </summary>
public static class QuestConditionEvaluator
{
    public static bool MatchesDay(int day, int minDay, int maxDay)
    {
        if (day < Mathf.Max(1, minDay))
        {
            return false;
        }

        if (maxDay > 0 && day > maxDay)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// When questId is empty, returns true. Otherwise checks completion and optional completion-day window.
    /// </summary>
    public static bool MatchesQuestCompletion(
        QuestManager qm,
        string questId,
        int completedOnExactDay,
        int completedOnMinDay,
        int completedOnMaxDay,
        bool requireCompleted = true)
    {
        if (string.IsNullOrEmpty(questId))
        {
            return true;
        }

        if (qm == null)
        {
            return !requireCompleted;
        }

        bool completed = qm.IsCompleted(questId);
        if (!requireCompleted)
        {
            return !completed;
        }

        if (!completed)
        {
            return false;
        }

        int completedDay = qm.GetQuestCompletedOnDay(questId);
        if (completedOnExactDay > 0 && completedDay != completedOnExactDay)
        {
            return false;
        }

        if (completedOnMinDay > 0 && completedDay < completedOnMinDay)
        {
            return false;
        }

        if (completedOnMaxDay > 0 && completedDay > completedOnMaxDay)
        {
            return false;
        }

        return true;
    }

    public static bool MatchesStoryFlag(StoryFlags flags, string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return true;
        }

        return flags != null && flags.HasFlag(flagId);
    }
}

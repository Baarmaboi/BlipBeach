using System;
using UnityEngine;

[Serializable]
public class StoryBeatRequirement
{
    public string questId;
    [Tooltip("0 = completed on any day.")]
    public int completedOnExactDay;
    [Tooltip("0 = ignore.")]
    public int completedOnMinDay;
    [Tooltip("0 = ignore.")]
    public int completedOnMaxDay;
}

/// <summary>
/// Narrative milestone checked from quests, schedules, and dialogue. All requirements must pass.
/// </summary>
[CreateAssetMenu(fileName = "StoryBeat_", menuName = "BongoBeach/Story Beat")]
public class StoryBeatDefinition : ScriptableObject
{
    public string beatId;
    [Tooltip("Set on StoryFlags when this beat becomes satisfied (usually after a quest completes).")]
    public string storyFlagIdToSet;

    public StoryBeatRequirement[] requirements;

    [Tooltip("Beat fails if any of these other beats are also satisfied (e.g. exclude 'perfect' from 'late').")]
    public StoryBeatDefinition[] beatsThatMustNotBeSatisfied;

    public bool IsSatisfied(QuestManager qm, StoryFlags flags = null)
    {
        if (qm == null || requirements == null || requirements.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < requirements.Length; i++)
        {
            StoryBeatRequirement req = requirements[i];
            if (!QuestConditionEvaluator.MatchesQuestCompletion(
                    qm,
                    req.questId,
                    req.completedOnExactDay,
                    req.completedOnMinDay,
                    req.completedOnMaxDay,
                    requireCompleted: true))
            {
                return false;
            }
        }

        if (beatsThatMustNotBeSatisfied != null)
        {
            for (int i = 0; i < beatsThatMustNotBeSatisfied.Length; i++)
            {
                StoryBeatDefinition exclude = beatsThatMustNotBeSatisfied[i];
                if (exclude != null && exclude.IsSatisfied(qm, flags))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

using System;
using UnityEngine;

[Serializable]
public class NPCScheduleRule
{
    [Tooltip("Editor note only — describes what this rule does.")]
    public string ruleLabel;
    public int minDay = 1;
    [Tooltip("0 = no upper limit.")]
    public int maxDay;
    [Tooltip("Quest that must be completed.")]
    public string questCompletedId;
    [Tooltip("Quest that must NOT be completed.")]
    public string questNotCompletedId;
    [Tooltip("0 = any completion day.")]
    public int questCompletedOnExactDay;
    [Tooltip("0 = ignore.")]
    public int questCompletedOnMinDay;
    [Tooltip("0 = ignore.")]
    public int questCompletedOnMaxDay;
    public string requiredStoryFlag;
    public StoryBeatDefinition requiredStoryBeat;
    public Transform spot;
}

/// <summary>
/// Moves an NPC using ordered schedule rules (first match wins), then defaultSpot.
/// Put SPECIFIC rules first (e.g. Day 3 + perfect beat → Tiki Hut), broad/default last.
/// </summary>
public class NPCSchedule : MonoBehaviour
{
    [SerializeField] private Transform defaultSpot;
    [SerializeField] private NPCScheduleRule[] rules;

    private void OnEnable()
    {
        Subscribe();
        ApplySchedule(GetCurrentDay());
    }

    private void Start()
    {
        Subscribe();
        ApplySchedule(GetCurrentDay());
    }

    private void OnDisable()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= OnDayStarted;
        }
    }

    private void Subscribe()
    {
        if (DayManager.Instance == null)
        {
            return;
        }

        DayManager.Instance.OnDayStarted -= OnDayStarted;
        DayManager.Instance.OnDayStarted += OnDayStarted;
    }

    private void OnDayStarted(int day)
    {
        ApplySchedule(day);
    }

    public void ApplySchedule(int day)
    {
        Transform target = ResolveSpot(day);
        if (target == null)
        {
            return;
        }

        transform.SetPositionAndRotation(target.position, target.rotation);
    }

    private Transform ResolveSpot(int day)
    {
        QuestManager qm = QuestManager.Instance;
        StoryFlags flags = StoryFlags.Instance;

        if (rules != null)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                if (RuleMatches(rules[i], day, qm, flags))
                {
                    return rules[i].spot;
                }
            }
        }

        return defaultSpot;
    }

    public static bool RuleMatches(NPCScheduleRule rule, int day, QuestManager qm, StoryFlags flags)
    {
        if (rule == null || rule.spot == null)
        {
            return false;
        }

        if (!QuestConditionEvaluator.MatchesDay(day, rule.minDay, rule.maxDay))
        {
            return false;
        }

        if (!QuestConditionEvaluator.MatchesQuestCompletion(
                qm,
                rule.questCompletedId,
                rule.questCompletedOnExactDay,
                rule.questCompletedOnMinDay,
                rule.questCompletedOnMaxDay,
                requireCompleted: true))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(rule.questNotCompletedId)
            && qm != null
            && qm.IsCompleted(rule.questNotCompletedId))
        {
            return false;
        }

        if (!QuestConditionEvaluator.MatchesStoryFlag(flags, rule.requiredStoryFlag))
        {
            return false;
        }

        if (rule.requiredStoryBeat != null
            && (qm == null || !rule.requiredStoryBeat.IsSatisfied(qm, flags)))
        {
            return false;
        }

        return true;
    }

    private static int GetCurrentDay()
    {
        return DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
    }

    private void OnDrawGizmosSelected()
    {
        DrawMarker(defaultSpot, Color.green);

        if (rules == null)
        {
            return;
        }

        for (int i = 0; i < rules.Length; i++)
        {
            Color color = Color.HSVToRGB((i * 0.17f) % 1f, 0.7f, 1f);
            DrawMarker(rules[i].spot, color);
        }
    }

    private void DrawMarker(Transform marker, Color color)
    {
        if (marker == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawLine(transform.position, marker.position);
        Gizmos.DrawWireSphere(marker.position, 0.35f);
    }
}

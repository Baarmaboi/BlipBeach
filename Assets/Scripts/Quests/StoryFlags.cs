using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple playthrough flags for narrative state (e.g. "tara_watch_returned").
/// Quest completion can also be checked via QuestManager.IsCompleted — use flags for non-quest events later.
/// </summary>
public class StoryFlags : MonoBehaviour
{
    public static StoryFlags Instance { get; private set; }

    private readonly HashSet<string> flags = new HashSet<string>();

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

    public void SetFlag(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        flags.Add(id);
    }

    public bool HasFlag(string id)
    {
        return !string.IsNullOrEmpty(id) && flags.Contains(id);
    }

    public void ClearFlag(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        flags.Remove(id);
    }
}

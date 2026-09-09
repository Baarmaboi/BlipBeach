using TMPro;
using UnityEngine;

/// <summary>
/// Shared screen-space interaction prompt on the player Canvas (same place as dig prompt).
/// NPCs request show each frame; nearest requester wins. Hides automatically if nobody requests.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    private object owner;
    private float ownerDistance = float.MaxValue;
    private int lastRequestFrame = -1;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        // No NPC (or other system) asked this frame — clear the prompt.
        if (lastRequestFrame != Time.frameCount && owner != null)
        {
            HideImmediate();
        }
    }

    /// <summary>Ask to show a prompt. Closest source this frame wins.</summary>
    public void RequestShow(object source, string text, float distance)
    {
        if (source == null)
        {
            return;
        }

        int frame = Time.frameCount;
        if (frame != lastRequestFrame)
        {
            lastRequestFrame = frame;
            owner = source;
            ownerDistance = distance;
            Apply(text);
            return;
        }

        if (distance < ownerDistance)
        {
            owner = source;
            ownerDistance = distance;
            Apply(text);
        }
    }

    public void HideImmediate()
    {
        owner = null;
        ownerDistance = float.MaxValue;
        lastRequestFrame = -1;

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void Apply(string text)
    {
        if (promptText != null)
        {
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }
    }
}

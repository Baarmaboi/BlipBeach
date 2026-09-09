using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Whistle + "beach closed" announcement at end of day, before the summary screen.
/// </summary>
public class DayEndSequenceUI : MonoBehaviour
{
    public static DayEndSequenceUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text announcementText;
    [SerializeField] private TMP_Text continueHintText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip whistleClip;
    [SerializeField] private string[] announcementLines = { "The beach is now closed." };
    [SerializeField] private float autoAdvanceSeconds = 0f;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private bool suppressInputUntilKeyUp;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    private void Start()
    {
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public IEnumerator PlayEndSequence()
    {
        SetGameplayPaused(true);

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (announcementText != null)
        {
            announcementText.text = BuildAnnouncementText();
        }

        if (continueHintText != null)
        {
            continueHintText.text = autoAdvanceSeconds > 0f ? "" : "E — continue";
        }

        PlayWhistle();

        suppressInputUntilKeyUp = Input.GetKey(KeyCode.E);
        float timer = 0f;
        bool done = false;

        while (!done)
        {
            if (suppressInputUntilKeyUp)
            {
                if (!Input.GetKey(KeyCode.E))
                {
                    suppressInputUntilKeyUp = false;
                }
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                done = true;
            }

            if (!done && autoAdvanceSeconds > 0f)
            {
                timer += Time.unscaledDeltaTime;
                if (timer >= autoAdvanceSeconds)
                {
                    done = true;
                }
            }

            yield return null;
        }

        HideImmediate();
    }

    private void PlayWhistle()
    {
        if (whistleClip == null)
        {
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(whistleClip);
            return;
        }

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(whistleClip, pos);
    }

    private string BuildAnnouncementText()
    {
        if (announcementLines == null || announcementLines.Length == 0)
        {
            return "The beach is now closed.";
        }

        return string.Join("\n", announcementLines);
    }

    private void HideImmediate()
    {
        if (panel != null)
        {
            panel.SetActive(false);
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

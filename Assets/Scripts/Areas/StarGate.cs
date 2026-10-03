using System.Collections;
using UnityEngine;

/// <summary>
/// Doorman star gate: talk with E, read QuestManager.Stars (never spend), unlock path blocker.
/// Put on the Gate root (or a parent). Do NOT put this logic on NPCInteract.
///
/// Animator triggers (optional — ceremony still works without clips):
/// - Player: "Present" (Trigger)
/// - Doorman: "InspectOpen" (Trigger) → falls back to Play("Doorman_open_gate")
/// </summary>
public class StarGate : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string gateId = "gate";
    [SerializeField] private int starsRequired = 1;
    [SerializeField] private string doormanName = "Doorman";

    [Header("Dialogue")]
    [TextArea(1, 3)]
    [SerializeField] private string[] notEnoughLines =
    {
        "You need {0} stars to pass. You've got {1}."
    };
    [TextArea(1, 3)]
    [SerializeField] private string[] enoughLines =
    {
        "You need {0} stars to pass. Let's see them stars, kid."
    };
    [TextArea(1, 3)]
    [SerializeField] private string[] alreadyUnlockedLines =
    {
        "Go on through."
    };

    [Header("Blocker")]
    [SerializeField] private Collider pathBlocker;

    [Header("Interaction")]
    [SerializeField] private float talkRange = 3f;
    [SerializeField] private string talkPromptMessage = "E to talk";
    [SerializeField] private Transform talkLookAt;
    [Tooltip("Assign the doorman character. If empty, finds nearest object named Doorman*.")]
    [SerializeField] private Transform doorman;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private MetalDetectorController metalDetector;

    [Header("Unlock ceremony")]
    [SerializeField] private float presentHoldSeconds = 2f;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator doormanAnimator;
    [SerializeField] private string playerPresentTrigger = "Present";
    [SerializeField] private string doormanInspectTrigger = "InspectOpen";
    [SerializeField] private string doormanOpenStateName = "Doorman_open_gate";

    private bool ceremonyRunning;
    private Coroutine ceremonyRoutine;

    private void Awake()
    {
        ResolveRefs();
        ApplyUnlockedVisualState();
    }

    private void OnEnable()
    {
        AreaUnlockState.OnChanged += ApplyUnlockedVisualState;
        SubscribeCycleReset();
    }

    private void Start()
    {
        SubscribeCycleReset();
        ResolveRefs();
        ApplyUnlockedVisualState();
    }

    private void OnDisable()
    {
        AreaUnlockState.OnChanged -= ApplyUnlockedVisualState;
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnCycleReset -= OnCycleReset;
        }

        if (ceremonyRoutine != null)
        {
            StopCoroutine(ceremonyRoutine);
            ceremonyRoutine = null;
            ceremonyRunning = false;
            SetGameplayPaused(false);
        }
    }

    private void SubscribeCycleReset()
    {
        if (DayManager.Instance == null)
        {
            return;
        }

        DayManager.Instance.OnCycleReset -= OnCycleReset;
        DayManager.Instance.OnCycleReset += OnCycleReset;
    }

    private void Update()
    {
        DialogueUI.UpdateNpcInteractGate();

        if (player == null || ceremonyRunning)
        {
            return;
        }

        float distance = Vector3.Distance(GetTalkOrigin(), player.position);
        bool canTalk = distance <= talkRange
            && IsGameplayInputAllowed()
            && !DialogueUI.IsOpen
            && !DialogueUI.SuppressNpcInteract
            && !QuestLogUI.IsOpen
            && !DaySummaryUI.IsOpen
            && !EconomyMenus.IsAnyOpen
            && !IsDetectorBusy();

        if (canTalk && InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.RequestShow(this, talkPromptMessage, distance);
        }

        if (!canTalk)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartTalk();
        }
    }

    private void StartTalk()
    {
        if (DialogueUI.Instance == null)
        {
            Debug.LogWarning($"StarGate '{gateId}': DialogueUI missing.");
            return;
        }

        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.HideImmediate();
        }

        Transform lookAt = talkLookAt != null ? talkLookAt : (doorman != null ? doorman : transform);
        int stars = QuestManager.Instance != null ? QuestManager.Instance.Stars : 0;

        if (AreaUnlockState.IsUnlocked(gateId))
        {
            DialogueUI.Instance.ShowLines(
                doormanName,
                FormatLines(alreadyUnlockedLines, starsRequired, stars),
                null,
                "E — close",
                lookAt);
            return;
        }

        if (stars < starsRequired)
        {
            DialogueUI.Instance.ShowLines(
                doormanName,
                FormatLines(notEnoughLines, starsRequired, stars),
                null,
                "E — close",
                lookAt);
            return;
        }

        DialogueUI.Instance.ShowLines(
            doormanName,
            FormatLines(enoughLines, starsRequired, stars),
            BeginUnlockCeremony,
            "E — continue",
            lookAt);
    }

    private void BeginUnlockCeremony()
    {
        if (ceremonyRunning || AreaUnlockState.IsUnlocked(gateId))
        {
            return;
        }

        ceremonyRoutine = StartCoroutine(UnlockCeremonyRoutine());
    }

    private IEnumerator UnlockCeremonyRoutine()
    {
        ceremonyRunning = true;

        // DialogueUI already closed and re-enabled input — lock again for the show.
        SetGameplayPaused(true);

        FirePlayerPresent();
        FireDoormanInspect();

        yield return new WaitForSeconds(Mathf.Max(0.1f, presentHoldSeconds));

        if (pathBlocker != null)
        {
            pathBlocker.enabled = false;
        }

        AreaUnlockState.Unlock(gateId);
        ApplyUnlockedVisualState();

        SetGameplayPaused(false);
        ceremonyRunning = false;
        ceremonyRoutine = null;
    }

    private void FirePlayerPresent()
    {
        if (playerAnimator == null || string.IsNullOrEmpty(playerPresentTrigger))
        {
            return;
        }

        if (HasTrigger(playerAnimator, playerPresentTrigger))
        {
            playerAnimator.SetTrigger(playerPresentTrigger);
        }
    }

    private void FireDoormanInspect()
    {
        if (doormanAnimator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(doormanInspectTrigger) && HasTrigger(doormanAnimator, doormanInspectTrigger))
        {
            doormanAnimator.SetTrigger(doormanInspectTrigger);
            return;
        }

        if (!string.IsNullOrEmpty(doormanOpenStateName))
        {
            doormanAnimator.Play(doormanOpenStateName, 0, 0f);
        }
    }

    private void OnCycleReset()
    {
        ApplyUnlockedVisualState();
    }

    /// <summary>Match collider to unlock flag (open = blocker disabled).</summary>
    public void ApplyUnlockedVisualState()
    {
        bool unlocked = AreaUnlockState.IsUnlocked(gateId);
        if (pathBlocker != null)
        {
            pathBlocker.enabled = !unlocked;
        }
    }

    private void ResolveRefs()
    {
        if (player == null)
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                player = movement.transform;
                playerMovement = movement;
            }
        }

        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        if (metalDetector == null && player != null)
        {
            metalDetector = player.GetComponent<MetalDetectorController>();
        }

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }

        if (playerAnimator == null && player != null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>();
        }

        if (pathBlocker == null)
        {
            pathBlocker = GetComponent<Collider>();
        }

        if (doorman == null)
        {
            doorman = FindNearestDoorman();
        }

        if (talkLookAt == null)
        {
            talkLookAt = doorman != null ? doorman : transform;
        }

        if (doormanAnimator == null && doorman != null)
        {
            doormanAnimator = doorman.GetComponentInChildren<Animator>();
        }
    }

    private Transform FindNearestDoorman()
    {
        // Prefer assigned ref; this is a soft auto-find for setup convenience.
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 origin = transform.position;

        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.name.StartsWith("Doorman"))
            {
                continue;
            }

            float d = Vector3.Distance(origin, go.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = go.transform;
            }
        }

        return best;
    }

    private Vector3 GetTalkOrigin()
    {
        return doorman != null ? doorman.position : transform.position;
    }

    private static string[] FormatLines(string[] templates, int required, int current)
    {
        if (templates == null || templates.Length == 0)
        {
            return new[] { "..." };
        }

        string[] result = new string[templates.Length];
        for (int i = 0; i < templates.Length; i++)
        {
            string line = templates[i] ?? "...";
            result[i] = string.Format(line, required, current);
        }

        return result;
    }

    private static bool HasTrigger(Animator animator, string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger
                && parameters[i].name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGameplayInputAllowed()
    {
        if (playerMovement != null && !playerMovement.InputEnabled)
        {
            return false;
        }

        if (DayManager.Instance != null && DayManager.Instance.IsDayTransitioning)
        {
            return false;
        }

        return true;
    }

    private bool IsDetectorBusy()
    {
        return metalDetector != null && metalDetector.BlocksNpcTalk;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.7f);
        Vector3 origin = Application.isPlaying
            ? GetTalkOrigin()
            : (doorman != null ? doorman.position : transform.position);
        Gizmos.DrawWireSphere(origin, talkRange);
    }
}

using TMPro;
using UnityEngine;

/// <summary>
/// Talk to an NPC. Quest + flavor dialogue come from QuestDefinition and/or NPCDefinition.
/// Placement is handled by NPCSchedule when present.
/// Dialogue opens immediately after mutual Y-facing.
/// DialogueUI drives the talk camera (EnterTalkMode / ExitTalkMode). No talk staging.
/// </summary>
public class NPCInteract : MonoBehaviour
{
    [Header("Profile (optional)")]
    [SerializeField] private NPCDefinition npcProfile;
    [Tooltip("Overrides quest from NPCDefinition when set.")]
    [SerializeField] private QuestDefinition quest;
    [Tooltip("Inline flavor lines when no NPCDefinition is used.")]
    [SerializeField] private FlavorDialogueEntry[] flavorDialogue;

    [Header("Interaction")]
    [SerializeField] private float talkRange = 3f;
    [SerializeField] private float faceTurnDuration = 0.28f;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MetalDetectorController metalDetector;
    [Tooltip("Legacy world-space prompt. Prefer InteractionPromptUI on the player Canvas.")]
    [SerializeField] private GameObject talkPrompt;
    [SerializeField] private TMP_Text talkPromptText;
    [SerializeField] private string talkPromptMessage = "E to talk";

    [Header("Face (optional)")]
    [Tooltip("Leave empty to use a FaceAnimator on this character.")]
    [SerializeField] private FaceAnimator faceAnimator;
    [Tooltip("Hold one atlas frame for the whole conversation. Off = do nothing.")]
    [SerializeField] private bool holdFaceWhileTalking;
    [Tooltip("Atlas frame while talking. Tara happy = 4.")]
    [SerializeField] private int talkFaceFrame = 4;

    private bool holdingTalkFace;
    private Quaternion savedNpcRotation;
    private bool shouldRestoreNpcFacing;
    private Rigidbody playerRigidbody;
    private ThirdPersonCamera thirdPersonCamera;
    private Coroutine faceTurnRoutine;
    private Coroutine restoreFacingRoutine;
    private QuestDefinition ResolvedQuest => quest != null ? quest : (npcProfile != null ? npcProfile.quest : null);

    private void Awake()
    {
        SetLegacyTalkPrompt(false);

        if (talkPromptText != null)
        {
            talkPromptText.text = talkPromptMessage;
        }

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

        if (holdFaceWhileTalking && faceAnimator == null)
        {
            faceAnimator = GetComponentInChildren<FaceAnimator>();
        }

        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }

        thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
    }

    private void OnEnable()
    {
        DialogueUI.Closed += OnDialogueClosed;
    }

    private void OnDisable()
    {
        DialogueUI.Closed -= OnDialogueClosed;
        StopFaceTurn();
        StopRestoreFacing();
        ReleaseTalkFace();
        RestoreNpcFacingImmediate();
        SetLegacyTalkPrompt(false);
    }

    private void Update()
    {
        DialogueUI.UpdateNpcInteractGate();

        if (player == null || !CanTalkAtAll())
        {
            SetLegacyTalkPrompt(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool playerInRange = distance <= talkRange;
        bool canTalk = playerInRange
            && IsGameplayInputAllowed()
            && !DialogueUI.IsOpen
            && !DialogueUI.SuppressNpcInteract
            && !QuestLogUI.IsOpen
            && !DaySummaryUI.IsOpen
            && !EconomyMenus.IsAnyOpen
            && !IsDetectorBusy();

        if (canTalk)
        {
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.RequestShow(this, talkPromptMessage, distance);
            }
            else
            {
                SetLegacyTalkPrompt(true);
            }
        }
        else
        {
            SetLegacyTalkPrompt(false);
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

    private bool CanTalkAtAll()
    {
        return ResolvedQuest != null
            || (npcProfile != null && FlavorDialogueHelper.HasAnyEntries(npcProfile.flavorDialogue))
            || FlavorDialogueHelper.HasAnyEntries(flavorDialogue);
    }

    private void StartTalk()
    {
        if (DialogueUI.Instance == null)
        {
            Debug.LogWarning("NPCInteract: DialogueUI missing.");
            return;
        }

        if (talkPrompt != null)
        {
            talkPrompt.SetActive(false);
        }

        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.HideImmediate();
        }

        HoldTalkFace();
        BeginTalkAfterFacing(() => OpenResolvedDialogue());
    }

    private void OpenResolvedDialogue()
    {
        int day = GetCurrentDay();
        QuestDefinition activeQuest = ResolvedQuest;

        if (activeQuest == null)
        {
            ShowFlavorDialogue(day);
            return;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("NPCInteract: QuestManager missing.");
            ReleaseTalkFace();
            CancelTalkFacing();
            return;
        }

        bool questUnlockedForTalk = IsQuestUnlockedForTalk(day, activeQuest);
        ActiveQuest active = QuestManager.Instance.FindActive(activeQuest.questId);

        if (questUnlockedForTalk && active != null && active.Status == QuestStatus.ReadyToTurnIn)
        {
            ShowLines(activeQuest.turnInDialogueLines,
                $"You found my {activeQuest.itemDisplayName}! Thank you so much!",
                () => QuestManager.Instance.CompleteQuest(activeQuest.questId),
                "E — return item");
            return;
        }

        if (questUnlockedForTalk && active != null && active.Status == QuestStatus.Active)
        {
            ShowLines(activeQuest.searchingDialogueLines,
                $"Any luck finding my {activeQuest.itemDisplayName}? Keep searching near {activeQuest.locationHint}.",
                null,
                "E — close");
            return;
        }

        if (QuestManager.Instance.IsCompleted(activeQuest.questId))
        {
            ShowCompletedDialogue(day, activeQuest);
            return;
        }

        if (QuestManager.Instance.CanOfferQuest(activeQuest))
        {
            ShowQuestOffer(day, activeQuest);
            return;
        }

        ShowBlockedOrFlavorDialogue(day, activeQuest);
    }

    private void ShowQuestOffer(int day, QuestDefinition activeQuest)
    {
        if (day >= 2
            && activeQuest.GetEarliestOfferDay() <= 1
            && activeQuest.day2IncompleteDialogueLines != null
            && activeQuest.day2IncompleteDialogueLines.Length > 0)
        {
            DialogueUI.Instance.ShowOffer(activeQuest, activeQuest.day2IncompleteDialogueLines, transform);
            return;
        }

        DialogueUI.Instance.ShowOffer(activeQuest, null, transform);
    }

    private void ShowBlockedOrFlavorDialogue(int day, QuestDefinition activeQuest)
    {
        string[] flavor = GetFlavorLines(day);
        if (flavor != null)
        {
            ShowLines(flavor, "...", null, "E — close");
            return;
        }

        string[] lines = activeQuest.GetBlockedDialogueForDay(day);
        string fallback = day switch
        {
            1 => "Not right now.",
            2 => "Come back later.",
            _ => "Maybe another time."
        };

        ShowLines(lines, fallback, null, "E — close");
    }

    private void ShowFlavorDialogue(int day)
    {
        string[] lines = GetFlavorLines(day);
        ShowLines(lines, "Nice day at the beach.", null, "E — close");
    }

    private string[] GetFlavorLines(int day)
    {
        QuestManager qm = QuestManager.Instance;
        StoryFlags flags = StoryFlags.Instance;

        if (npcProfile != null)
        {
            string[] fromProfile = npcProfile.GetBestFlavorDialogue(day, qm, flags);
            if (fromProfile != null)
            {
                return fromProfile;
            }
        }

        return FlavorDialogueHelper.GetBestEntry(flavorDialogue, day, qm, flags);
    }

    private void ShowCompletedDialogue(int day, QuestDefinition activeQuest)
    {
        QuestManager qm = QuestManager.Instance;
        StoryFlags flags = StoryFlags.Instance;

        string[] lines = activeQuest.GetBestCompletedDialogue(day, qm, flags);

        if (lines == null && day >= 2)
        {
            lines = Pick(activeQuest.day2CompletedDialogueLines, activeQuest.completedDialogueLines,
                "Thanks again for finding my things!");
        }

        if (lines == null)
        {
            lines = GetLines(activeQuest.completedDialogueLines, "Thanks again!");
        }

        ShowLines(lines, "Thanks again!", null, "E — close");
    }

    private bool IsQuestUnlockedForTalk(int day, QuestDefinition activeQuest)
    {
        if (QuestManager.Instance.HasQuest(activeQuest.questId)
            || QuestManager.Instance.IsCompleted(activeQuest.questId))
        {
            return true;
        }

        return day >= activeQuest.GetEarliestOfferDay();
    }

    private void HoldTalkFace()
    {
        if (!holdFaceWhileTalking || talkFaceFrame < 0)
        {
            return;
        }

        if (faceAnimator == null)
        {
            faceAnimator = GetComponentInChildren<FaceAnimator>();
        }

        if (faceAnimator == null)
        {
            return;
        }

        holdingTalkFace = true;
        faceAnimator.HoldFrame(talkFaceFrame);
    }

    private void ReleaseTalkFace()
    {
        if (!holdingTalkFace)
        {
            return;
        }

        holdingTalkFace = false;
        faceAnimator?.ClearHold();
    }

    private void OnDialogueClosed()
    {
        ReleaseTalkFace();
        BeginRestoreNpcFacing();
    }

    /// <summary>
    /// Mutual Y-facing only — no slide, no TalkStandPoint, no staging.
    /// Quick ease, then open dialogue.
    /// </summary>
    private void BeginTalkAfterFacing(System.Action openDialogue)
    {
        StopFaceTurn();
        faceTurnRoutine = StartCoroutine(FaceEachOtherThenOpen(openDialogue));
    }

    private System.Collections.IEnumerator FaceEachOtherThenOpen(System.Action openDialogue)
    {
        if (player == null)
        {
            openDialogue?.Invoke();
            faceTurnRoutine = null;
            yield break;
        }

        savedNpcRotation = transform.rotation;
        shouldRestoreNpcFacing = true;

        if (playerMovement != null)
        {
            playerMovement.SetInputEnabled(false);
        }

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }

        Quaternion playerStart = playerRigidbody != null ? playerRigidbody.rotation : player.rotation;
        Quaternion npcStart = transform.rotation;

        Vector3 toNpc = transform.position - player.position;
        toNpc.y = 0f;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        Quaternion playerEnd = playerStart;
        if (toNpc.sqrMagnitude > 0.0001f)
        {
            playerEnd = Quaternion.LookRotation(toNpc.normalized, Vector3.up);
        }

        Quaternion npcEnd = npcStart;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            npcEnd = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }

        float duration = Mathf.Max(0.01f, faceTurnDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            // Ease-out so it feels snappy without a hard snap.
            u = 1f - (1f - u) * (1f - u);

            ApplyPlayerRotation(Quaternion.Slerp(playerStart, playerEnd, u));
            transform.rotation = Quaternion.Slerp(npcStart, npcEnd, u);
            thirdPersonCamera?.SyncYawToTarget();
            yield return null;
        }

        ApplyPlayerRotation(playerEnd);
        transform.rotation = npcEnd;
        thirdPersonCamera?.SyncYawToTarget();

        faceTurnRoutine = null;
        openDialogue?.Invoke();
    }

    private void ApplyPlayerRotation(Quaternion rotation)
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.MoveRotation(rotation);
        }
        else if (player != null)
        {
            player.rotation = rotation;
        }
    }

    private void StopFaceTurn()
    {
        if (faceTurnRoutine == null)
        {
            return;
        }

        StopCoroutine(faceTurnRoutine);
        faceTurnRoutine = null;
    }

    private void CancelTalkFacing()
    {
        StopFaceTurn();
        RestoreNpcFacingImmediate();

        if (playerMovement != null && !DialogueUI.IsOpen)
        {
            playerMovement.SetInputEnabled(true);
        }
    }

    private void BeginRestoreNpcFacing()
    {
        if (!shouldRestoreNpcFacing)
        {
            return;
        }

        StopRestoreFacing();
        restoreFacingRoutine = StartCoroutine(RestoreNpcFacingOverTime());
    }

    private System.Collections.IEnumerator RestoreNpcFacingOverTime()
    {
        Quaternion start = transform.rotation;
        Quaternion end = savedNpcRotation;
        shouldRestoreNpcFacing = false;

        float duration = Mathf.Max(0.01f, faceTurnDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            u = 1f - (1f - u) * (1f - u);
            transform.rotation = Quaternion.Slerp(start, end, u);
            yield return null;
        }

        transform.rotation = end;
        restoreFacingRoutine = null;
    }

    private void RestoreNpcFacingImmediate()
    {
        StopRestoreFacing();

        if (!shouldRestoreNpcFacing)
        {
            return;
        }

        shouldRestoreNpcFacing = false;
        transform.rotation = savedNpcRotation;
    }

    private void StopRestoreFacing()
    {
        if (restoreFacingRoutine == null)
        {
            return;
        }

        StopCoroutine(restoreFacingRoutine);
        restoreFacingRoutine = null;
    }

    private void ShowLines(string[] configured, string fallback, System.Action onDone, string finalHint)
    {
        DialogueUI.Instance.ShowLines(GetDisplayName(), GetLines(configured, fallback), onDone, finalHint, transform);
    }

    private string GetDisplayName()
    {
        if (npcProfile != null && !string.IsNullOrEmpty(npcProfile.GetDisplayName()))
        {
            return npcProfile.GetDisplayName();
        }

        if (ResolvedQuest != null && !string.IsNullOrEmpty(ResolvedQuest.npcName))
        {
            return ResolvedQuest.npcName;
        }

        return gameObject.name;
    }

    private static string[] GetLines(string[] configured, string fallback)
    {
        if (configured != null && configured.Length > 0)
        {
            return configured;
        }

        return new[] { fallback };
    }

    private static string[] Pick(string[] preferred, string[] secondary, string fallback)
    {
        if (preferred != null && preferred.Length > 0)
        {
            return preferred;
        }

        return GetLines(secondary, fallback);
    }

    private static int GetCurrentDay()
    {
        return DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
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

    private void SetLegacyTalkPrompt(bool visible)
    {
        if (talkPrompt != null)
        {
            talkPrompt.SetActive(visible);
        }
    }

    private bool IsDetectorBusy()
    {
        return metalDetector != null && metalDetector.BlocksNpcTalk;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, talkRange);
    }
}

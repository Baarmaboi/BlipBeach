using UnityEngine;

/// <summary>
/// World stall: E opens the Canvas MarketUI. Place this on a stall mesh in the scene.
/// </summary>
public class MarketStall : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private string promptMessage = "E — market (sell treasures)";
    [Tooltip("World camera pose for the shop shot (e.g. child ShopCamPosition).")]
    [SerializeField] private Transform shopCamPosition;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MetalDetectorController metalDetector;

    private void Awake()
    {
        ResolvePlayer();
    }

    private void Update()
    {
        DialogueUI.UpdateNpcInteractGate();

        if (player == null)
        {
            ResolvePlayer();
            if (player == null)
            {
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool canUse = distance <= interactRange
            && IsGameplayInputAllowed()
            && !DialogueUI.IsOpen
            && !DialogueUI.SuppressNpcInteract
            && !QuestLogUI.IsOpen
            && !DaySummaryUI.IsOpen
            && !EconomyMenus.IsAnyOpen
            && !IsDetectorBusy();

        if (canUse && InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.RequestShow(this, promptMessage, distance);
        }

        if (!canUse)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            OpenMarket();
        }
    }

    private void OpenMarket()
    {
        if (MarketUI.Instance == null)
        {
            Debug.LogWarning("MarketStall: MarketUI missing from the Canvas.");
            return;
        }

        if (shopCamPosition == null)
        {
            Debug.LogWarning("MarketStall: ShopCamPosition not assigned — opening UI without shop camera shot.");
        }

        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.HideImmediate();
        }

        MarketUI.Instance.Open(shopCamPosition);
    }

    private void ResolvePlayer()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

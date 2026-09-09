using UnityEngine;

/// <summary>
/// Something diggable under the sand.
/// Lost items (quest) need UnlockForQuest(); treasure/clutter is detectable immediately.
/// </summary>
public class BuriedItem : MonoBehaviour
{
    [SerializeField] private string itemId = "";
    [Tooltip("True = lost/quest item (hidden until quest accepted). False = buried treasure/clutter.")]
    [SerializeField] private bool requiresQuest;

    [Header("Reward panel text")]
    [Tooltip("Shown for lost items, e.g. Watch / Engagement Ring.")]
    [SerializeField] private string displayName = "";
    [Tooltip("Owner for lost items, e.g. Tara / Tom. Leave empty for treasure.")]
    [SerializeField] private string ownerName = "";

    [Header("Treasure (non-quest)")]
    [Tooltip("Assigned by DigAreaSpawner for clutter. Leave empty on Lost Items.")]
    [SerializeField] private TreasureDefinition treasureDefinition;
    [Tooltip("Simulated depth 1-3. Lost Items ignore this.")]
    [SerializeField] private DepthLevel treasureDepth = DepthLevel.One;

    private Collider[] colliders;
    private bool isDetectable;

    public string ItemId => itemId;
    public bool RequiresQuest => requiresQuest;
    public string DisplayName => displayName;
    public string OwnerName => ownerName;
    public TreasureDefinition Treasure => treasureDefinition;
    public bool IsTreasure => !requiresQuest && treasureDefinition != null;
    public DepthLevel TreasureDepth => treasureDepth;
    public bool IsDug { get; private set; }
    public bool IsDetectable => isDetectable;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        // Quest items stay hidden until UnlockForQuest(); clutter is detectable immediately.
        SetDetectable(!requiresQuest);
    }

    public void UnlockForQuest()
    {
        SetDetectable(true);
    }

    /// <summary>Turns this buried clutter into a sellable treasure. No-op for quest items.</summary>
    public void ApplyTreasure(TreasureDefinition definition)
    {
        if (requiresQuest || definition == null)
        {
            return;
        }

        treasureDefinition = definition;
        displayName = definition.FullDisplayName;
        ownerName = "";
        treasureDepth = DepthLevel.One;
    }

    /// <summary>Set once at spawn. No-op for quest items. Do not re-roll on dig.</summary>
    public void SetTreasureDepth(DepthLevel depth)
    {
        if (requiresQuest)
        {
            return;
        }

        treasureDepth = depth;
    }

    public void Dig()
    {
        if (IsDug)
        {
            return;
        }

        IsDug = true;

        if (IsTreasure && PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.Add(treasureDefinition);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.NotifyItemDug(this);
        }
    }

    public string GetRewardTitle()
    {
        if (IsTreasure)
        {
            return "Treasure found";
        }

        return requiresQuest ? "Lost item found" : "Buried item found";
    }

    public string GetRewardBody()
    {
        if (IsTreasure)
        {
            return $"You found a {treasureDefinition.FullDisplayName} — {DepthLevels.RangeLabel(treasureDepth)}!";
        }

        if (!requiresQuest)
        {
            return "You found a buried item.";
        }

        bool hasOwner = !string.IsNullOrEmpty(ownerName);
        bool hasName = !string.IsNullOrEmpty(displayName);

        if (hasOwner && hasName)
        {
            return $"You found a lost item — {ownerName}'s {displayName}!";
        }

        if (hasName)
        {
            return $"You found a lost item — {displayName}!";
        }

        if (hasOwner)
        {
            return $"You found a lost item belonging to {ownerName}!";
        }

        return "You found a lost item.";
    }

    private void SetDetectable(bool detectable)
    {
        isDetectable = detectable;

        if (colliders == null)
        {
            colliders = GetComponentsInChildren<Collider>(true);
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = detectable;
            }
        }
    }
}

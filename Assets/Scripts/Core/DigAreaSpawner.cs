using System.Collections.Generic;
using UnityEngine;

public class DigAreaSpawner : MonoBehaviour
{
    [Header("Clutter / Treasure")]
    [SerializeField] private BuriedItem clutterPrefab;
    [SerializeField] private int clutterCount = 5;
    [Tooltip("If empty, all common metals. Set e.g. Lead+Aluminum on a pier.")]
    [SerializeField] private MetalType[] favoredMetals;
    [SerializeField] [Range(0f, 1f)] private float preciousChance = 0.06f;

    [Header("Quest Items")]
    [Tooltip("One slot per unique lost item. Each assigned prefab spawns once.")]
    [SerializeField] private BuriedItem[] questItemPrefabs;

    [Header("Placement")]
    [SerializeField] private float minSpacing = 2f;
    [SerializeField] private int maxPlaceAttempts = 30;
    [Tooltip("X/Z = area width. Y = how high above the area the ground raycast starts.")]
    [SerializeField] private Vector3 areaSize = new Vector3(8f, 5f, 8f);
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private bool drawGizmo = true;

    [Header("Debug")]
    [Tooltip("Press this key in Play mode to unlock all quest items under this DigArea.")]
    [SerializeField] private KeyCode debugUnlockKey = KeyCode.U;

    private readonly List<Vector3> placedPositions = new List<Vector3>();
    private TreasureCatalog catalog;

    private void Start()
    {
        catalog = TreasureCatalog.Load();
        SpawnAll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugUnlockKey))
        {
            DebugUnlockAllQuestItems();
        }
    }

    private void SpawnAll()
    {
        placedPositions.Clear();

        bool hasQuestSlots = questItemPrefabs != null && questItemPrefabs.Length > 0;
        if (clutterPrefab == null && !hasQuestSlots)
        {
            Debug.LogWarning("DigAreaSpawner: assign a clutter prefab and/or quest item slots.", this);
            return;
        }

        SpawnGroup(clutterPrefab, clutterCount, "Clutter");
        SpawnQuestSlots();
    }

    private void SpawnQuestSlots()
    {
        if (questItemPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < questItemPrefabs.Length; i++)
        {
            BuriedItem prefab = questItemPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            if (!TryFindSpawnPoint(out Vector3 spawnPoint))
            {
                Debug.LogWarning($"DigAreaSpawner: could not place quest slot {i} ({prefab.name}).", this);
                continue;
            }

            BuriedItem item = Instantiate(prefab, spawnPoint, Quaternion.identity, transform);
            item.name = prefab.name;
            placedPositions.Add(spawnPoint);
        }
    }

    private void SpawnGroup(BuriedItem prefab, int count, string label)
    {
        if (prefab == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!TryFindSpawnPoint(out Vector3 spawnPoint))
            {
                Debug.LogWarning($"DigAreaSpawner: could not place {label} item {i + 1}/{count}.", this);
                continue;
            }

            BuriedItem item = Instantiate(prefab, spawnPoint, Quaternion.identity, transform);
            ApplyTreasureIfClutter(item);
            if (!item.IsTreasure)
            {
                item.name = $"{prefab.name}_{i + 1}";
            }
            placedPositions.Add(spawnPoint);
        }
    }

    private bool TryFindSpawnPoint(out Vector3 spawnPoint)
    {
        spawnPoint = Vector3.zero;

        for (int attempt = 0; attempt < maxPlaceAttempts; attempt++)
        {
            Vector3 localPoint = new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                areaSize.y,
                Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f));

            Vector3 worldPoint = transform.TransformPoint(localPoint);

            if (!Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, areaSize.y * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (!HasMinSpacing(hit.point))
            {
                continue;
            }

            spawnPoint = hit.point;
            return true;
        }

        return false;
    }

    private bool HasMinSpacing(Vector3 candidate)
    {
        float minSpacingSqr = minSpacing * minSpacing;

        for (int i = 0; i < placedPositions.Count; i++)
        {
            if ((placedPositions[i] - candidate).sqrMagnitude < minSpacingSqr)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyTreasureIfClutter(BuriedItem item)
    {
        if (item == null || item.RequiresQuest || catalog == null)
        {
            return;
        }

        TreasureDefinition definition = catalog.PickForArea(ResolvedFavoredMetals(), preciousChance);
        item.ApplyTreasure(definition);
        if (definition != null)
        {
            DepthLevel depth = definition.RollDepth();
            item.SetTreasureDepth(depth);
            item.name = $"{item.DisplayName} [{DepthLevels.RangeLabel(depth)}]";
        }
    }

    private MetalType[] ResolvedFavoredMetals()
    {
        if (favoredMetals != null && favoredMetals.Length > 0)
        {
            return favoredMetals;
        }

        return MetalTypes.Common;
    }

    [ContextMenu("Debug/Unlock All Quest Items In DigArea")]
    public void DebugUnlockAllQuestItems()
    {
        BuriedItem[] items = GetComponentsInChildren<BuriedItem>(true);
        int unlocked = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].RequiresQuest && !items[i].IsDetectable)
            {
                items[i].UnlockForQuest();
                unlocked++;
            }
        }

        Debug.Log($"DigAreaSpawner: unlocked {unlocked} quest item(s).", this);
    }

    [ContextMenu("Debug/Unlock Quest Item By Prefab ItemId")]
    public void DebugUnlockQuestItemByPrefabId()
    {
        if (questItemPrefabs == null || questItemPrefabs.Length == 0)
        {
            Debug.LogWarning("DigAreaSpawner: no quest item slots assigned.", this);
            return;
        }

        // Unlocks the first slot that has an itemId (handy for quick testing).
        for (int i = 0; i < questItemPrefabs.Length; i++)
        {
            if (questItemPrefabs[i] != null && !string.IsNullOrEmpty(questItemPrefabs[i].ItemId))
            {
                UnlockByItemId(questItemPrefabs[i].ItemId);
                return;
            }
        }

        Debug.LogWarning("DigAreaSpawner: no quest slot prefab has an itemId set.", this);
    }

    public void UnlockByItemId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        BuriedItem[] items = GetComponentsInChildren<BuriedItem>(true);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].ItemId == id && items[i].RequiresQuest)
            {
                items[i].UnlockForQuest();
                Debug.Log($"DigAreaSpawner: unlocked itemId '{id}'.", this);
                return;
            }
        }

        Debug.LogWarning($"DigAreaSpawner: no quest item with itemId '{id}' found under this DigArea.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.up * (areaSize.y * 0.5f), areaSize);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.up * (areaSize.y * 0.5f), areaSize);
    }
}

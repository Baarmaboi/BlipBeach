using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Market sell UI: metal tabs, clickable item list, Sell All + Exit buttons.
/// Title / tab labels / button labels are authored in the Canvas — not written by code.
/// </summary>
public class MarketUI : MonoBehaviour
{
    public static MarketUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [Tooltip("ScrollRect Content — rows spawn here.")]
    [SerializeField] private Transform itemListRoot;
    [SerializeField] private MarketSellRow itemRowPrefab;
    [SerializeField] private Button sellAllButton;
    [SerializeField] private Button exitButton;
    [Tooltip("Optional. If empty, tabs are found under panel/Tabs by name.")]
    [SerializeField] private MarketMetalTab[] metalTabs;

    [Header("Optional legacy")]
    [Tooltip("Left empty — shop chrome text is authored in the Canvas.")]
    [SerializeField] private TMP_Text bodyText;

    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private ShopCommentsUI shopComments;

    private bool suppressInputUntilKeyUp;
    private bool enteredFixedShot;
    private readonly List<MarketSellRow> activeRows = new List<MarketSellRow>();
    private readonly List<TabEntry> tabs = new List<TabEntry>();
    private int selectedTabIndex;
    private bool filterShowAll = true;
    private MetalType filterMetal;

    private struct TabEntry
    {
        public Button button;
        public bool showAll;
        public MetalType metal;
        public MarketMetalTab tabComponent;
    }

    private void Awake()
    {
        Instance = this;

        ResolveReferences();
        WireStaticButtons();
        CollectTabs();

        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;
    }

    private void OnDestroy()
    {
        UnwireStaticButtons();

        if (Instance == this)
        {
            Instance = null;
        }

        IsOpen = false;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // Late resolve if Canvas children weren't ready in Awake.
        ResolveReferences();
        WireStaticButtons();
        if (tabs.Count == 0)
        {
            CollectTabs();
        }

        Subscribe();
        SelectTab(0, refreshList: false);
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged -= Refresh;
        }

        if (DetectorProgress.Instance != null)
        {
            DetectorProgress.Instance.OnChanged -= Refresh;
        }
    }

    private void Subscribe()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged -= Refresh;
            PlayerInventory.Instance.OnChanged += Refresh;
        }

        if (DetectorProgress.Instance != null)
        {
            DetectorProgress.Instance.OnChanged -= Refresh;
            DetectorProgress.Instance.OnChanged += Refresh;
        }
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (suppressInputUntilKeyUp)
        {
            if (!Input.GetKey(KeyCode.E) && !Input.GetKey(KeyCode.Q))
            {
                suppressInputUntilKeyUp = false;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TrySellAll();
        }
    }

    public void Open(Transform shopCam = null)
    {
        if (panel == null)
        {
            Debug.LogWarning("MarketUI: assign a Panel on the Canvas.");
            return;
        }

        if (InventoryUI.IsOpen && InventoryUI.Instance != null)
        {
            InventoryUI.Instance.Close();
        }

        if (QuestLogUI.IsOpen && QuestLogUI.Instance != null)
        {
            QuestLogUI.Instance.Close();
        }

        if (ShopUI.IsOpen && ShopUI.Instance != null)
        {
            ShopUI.Instance.Close();
        }

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }

        enteredFixedShot = false;
        if (shopCam != null && thirdPersonCamera != null)
        {
            thirdPersonCamera.EnterFixedShot(shopCam);
            enteredFixedShot = true;
        }

        panel.SetActive(true);
        IsOpen = true;
        suppressInputUntilKeyUp = Input.GetKey(KeyCode.E);
        SelectTab(0, refreshList: true);
        SetGameplayPaused(true);
        shopComments?.ShowWelcome();
    }

    public void Close()
    {
        shopComments?.Hide();

        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;

        if (enteredFixedShot && thirdPersonCamera != null)
        {
            thirdPersonCamera.ExitFixedShot();
            enteredFixedShot = false;
        }

        if (!DialogueUI.IsOpen && !DaySummaryUI.IsOpen && !QuestLogUI.IsOpen
            && !InventoryUI.IsOpen && !ShopUI.IsOpen)
        {
            SetGameplayPaused(false);
        }
    }

    public void Refresh()
    {
        if (!IsOpen && (panel == null || !panel.activeInHierarchy))
        {
            return;
        }

        RebuildItemList();
    }

    private void ResolveReferences()
    {
        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }

        if (shopComments == null)
        {
            shopComments = FindFirstObjectByType<ShopCommentsUI>(FindObjectsInactive.Include);
        }

        if (panel == null)
        {
            return;
        }

        if (itemListRoot == null)
        {
            ScrollRect scroll = panel.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null && scroll.content != null)
            {
                itemListRoot = scroll.content;
            }
        }

        if (sellAllButton == null)
        {
            sellAllButton = FindButtonByName(panel.transform, "Sell All Button");
        }

        if (exitButton == null)
        {
            exitButton = FindButtonByName(panel.transform, "Exit  Button")
                ?? FindButtonByName(panel.transform, "Exit Button");
        }
    }

    private void WireStaticButtons()
    {
        if (sellAllButton != null)
        {
            sellAllButton.onClick.RemoveListener(TrySellAll);
            sellAllButton.onClick.AddListener(TrySellAll);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Close);
            exitButton.onClick.AddListener(Close);
        }
    }

    private void UnwireStaticButtons()
    {
        if (sellAllButton != null)
        {
            sellAllButton.onClick.RemoveListener(TrySellAll);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Close);
        }
    }

    private void CollectTabs()
    {
        foreach (TabEntry entry in tabs)
        {
            if (entry.button != null)
            {
                entry.button.onClick.RemoveAllListeners();
            }
        }

        tabs.Clear();

        if (metalTabs != null && metalTabs.Length > 0)
        {
            for (int i = 0; i < metalTabs.Length; i++)
            {
                MarketMetalTab tab = metalTabs[i];
                if (tab == null)
                {
                    continue;
                }

                Button button = tab.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                AddTab(button, tab.showAll, tab.metal, tab);
            }
        }
        else if (panel != null)
        {
            Transform tabsRoot = panel.transform.Find("Tabs");
            if (tabsRoot == null)
            {
                tabsRoot = FindDeepChild(panel.transform, "Tabs");
            }

            if (tabsRoot != null)
            {
                for (int i = 0; i < tabsRoot.childCount; i++)
                {
                    Transform child = tabsRoot.GetChild(i);
                    Button button = child.GetComponent<Button>();
                    if (button == null)
                    {
                        continue;
                    }

                    MarketMetalTab tabComp = child.GetComponent<MarketMetalTab>();
                    if (tabComp != null)
                    {
                        AddTab(button, tabComp.showAll, tabComp.metal, tabComp);
                        continue;
                    }

                    if (TryParseTabName(child.name, out bool showAll, out MetalType metal))
                    {
                        AddTab(button, showAll, metal, null);
                    }
                }
            }
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            tabs[i].button.onClick.AddListener(() => SelectTab(index, refreshList: true));
        }
    }

    private void AddTab(Button button, bool showAll, MetalType metal, MarketMetalTab tabComponent)
    {
        tabs.Add(new TabEntry
        {
            button = button,
            showAll = showAll,
            metal = metal,
            tabComponent = tabComponent
        });
    }

    private static bool TryParseTabName(string objectName, out bool showAll, out MetalType metal)
    {
        showAll = false;
        metal = MetalType.Iron;

        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        if (objectName.Equals("TabAll", StringComparison.OrdinalIgnoreCase)
            || objectName.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            showAll = true;
            return true;
        }

        string token = objectName;
        if (token.StartsWith("Tab", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(3);
        }

        if (Enum.TryParse(token, ignoreCase: true, out metal))
        {
            return true;
        }

        return false;
    }

    private void SelectTab(int index, bool refreshList)
    {
        if (tabs.Count == 0)
        {
            filterShowAll = true;
            if (refreshList)
            {
                RebuildItemList();
            }

            return;
        }

        selectedTabIndex = Mathf.Clamp(index, 0, tabs.Count - 1);
        TabEntry selected = tabs[selectedTabIndex];
        filterShowAll = selected.showAll;
        filterMetal = selected.metal;
        RefreshTabVisuals();

        if (refreshList)
        {
            RebuildItemList();
        }
    }

    private void RefreshTabVisuals()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            Button button = tabs[i].button;
            if (button == null)
            {
                continue;
            }

            // Selected tab stays non-interactable so it reads as “active”.
            button.interactable = i != selectedTabIndex;
        }
    }

    private void RebuildItemList()
    {
        ClearItemRows();

        if (itemListRoot == null)
        {
            Debug.LogWarning("MarketUI: Item List Root / Scroll Content missing.");
            return;
        }

        EnsureItemListLayout();

        var items = PlayerInventory.Instance != null ? PlayerInventory.Instance.Treasures : null;
        MarketManager market = MarketManager.Instance;
        if (items == null || items.Count == 0)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            TreasureDefinition def = items[i];
            if (def == null || !PassesFilter(def))
            {
                continue;
            }

            int price = market != null ? market.GetSellPrice(def) : def.baseValue;
            string label = $"{def.FullDisplayName}  ${price}";

            MarketSellRow row = CreateRow();
            if (row == null)
            {
                continue;
            }

            PrepareRowForListLayout(row);
            int inventoryIndex = i;
            row.Bind(inventoryIndex, label, TrySellAt);
            activeRows.Add(row);
        }
    }

    private bool PassesFilter(TreasureDefinition def)
    {
        return filterShowAll || def.metalType == filterMetal;
    }

    private MarketSellRow CreateRow()
    {
        if (itemRowPrefab != null)
        {
            return Instantiate(itemRowPrefab, itemListRoot);
        }

        return CreateFallbackRow(itemListRoot);
    }

    private void EnsureItemListLayout()
    {
        if (itemListRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layout = itemListRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = itemListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = Mathf.Max(layout.spacing, 4f);

        ContentSizeFitter fitter = itemListRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = itemListRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void PrepareRowForListLayout(MarketSellRow row)
    {
        if (row == null)
        {
            return;
        }

        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = row.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = 36f;
        layoutElement.preferredHeight = 36f;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 0f;

        RectTransform rect = row.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 36f);
        rect.anchoredPosition = Vector2.zero;
    }

    private static MarketSellRow CreateFallbackRow(Transform parent)
    {
        GameObject root = new GameObject("MarketSellRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MarketSellRow));
        root.transform.SetParent(parent, false);

        Image image = root.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(root.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 2f);
        textRect.offsetMax = new Vector2(-10f, -2f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return root.GetComponent<MarketSellRow>();
    }

    private void ClearItemRows()
    {
        activeRows.Clear();

        if (itemListRoot == null)
        {
            return;
        }

        for (int i = itemListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(itemListRoot.GetChild(i).gameObject);
        }
    }

    private void TrySellAt(int index)
    {
        if (!IsOpen)
        {
            return;
        }

        PlayerInventory inventory = PlayerInventory.Instance;
        MarketManager market = MarketManager.Instance;
        if (inventory == null || market == null)
        {
            return;
        }

        if (inventory.SellAt(index, market) <= 0)
        {
            return;
        }

        shopComments?.ShowSellReaction();
    }

    private void TrySellAll()
    {
        if (!IsOpen)
        {
            return;
        }

        PlayerInventory inventory = PlayerInventory.Instance;
        MarketManager market = MarketManager.Instance;
        if (inventory == null || market == null || inventory.Treasures.Count == 0)
        {
            return;
        }

        if (filterShowAll)
        {
            if (inventory.Treasures.Count == 0)
            {
                return;
            }

            inventory.SellAll(market);
        }
        else
        {
            bool soldAny = false;
            for (int i = inventory.Treasures.Count - 1; i >= 0; i--)
            {
                TreasureDefinition def = inventory.Treasures[i];
                if (def != null && def.metalType == filterMetal)
                {
                    if (inventory.SellAt(i, market) > 0)
                    {
                        soldAny = true;
                    }
                }
            }

            if (!soldAny)
            {
                return;
            }
        }

        shopComments?.ShowSellReaction();
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

    private static Button FindButtonByName(Transform root, string objectName)
    {
        Transform found = FindDeepChild(root, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, objectName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}

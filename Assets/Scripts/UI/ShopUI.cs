using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detector upgrade shop UI (ShopPanel). Lists buyable upgrades; no metal tabs / sell-all.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [Tooltip("ScrollRect Content — upgrade rows spawn here.")]
    [SerializeField] private Transform itemListRoot;
    [SerializeField] private ShopUpgradeRow upgradeRowPrefab;
    [SerializeField] private Button exitButton;

    [Header("Upgrade labels (base text; price/status appended in code)")]
    [SerializeField] private string depthModule1Label = "Depth module 15–30cm";
    [SerializeField] private string depthModule2Label = "Depth module 30–50cm";

    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private ShopCommentsUI shopComments;

    private bool suppressInputUntilKeyUp;
    private bool enteredFixedShot;
    private readonly List<ShopUpgradeRow> activeRows = new List<ShopUpgradeRow>();

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
        WireExitButton();

        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;
    }

    private void OnDestroy()
    {
        UnwireExitButton();

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
        ResolveReferences();
        WireExitButton();
        Subscribe();
    }

    private void OnDisable()
    {
        if (DetectorProgress.Instance != null)
        {
            DetectorProgress.Instance.OnChanged -= Refresh;
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged -= Refresh;
        }
    }

    private void Subscribe()
    {
        if (DetectorProgress.Instance != null)
        {
            DetectorProgress.Instance.OnChanged -= Refresh;
            DetectorProgress.Instance.OnChanged += Refresh;
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged -= Refresh;
            PlayerInventory.Instance.OnChanged += Refresh;
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
        }
    }

    public void Open(Transform shopCam = null)
    {
        if (panel == null)
        {
            Debug.LogWarning("ShopUI: assign ShopPanel on the Canvas.");
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

        if (MarketUI.IsOpen && MarketUI.Instance != null)
        {
            MarketUI.Instance.Close();
        }

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
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
        Refresh();
        SetGameplayPaused(true);
        shopComments?.ShowWelcome(ShopCommentSource.UpgradeShop);
    }

    public void Close()
    {
        shopComments?.Hide();
        ClearRows();

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
            && !InventoryUI.IsOpen && !MarketUI.IsOpen)
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

        RebuildUpgradeList();
    }

    private void ResolveReferences()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

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

        if (exitButton == null)
        {
            Transform found = FindDeepChild(panel.transform, "Exit  Button")
                ?? FindDeepChild(panel.transform, "Exit Button");
            if (found != null)
            {
                exitButton = found.GetComponent<Button>();
            }
        }
    }

    private void WireExitButton()
    {
        if (exitButton == null)
        {
            return;
        }

        exitButton.onClick.RemoveListener(Close);
        exitButton.onClick.AddListener(Close);
    }

    private void UnwireExitButton()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Close);
        }
    }

    private void RebuildUpgradeList()
    {
        ClearRows();

        if (itemListRoot == null)
        {
            Debug.LogWarning("ShopUI: item list Content missing.");
            return;
        }

        EnsureListLayout();

        DetectorProgress progress = DetectorProgress.Instance;
        PlayerInventory inventory = PlayerInventory.Instance;
        int cash = inventory != null ? inventory.Cash : 0;

        if (progress == null)
        {
            return;
        }

        AddUpgradeRow(
            moduleIndex: 1,
            FormatUpgradeLine(
                depthModule1Label,
                progress.DepthModule1Price,
                cash,
                owned: progress.HasDepth2Module,
                locked: false));

        AddUpgradeRow(
            moduleIndex: 2,
            FormatUpgradeLine(
                depthModule2Label,
                progress.DepthModule2Price,
                cash,
                owned: progress.HasDepth3Module,
                locked: !progress.HasDepth2Module));
    }

    private void AddUpgradeRow(int moduleIndex, (string text, bool canBuy) line)
    {
        ShopUpgradeRow row = CreateRow();
        if (row == null)
        {
            return;
        }

        PrepareRowLayout(row);
        row.Bind(moduleIndex, line.text, line.canBuy, TryBuyModule);
        activeRows.Add(row);
    }

    private static (string text, bool canBuy) FormatUpgradeLine(
        string label,
        int price,
        int cash,
        bool owned,
        bool locked)
    {
        if (owned)
        {
            return ($"{label}  OWNED", false);
        }

        if (locked)
        {
            return ($"{label}  ${price}  (buy 15–30 first)", false);
        }

        if (cash < price)
        {
            return ($"{label}  ${price}  (need ${price - cash} more)", false);
        }

        return ($"{label}  ${price}", true);
    }

    private ShopUpgradeRow CreateRow()
    {
        if (upgradeRowPrefab != null)
        {
            return Instantiate(upgradeRowPrefab, itemListRoot);
        }

        return CreateFallbackRow(itemListRoot);
    }

    private void EnsureListLayout()
    {
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

    private static void PrepareRowLayout(ShopUpgradeRow row)
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

    private static ShopUpgradeRow CreateFallbackRow(Transform parent)
    {
        GameObject root = new GameObject(
            "ShopUpgradeRow",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(ShopUpgradeRow));
        root.transform.SetParent(parent, false);

        Image image = root.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textGo.transform.SetParent(root.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 2f);
        textRect.offsetMax = new Vector2(-10f, -2f);

        TMPro.TextMeshProUGUI tmp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return root.GetComponent<ShopUpgradeRow>();
    }

    private void ClearRows()
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

    private void TryBuyModule(int moduleIndex)
    {
        if (!IsOpen)
        {
            return;
        }

        DetectorProgress progress = DetectorProgress.Instance;
        PlayerInventory inventory = PlayerInventory.Instance;
        if (progress == null || inventory == null)
        {
            return;
        }

        bool bought = moduleIndex == 1
            ? progress.TryBuyDepthModule1(inventory)
            : progress.TryBuyDepthModule2(inventory);

        if (!bought)
        {
            Debug.Log("Shop: could not buy that upgrade (owned, locked, or not enough cash).");
            Refresh();
            return;
        }

        Refresh();
        shopComments?.ShowBuyReaction(moduleIndex);
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

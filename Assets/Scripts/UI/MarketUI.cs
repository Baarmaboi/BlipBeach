using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Sell panel on the player Canvas. Opened by MarketStall. Metal rates live on MarketPriceBoard, not here.
/// </summary>
public class MarketUI : MonoBehaviour
{
    public static MarketUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private bool suppressInputUntilKeyUp;

    private void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;
    }

    private void OnDestroy()
    {
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
        Subscribe();
        Refresh();
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
            if (!Input.GetKey(KeyCode.E) && !Input.GetKey(KeyCode.Q)
                && !Input.GetKey(KeyCode.Alpha1) && !Input.GetKey(KeyCode.Alpha2))
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

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryBuyDepthModule(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryBuyDepthModule(2);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TrySellAll();
        }
    }

    public void Open()
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

        panel.SetActive(true);
        IsOpen = true;
        suppressInputUntilKeyUp = Input.GetKey(KeyCode.E);
        Refresh();
        SetGameplayPaused(true);
    }

    public void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        IsOpen = false;

        if (!DialogueUI.IsOpen && !DaySummaryUI.IsOpen && !QuestLogUI.IsOpen && !InventoryUI.IsOpen)
        {
            SetGameplayPaused(false);
        }
    }

    public void Refresh()
    {
        if (bodyText == null)
        {
            return;
        }

        bodyText.text = BuildBody();
    }

    private void TrySellAll()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        MarketManager market = MarketManager.Instance;
        if (inventory == null || market == null || inventory.Treasures.Count == 0)
        {
            return;
        }

        inventory.SellAll(market);
        Refresh();
        Close();
    }

    private void TryBuyDepthModule(int moduleIndex)
    {
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
            Debug.Log("Market: could not buy that depth module (owned, locked, or not enough cash).");
        }

        Refresh();
    }

    private static string BuildBody()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Sell treasures");
        builder.AppendLine();

        var items = PlayerInventory.Instance != null ? PlayerInventory.Instance.Treasures : null;
        MarketManager market = MarketManager.Instance;
        if (items == null || items.Count == 0)
        {
            builder.AppendLine("Nothing to sell.");
            builder.AppendLine();
            AppendUpgradeSection(builder);
            builder.Append("Q — leave");
            return builder.ToString();
        }

        for (int i = 0; i < items.Count; i++)
        {
            TreasureDefinition def = items[i];
            if (def == null)
            {
                continue;
            }

            int price = market != null ? market.GetSellPrice(def) : def.baseValue;
            builder.AppendLine($"• {def.FullDisplayName}  ${price}");
        }

        int total = PlayerInventory.Instance.PreviewSellAll(market);
        builder.AppendLine();
        builder.AppendLine($"E — sell all (${total})");
        AppendUpgradeSection(builder);
        builder.Append("Q — leave");
        return builder.ToString();
    }

    private static void AppendUpgradeSection(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("Detector upgrades");
        DetectorProgress progress = DetectorProgress.Instance;
        PlayerInventory inventory = PlayerInventory.Instance;
        int cash = inventory != null ? inventory.Cash : 0;
        if (progress == null)
        {
            builder.AppendLine("(DetectorProgress missing from GameSystems)");
            builder.AppendLine();
            return;
        }

        builder.AppendLine(FormatModuleLine(
            1,
            "15-30cm",
            progress.DepthModule1Price,
            cash,
            progress.HasDepth2Module,
            locked: false));
        builder.AppendLine(FormatModuleLine(
            2,
            "30-50cm",
            progress.DepthModule2Price,
            cash,
            progress.HasDepth3Module,
            locked: !progress.HasDepth2Module));
        builder.AppendLine();
    }

    private static string FormatModuleLine(int key, string label, int price, int cash, bool owned, bool locked)
    {
        if (owned)
        {
            return $"{key} — Depth {label}  OWNED";
        }

        if (locked)
        {
            return $"{key} — Depth {label}  ${price}  (buy 15-30 first)";
        }

        string afford = cash >= price ? $"${price}" : $"${price}  (need ${price - cash} more)";
        return $"{key} — Depth {label}  {afford}";
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

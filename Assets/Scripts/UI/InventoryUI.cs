using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Toggle with I. Assign panel + body text on the player Canvas (same pattern as QuestLogUI).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

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
    }

    private void Subscribe()
    {
        if (PlayerInventory.Instance == null)
        {
            return;
        }

        PlayerInventory.Instance.OnChanged -= Refresh;
        PlayerInventory.Instance.OnChanged += Refresh;
    }

    private void Update()
    {
        if (DialogueUI.IsOpen || DaySummaryUI.IsOpen || MarketUI.IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (panel == null)
        {
            Debug.LogWarning("InventoryUI: assign a Panel on the Canvas.");
            return;
        }

        if (QuestLogUI.IsOpen && QuestLogUI.Instance != null)
        {
            QuestLogUI.Instance.Close();
        }

        panel.SetActive(true);
        IsOpen = true;
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

        if (!DialogueUI.IsOpen && !DaySummaryUI.IsOpen && !QuestLogUI.IsOpen && !MarketUI.IsOpen)
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

    private static string BuildBody()
    {
        StringBuilder builder = new StringBuilder();
        int cash = PlayerInventory.Instance != null ? PlayerInventory.Instance.Cash : 0;
        builder.AppendLine($"Cash: ${cash}");
        builder.AppendLine();

        var items = PlayerInventory.Instance != null ? PlayerInventory.Instance.Treasures : null;
        if (items == null || items.Count == 0)
        {
            builder.AppendLine("No treasures yet.");
            builder.AppendLine("Dig buried items, then sell them at the market stall.");
            builder.AppendLine();
            builder.Append("I — close");
            return builder.ToString();
        }

        MarketManager market = MarketManager.Instance;
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

        builder.AppendLine();
        builder.Append("I — close");
        return builder.ToString();
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

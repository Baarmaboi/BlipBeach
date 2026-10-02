using TMPro;
using UnityEngine;

/// <summary>
/// Which shop is driving the comment bubble.
/// </summary>
public enum ShopCommentSource
{
    /// <summary>Treasure market stall (sell).</summary>
    Market = 0,
    /// <summary>ShopKeeper upgrade shop (buy).</summary>
    UpgradeShop = 1
}

/// <summary>
/// Reactive comment bubble for market / upgrade shops.
/// Not dialogue: no E-advance — just reacts to open / sell / buy.
/// </summary>
public class ShopCommentsUI : MonoBehaviour
{
    private static readonly int ShakeTrigger = Animator.StringToHash("Shake");

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Animator panelAnimator;

    [Header("Market stall — greeting")]
    [SerializeField] private string[] welcomeLines =
    {
        "Morning! Dig up anything shiny?",
        "Welcome back, beachcomber!",
        "Got treasures? I've got cash.",
        "Sun's out, detectors out — what'cha got?",
        "Step right up! Best rates on the boardwalk."
    };

    [Header("Market stall — after sell")]
    [SerializeField] private string[] sellLines =
    {
        "Ooh, nice haul! Pleasure doing business.",
        "Ka-ching! Those'll look great in the case.",
        "Sold! Don't spend it all on ice cream.",
        "Sweet finds — beach treats you well today.",
        "I'll take 'em! Come back when the tide turns."
    };

    [Header("Upgrade shop (ShopKeeper) — greeting")]
    [SerializeField] private string[] upgradeWelcomeLines =
    {
        "Looking to dig deeper? You're in the right place.",
        "Detector upgrades — make that coil earn its keep.",
        "Hey! Need more range under the sand?",
        "Welcome! Fresh modules, fair prices.",
        "ShopKeeper here. Let's soup up that detector."
    };

    [Header("Upgrade shop — after buy")]
    [SerializeField] private string[] buyUpgradeLines =
    {
        "Deeper digs, bigger scores — smart buy!",
        "Upgrade installed. Go find the good stuff!",
        "Now we're talking. That coil means business.",
        "Depth unlocked! The sand's got secrets."
    };

    [SerializeField] private string[] buyModule1Lines =
    {
        "15–30cm? Perfect for bottle caps and charms.",
        "Mid-depth unlocked — watch those pull tabs fly!"
    };

    [SerializeField] private string[] buyModule2Lines =
    {
        "30–50cm! You're hunting the deep stuff now.",
        "Serious depth. Rings and relics, here we come."
    };

    private bool hasShownComment;

    private void Awake()
    {
        if (panelAnimator == null && panel != null)
        {
            panelAnimator = panel.GetComponentInChildren<Animator>(true);
        }

        if (panelAnimator == null)
        {
            panelAnimator = GetComponentInChildren<Animator>(true);
        }

        Hide();
    }

    /// <summary>Market stall greeting (backward-compatible default).</summary>
    public void ShowWelcome()
    {
        ShowWelcome(ShopCommentSource.Market);
    }

    /// <summary>First open for the given shop: appear anim handles entrance — no shake.</summary>
    public void ShowWelcome(ShopCommentSource source)
    {
        string[] lines = source == ShopCommentSource.UpgradeShop
            ? upgradeWelcomeLines
            : welcomeLines;
        string fallback = source == ShopCommentSource.UpgradeShop
            ? "Looking for upgrades?"
            : "Welcome!";

        SetComment(PickRandom(lines, fallback), playShake: false);
    }

    /// <summary>Market stall — after selling treasures.</summary>
    public void ShowSellReaction()
    {
        SetComment(PickRandom(sellLines, "Thanks for the treasures!"), playShake: true);
    }

    /// <summary>Upgrade shop — after buying a depth module.</summary>
    public void ShowBuyReaction(int moduleIndex)
    {
        string[] preferred = moduleIndex == 1 ? buyModule1Lines
            : moduleIndex == 2 ? buyModule2Lines
            : null;

        string line = PickRandom(preferred, null);
        if (string.IsNullOrEmpty(line))
        {
            line = PickRandom(buyUpgradeLines, "Nice upgrade!");
        }

        SetComment(line, playShake: true);
    }

    public void Hide()
    {
        hasShownComment = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void SetComment(string line, bool playShake)
    {
        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);
        }

        if (bodyText != null)
        {
            bodyText.text = line;
        }

        if (playShake && hasShownComment)
        {
            PlayShake();
        }
        else if (panelAnimator != null)
        {
            panelAnimator.ResetTrigger(ShakeTrigger);
        }

        hasShownComment = true;
    }

    private void PlayShake()
    {
        if (panelAnimator == null)
        {
            return;
        }

        panelAnimator.ResetTrigger(ShakeTrigger);
        panelAnimator.SetTrigger(ShakeTrigger);
    }

    private static string PickRandom(string[] lines, string fallback)
    {
        if (lines != null && lines.Length > 0)
        {
            for (int attempt = 0; attempt < lines.Length; attempt++)
            {
                string candidate = lines[Random.Range(0, lines.Length)];
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }

        return string.IsNullOrEmpty(fallback) ? "..." : fallback;
    }
}

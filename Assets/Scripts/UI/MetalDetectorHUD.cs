using TMPro;
using UnityEngine;

public class MetalDetectorHUD : MonoBehaviour
{
    [SerializeField] private GameObject detectingHud;
    [SerializeField] private TMP_Text scanText;
    [SerializeField] private TMP_Text digPromptText;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private TMP_Text rewardTitleText;
    [SerializeField] private TMP_Text rewardBodyText;
    [SerializeField] private TMP_Text depthModeText;

    private void Awake()
    {
        HideAll();
    }

    public void HideAll()
    {
        if (detectingHud != null)
        {
            detectingHud.SetActive(false);
        }

        SetDigPromptVisible(false);
        HideReward();
    }

    public void SetDetectingVisible(bool visible)
    {
        if (detectingHud != null)
        {
            detectingHud.SetActive(visible);
        }

        if (visible)
        {
            SetScanStatus(false);
            RefreshDepthDisplay();
        }
        else
        {
            SetDigPromptVisible(false);
        }
    }

    public void SetScanStatus(bool itemFound)
    {
        if (scanText != null)
        {
            scanText.text = itemFound ? "ITEM FOUND" : "NOTHING";
        }
    }

    public void SetDigging()
    {
        if (scanText != null)
        {
            scanText.text = "DIGGING...";
        }

        SetDigPromptVisible(false);
    }

    public void SetDigPromptVisible(bool visible)
    {
        if (digPromptText == null)
        {
            return;
        }

        digPromptText.gameObject.SetActive(visible);
        if (visible)
        {
            digPromptText.text = "E, dig";
        }
    }

    public void RefreshDepthDisplay()
    {
        if (depthModeText == null)
        {
            return;
        }

        DetectorProgress progress = DetectorProgress.Instance;
        if (progress == null)
        {
            depthModeText.text = "DEPTH: 0-15\nQ / wheel — change depth";
            return;
        }

        string mode = DepthLevels.ModeLabel(progress.ActiveMode, progress.MaxUnlockedDepth);
        string unlocked = progress.MaxUnlockedDepth >= 3
            ? "Unlocked: 0-50cm"
            : progress.MaxUnlockedDepth >= 2
                ? "Unlocked: 0-30cm"
                : "Unlocked: 0-15cm";
        depthModeText.text = $"{mode}\n{unlocked}\nQ / wheel — change depth";
    }

    public void ShowReward(BuriedItem item)
    {
        // Digging text lives on the detecting HUD — hide it while the reward is open.
        if (detectingHud != null)
        {
            detectingHud.SetActive(false);
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
        }

        if (rewardTitleText != null)
        {
            rewardTitleText.text = item != null ? item.GetRewardTitle() : "Item found";
        }

        if (rewardBodyText != null)
        {
            rewardBodyText.text = item != null ? item.GetRewardBody() : "You found something.";
        }

        SetDigPromptVisible(false);
    }

    public void HideReward()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }
}

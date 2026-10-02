using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clickable upgrade row in the detector shop list.
/// </summary>
public class ShopUpgradeRow : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private int moduleIndex;
    private Action<int> onBuyClicked;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Bind(int module, string displayText, bool canBuy, Action<int> buyClicked)
    {
        moduleIndex = module;
        onBuyClicked = buyClicked;

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        if (label != null)
        {
            label.text = displayText;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            return;
        }

        button.interactable = canBuy;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        onBuyClicked?.Invoke(moduleIndex);
    }
}

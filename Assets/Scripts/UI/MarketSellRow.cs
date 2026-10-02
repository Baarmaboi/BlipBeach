using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One clickable sell row in the market item list.
/// </summary>
public class MarketSellRow : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private int inventoryIndex;
    private Action<int> onSellClicked;

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

    public void Bind(int index, string displayText, Action<int> sellClicked)
    {
        inventoryIndex = index;
        onSellClicked = sellClicked;

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

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        onSellClicked?.Invoke(inventoryIndex);
    }
}

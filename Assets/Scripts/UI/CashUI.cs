using TMPro;
using UnityEngine;

/// <summary>
/// Always-on cash readout. Put on a Canvas Text (TMP) like StarsUI.
/// </summary>
public class CashUI : MonoBehaviour
{
    [SerializeField] private TMP_Text cashText;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
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

    private void Refresh()
    {
        if (cashText == null)
        {
            cashText = GetComponent<TMP_Text>();
        }

        if (cashText == null)
        {
            return;
        }

        int cash = PlayerInventory.Instance != null ? PlayerInventory.Instance.Cash : 0;
        cashText.text = $"Cash: ${cash}";
    }
}

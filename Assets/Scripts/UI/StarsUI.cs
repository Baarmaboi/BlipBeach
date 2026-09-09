using TMPro;
using UnityEngine;

public class StarsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text starsText;

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
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStarsChanged -= Refresh;
        }
    }

    private void Subscribe()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.OnStarsChanged -= Refresh;
        QuestManager.Instance.OnStarsChanged += Refresh;
    }

    private void Refresh()
    {
        if (starsText == null)
        {
            return;
        }

        int stars = QuestManager.Instance != null ? QuestManager.Instance.Stars : 0;
        starsText.text = $"Stars: {stars}";
    }
}

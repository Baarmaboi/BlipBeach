using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Brief "Day X" splash at the start of each morning.
/// </summary>
public class DayTitleUI : MonoBehaviour
{
    public static DayTitleUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float holdSeconds = 2f;
    [SerializeField] private float fadeOutSeconds = 0.75f;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public IEnumerator PlayShowDay(int day)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = $"Day {day}";
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(holdSeconds);

        if (canvasGroup != null && fadeOutSeconds > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutSeconds);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        HideImmediate();
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}

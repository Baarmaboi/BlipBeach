using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen black fade overlay for day transitions.
/// </summary>
public class ScreenFadeUI : MonoBehaviour
{
    public static ScreenFadeUI Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 1f;

    public bool IsFading { get; private set; }

    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;

        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        SetAlpha(1f);
        SetRaycastBlock(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetImmediate(float alpha)
    {
        StopFade();
        SetAlpha(alpha);
        SetRaycastBlock(alpha > 0.01f);
        IsFading = false;
    }

    public Coroutine FadeOut(float duration = -1f)
    {
        return StartFade(1f, duration);
    }

    public Coroutine FadeIn(float duration = -1f)
    {
        return StartFade(0f, duration);
    }

    public IEnumerator FadeOutRoutine(float duration = -1f)
    {
        yield return StartFade(1f, duration);
    }

    public IEnumerator FadeInRoutine(float duration = -1f)
    {
        yield return StartFade(0f, duration);
    }

    private Coroutine StartFade(float targetAlpha, float duration)
    {
        StopFade();
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration < 0f ? defaultFadeDuration : duration));
        return fadeRoutine;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        IsFading = true;
        SetRaycastBlock(true);

        float startAlpha = fadeImage != null ? fadeImage.color.a : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        SetRaycastBlock(targetAlpha > 0.01f);
        IsFading = false;
        fadeRoutine = null;
    }

    private void StopFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(alpha);
        fadeImage.color = c;
    }

    private void SetRaycastBlock(bool block)
    {
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = block;
        }
    }
}

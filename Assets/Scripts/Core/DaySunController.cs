using UnityEngine;

/// <summary>
/// Rotates the sun (Directional Light) from morning to evening as the day timer runs.
/// </summary>
public class DaySunController : MonoBehaviour
{
    [SerializeField] private Transform sun;
    [SerializeField] private Light sunLight;
    [SerializeField] private float morningPitch = 50f;
    [SerializeField] private float eveningPitch = 8f;
    [SerializeField] private float sunYaw = 170f;
    [SerializeField] private bool useEveningTint = true;
    [SerializeField] private Color morningColor = Color.white;
    [SerializeField] private Color eveningColor = new Color(1f, 0.72f, 0.45f);

    private void Awake()
    {
        if (sun == null)
        {
            GameObject sunObject = GameObject.Find("Sun");
            if (sunObject != null)
            {
                sun = sunObject.transform;
            }
        }

        if (sunLight == null && sun != null)
        {
            sunLight = sun.GetComponent<Light>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        ApplySunRotation();
    }

    private void OnDisable()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnTimeUpdated -= ApplySunRotation;
            DayManager.Instance.OnDayStarted -= OnDayStarted;
        }
    }

    private void Start()
    {
        Subscribe();
        ApplySunRotation();
    }

    private void Subscribe()
    {
        if (DayManager.Instance == null)
        {
            return;
        }

        DayManager.Instance.OnTimeUpdated -= ApplySunRotation;
        DayManager.Instance.OnTimeUpdated += ApplySunRotation;
        DayManager.Instance.OnDayStarted -= OnDayStarted;
        DayManager.Instance.OnDayStarted += OnDayStarted;
    }

    private void OnDayStarted(int day)
    {
        // Reset behind the black fade so the player never sees the evening sun on a new morning.
        SnapToMorning();
    }

    /// <summary>Morning sun pose — used at day start even while the screen is faded out.</summary>
    public void SnapToMorning()
    {
        if (sun == null)
        {
            return;
        }

        sun.rotation = Quaternion.Euler(morningPitch, sunYaw, 0f);

        if (sunLight != null && useEveningTint)
        {
            sunLight.color = morningColor;
        }
    }

    private void ApplySunRotation()
    {
        if (sun == null || DayManager.Instance == null)
        {
            return;
        }

        // Freeze lighting during day transitions so it does not jump mid-fade.
        if (DayManager.Instance.IsDayTransitioning)
        {
            return;
        }

        float progress = DayManager.Instance.DayProgress;
        float pitch = Mathf.Lerp(morningPitch, eveningPitch, progress);
        sun.rotation = Quaternion.Euler(pitch, sunYaw, 0f);

        if (sunLight != null && useEveningTint)
        {
            sunLight.color = Color.Lerp(morningColor, eveningColor, progress);
        }
    }
}

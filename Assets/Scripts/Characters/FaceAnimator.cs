using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// One-row face atlas. Default frames: idle | blink | look left | look right | (optional extra, e.g. happy).
/// Switches frames by changing the face material's texture offset.
/// Call HoldFrame(index) / ClearHold() from talks or other events.
/// </summary>
[DisallowMultipleComponent]
public class FaceAnimator : MonoBehaviour
{
    private const int IdleFrame = 0;
    private const int BlinkFrame = 1;
    private const int LookLeftFrame = 2;
    private const int LookRightFrame = 3;

    [Header("Face")]
    [Tooltip("Leave empty to use the renderer on this object.")]
    [SerializeField] private Renderer faceRenderer;
    [Tooltip("Which material slot is the face. Usually 0.")]
    [SerializeField] private int materialIndex;

    [Header("Atlas")]
    [Tooltip("How many face images sit in one row. Reed 4, Tara 5 (last is happy).")]
    [FormerlySerializedAs("atlasColumns")]
    [SerializeField] [Min(1)] private int framesInRow = 4;

    [Header("Blink")]
    [FormerlySerializedAs("enableAutoBlink")]
    [SerializeField] private bool autoBlink = true;
    [Tooltip("Shortest wait (seconds) before the next blink.")]
    [SerializeField] private float minSecondsBetweenBlinks = 2f;
    [Tooltip("Longest wait (seconds) before the next blink.")]
    [SerializeField] private float maxSecondsBetweenBlinks = 5f;
    [Tooltip("How long the eyes stay closed.")]
    [FormerlySerializedAs("blinkDuration")]
    [SerializeField] [Min(0.02f)] private float eyesClosedDuration = 0.15f;

    [Header("Look Around")]
    [SerializeField] private bool autoLookAround = true;
    [Tooltip("Shortest wait (seconds) looking forward before a glance.")]
    [SerializeField] private float minSecondsBetweenLooks = 2.5f;
    [Tooltip("Longest wait (seconds) looking forward before a glance.")]
    [SerializeField] private float maxSecondsBetweenLooks = 6f;
    [Tooltip("Shortest time (seconds) spent looking left or right.")]
    [SerializeField] private float minLookHold = 0.7f;
    [Tooltip("Longest time (seconds) spent looking left or right.")]
    [SerializeField] private float maxLookHold = 1.8f;

    private Material faceMaterial;
    private bool createdMaterialInstance;
    private int shownFrame = int.MinValue;
    private int restFrame = IdleFrame;
    private bool holding;
    private bool blinking;
    private float blinkTimer;
    private float waitUntilBlink;
    private float lookTimer;

    public bool IsBlinking => blinking;
    public bool IsHolding => holding;

    private void Reset()
    {
        faceRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        if (faceRenderer == null)
        {
            faceRenderer = GetComponent<Renderer>();
        }

        GrabMaterial();
        ScheduleNextBlink();
        ScheduleNextLookFromIdle();
        ShowFrame(IdleFrame, force: true);
    }

    private void OnDestroy()
    {
        if (createdMaterialInstance && faceMaterial != null)
        {
            Destroy(faceMaterial);
        }
    }

    private void OnDisable()
    {
        blinking = false;
        holding = false;
        restFrame = IdleFrame;
        if (faceMaterial != null)
        {
            ShowFrame(IdleFrame, force: true);
        }
    }

    private void OnValidate()
    {
        framesInRow = Mathf.Max(1, framesInRow);
        materialIndex = Mathf.Max(0, materialIndex);
        minSecondsBetweenBlinks = Mathf.Max(0.1f, minSecondsBetweenBlinks);
        maxSecondsBetweenBlinks = Mathf.Max(minSecondsBetweenBlinks, maxSecondsBetweenBlinks);
        eyesClosedDuration = Mathf.Max(0.02f, eyesClosedDuration);
        minSecondsBetweenLooks = Mathf.Max(0.1f, minSecondsBetweenLooks);
        maxSecondsBetweenLooks = Mathf.Max(minSecondsBetweenLooks, maxSecondsBetweenLooks);
        minLookHold = Mathf.Max(0.1f, minLookHold);
        maxLookHold = Mathf.Max(minLookHold, maxLookHold);
    }

    private void Update()
    {
        UpdateBlink();
        UpdateLook();
        ShowFrame(blinking ? BlinkFrame : restFrame, force: false);
    }

    /// <summary>Keep this atlas frame on until ClearHold(). Pauses look-around. Blink still skipped while held.</summary>
    public void HoldFrame(int frame)
    {
        holding = true;
        restFrame = frame;
        blinking = false;
        ShowFrame(frame, force: true);
    }

    /// <summary>Return to idle and resume blink / look-around.</summary>
    public void ClearHold()
    {
        holding = false;
        blinking = false;
        restFrame = IdleFrame;
        ScheduleNextLookFromIdle();
        ShowFrame(IdleFrame, force: true);
    }

    /// <summary>Close the eyes now, then return to idle or the current glance.</summary>
    public void Blink()
    {
        blinking = true;
        blinkTimer = eyesClosedDuration;
        ScheduleNextBlink();
    }

    [ContextMenu("Blink Now")]
    private void BlinkFromMenu()
    {
        Blink();
    }

    private void UpdateBlink()
    {
        if (blinking)
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinking = false;
            }

            return;
        }

        if (!autoBlink || holding)
        {
            return;
        }

        waitUntilBlink -= Time.deltaTime;
        if (waitUntilBlink <= 0f)
        {
            Blink();
        }
    }

    private void UpdateLook()
    {
        if (holding)
        {
            return;
        }

        if (!autoLookAround || framesInRow <= LookRightFrame)
        {
            restFrame = IdleFrame;
            return;
        }

        lookTimer -= Time.deltaTime;
        if (lookTimer > 0f)
        {
            return;
        }

        if (restFrame == IdleFrame)
        {
            restFrame = Random.value < 0.5f ? LookLeftFrame : LookRightFrame;
            lookTimer = Random.Range(minLookHold, maxLookHold);
        }
        else
        {
            restFrame = IdleFrame;
            ScheduleNextLookFromIdle();
        }
    }

    private void GrabMaterial()
    {
        if (faceRenderer == null)
        {
            return;
        }

        Material[] shared = faceRenderer.sharedMaterials;
        if (materialIndex < 0 || materialIndex >= shared.Length || shared[materialIndex] == null)
        {
            Debug.LogWarning($"{nameof(FaceAnimator)} on '{name}': no material in slot {materialIndex}.", this);
            return;
        }

        Material[] instances = faceRenderer.materials;
        faceMaterial = instances[materialIndex];
        createdMaterialInstance = true;
    }

    private void ShowFrame(int frame, bool force)
    {
        if (faceMaterial == null)
        {
            GrabMaterial();
        }

        if (faceMaterial == null)
        {
            return;
        }

        int clamped = Mathf.Clamp(frame, 0, framesInRow - 1);
        if (!force && clamped == shownFrame)
        {
            return;
        }

        float tileW = 1f / framesInRow;
        Vector2 tiling = new Vector2(tileW, 1f);
        Vector2 offset = new Vector2(clamped * tileW, 0f);

        faceMaterial.mainTextureScale = tiling;
        faceMaterial.mainTextureOffset = offset;
        SetTextureST("_BaseColorMap", tiling, offset);
        SetTextureST("_MainTex", tiling, offset);
        SetTextureST("_UnlitColorMap", tiling, offset);

        shownFrame = clamped;
    }

    private void SetTextureST(string propertyName, Vector2 tiling, Vector2 offset)
    {
        if (!faceMaterial.HasProperty(propertyName))
        {
            return;
        }

        faceMaterial.SetTextureScale(propertyName, tiling);
        faceMaterial.SetTextureOffset(propertyName, offset);
    }

    private void ScheduleNextBlink()
    {
        waitUntilBlink = Random.Range(minSecondsBetweenBlinks, maxSecondsBetweenBlinks);
    }

    private void ScheduleNextLookFromIdle()
    {
        lookTimer = Random.Range(minSecondsBetweenLooks, maxSecondsBetweenLooks);
    }
}

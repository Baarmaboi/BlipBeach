using UnityEngine;

/// <summary>
/// Orbit follow camera with talk mode (dialogue) and fixed-shot mode (shop).
/// Yaw is preserved during special modes so player facing stays correct after exit.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float pitchMin = -30f;
    [SerializeField] private float pitchMax = 60f;

    [Header("Follow")]
    [SerializeField] private float followSmoothness = 10f;

    [Header("Talk camera")]
    [Tooltip("Child on the player — camera blends to this pose when dialogue opens.")]
    [SerializeField] private Transform talkCamAnchor;
    [Tooltip("Fallback look target if EnterTalkMode is called with null (e.g. player TalkLookAt).")]
    [SerializeField] private Transform defaultTalkLookAt;
    [SerializeField] private float talkBlendDuration = 0.45f;
    [SerializeField] private AnimationCurve talkBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Raises the look point so we aim near the NPC's head when given their root transform.")]
    [SerializeField] private float talkLookHeightBoost = 1.35f;

    [Header("Fixed shot (shop)")]
    [Tooltip("Blend duration for shop / world fixed shots. Falls back to talk blend if <= 0.")]
    [SerializeField] private float fixedShotBlendDuration = 0.45f;
    [SerializeField] private AnimationCurve fixedShotBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float yaw;
    private float pitch;
    private Vector3 followVelocity;

    private enum Mode
    {
        Orbit,
        BlendToTalk,
        Talk,
        BlendToFixed,
        Fixed,
        BlendToOrbit
    }

    private Mode mode = Mode.Orbit;
    private Transform talkLookAt;
    private Transform fixedShotAnchor;
    private float blendElapsed;
    private Vector3 blendFromPos;
    private Quaternion blendFromRot;

    public float Yaw => yaw;
    public bool LookEnabled { get; set; } = true;
    public bool IsInTalkMode => mode == Mode.Talk || mode == Mode.BlendToTalk;
    public bool IsInFixedShotMode => mode == Mode.Fixed || mode == Mode.BlendToFixed;

    private void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }

        pitch = 15f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Blend from orbit to TalkCamAnchor. Optional lookAt = NPC (or a head marker).</summary>
    public void EnterTalkMode(Transform lookAt = null)
    {
        talkLookAt = lookAt != null ? lookAt : defaultTalkLookAt;
        fixedShotAnchor = null;
        LookEnabled = false;
        followVelocity = Vector3.zero;

        blendFromPos = transform.position;
        blendFromRot = transform.rotation;
        blendElapsed = 0f;
        mode = Mode.BlendToTalk;
    }

    /// <summary>Blend from talk pose back to the normal orbit follow. Does not change yaw.</summary>
    public void ExitTalkMode()
    {
        if (mode != Mode.Talk && mode != Mode.BlendToTalk)
        {
            return;
        }

        talkLookAt = null;
        BeginBlendToOrbit();
    }

    /// <summary>Blend to an exact world camera pose (position + rotation) from a scene Transform.</summary>
    public void EnterFixedShot(Transform shotAnchor)
    {
        if (shotAnchor == null)
        {
            Debug.LogWarning("ThirdPersonCamera: EnterFixedShot called with null shotAnchor.");
            return;
        }

        fixedShotAnchor = shotAnchor;
        LookEnabled = false;
        followVelocity = Vector3.zero;

        blendFromPos = transform.position;
        blendFromRot = transform.rotation;
        blendElapsed = 0f;
        mode = Mode.BlendToFixed;
    }

    /// <summary>Blend from fixed shot back to orbit. Does not change yaw.</summary>
    public void ExitFixedShot()
    {
        if (mode != Mode.Fixed && mode != Mode.BlendToFixed)
        {
            return;
        }

        fixedShotAnchor = null;
        BeginBlendToOrbit();
    }

    /// <summary>Sync orbit yaw to the follow target's current Y rotation (after talk facing).</summary>
    public void SyncYawToTarget()
    {
        if (target != null)
        {
            SetYaw(target.eulerAngles.y);
        }
    }

    public void SetYaw(float yawDegrees)
    {
        yaw = Mathf.Repeat(yawDegrees, 360f);
    }

    private void BeginBlendToOrbit()
    {
        followVelocity = Vector3.zero;
        blendFromPos = transform.position;
        blendFromRot = transform.rotation;
        blendElapsed = 0f;
        mode = Mode.BlendToOrbit;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Mouse look only in free orbit — special modes keep yaw stable.
        if (LookEnabled && mode == Mode.Orbit)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        switch (mode)
        {
            case Mode.Orbit:
                ApplyOrbitFollow();
                break;
            case Mode.BlendToTalk:
                UpdateBlend(BlendDestination.Talk);
                break;
            case Mode.Talk:
                ApplyTalkPoseImmediate();
                break;
            case Mode.BlendToFixed:
                UpdateBlend(BlendDestination.Fixed);
                break;
            case Mode.Fixed:
                ApplyFixedPoseImmediate();
                break;
            case Mode.BlendToOrbit:
                UpdateBlend(BlendDestination.Orbit);
                break;
        }
    }

    private enum BlendDestination
    {
        Talk,
        Fixed,
        Orbit
    }

    private void ApplyOrbitFollow()
    {
        Vector3 focusPoint = target.position + Vector3.up * height;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = focusPoint + orbitRotation * Vector3.back * distance;

        float smoothTime = 1f / Mathf.Max(followSmoothness, 0.01f);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
        // Match pre-talk-camera behavior: aim from the actual camera position (not a lagged slerp).
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position);
    }

    private void ApplyTalkPoseImmediate()
    {
        GetTalkPose(out Vector3 pos, out Quaternion rot);
        transform.position = pos;
        transform.rotation = rot;
    }

    private void ApplyFixedPoseImmediate()
    {
        GetFixedPose(out Vector3 pos, out Quaternion rot);
        transform.position = pos;
        transform.rotation = rot;
    }

    private void UpdateBlend(BlendDestination destination)
    {
        GetBlendSettings(destination, out float duration, out AnimationCurve curve);
        duration = Mathf.Max(0.01f, duration);
        blendElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(blendElapsed / duration);
        float curved = curve != null && curve.keys.Length > 0
            ? curve.Evaluate(t)
            : t;

        GetBlendEndPose(destination, out Vector3 endPos, out Quaternion endRot);
        transform.position = Vector3.Lerp(blendFromPos, endPos, curved);
        transform.rotation = Quaternion.Slerp(blendFromRot, endRot, curved);

        if (t >= 1f)
        {
            mode = destination switch
            {
                BlendDestination.Talk => Mode.Talk,
                BlendDestination.Fixed => Mode.Fixed,
                _ => Mode.Orbit
            };
            followVelocity = Vector3.zero;
        }
    }

    private void GetBlendSettings(BlendDestination destination, out float duration, out AnimationCurve curve)
    {
        if (destination == BlendDestination.Fixed)
        {
            duration = fixedShotBlendDuration > 0f ? fixedShotBlendDuration : talkBlendDuration;
            curve = fixedShotBlendCurve != null && fixedShotBlendCurve.keys.Length > 0
                ? fixedShotBlendCurve
                : talkBlendCurve;
            return;
        }

        duration = talkBlendDuration;
        curve = talkBlendCurve;
    }

    private void GetBlendEndPose(BlendDestination destination, out Vector3 endPos, out Quaternion endRot)
    {
        switch (destination)
        {
            case BlendDestination.Talk:
                GetTalkPose(out endPos, out endRot);
                break;
            case BlendDestination.Fixed:
                GetFixedPose(out endPos, out endRot);
                break;
            default:
                // Recompute orbit end each frame so the return target tracks the player.
                GetOrbitPose(out endPos, out endRot);
                break;
        }
    }

    private void GetOrbitPose(out Vector3 position, out Quaternion rotation)
    {
        Vector3 focusPoint = target.position + Vector3.up * height;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        position = focusPoint + orbitRotation * Vector3.back * distance;
        rotation = Quaternion.LookRotation(focusPoint - position, Vector3.up);
    }

    private void GetFixedPose(out Vector3 position, out Quaternion rotation)
    {
        if (fixedShotAnchor != null)
        {
            position = fixedShotAnchor.position;
            rotation = fixedShotAnchor.rotation;
            return;
        }

        GetOrbitPose(out position, out rotation);
    }

    private void GetTalkPose(out Vector3 position, out Quaternion rotation)
    {
        if (talkCamAnchor != null)
        {
            position = talkCamAnchor.position;
        }
        else
        {
            // Fallback: slight side angle if anchor is missing.
            GetOrbitPose(out position, out _);
        }

        Vector3 lookPoint;
        if (talkLookAt != null)
        {
            lookPoint = talkLookAt.position + Vector3.up * talkLookHeightBoost;
        }
        else if (talkCamAnchor != null)
        {
            lookPoint = talkCamAnchor.position + talkCamAnchor.forward;
        }
        else if (target != null)
        {
            lookPoint = target.position + Vector3.up * height;
        }
        else
        {
            lookPoint = position + transform.forward;
        }

        Vector3 toLook = lookPoint - position;
        if (toLook.sqrMagnitude < 0.0001f)
        {
            rotation = transform.rotation;
        }
        else
        {
            rotation = Quaternion.LookRotation(toLook.normalized, Vector3.up);
        }
    }
}

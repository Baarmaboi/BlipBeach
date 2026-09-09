using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float airControl = 0.35f;
    [Tooltip("Keeps similar flat-ground speed when running up/down slopes.")]
    [SerializeField] private bool compensateHorizontalSpeedOnSlopes = true;

    [Header("Camera")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundProbeDistance = 0.35f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private float maxWalkableSlope = 45f;
    [Tooltip("Invisible ledge ramp colliders on this layer may use a steeper walk limit.")]
    [SerializeField] private LayerMask stepRampLayers;
    [SerializeField] private float maxStepRampSlope = 80f;
    [Tooltip("Smooths slope normal changes to stop speed spikes on uneven ground.")]
    [SerializeField] private float groundNormalSmoothing = 12f;
    [Tooltip("Briefly stay grounded if the probe misses for a frame.")]
    [SerializeField] private float groundedGraceTime = 0.1f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [Tooltip("Ignore ground briefly after jumping so Jump→Run cannot fire while still near the floor.")]
    [SerializeField] private float jumpGroundedLockout = 0.2f;
    [Tooltip("Grace period after leaving ground where jump still works.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier = 2f;

    [Header("Walls")]
    [SerializeField] private float wallCheckDistance = 0.45f;

    [Header("Fall Animation")]
    [Tooltip("Vertical speed must be below this to count as falling (not rising/jump apex).")]
    [SerializeField] private float fallVelocityThreshold = -0.5f;

    public float SpeedMultiplier { get; set; } = 1f;
    public bool IsGrounded { get; private set; } = true;
    public bool IsFalling { get; private set; }
    public float PlanarSpeed { get; private set; }
    public bool IsDetectingLocomotion => detectingLocomotion;
    /// <summary>False while day transitions, dialogue, dig reward, etc. pause gameplay.</summary>
    public bool InputEnabled => inputEnabled;
    public float DetectMoveX { get; private set; }
    public float DetectMoveZ { get; private set; }

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerAnimationController playerAnimation;
    private float horizontalInput;
    private float verticalInput;
    private bool jumpPressed;
    private bool detectingLocomotion;
    private bool inputEnabled = true;
    private bool detectAnimMoveLocked;
    private float groundedLockoutTimer;
    private float groundedGraceTimer;
    private float coyoteTimer;
    private bool isGroundedForMovement;
    private Vector3 groundNormal = Vector3.up;
    private RaycastHit groundHit;

    public void SetDetectingLocomotion(bool enabled)
    {
        detectingLocomotion = enabled;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
        {
            ClearMoveInput();
        }
    }

    public void SetDetectAnimMoveLocked(bool locked)
    {
        detectAnimMoveLocked = locked;
        if (locked)
        {
            ClearMoveInput();
        }
    }

    private void ClearMoveInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        jumpPressed = false;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerAnimation = GetComponent<PlayerAnimationController>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        if (!inputEnabled || detectAnimMoveLocked)
        {
            DetectMoveX = 0f;
            DetectMoveZ = 0f;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space) && !detectingLocomotion)
        {
            jumpPressed = true;
        }

        UpdateDetectMoveValues();
    }

    private void UpdateDetectMoveValues()
    {
        if (detectingLocomotion)
        {
            DetectMoveX = horizontalInput;
            DetectMoveZ = verticalInput;
        }
        else
        {
            DetectMoveX = 0f;
            DetectMoveZ = 0f;
        }
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();

        float yaw = thirdPersonCamera != null ? thirdPersonCamera.Yaw : transform.eulerAngles.y;
        Quaternion cameraYawRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput);
        bool hasInput = inputDirection.sqrMagnitude > 0.001f;
        if (hasInput)
        {
            inputDirection.Normalize();
            rb.WakeUp();
        }

        Vector3 wishDirection = cameraYawRotation * inputDirection;
        float targetSpeed = moveSpeed * SpeedMultiplier;
        Vector3 wishVelocity = wishDirection * targetSpeed;

        if (isGroundedForMovement)
        {
            ApplyGroundedMovement(wishVelocity, hasInput);
        }
        else
        {
            ApplyAirMovement(wishVelocity, hasInput);
            ApplyExtraFallGravity();
        }

        ApplyWallSlide(wishDirection, wishVelocity, hasInput);

        PlanarSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

        ApplyRotation(cameraYawRotation, wishDirection, hasInput);
        TryJump();
        UpdateFallingState();
    }

    private void UpdateFallingState()
    {
        if (IsGrounded || groundedLockoutTimer > 0f)
        {
            IsFalling = false;
            return;
        }

        IsFalling = rb.linearVelocity.y < fallVelocityThreshold;
    }

    private void ApplyGroundedMovement(Vector3 wishVelocity, bool hasInput)
    {
        float accelRate = hasInput ? acceleration : deceleration;
        bool onSlope = Vector3.Dot(groundNormal, Vector3.up) < 0.95f;

        Vector3 targetVelocity = wishVelocity;
        if (onSlope)
        {
            targetVelocity = Vector3.ProjectOnPlane(wishVelocity, groundNormal);
            if (targetVelocity.sqrMagnitude > 0.001f)
            {
                float alongSlopeSpeed = wishVelocity.magnitude;
                if (compensateHorizontalSpeedOnSlopes)
                {
                    float slopeAlignment = Mathf.Clamp(Vector3.Dot(groundNormal, Vector3.up), 0.25f, 1f);
                    alongSlopeSpeed /= slopeAlignment;
                }

                targetVelocity = targetVelocity.normalized * alongSlopeSpeed;
            }
        }

        Vector3 current = rb.linearVelocity;

        if (onSlope)
        {
            Vector3 currentOnSlope = Vector3.ProjectOnPlane(current, groundNormal);
            Vector3 newOnSlope = Vector3.MoveTowards(currentOnSlope, targetVelocity, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = newOnSlope;
            return;
        }

        // Flat ground — keep vertical velocity from physics; only drive horizontal (original working pattern).
        Vector3 currentHorizontal = new Vector3(current.x, 0f, current.z);
        Vector3 targetHorizontal = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
        Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontal, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newHorizontal.x, current.y, newHorizontal.z);
    }

    private void ApplyAirMovement(Vector3 wishVelocity, bool hasInput)
    {
        float accelRate = hasInput ? acceleration : deceleration;

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetHorizontal = new Vector3(wishVelocity.x, 0f, wishVelocity.z);
        Vector3 newHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            targetHorizontal,
            accelRate * airControl * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);
    }

    private void ApplyExtraFallGravity()
    {
        if (rb.linearVelocity.y >= 0f)
        {
            return;
        }

        rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
    }

    private void ApplyWallSlide(Vector3 wishDirection, Vector3 wishVelocity, bool hasInput)
    {
        Vector3 checkDirection = wishDirection;
        if (checkDirection.sqrMagnitude < 0.001f)
        {
            checkDirection = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        if (checkDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 wallNormal = FindWallNormal(checkDirection);
        if (wallNormal == Vector3.zero)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;

        if (hasInput)
        {
            Vector3 slideVelocity = Vector3.ProjectOnPlane(wishVelocity, wallNormal);
            velocity = new Vector3(slideVelocity.x, velocity.y, slideVelocity.z);
        }
        else
        {
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 slideVelocity = Vector3.ProjectOnPlane(horizontal, wallNormal);
            velocity = new Vector3(slideVelocity.x, velocity.y, slideVelocity.z);
        }

        rb.linearVelocity = velocity;
    }

    private Vector3 FindWallNormal(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        direction.Normalize();
        GetCapsulePoints(out Vector3 bottom, out Vector3 top);
        float radius = capsule.radius * 0.95f;

        if (!Physics.CapsuleCast(
                bottom,
                top,
                radius,
                direction,
                out RaycastHit hit,
                wallCheckDistance,
                GetProbeMask(),
                QueryTriggerInteraction.Ignore)
            || !IsValidWallHit(hit))
        {
            return Vector3.zero;
        }

        Vector3 normal = hit.normal;
        normal.y = 0f;
        return normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.zero;
    }

    private bool IsValidWallHit(RaycastHit hit)
    {
        if (hit.collider == null || IsSelfCollider(hit.collider))
        {
            return false;
        }

        if (IsStepRamp(hit.collider))
        {
            return false;
        }

        return Vector3.Angle(hit.normal, Vector3.up) > maxWalkableSlope;
    }

    private void GetCapsulePoints(out Vector3 bottom, out Vector3 top)
    {
        float radius = capsule.radius * 0.95f;
        float halfHeight = Mathf.Max(capsule.height * 0.5f - radius, 0.01f);
        Vector3 worldCenter = transform.TransformPoint(capsule.center);
        bottom = worldCenter + Vector3.down * halfHeight;
        top = worldCenter + Vector3.up * halfHeight;
    }

    private void ApplyRotation(Quaternion cameraYawRotation, Vector3 wishDirection, bool hasInput)
    {
        // Preserve talk-facing (and any other intentional freeze) while input is off.
        if (!inputEnabled)
        {
            return;
        }

        if (detectingLocomotion)
        {
            rb.MoveRotation(cameraYawRotation);
            return;
        }

        if (hasInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(wishDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }

    private void TryJump()
    {
        if (!jumpPressed)
        {
            return;
        }

        jumpPressed = false;

        if (!CanJump())
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        groundedLockoutTimer = jumpGroundedLockout;
        groundedGraceTimer = 0f;
        coyoteTimer = 0f;
        IsGrounded = false;
        isGroundedForMovement = false;
        groundNormal = Vector3.up;
        playerAnimation?.PlayJump();
    }

    private bool CanJump()
    {
        return isGroundedForMovement || coyoteTimer > 0f;
    }

    private void UpdateGroundedState()
    {
        if (groundedLockoutTimer > 0f)
        {
            groundedLockoutTimer -= Time.fixedDeltaTime;
            IsGrounded = false;
            isGroundedForMovement = false;
            groundNormal = Vector3.up;
            groundedGraceTimer = 0f;
            return;
        }

        bool probeHit = ProbeGround(out groundHit);
        if (probeHit)
        {
            Vector3 targetNormal = groundHit.normal;
            float smooth = 1f - Mathf.Exp(-groundNormalSmoothing * Time.fixedDeltaTime);
            groundNormal = Vector3.Slerp(groundNormal, targetNormal, smooth).normalized;
            groundedGraceTimer = groundedGraceTime;
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;

            if (groundedGraceTimer > 0f)
            {
                groundedGraceTimer -= Time.fixedDeltaTime;
            }
            else
            {
                groundNormal = Vector3.up;
            }
        }

        bool probeOrGrace = probeHit || groundedGraceTimer > 0f;
        isGroundedForMovement = probeOrGrace;

        // Probe-only — reliable on slopes. Post-jump lockout handles animation; coyote helps probe misses.
        IsGrounded = probeOrGrace;
    }

    private int GetProbeMask()
    {
        // Always ignore the Player layer so child colliders never block the probe.
        return groundLayers.value & ~(1 << gameObject.layer);
    }

    private bool ProbeGround(out RaycastHit hit)
    {
        hit = default;

        float radius = capsule.radius * 0.95f;
        float halfHeight = Mathf.Max(capsule.height * 0.5f - radius, 0.01f);
        Vector3 worldCenter = transform.TransformPoint(capsule.center);
        Vector3 bottom = worldCenter + Vector3.down * halfHeight;
        int mask = GetProbeMask();
        float castDistance = groundProbeDistance + groundCheckDistance;

        // 1) Sphere cast from feet
        Vector3 sphereOrigin = bottom + Vector3.up * radius;
        if (Physics.SphereCast(sphereOrigin, radius * 0.9f, Vector3.down, out hit, castDistance + radius, mask, QueryTriggerInteraction.Ignore)
            && IsValidGroundHit(hit))
        {
            return true;
        }

        // 2) Ray cast from capsule center (matches the old working check)
        float rayLength = halfHeight + castDistance;
        if (Physics.Raycast(worldCenter, Vector3.down, out hit, rayLength, mask, QueryTriggerInteraction.Ignore)
            && IsValidGroundHit(hit))
        {
            return true;
        }

        // 3) Overlap at feet — catches floor even when cast starts slightly wrong
        Collider[] overlaps = Physics.OverlapSphere(bottom, radius * 0.75f, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (!IsValidGroundCollider(overlaps[i]))
            {
                continue;
            }

            Vector3 rayOrigin = bottom + Vector3.up * castDistance;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, castDistance * 2f, mask, QueryTriggerInteraction.Ignore)
                && hit.collider == overlaps[i])
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidGroundHit(RaycastHit candidate)
    {
        if (candidate.collider == null || IsSelfCollider(candidate.collider))
        {
            return false;
        }

        float maxSlope = GetMaxSlopeForCollider(candidate.collider);
        return Vector3.Angle(candidate.normal, Vector3.up) <= maxSlope;
    }

    private float GetMaxSlopeForCollider(Collider collider)
    {
        return IsStepRamp(collider) ? maxStepRampSlope : maxWalkableSlope;
    }

    private bool IsStepRamp(Collider collider)
    {
        return collider != null && (stepRampLayers.value & (1 << collider.gameObject.layer)) != 0;
    }

    private bool IsValidGroundCollider(Collider candidate)
    {
        return candidate != null && !IsSelfCollider(candidate);
    }

    private bool IsSelfCollider(Collider other)
    {
        return other.transform == transform || other.transform.IsChildOf(transform);
    }
}

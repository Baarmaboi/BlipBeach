using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float moveThreshold = 0.1f;
    [SerializeField] private float detectMoveDeadzone = 0.1f;
    [SerializeField] private string detectLowerBodyLayerName = "DetectLowerBody";
    [SerializeField] private string detectUpperBodyLayerName = "DetectUpperBody";

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsDetectingHash = Animator.StringToHash("IsDetecting");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsRewardOpenHash = Animator.StringToHash("IsRewardOpen");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DetectStartHash = Animator.StringToHash("DetectStart");
    private static readonly int DetectEndHash = Animator.StringToHash("DetectEnd");
    private static readonly int DigHash = Animator.StringToHash("Dig");
    private static readonly int ItemGetHash = Animator.StringToHash("ItemGet");
    private static readonly int MoveXHash = Animator.StringToHash("moveX");
    private static readonly int MoveZHash = Animator.StringToHash("moveZ");

    private static readonly int DetectStartStateHash = Animator.StringToHash("Detect-Start");
    private static readonly int DetectEndStateHash = Animator.StringToHash("Detect-End");

    private bool waitingForDetectAnim;
    private bool isDetecting;
    private int lockedDetectStateHash;
    private bool sawLockedDetectState;
    private float detectAnimLockTimer;
    private int detectLowerBodyLayerIndex = -1;
    private int detectUpperBodyLayerIndex = -1;
    private const float DetectAnimLockTimeout = 2f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (animator != null)
        {
            detectLowerBodyLayerIndex = animator.GetLayerIndex(detectLowerBodyLayerName);
            detectUpperBodyLayerIndex = animator.GetLayerIndex(detectUpperBodyLayerName);
        }
    }

    private void Update()
    {
        if (animator == null || playerMovement == null)
        {
            return;
        }

        float speed = playerMovement.PlanarSpeed;
        bool isMoving = speed > moveThreshold;

        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsGroundedHash, playerMovement.IsGrounded);
        animator.SetBool(IsFallingHash, playerMovement.IsFalling);
        animator.SetBool(IsMovingHash, isMoving);

        UpdateDetectLocomotionBlend();
        UpdateDetectLayerWeights();
        UpdateDetectAnimMoveLock();
    }

    public void PlayJump()
    {
        if (animator != null)
        {
            animator.SetTrigger(JumpHash);
        }
    }

    public void SetDetecting(bool detecting)
    {
        if (animator == null)
        {
            return;
        }

        isDetecting = detecting;
        animator.SetBool(IsDetectingHash, detecting);
        animator.SetTrigger(detecting ? DetectStartHash : DetectEndHash);
        BeginDetectAnimMoveLock(detecting ? DetectStartStateHash : DetectEndStateHash);
    }

    public void EndDetectingImmediate()
    {
        if (animator == null)
        {
            return;
        }

        isDetecting = false;
        waitingForDetectAnim = false;
        sawLockedDetectState = false;
        playerMovement?.SetDetectAnimMoveLocked(false);

        animator.SetBool(IsDetectingHash, false);
        animator.SetFloat(MoveXHash, 0f);
        animator.SetFloat(MoveZHash, 0f);

        if (detectLowerBodyLayerIndex >= 0)
        {
            animator.SetLayerWeight(detectLowerBodyLayerIndex, 0f);
        }

        if (detectUpperBodyLayerIndex >= 0)
        {
            animator.SetLayerWeight(detectUpperBodyLayerIndex, 0f);
        }
    }

    public void PlayDig()
    {
        if (animator != null)
        {
            animator.SetTrigger(DigHash);
        }
    }

    public void PlayItemGet()
    {
        if (animator != null)
        {
            // Keeps Item Get from instantly exiting while IsDetecting is already true.
            animator.SetBool(IsRewardOpenHash, true);
            animator.SetTrigger(ItemGetHash);
        }
    }

    public void SetRewardOpen(bool open)
    {
        if (animator != null)
        {
            animator.SetBool(IsRewardOpenHash, open);
        }
    }

    private void UpdateDetectLocomotionBlend()
    {
        float moveX = ApplyDetectMoveDeadzone(playerMovement.DetectMoveX);
        float moveZ = ApplyDetectMoveDeadzone(playerMovement.DetectMoveZ);
        animator.SetFloat(MoveXHash, moveX);
        animator.SetFloat(MoveZHash, moveZ);
    }

    private void UpdateDetectLayerWeights()
    {
        // Wait for Detect-Start / Detect-End to finish before overlay layers take over.
        float weight = isDetecting && !waitingForDetectAnim ? 1f : 0f;

        if (detectLowerBodyLayerIndex >= 0)
        {
            animator.SetLayerWeight(detectLowerBodyLayerIndex, weight);
        }

        if (detectUpperBodyLayerIndex >= 0)
        {
            animator.SetLayerWeight(detectUpperBodyLayerIndex, weight);
        }
    }

    private float ApplyDetectMoveDeadzone(float value)
    {
        return Mathf.Abs(value) < detectMoveDeadzone ? 0f : value;
    }

    private void BeginDetectAnimMoveLock(int stateHash)
    {
        waitingForDetectAnim = true;
        lockedDetectStateHash = stateHash;
        sawLockedDetectState = false;
        detectAnimLockTimer = 0f;
        playerMovement?.SetDetectAnimMoveLocked(true);
    }

    private void UpdateDetectAnimMoveLock()
    {
        if (!waitingForDetectAnim)
        {
            return;
        }

        detectAnimLockTimer += Time.deltaTime;
        if (detectAnimLockTimer >= DetectAnimLockTimeout)
        {
            EndDetectAnimMoveLock();
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        if (info.shortNameHash == lockedDetectStateHash)
        {
            sawLockedDetectState = true;

            // Wait until the locked clip finishes (and is not blending out yet).
            if (info.normalizedTime >= 1f && !animator.IsInTransition(0))
            {
                EndDetectAnimMoveLock();
            }

            return;
        }

        if (sawLockedDetectState)
        {
            EndDetectAnimMoveLock();
        }
    }

    private void EndDetectAnimMoveLock()
    {
        waitingForDetectAnim = false;
        sawLockedDetectState = false;
        playerMovement?.SetDetectAnimMoveLocked(false);
    }
}

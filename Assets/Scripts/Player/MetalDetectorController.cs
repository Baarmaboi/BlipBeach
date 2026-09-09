using UnityEngine;

public class MetalDetectorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private DetectorScanZone scanZone;
    [SerializeField] private MetalDetectorHUD hud;
    [SerializeField] private AudioSource detectAudio;
    [SerializeField] private PlayerAnimationController playerAnimation;

    [Header("Detecting")]
    [SerializeField] private float detectingSpeedMultiplier = 0.4f;
    [SerializeField] private float digDuration = 1.5f;
    [SerializeField] private AudioClip detectIdleLoopClip;
    [SerializeField] private AudioClip detectFoundLoopClip;
    [SerializeField] private float detectVolume = 0.6f;

    private bool isDetecting;
    private bool isDigging;
    private bool isRewardOpen;
    private float digTimer;
    private BuriedItem itemBeingDug;
    private DetectSoundMode currentSoundMode;
    private AudioClip runtimeBeepClip;

    public bool BlocksNpcTalk => isDetecting || isDigging || isRewardOpen;

    private enum DetectSoundMode
    {
        None,
        Idle,
        Found
    }

    private void Awake()
    {
        if (scanZone != null)
        {
            scanZone.gameObject.SetActive(false);
        }

        EnsureAudioSource();
        runtimeBeepClip = CreateBeepClip();

        if (playerAnimation == null)
        {
            playerAnimation = GetComponent<PlayerAnimationController>();
        }
    }

    private void OnDestroy()
    {
        if (runtimeBeepClip != null)
        {
            Destroy(runtimeBeepClip);
        }
    }

    private void Update()
    {
        if (DialogueUI.IsOpen || QuestLogUI.IsOpen || DaySummaryUI.IsOpen || EconomyMenus.IsAnyOpen)
        {
            if (isDetecting)
            {
                SetDetecting(false);
            }

            return;
        }

        if (isRewardOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                CloseReward();
            }

            return;
        }

        if (isDigging)
        {
            digTimer += Time.deltaTime;
            if (digTimer >= digDuration)
            {
                FinishDig();
            }

            return;
        }

        // Match movement: no begin/toggle detect while day start/end (or other UI) has paused input.
        if (!CanUseDetectControls())
        {
            if (isDetecting)
            {
                SetDetecting(false);
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetDetecting(!isDetecting);
            return;
        }

        if (!isDetecting)
        {
            return;
        }

        HandleDepthModeInput();

        BuriedItem item = scanZone != null ? scanZone.CurrentUndugItem : null;
        bool itemFound = item != null;

        if (hud != null)
        {
            hud.SetScanStatus(itemFound);
            hud.SetDigPromptVisible(itemFound);
            hud.RefreshDepthDisplay();
        }

        UpdateDetectSound(itemFound);

        if (itemFound && Input.GetKeyDown(KeyCode.E))
        {
            StartDig(item);
        }
    }

    private bool CanUseDetectControls()
    {
        if (playerMovement != null && !playerMovement.InputEnabled)
        {
            return false;
        }

        if (DayManager.Instance != null && DayManager.Instance.IsDayTransitioning)
        {
            return false;
        }

        return true;
    }

    private void HandleDepthModeInput()
    {
        DetectorProgress progress = DetectorProgress.Instance;
        if (progress == null)
        {
            return;
        }

        int direction = 0;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            direction = 1;
        }
        else if (Input.mouseScrollDelta.y > 0.1f)
        {
            direction = 1;
        }
        else if (Input.mouseScrollDelta.y < -0.1f)
        {
            direction = -1;
        }

        if (direction == 0)
        {
            return;
        }

        progress.CycleMode(direction);
    }

    private void SetDetecting(bool detecting)
    {
        isDetecting = detecting;

        if (playerMovement != null)
        {
            playerMovement.SpeedMultiplier = detecting ? detectingSpeedMultiplier : 1f;
            playerMovement.SetDetectingLocomotion(detecting);
        }

        if (scanZone != null)
        {
            scanZone.Clear();
            scanZone.gameObject.SetActive(detecting);
        }

        if (hud != null)
        {
            hud.SetDetectingVisible(detecting);
            hud.HideReward();
            if (detecting)
            {
                hud.RefreshDepthDisplay();
            }
        }

        if (detecting)
        {
            UpdateDetectSound(false);
        }
        else
        {
            StopDetectSound();
        }

        playerAnimation?.SetDetecting(detecting);
    }

    private void StartDig(BuriedItem item)
    {
        isDigging = true;
        digTimer = 0f;
        itemBeingDug = item;

        EndDetectModeForDig();

        SetPlayerControlsEnabled(false);

        StopDetectSound();

        if (hud != null)
        {
            hud.SetDigging();
        }

        playerAnimation?.PlayDig();
    }

    private void EndDetectModeForDig()
    {
        isDetecting = false;

        if (playerMovement != null)
        {
            playerMovement.SetDetectingLocomotion(false);
        }

        if (scanZone != null)
        {
            scanZone.gameObject.SetActive(false);
        }

        playerAnimation?.EndDetectingImmediate();
    }

    private void FinishDig()
    {
        isDigging = false;

        BuriedItem dugItem = itemBeingDug;
        if (dugItem != null)
        {
            dugItem.Dig();
        }

        itemBeingDug = null;
        isRewardOpen = true;

        if (hud != null)
        {
            hud.ShowReward(dugItem);
        }

        playerAnimation?.PlayItemGet();

        // Controls already disabled for dig; keep them off for the reward panel.
        SetPlayerControlsEnabled(false);
    }

    private void CloseReward()
    {
        isRewardOpen = false;
        if (hud != null)
        {
            hud.HideReward();
        }

        SetPlayerControlsEnabled(true);
        playerAnimation?.SetRewardOpen(false);

        SetDetecting(true);
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.SetInputEnabled(enabled);
            playerMovement.SpeedMultiplier = enabled
                ? (isDetecting ? detectingSpeedMultiplier : 1f)
                : 0f;
        }

        SetCameraLookEnabled(enabled);
    }

    private void SetCameraLookEnabled(bool enabled)
    {
        if (thirdPersonCamera == null)
        {
            return;
        }

        thirdPersonCamera.LookEnabled = enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    private void UpdateDetectSound(bool itemFound)
    {
        DetectSoundMode desired = itemFound ? DetectSoundMode.Found : DetectSoundMode.Idle;
        if (desired == currentSoundMode)
        {
            return;
        }

        currentSoundMode = desired;

        if (itemFound)
        {
            PlayLoop(detectFoundLoopClip != null ? detectFoundLoopClip : runtimeBeepClip);
        }
        else if (detectIdleLoopClip != null)
        {
            PlayLoop(detectIdleLoopClip);
        }
        else
        {
            // No idle clip assigned — stay silent without retriggering every frame.
            if (detectAudio != null && detectAudio.isPlaying)
            {
                detectAudio.Stop();
            }
        }
    }

    private void PlayLoop(AudioClip clip)
    {
        EnsureAudioSource();
        if (detectAudio == null || clip == null)
        {
            return;
        }

        detectAudio.clip = clip;
        detectAudio.loop = true;
        detectAudio.mute = false;
        detectAudio.enabled = true;
        detectAudio.spatialBlend = 0f;
        detectAudio.volume = detectVolume;
        detectAudio.Play();
    }

    private void StopDetectSound()
    {
        currentSoundMode = DetectSoundMode.None;

        if (detectAudio != null && detectAudio.isPlaying)
        {
            detectAudio.Stop();
        }
    }

    private void EnsureAudioSource()
    {
        if (detectAudio == null)
        {
            detectAudio = GetComponent<AudioSource>();
        }

        if (detectAudio == null)
        {
            detectAudio = gameObject.AddComponent<AudioSource>();
        }

        detectAudio.playOnAwake = false;
        detectAudio.loop = true;
        detectAudio.spatialBlend = 0f;
        detectAudio.volume = detectVolume;
        detectAudio.mute = false;
    }

    // Short pulsing beep so detecting works without an imported AudioClip.
    private static AudioClip CreateBeepClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.25f;
        const float frequency = 880f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = t < 0.05f ? 1f : 0f;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("DetectorBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

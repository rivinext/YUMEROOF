using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles triggering and cleaning up of player emote animations.
/// </summary>
public class PlayerEmoteController : MonoBehaviour
{
    [System.Serializable]
    public class EmoteEntry
    {
        [Tooltip("Identifier used to trigger this emote from code/UI.")]
        public string emoteId;

        [Tooltip("Animator state name that should play when the emote is triggered.")]
        public string stateName;

        [Tooltip("Animation clip that defines the emote's duration. Optional but recommended for accurate timing.")]
        public AnimationClip animationClip;

        [Tooltip("If true the player controller keeps movement input enabled while the emote plays.")]
        public bool allowMovement = false;

        [Tooltip("If true the emote will loop until StopActiveEmote is called.")]
        public bool loop = false;

        [Tooltip("Cross-fade duration when transitioning into the emote state.")]
        public float crossFadeIn = 0.15f;

        [Tooltip("If true the emote waits for movement input before finishing when not looping.")]
        public bool waitForMovementInput = false;

        [Tooltip("Cross-fade duration when returning to the baseline idle state.")]
        public float crossFadeOut = 0.15f;

        private int stateHash = 0;

        public int StateHash
        {
            get
            {
                if (stateHash == 0 && !string.IsNullOrEmpty(stateName))
                {
                    stateHash = Animator.StringToHash(stateName);
                }

                return stateHash;
            }
        }

        public float ClipLength => animationClip != null ? animationClip.length : 0f;

        public void RefreshCachedValues()
        {
            stateHash = 0;
            _ = StateHash;
        }
    }

    [Header("Dependencies")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerBlinkController blinkController;
    [SerializeField] private PlayerIdleSleepController sleepController;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerEyeBlendShapeController eyeBlendShapeController;
    [SerializeField] private PlayerEmoteEyeSliderPanel eyeSliderPanel;
    [SerializeField] private PlayerMouthBlendShapeController mouthBlendShapeController;
    [SerializeField] private PlayerEmoteMouthSliderPanel mouthSliderPanel;

    [Header("Baseline States")]
    [SerializeField, Tooltip("Animator state used when the player is standing idle.")]
    private string standIdleStateName = "Idle";

    [SerializeField, Tooltip("Animator state used when the player is sitting idle.")]
    private string sitIdleStateName = "SitIdle";

    [Header("Emotes")]
    [SerializeField] private List<EmoteEntry> emoteEntries = new List<EmoteEntry>();

    [SerializeField, Tooltip("Fallback cross-fade duration used when an emote entry does not define an exit duration.")]
    private float defaultCrossFadeOut = 0.15f;

    private readonly Dictionary<string, EmoteEntry> emoteLookup = new Dictionary<string, EmoteEntry>();

    private Coroutine activeCoroutine;
    private EmoteEntry activeEmote;
    private bool stopRequested;
    private bool inputLockedByEmote;
    private bool blinkLockedByEmote;

    public bool IsBlinkLocked => blinkLockedByEmote;
    public bool IsEmoteActive => activeEmote != null;

    private int standIdleStateHash;
    private int sitIdleStateHash;

    private const float FallbackEmoteDuration = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (blinkController == null)
        {
            blinkController = GetComponent<PlayerBlinkController>();
        }

        if (sleepController == null)
        {
            sleepController = GetComponent<PlayerIdleSleepController>();
        }

        if (eyeBlendShapeController == null)
        {
            eyeBlendShapeController = GetComponent<PlayerEyeBlendShapeController>();
        }

        if (mouthBlendShapeController == null)
        {
            mouthBlendShapeController = GetComponent<PlayerMouthBlendShapeController>();
        }

        ResolveSliderPanels();

        CacheBaselineStateHashes();
        RebuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveSliderPanels();
        CacheBaselineStateHashes();
        RebuildLookup();
    }
#endif

    /// <summary>
    /// Triggers the specified emote.
    /// </summary>
    /// <param name="emoteId">Identifier defined on the emote entry.</param>
    public void PlayEmote(string emoteId)
    {
        if (!isActiveAndEnabled || string.IsNullOrEmpty(emoteId))
        {
            return;
        }

        if (!emoteLookup.TryGetValue(emoteId, out EmoteEntry entry) || entry == null)
        {
            Debug.LogWarning($"Emote '{emoteId}' is not defined.", this);
            return;
        }

        StopActiveEmoteImmediate(false, preserveManualControls: true);

        if (animator == null)
        {
            Debug.LogWarning("PlayerEmoteController requires an Animator reference.", this);
            return;
        }

        activeCoroutine = StartCoroutine(PlayEmoteRoutine(entry));
    }

    /// <summary>
    /// Requests the currently playing emote to stop. Primarily used for looping emotes.
    /// </summary>
    public void StopActiveEmote()
    {
        if (activeCoroutine == null)
        {
            return;
        }

        stopRequested = true;
    }

    private void OnDisable()
    {
        StopActiveEmoteImmediate(true);
    }

    private void OnDestroy()
    {
        StopActiveEmoteImmediate(true);
    }

    public IReadOnlyDictionary<string, EmoteEntry> GetEmoteLookup() => emoteLookup;

    public void ForceReturnToIdle(float fadeDuration = 0.1f)
    {
        StopActiveEmoteImmediate(false);
        CrossFadeToBaseline(Mathf.Max(0f, fadeDuration));
    }

    private IEnumerator PlayEmoteRoutine(EmoteEntry entry)
    {
        activeEmote = entry;
        stopRequested = false;

        if (blinkController != null)
        {
            blinkController.SetBlinkingEnabled(false);
            blinkController.ResetBlinkState();
            blinkLockedByEmote = true;
        }

        bool shouldLockInput = playerController != null && !entry.allowMovement;
        if (shouldLockInput)
        {
            playerController.SetInputEnabled(false);
            inputLockedByEmote = true;
        }

        if (eyeBlendShapeController != null)
        {
            eyeBlendShapeController.SetManualControlActive(true);
        }

        if (mouthBlendShapeController != null)
        {
            mouthBlendShapeController.SetManualControlActive(true);
        }

        if (eyeSliderPanel != null)
        {
            eyeSliderPanel.SetEmoteModeActive(true);
            if (eyeBlendShapeController != null)
            {
                eyeSliderPanel.RefreshSliderValues(eyeBlendShapeController.LeftEyeWeight, eyeBlendShapeController.RightEyeWeight);
            }
            else
            {
                eyeSliderPanel.ResetSliders();
            }
        }

        if (mouthSliderPanel != null)
        {
            mouthSliderPanel.SetEmoteModeActive(true);
            if (mouthBlendShapeController != null)
            {
                mouthSliderPanel.RefreshSliderValues(mouthBlendShapeController.VerticalWeight, mouthBlendShapeController.HorizontalWeight);
            }
            else
            {
                mouthSliderPanel.ResetSliders();
            }
        }

        if (sleepController != null)
        {
            sleepController.ForceState(playerController != null && playerController.IsSitting);
        }

        int targetStateHash = entry.StateHash;
        if (targetStateHash == 0)
        {
            Debug.LogWarning($"Emote '{entry.emoteId}' does not have a valid state name.", this);
            RestorePostEmoteState(true);
            activeCoroutine = null;
            yield break;
        }

        animator.CrossFade(targetStateHash, Mathf.Max(0f, entry.crossFadeIn), 0, 0f);

        if (entry.loop)
        {
            while (!stopRequested)
            {
                yield return null;
            }
        }
        else
        {
            if (entry.waitForMovementInput)
            {
                float deadZone = playerController != null ? Mathf.Max(0f, playerController.inputDeadZone) : 0f;
                while (!stopRequested)
                {
                    float horizontal = Input.GetAxisRaw("Horizontal");
                    float vertical = Input.GetAxisRaw("Vertical");
                    Vector2 inputVector = new Vector2(horizontal, vertical);
                    float inputMagnitude = inputVector.magnitude;
                    if ((deadZone > 0f && inputMagnitude >= deadZone) || (deadZone <= 0f && inputMagnitude > 0f))
                    {
                        break;
                    }

                    yield return null;
                }
            }
            else
            {
                float duration = Mathf.Max(GetEmoteDuration(entry), 0f);
                float elapsed = 0f;
                while (elapsed < duration && !stopRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        RestorePostEmoteState(true);
        activeCoroutine = null;
    }

    private void StopActiveEmoteImmediate(bool crossFadeToIdle, bool preserveManualControls = false)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        if (activeEmote != null || inputLockedByEmote || blinkLockedByEmote)
        {
            RestorePostEmoteState(crossFadeToIdle, preserveManualControls);
        }
        else if (crossFadeToIdle)
        {
            CrossFadeToBaseline(defaultCrossFadeOut);
        }
    }

    private void RestorePostEmoteState(bool crossFadeToIdle, bool preserveManualControls = false)
    {
        if (inputLockedByEmote && playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
        inputLockedByEmote = false;

        if (blinkLockedByEmote && blinkController != null)
        {
            blinkController.SetBlinkingEnabled(true);
            blinkController.NotifyActive();
        }
        blinkLockedByEmote = false;

        if (!preserveManualControls && eyeBlendShapeController != null)
        {
            eyeBlendShapeController.SetManualControlActive(false);
        }

        if (!preserveManualControls && mouthBlendShapeController != null)
        {
            mouthBlendShapeController.SetManualControlActive(false);
        }

        if (!preserveManualControls && eyeSliderPanel != null)
        {
            eyeSliderPanel.SetEmoteModeActive(false);
            eyeSliderPanel.ResetSliders();
        }

        if (!preserveManualControls && mouthSliderPanel != null)
        {
            mouthSliderPanel.SetEmoteModeActive(false);
            mouthSliderPanel.ResetSliders();
        }

        if (sleepController != null)
        {
            sleepController.ForceState(playerController != null && playerController.IsSitting);
        }

        if (crossFadeToIdle)
        {
            float fadeDuration = activeEmote != null ? Mathf.Max(0f, activeEmote.crossFadeOut) : Mathf.Max(0f, defaultCrossFadeOut);
            CrossFadeToBaseline(fadeDuration);
        }

        activeEmote = null;
        stopRequested = false;
    }

    private void CrossFadeToBaseline(float fadeDuration)
    {
        if (animator == null)
        {
            return;
        }

        int baselineHash = GetBaselineStateHash();
        if (baselineHash != 0)
        {
            animator.CrossFade(baselineHash, fadeDuration, 0, 0f);
        }
    }

    private int GetBaselineStateHash()
    {
        bool isSitting = playerController != null && playerController.IsSitting;
        if (isSitting && sitIdleStateHash != 0)
        {
            return sitIdleStateHash;
        }

        return standIdleStateHash != 0 ? standIdleStateHash : sitIdleStateHash;
    }

    private void CacheBaselineStateHashes()
    {
        standIdleStateHash = !string.IsNullOrEmpty(standIdleStateName) ? Animator.StringToHash(standIdleStateName) : 0;
        sitIdleStateHash = !string.IsNullOrEmpty(sitIdleStateName) ? Animator.StringToHash(sitIdleStateName) : 0;
    }

    private void ResolveSliderPanels()
    {
        if (eyeSliderPanel == null)
        {
            eyeSliderPanel = GetComponentInChildren<PlayerEmoteEyeSliderPanel>(true);

            if (eyeSliderPanel == null)
            {
#if UNITY_2023_1_OR_NEWER
                eyeSliderPanel = FindFirstObjectByType<PlayerEmoteEyeSliderPanel>();
#else
                eyeSliderPanel = FindObjectOfType<PlayerEmoteEyeSliderPanel>();
#endif
            }
        }

        if (mouthSliderPanel == null)
        {
            mouthSliderPanel = GetComponentInChildren<PlayerEmoteMouthSliderPanel>(true);

            if (mouthSliderPanel == null)
            {
#if UNITY_2023_1_OR_NEWER
                mouthSliderPanel = FindFirstObjectByType<PlayerEmoteMouthSliderPanel>();
#else
                mouthSliderPanel = FindObjectOfType<PlayerEmoteMouthSliderPanel>();
#endif
            }
        }
    }

    private void RebuildLookup()
    {
        emoteLookup.Clear();

        if (emoteEntries == null)
        {
            return;
        }

        for (int i = 0; i < emoteEntries.Count; i++)
        {
            EmoteEntry entry = emoteEntries[i];
            if (entry == null)
            {
                continue;
            }

            entry.RefreshCachedValues();

            if (string.IsNullOrEmpty(entry.emoteId))
            {
                continue;
            }

            string key = entry.emoteId.Trim();
            if (key.Length == 0)
            {
                continue;
            }

            if (emoteLookup.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate emote id detected: '{key}'. Skipping entry at index {i}.", this);
                continue;
            }

            emoteLookup.Add(key, entry);
        }
    }

    private float GetEmoteDuration(EmoteEntry entry)
    {
        if (entry == null)
        {
            return FallbackEmoteDuration;
        }

        float clipLength = entry.ClipLength;
        if (clipLength > 0f)
        {
            return clipLength;
        }

        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(entry.stateName))
        {
            return FallbackEmoteDuration;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == entry.stateName)
            {
                return clip.length;
            }
        }

        return FallbackEmoteDuration;
    }
}

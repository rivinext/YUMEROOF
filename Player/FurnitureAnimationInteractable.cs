using UnityEngine;

/// <summary>
/// 家具などのオブジェクトに割り当てて、プレイヤーがEキーでインタラクトした際に
/// アニメーションを再生・停止させるためのコンポーネント。
/// </summary>
public class FurnitureAnimationInteractable : MonoBehaviour, IInteractable
{
    public enum InteractionAnimationMode
    {
        PlayOnce,
        ToggleLoop,
    }

    public enum PlayOnceMethod
    {
        Trigger,
        PlayState,
    }

    [Header("Common")]
    [Tooltip("操作対象のAnimator。未指定の場合は子階層から自動取得を試みます。")]
    [SerializeField] private Animator targetAnimator;

    [Tooltip("インタラクト時の挙動タイプ。\nPlayOnce: 1回だけ再生。\nToggleLoop: ループ状態をオン/オフ。")]
    [SerializeField] private InteractionAnimationMode interactionMode = InteractionAnimationMode.PlayOnce;

    [Header("Play Once Settings")]
    [SerializeField] private PlayOnceMethod playOnceMethod = PlayOnceMethod.Trigger;
    [Tooltip("Trigger方式を使用する場合に送信するトリガー名。")]
    [SerializeField] private string playOnceTriggerName = "Play";
    [Tooltip("State再生方式を使用する場合のステート名。")]
    [SerializeField] private string playOnceStateName = string.Empty;
    [Tooltip("State再生方式を使用する場合のレイヤー番号。")]
    [SerializeField] private int playOnceLayerIndex = 0;
    [Tooltip("State再生方式を使用する場合のNormalizedTime。")]
    [SerializeField] private float playOnceStateNormalizedTime = 0f;

    [Header("Loop Toggle Settings")]
    [Tooltip("ループ状態を制御するBoolパラメーター名。空の場合は設定しません。")]
    [SerializeField] private string loopBoolParameter = "IsLooping";
    [Tooltip("ループ開始時に送信するトリガー名。空の場合は送信しません。")]
    [SerializeField] private string loopStartTrigger = string.Empty;
    [Tooltip("ループ停止時に送信するトリガー名。空の場合は送信しません。")]
    [SerializeField] private string loopStopTrigger = string.Empty;
    [Tooltip("シーン開始時にループを有効にしたい場合は true。")]
    [SerializeField] private bool startLoopActive = false;

    [Header("Audio Settings")]
    [Tooltip("Eキーでインタラクトした際に再生する効果音。")]
    [SerializeField] private AudioClip interactSfx;
    [Range(0f, 1f)]
    [SerializeField] private float interactSfxVolume = 1f;
    [Tooltip("同じオブジェクトを連打した際のクールダウン時間(秒)。0 で無効。")]
    [SerializeField] private float interactSfxCooldown = 0.1f;

    private AudioSource audioSource;
    private float nextPlayableSfxTime;

    private bool isLooping;

    /// <summary>
    /// 現在ループ状態かどうかを取得します。
    /// </summary>
    public bool IsLooping => isLooping;

    private void Reset()
    {
        TryAssignAnimator();
    }

    private void Awake()
    {
        TryAssignAnimator(); // 既存：子から Animator を拾う:contentReference[oaicite:1]{index=1}

        if (targetAnimator != null)
        {
            // ここを追加：ゲームの timeScale に従う
            targetAnimator.updateMode = AnimatorUpdateMode.Normal;

            // 念のためデフォルト速度に戻す（他所で speed を弄っているプロジェクト対策）
            targetAnimator.speed = 1f;
        }

        // 既存ロジック：初期ループ状態の反映など
        isLooping = startLoopActive;                        //:contentReference[oaicite:2]{index=2}
        if (interactionMode == InteractionAnimationMode.ToggleLoop)
        {
            ApplyLoopState();                               //:contentReference[oaicite:3]{index=3}
        }
    }

    private void OnEnable()
    {
        SetupAudioSource();
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
    }

    private void OnDisable()
    {
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
    }

    private void TryAssignAnimator()
    {
        if (targetAnimator == null)
        {
            targetAnimator = GetComponentInChildren<Animator>();
        }
    }

    public void Interact()
    {
        if (targetAnimator == null)
        {
            Debug.LogWarning($"[{nameof(FurnitureAnimationInteractable)}] Animator が見つかりません。", this);
            return;
        }

        PlayInteractSfx();

        switch (interactionMode)
        {
            case InteractionAnimationMode.PlayOnce:
                HandlePlayOnce();
                break;
            case InteractionAnimationMode.ToggleLoop:
                ToggleLoop();
                break;
            default:
                Debug.LogWarning($"[{nameof(FurnitureAnimationInteractable)}] 未対応のインタラクションモードです: {interactionMode}");
                break;
        }
    }

    private void HandlePlayOnce()
    {
        switch (playOnceMethod)
        {
            case PlayOnceMethod.Trigger:
                if (!string.IsNullOrEmpty(playOnceTriggerName))
                {
                    targetAnimator.ResetTrigger(playOnceTriggerName);
                    targetAnimator.SetTrigger(playOnceTriggerName);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(FurnitureAnimationInteractable)}] Trigger名が設定されていません。", this);
                }
                break;
            case PlayOnceMethod.PlayState:
                if (!string.IsNullOrEmpty(playOnceStateName))
                {
                    targetAnimator.Play(playOnceStateName, playOnceLayerIndex, playOnceStateNormalizedTime);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(FurnitureAnimationInteractable)}] State名が設定されていません。", this);
                }
                break;
            default:
                Debug.LogWarning($"[{nameof(FurnitureAnimationInteractable)}] 未対応のPlayOnce方式です: {playOnceMethod}");
                break;
        }
    }

    private void ToggleLoop()
    {
        isLooping = !isLooping;
        ApplyLoopState();
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void HandleSfxVolumeChanged(float volume)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.volume = Mathf.Clamp01(volume);
    }

    private void PlayInteractSfx()
    {
        if (interactSfx == null || audioSource == null)
        {
            return;
        }

        if (interactSfxCooldown > 0f && Time.time < nextPlayableSfxTime)
        {
            return;
        }

        float volume = interactSfxVolume * AudioManager.CurrentSfxVolume;
        if (volume <= 0f)
        {
            return;
        }

        audioSource.PlayOneShot(interactSfx, volume);
        nextPlayableSfxTime = Time.time + Mathf.Max(0f, interactSfxCooldown);
    }

    /// <summary>
    /// 外部からループ状態を設定する場合に使用します。
    /// </summary>
    public void SetLooping(bool looping)
    {
        if (isLooping == looping)
            return;

        isLooping = looping;
        ApplyLoopState();
    }

    private void ApplyLoopState()
    {
        if (targetAnimator == null)
            return;

        if (!string.IsNullOrEmpty(loopBoolParameter))
        {
            targetAnimator.SetBool(loopBoolParameter, isLooping);
        }

        if (isLooping)
        {
            SendTriggerIfNeeded(loopStartTrigger);
        }
        else
        {
            SendTriggerIfNeeded(loopStopTrigger);
        }
    }

    private void SendTriggerIfNeeded(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
            return;

        targetAnimator.ResetTrigger(triggerName);
        targetAnimator.SetTrigger(triggerName);
    }
}

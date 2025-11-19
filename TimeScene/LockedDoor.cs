using UnityEngine;

/// <summary>
/// シーン遷移を行わず、ロック音のみを再生するドアコンポーネント。
/// SceneDoor と同様に AudioManager の音量通知に追従します。
/// </summary>
public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip lockedSfx;
    [Range(0f, 1f)]
    [SerializeField] private float lockedSfxVolume = 1f;
    [Tooltip("同じロック音を連打した際のクールダウン時間 (秒)。0 で無効。")]
    [SerializeField] private float sfxCooldown = 0.4f;

    private AudioSource audioSource;
    private float nextPlayableTime;

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

    public void Interact()
    {
        Debug.Log("ロックされたドア: Eキーを受け付けました。");
        TryPlayLockedSfx();
    }

    private void TryPlayLockedSfx()
    {
        if (lockedSfx == null || audioSource == null)
        {
            Debug.LogWarning("ロック音の設定が不足しています。");
            return;
        }

        if (sfxCooldown > 0f && Time.time < nextPlayableTime)
        {
            Debug.Log("ロック音はクールダウン中のため再生されませんでした。");
            return;
        }

        audioSource.PlayOneShot(lockedSfx, lockedSfxVolume * AudioManager.CurrentSfxVolume);
        Debug.Log("ロック音を再生しました。");
        nextPlayableTime = Time.time + Mathf.Max(0f, sfxCooldown);
    }

}

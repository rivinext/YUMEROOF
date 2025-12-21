using UnityEngine;

/// <summary>
/// シーン遷移などは行わず、インタラクトで効果音のみを再生するコンポーネント。
/// AudioManager の SFX 音量変更通知に追従し、クールダウン制御も行います。
/// </summary>
public class SfxOnlyInteractable : MonoBehaviour, IInteractable
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
        TryPlayLockedSfx();
    }

    private void TryPlayLockedSfx()
    {
        if (lockedSfx == null || audioSource == null)
        {
            return;
        }

        if (sfxCooldown > 0f && Time.time < nextPlayableTime)
        {
            return;
        }

        audioSource.PlayOneShot(lockedSfx, lockedSfxVolume * AudioManager.CurrentSfxVolume);
        nextPlayableTime = Time.time + Mathf.Max(0f, sfxCooldown);
    }
}

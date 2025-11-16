using UnityEngine;

public class InventoryCardAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private InventoryCardAudioProfile audioProfile;

    private float lastPlaybackTime = float.NegativeInfinity;
    private float currentSfxVolume = 1f;
    private bool isSubscribedToVolumeEvent;

    public InventoryCardAudioProfile AudioProfile
    {
        get => audioProfile;
        set
        {
            audioProfile = value;
            ApplyAudioProfileSettings();
        }
    }

    public AudioSource AudioSource => audioSource;

    void Awake()
    {
        EnsureAudioSource();
        ApplyAudioProfileSettings();
    }

    void OnEnable()
    {
        SubscribeToVolumeChanges();
    }

    void OnDisable()
    {
        UnsubscribeFromVolumeChanges();
    }

    void OnDestroy()
    {
        UnsubscribeFromVolumeChanges();
    }

    public void PlayHover()
    {
        TryPlayClip(audioProfile != null ? audioProfile.hoverClip : null);
    }

    public void PlayClick()
    {
        TryPlayClip(audioProfile != null ? audioProfile.clickClip : null);
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void ApplyAudioProfileSettings()
    {
        if (audioSource == null || audioProfile == null)
        {
            return;
        }

        if (audioProfile.outputMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = audioProfile.outputMixerGroup;
        }

        audioSource.rolloffMode = audioProfile.rolloffMode;
    }

    private void SubscribeToVolumeChanges()
    {
        if (isSubscribedToVolumeEvent)
        {
            return;
        }

        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
        isSubscribedToVolumeEvent = true;
    }

    private void UnsubscribeFromVolumeChanges()
    {
        if (!isSubscribedToVolumeEvent)
        {
            return;
        }

        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        isSubscribedToVolumeEvent = false;
    }

    private void HandleSfxVolumeChanged(float value)
    {
        currentSfxVolume = Mathf.Clamp01(value);
    }

    private void TryPlayClip(AudioClip clip)
    {
        if (audioSource == null || audioProfile == null || clip == null)
        {
            return;
        }

        float cooldown = Mathf.Max(0f, audioProfile.cooldownSeconds);
        if (Time.unscaledTime - lastPlaybackTime < cooldown)
        {
            return;
        }

        lastPlaybackTime = Time.unscaledTime;

        float pitchOffset = Mathf.Max(0f, audioProfile.pitchRandomization);
        float pitch = 1f;
        if (pitchOffset > 0f)
        {
            pitch = Random.Range(1f - pitchOffset, 1f + pitchOffset);
        }

        PlayClipInternal(clip, Mathf.Max(0f, audioProfile.baseVolume) * currentSfxVolume, pitch);
    }

    protected virtual void PlayClipInternal(AudioClip clip, float volume, float pitch)
    {
        if (audioSource == null)
        {
            return;
        }

        float originalPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
        audioSource.pitch = originalPitch;
    }
}

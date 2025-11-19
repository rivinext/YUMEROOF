using UnityEngine;

public class SceneDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public string targetSceneName;      // 遷移先シーン名
    public string spawnPointName;       // 遷移先のスポーンポイント名

    [Header("Audio Settings")]
    [SerializeField] private AudioClip transitionSfx;
    [Range(0f, 1f)]
    [SerializeField] private float transitionSfxVolume = 1f;

    private AudioSource audioSource;

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
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void Interact()
    {
        if (!SceneTransitionManager.Instance.IsTransitioning)
        {
            if (transitionSfx != null && audioSource != null)
            {
                audioSource.PlayOneShot(transitionSfx, transitionSfxVolume * AudioManager.CurrentSfxVolume);
            }

            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, spawnPointName, true);
        }
    }
}

using System.Collections;
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
    [Tooltip("シーン遷移前に効果音を再生して待機する時間（秒）。0 で即時遷移。")]
    [SerializeField] private float transitionLeadTime = 0.05f;

    private AudioSource audioSource;
    private Coroutine transitionCoroutine;
    private Collider interactionCollider;

    private void OnEnable()
    {
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
        SetupAudioSource();
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
    }

    private void OnDisable()
    {
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }
#endif

    public void Interact()
    {
        if (!SceneTransitionManager.Instance.IsTransitioning)
        {
            if (transitionCoroutine == null)
            {
                transitionCoroutine = StartCoroutine(TransitionRoutine());
            }
        }
    }

    private IEnumerator TransitionRoutine()
    {
        yield return PlayTransitionSfx();

        SceneTransitionManager.Instance.TransitionToScene(
            targetSceneName,
            spawnPointName);

        transitionCoroutine = null;
    }

    private IEnumerator PlayTransitionSfx()
    {
        if (transitionSfx == null || audioSource == null)
        {
            yield break;
        }

        audioSource.PlayOneShot(transitionSfx, transitionSfxVolume * AudioManager.CurrentSfxVolume);

        if (transitionLeadTime > 0f)
        {
            yield return new WaitForSeconds(transitionLeadTime);
        }
        else
        {
            yield return null;
        }
    }
}

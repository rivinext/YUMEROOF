using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンごとの BGM AudioSource を切り替える常駐マネージャー。
/// </summary>
public class BgmRouter : MonoBehaviour
{
    [System.Serializable]
    private struct SceneBinding
    {
        [Tooltip("対象シーン名 (SceneA など)")]
        public string sceneName;

        [Tooltip("該当シーンで使用する AudioSource")]
        public AudioSource audioSource;
    }

    public static BgmRouter Instance { get; private set; }

    [SerializeField, Tooltip("フェード時間(秒)")]
    private float fadeSeconds = 0.75f;

    [SerializeField]
    private SceneBinding[] bindings;

    private readonly Dictionary<string, AudioSource> sceneToSource = new Dictionary<string, AudioSource>();
    private readonly Dictionary<AudioSource, float> sourceVolumes = new Dictionary<AudioSource, float>();
    private AudioSource currentSource;
    private Coroutine transitionRoutine;
    private float volumeMultiplier = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sceneToSource.Clear();
        foreach (var entry in bindings)
        {
            if (entry.audioSource == null || string.IsNullOrEmpty(entry.sceneName))
            {
                continue;
            }

            if (!sceneToSource.ContainsKey(entry.sceneName))
            {
                sceneToSource.Add(entry.sceneName, entry.audioSource);
            }

            if (!sourceVolumes.ContainsKey(entry.audioSource))
            {
                sourceVolumes.Add(entry.audioSource, entry.audioSource.volume);
            }

            entry.audioSource.playOnAwake = false;
            entry.audioSource.loop = true;
            entry.audioSource.transform.SetParent(transform, worldPositionStays: false);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        AudioVolumeManager.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        HandleBgmVolumeChanged(AudioVolumeManager.BgmVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            AudioVolumeManager.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        }
    }

    private void Start()
    {
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!sceneToSource.TryGetValue(scene.name, out var next))
        {
            if (currentSource != null)
            {
                if (transitionRoutine != null)
                {
                    StopCoroutine(transitionRoutine);
                }

                transitionRoutine = StartCoroutine(FadeOutAndStopCurrent());
            }

            return;
        }

        if (next == currentSource)
        {
            if (!currentSource.isPlaying)
            {
                currentSource.UnPause();
            }

            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(SwitchSource(next));
    }

    private IEnumerator SwitchSource(AudioSource next)
    {
        var previous = currentSource;
        currentSource = next;

        if (previous != null && previous.isPlaying)
        {
            yield return FadeVolume(previous, 0f);
            previous.Stop();
            RestoreVolume(previous);
        }

        float targetVolume = GetTargetVolume(next);

        if (!next.isPlaying)
        {
            next.Play();
        }

        next.volume = 0f;
        yield return FadeVolume(next, targetVolume);

        transitionRoutine = null;
    }

    private IEnumerator FadeOutAndStopCurrent()
    {
        var source = currentSource;
        currentSource = null;

        if (source != null && source.isPlaying)
        {
            yield return FadeVolume(source, 0f);
            source.Stop();
            RestoreVolume(source);
        }

        transitionRoutine = null;
    }

    private IEnumerator FadeVolume(AudioSource source, float target)
    {
        float start = source.volume;
        float elapsed = 0f;
        float duration = Mathf.Max(fadeSeconds, 0.001f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float dynamicTarget = target <= 0f ? 0f : GetTargetVolume(source);
            source.volume = Mathf.Lerp(start, dynamicTarget, t);
            yield return null;
        }

        source.volume = target <= 0f ? 0f : GetTargetVolume(source);
    }

    private float GetTargetVolume(AudioSource source)
    {
        if (source == null)
        {
            return 0f;
        }

        if (sourceVolumes.TryGetValue(source, out var volume))
        {
            return volume * volumeMultiplier;
        }

        return source.volume * volumeMultiplier;
    }

    private void RestoreVolume(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        float target = GetTargetVolume(source);
        source.volume = target;
    }

    private void StopCurrent()
    {
        if (currentSource == null)
        {
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        currentSource.Stop();
        RestoreVolume(currentSource);
    }

    public void PauseCurrent(bool pause)
    {
        if (currentSource == null)
        {
            return;
        }

        if (pause)
        {
            currentSource.Pause();
        }
        else
        {
            currentSource.UnPause();
        }
    }

    private void HandleBgmVolumeChanged(float value)
    {
        volumeMultiplier = Mathf.Clamp01(value);

        foreach (var source in sceneToSource.Values)
        {
            if (source == null)
            {
                continue;
            }

            if (source == currentSource || !source.isPlaying)
            {
                source.volume = GetTargetVolume(source);
            }
        }
    }
}

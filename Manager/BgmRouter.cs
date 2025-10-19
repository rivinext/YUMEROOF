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
    private readonly Dictionary<AudioSource, float> sourceBaseVolumes = new Dictionary<AudioSource, float>();
    private AudioSource currentSource;
    private Coroutine transitionRoutine;

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

            if (!sourceBaseVolumes.ContainsKey(entry.audioSource))
            {
                sourceBaseVolumes.Add(entry.audioSource, Mathf.Max(entry.audioSource.volume, 0f));
            }

            entry.audioSource.playOnAwake = false;
            entry.audioSource.loop = true;
            entry.audioSource.transform.SetParent(transform, worldPositionStays: false);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
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
                StopCurrent();
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
            ResetVolume(previous);
        }

        if (!next.isPlaying)
        {
            next.volume = 0f;
            next.Play();
        }

        float targetVolume = GetTargetVolume(next);
        yield return FadeVolume(next, targetVolume);

        transitionRoutine = null;
    }

    private IEnumerator FadeOutAndStop(AudioSource source)
    {
        if (source == null)
        {
            yield break;
        }

        yield return FadeVolume(source, 0f);
        source.Stop();
        ResetVolume(source);

        if (currentSource == source)
        {
            currentSource = null;
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
            source.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }

        source.volume = target;
    }

    private float GetTargetVolume(AudioSource source)
    {
        if (source == null)
        {
            return 0f;
        }

        if (sourceBaseVolumes.TryGetValue(source, out float stored))
        {
            return stored;
        }

        return Mathf.Max(source.volume, 0f);
    }

    private void ResetVolume(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (sourceBaseVolumes.TryGetValue(source, out float stored))
        {
            source.volume = stored;
        }
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
        }

        transitionRoutine = StartCoroutine(FadeOutAndStop(currentSource));
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
}

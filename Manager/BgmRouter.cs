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
        }

        if (!next.isPlaying)
        {
            next.Play();
        }

        next.volume = Mathf.Max(next.volume, 0.0001f);
        yield return FadeVolume(next, 1f);

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

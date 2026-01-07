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
    private class SceneBinding
    {
        [Tooltip("対象シーン名 (SceneA など)")]
        public string sceneName;

        [Tooltip("BGM 継続判定用のグループ ID (同一 ID の場合は継続)")]
        public string bgmGroupId;

        [Tooltip("該当シーンで使用する AudioSource(単体用)")]
        public AudioSource audioSource;

        [Tooltip("該当シーンで使用する AudioSource(複数候補)")]
        public AudioSource[] audioSources;

        [Tooltip("固定で再生する AudioClip (SceneA 用)")]
        public AudioClip singleClip;

        [Tooltip("ランダム再生する AudioClip 配列 (SceneB/C/D 用)")]
        public AudioClip[] clips;

        [Tooltip("複数クリップをランダム再生する場合は true")]
        public bool useMultipleClips;
    }

    public static BgmRouter Instance { get; private set; }

    [SerializeField, Tooltip("フェード時間(秒)")]
    private float fadeSeconds = 0.75f;

    [SerializeField]
    private SceneBinding[] bindings;

    private readonly Dictionary<string, SceneBinding> sceneBindings = new Dictionary<string, SceneBinding>();
    private readonly Dictionary<AudioSource, float> sourceVolumes = new Dictionary<AudioSource, float>();
    private AudioSource currentSource;
    private Coroutine transitionRoutine;
    private Coroutine playlistRoutine;
    private float volumeMultiplier = 1f;
    private string currentSceneName;
    private SceneBinding currentBinding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sceneBindings.Clear();
        foreach (var entry in bindings)
        {
            if (string.IsNullOrEmpty(entry.sceneName))
            {
                continue;
            }

            if (!sceneBindings.ContainsKey(entry.sceneName))
            {
                sceneBindings.Add(entry.sceneName, entry);
            }

            foreach (var source in EnumerateSources(entry))
            {
                if (source == null)
                {
                    continue;
                }

                if (!sourceVolumes.ContainsKey(source))
                {
                    sourceVolumes.Add(source, source.volume);
                }

                source.playOnAwake = false;
                source.loop = false;
                source.transform.SetParent(transform, worldPositionStays: false);
            }
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        AudioManager.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        HandleBgmVolumeChanged(AudioManager.CurrentBgmVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            AudioManager.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        }
    }

    private void Start()
    {
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!sceneBindings.TryGetValue(scene.name, out var next))
        {
            if (currentSource != null)
            {
                if (transitionRoutine != null)
                {
                    StopCoroutine(transitionRoutine);
                }

                StopPlaylist();
                transitionRoutine = StartCoroutine(FadeOutAndStopCurrent());
            }

            return;
        }

        if (ShouldContinueBgm(next))
        {
            currentSceneName = next.sceneName;
            currentBinding = next;
            if (currentSource != null && currentSource.isPlaying)
            {
                return;
            }

            StartBindingPlayback(next);
            return;
        }

        if (currentSceneName == next.sceneName)
        {
            if (currentSource != null && currentSource.isPlaying)
            {
                return;
            }

            StartBindingPlayback(next);
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(SwitchBinding(next));
    }

    private IEnumerator SwitchBinding(SceneBinding nextBinding)
    {
        var previous = currentSource;
        currentSource = null;
        currentSceneName = nextBinding.sceneName;
        currentBinding = nextBinding;

        StopPlaylist();

        if (previous != null && previous.isPlaying)
        {
            yield return FadeVolume(previous, 0f);
            previous.Stop();
            RestoreVolume(previous);
        }

        yield return StartBindingPlaybackRoutine(nextBinding);
        transitionRoutine = null;
    }

    private void StartBindingPlayback(SceneBinding binding)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        transitionRoutine = StartCoroutine(StartBindingPlaybackRoutine(binding));
    }

    private IEnumerator StartBindingPlaybackRoutine(SceneBinding binding)
    {
        currentSceneName = binding.sceneName;
        currentBinding = binding;
        if (ShouldUseMultipleClips(binding))
        {
            StopPlaylist();
            playlistRoutine = StartCoroutine(PlayRandomClips(binding));
            yield break;
        }

        var source = GetPrimarySource(binding);
        if (source == null)
        {
            yield break;
        }

        currentSource = source;
        ConfigureSourceForSingle(binding, source);

        yield return PlayWithFade(source);
    }

    private IEnumerator FadeOutAndStopCurrent()
    {
        var source = currentSource;
        currentSource = null;
        currentSceneName = null;
        currentBinding = null;
        StopPlaylist();

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

    private IEnumerator PlayWithFade(AudioSource source)
    {
        if (source == null)
        {
            yield break;
        }

        source.volume = 0f;
        source.Play();
        yield return FadeVolume(source, GetTargetVolume(source));
    }

    private IEnumerator PlayRandomClips(SceneBinding binding)
    {
        while (IsSameBgmGroup(binding, currentBinding))
        {
            var source = ChooseRandomSource(binding);
            var clip = ChooseRandomClip(binding);

            if (source == null || clip == null)
            {
                yield break;
            }

            currentSource = source;
            ConfigureSourceForPlaylist(source);
            source.clip = clip;
            source.volume = 0f;
            source.Play();
            yield return FadeVolume(source, GetTargetVolume(source));

            float clipLength = Mathf.Max(clip.length, 0.01f);
            float fadeOutStart = Mathf.Max(clipLength - fadeSeconds, 0f);
            float elapsed = 0f;

            while (IsSameBgmGroup(binding, currentBinding) && source.isPlaying && elapsed < fadeOutStart)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!IsSameBgmGroup(binding, currentBinding))
            {
                yield break;
            }

            if (source.isPlaying)
            {
                yield return FadeVolume(source, 0f);
                source.Stop();
                RestoreVolume(source);
            }

            source.volume = 0f;
        }
    }

    private bool ShouldUseMultipleClips(SceneBinding binding)
    {
        return binding.useMultipleClips && binding.clips != null && binding.clips.Length > 0;
    }

    private bool ShouldContinueBgm(SceneBinding nextBinding)
    {
        if (currentBinding == null || nextBinding == null)
        {
            return false;
        }

        return IsSameBgmGroup(currentBinding, nextBinding);
    }

    private bool IsSameBgmGroup(SceneBinding left, SceneBinding right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        bool hasLeftGroup = !string.IsNullOrEmpty(left.bgmGroupId);
        bool hasRightGroup = !string.IsNullOrEmpty(right.bgmGroupId);

        if (hasLeftGroup || hasRightGroup)
        {
            return hasLeftGroup && hasRightGroup && left.bgmGroupId == right.bgmGroupId;
        }

        return left.sceneName == right.sceneName;
    }

    private void ConfigureSourceForSingle(SceneBinding binding, AudioSource source)
    {
        source.loop = true;
        if (binding.singleClip != null)
        {
            source.clip = binding.singleClip;
        }
    }

    private void ConfigureSourceForPlaylist(AudioSource source)
    {
        source.loop = false;
    }

    private AudioSource GetPrimarySource(SceneBinding binding)
    {
        foreach (var source in EnumerateSources(binding))
        {
            if (source != null)
            {
                return source;
            }
        }

        return null;
    }

    private AudioSource ChooseRandomSource(SceneBinding binding)
    {
        var sources = new List<AudioSource>();
        foreach (var source in EnumerateSources(binding))
        {
            if (source != null)
            {
                sources.Add(source);
            }
        }

        if (sources.Count == 0)
        {
            return null;
        }

        return sources[Random.Range(0, sources.Count)];
    }

    private AudioClip ChooseRandomClip(SceneBinding binding)
    {
        if (binding.clips == null || binding.clips.Length == 0)
        {
            return null;
        }

        return binding.clips[Random.Range(0, binding.clips.Length)];
    }

    private IEnumerable<AudioSource> EnumerateSources(SceneBinding binding)
    {
        if (binding.audioSources != null && binding.audioSources.Length > 0)
        {
            for (int i = 0; i < binding.audioSources.Length; i++)
            {
                yield return binding.audioSources[i];
            }
        }

        if (binding.audioSource != null)
        {
            yield return binding.audioSource;
        }
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

        StopPlaylist();
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

        foreach (var source in sourceVolumes.Keys)
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

    private void StopPlaylist()
    {
        if (playlistRoutine != null)
        {
            StopCoroutine(playlistRoutine);
            playlistRoutine = null;
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// ゲーム全体のBGMおよびSFX音量を管理する永続オブジェクト。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public static event Action<float> OnBgmVolumeChanged;
    public static event Action<float> OnSfxVolumeChanged;

    private const float DefaultBgmVolume = 0.3f;
    private const float DefaultSfxVolume = 0.7f;

    [SerializeField, Range(0f, 1f)] private float initialBgmVolume = DefaultBgmVolume;
    [SerializeField, Range(0f, 1f)] private float initialSfxVolume = DefaultSfxVolume;

    private float bgmVolume = DefaultBgmVolume;
    private float sfxVolume = DefaultSfxVolume;

    public static float CurrentBgmVolume => Instance != null ? Instance.bgmVolume : DefaultBgmVolume;
    public static float CurrentSfxVolume => Instance != null ? Instance.sfxVolume : DefaultSfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmVolume = Mathf.Clamp01(initialBgmVolume);
        sfxVolume = Mathf.Clamp01(initialSfxVolume);

        ApplyVolumesToListeners();
    }

    /// <summary>
    /// 現在のBGM音量を設定する。
    /// </summary>
    public static void SetBgmVolume(float value)
    {
        if (Instance == null)
        {
            Debug.LogWarning("AudioManager instance is not ready yet.");
            return;
        }

        Instance.SetBgmVolumeInternal(value);
    }

    /// <summary>
    /// 現在のSFX音量を設定する。
    /// </summary>
    public static void SetSfxVolume(float value)
    {
        if (Instance == null)
        {
            Debug.LogWarning("AudioManager instance is not ready yet.");
            return;
        }

        Instance.SetSfxVolumeInternal(value);
    }

    /// <summary>
    /// 登録済みのリスナーに現在のBGM/SFX音量を即時通知する。
    /// </summary>
    public void ApplyVolumesToListeners()
    {
        OnBgmVolumeChanged?.Invoke(CurrentBgmVolume);
        OnSfxVolumeChanged?.Invoke(CurrentSfxVolume);
    }

    private void SetBgmVolumeInternal(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(clamped, bgmVolume))
        {
            return;
        }

        bgmVolume = clamped;
        OnBgmVolumeChanged?.Invoke(clamped);
    }

    private void SetSfxVolumeInternal(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(clamped, sfxVolume))
        {
            return;
        }

        sfxVolume = clamped;
        OnSfxVolumeChanged?.Invoke(clamped);
    }
}

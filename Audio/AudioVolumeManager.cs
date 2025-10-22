using System;
using UnityEngine;

/// <summary>
/// ゲーム全体のBGMおよびSFX音量を管理する静的マネージャー。
/// </summary>
public static class AudioVolumeManager
{
    public static event Action<float> OnBgmVolumeChanged;
    public static event Action<float> OnSfxVolumeChanged;

    private static float bgmVolume = 1f;
    private static float sfxVolume = 1f;

    /// <summary>
    /// 0-1の正規化値で表現されたBGM音量。
    /// </summary>
    public static float BgmVolume
    {
        get => bgmVolume;
        set => SetBgmVolume(value);
    }

    /// <summary>
    /// 0-1の正規化値で表現されたSFX音量。
    /// </summary>
    public static float SfxVolume
    {
        get => sfxVolume;
        set => SetSfxVolume(value);
    }

    public static void SetBgmVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(clamped, bgmVolume))
        {
            return;
        }

        bgmVolume = clamped;
        OnBgmVolumeChanged?.Invoke(clamped);
    }

    public static void SetSfxVolume(float value)
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

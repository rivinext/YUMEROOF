using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class UIButtonSoundBinderPersistent : MonoBehaviour
{
    public static UIButtonSoundBinderPersistent Instance { get; private set; }

    [Header("Assign UI Sound Clips")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Range(0f, 1f)] public float hoverVolume = 0.6f;
    [Range(0f, 1f)] public float clickVolume = 0.8f;

    [Header("Anti-Spam")]
    [Tooltip("連続でUIを跨いだ際の鳴りすぎ防止")]
    public float hoverCooldown = 0.06f;

    AudioSource sfxSource;
    float lastHoverTime;

    // すでにバインドしたボタンを記録（重複登録を防ぐ）
    readonly HashSet<int> bound = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = GetComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f; // 2D再生
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 次フレームで実行（UI生成完了を待つ）
        StartCoroutine(BindAllButtonsNextFrame());
    }

    System.Collections.IEnumerator BindAllButtonsNextFrame()
    {
        yield return null; // 1フレーム待つ
        RebindAll();       // 新シーンの全ボタンへ割り当て
    }

    /// <summary>
    /// シーン内（Activeの全て）のButtonにホバー＆クリック音を割り当て
    /// ランタイムでボタン生成した直後にも呼べます
    /// </summary>
    public void RebindAll()
    {
        var buttons = FindObjectsOfType<Button>(true);
        foreach (var btn in buttons)
        {
            int id = btn.GetInstanceID();
            if (bound.Contains(id)) continue;

            AddHoverSound(btn);  // マウスホバー
            AddSelectSound(btn); // キー/パッドでのフォーカス
            AddClickSound(btn);  // クリック/決定

            bound.Add(id);
        }
    }

    void AddHoverSound(Button btn)
    {
        var trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ =>
        {
            if (!btn.interactable) return;
            if (hoverClip == null || sfxSource == null) return;
            if (Time.unscaledTime - lastHoverTime < hoverCooldown) return;

            sfxSource.PlayOneShot(hoverClip, hoverVolume);
            lastHoverTime = Time.unscaledTime;
        });
        trigger.triggers.Add(entry);
    }

    void AddSelectSound(Button btn)
    {
        // キーボード/ゲームパッドでフォーカスが当たったとき
        var trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        entry.callback.AddListener(_ =>
        {
            if (!btn.interactable) return;
            if (hoverClip == null || sfxSource == null) return;
            if (Time.unscaledTime - lastHoverTime < hoverCooldown) return;

            sfxSource.PlayOneShot(hoverClip, hoverVolume);
            lastHoverTime = Time.unscaledTime;
        });
        trigger.triggers.Add(entry);
    }

    void AddClickSound(Button btn)
    {
        btn.onClick.AddListener(() =>
        {
            if (clickClip == null || sfxSource == null) return;
            // 無効ボタンはonClick自体が呼ばれないが、念のためチェック
            if (!btn.interactable) return;

            sfxSource.PlayOneShot(clickClip, clickVolume);
        });
    }
}

using UnityEngine;

/// <summary>
/// Animatorを使った最小構成のまばたき制御。
/// </summary>
public class PlayerBlinkController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string blinkEnabledBoolName = "BlinkEnabled";
    [SerializeField] private string idleBoolName = "IsIdle";

    private int blinkEnabledBoolHash = -1;
    private int idleBoolHash = -1;

    private bool blinkingEnabled = true;
    private bool isIdle = false;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (!string.IsNullOrEmpty(blinkEnabledBoolName))
        {
            blinkEnabledBoolHash = Animator.StringToHash(blinkEnabledBoolName);
        }

        if (!string.IsNullOrEmpty(idleBoolName))
        {
            idleBoolHash = Animator.StringToHash(idleBoolName);
        }

        ApplyBlinkingEnabled();
        ApplyIdleState();
    }

    /// <summary>
    /// まばたきの有効/無効を切り替える。
    /// </summary>
    public void SetBlinkingEnabled(bool enabled)
    {
        if (blinkingEnabled == enabled)
        {
            return;
        }

        blinkingEnabled = enabled;
        ApplyBlinkingEnabled();

        if (!blinkingEnabled)
        {
            SetIdle(false);
        }
        else
        {
            ApplyIdleState();
        }
    }

    /// <summary>
    /// プレイヤーが操作されていない状態を通知。
    /// </summary>
    public void NotifyInactive(float deltaTime)
    {
        if (!blinkingEnabled)
        {
            return;
        }

        SetIdle(true);
    }

    /// <summary>
    /// プレイヤーが再び操作されたことを通知。
    /// </summary>
    public void NotifyActive()
    {
        if (!blinkingEnabled)
        {
            return;
        }

        SetIdle(false);
    }

    /// <summary>
    /// まばたき状態をリセット。
    /// </summary>
    public void ResetBlinkState()
    {
        if (!blinkingEnabled)
        {
            return;
        }

        SetIdle(false);
    }

    private void SetIdle(bool value)
    {
        if (isIdle == value)
        {
            return;
        }

        isIdle = value;
        ApplyIdleState();
    }

    private void ApplyBlinkingEnabled()
    {
        if (animator != null && blinkEnabledBoolHash != -1)
        {
            animator.SetBool(blinkEnabledBoolHash, blinkingEnabled);
        }
    }

    private void ApplyIdleState()
    {
        if (animator != null && idleBoolHash != -1)
        {
            animator.SetBool(idleBoolHash, blinkingEnabled && isIdle);
        }
    }
}

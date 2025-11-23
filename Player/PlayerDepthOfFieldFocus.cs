using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// グローバルボリューム上のDepth of Fieldをプレイヤーに合わせて制御するヘルパー。
/// </summary>
[DisallowMultipleComponent]
public class PlayerDepthOfFieldFocus : MonoBehaviour
{
    [Header("References")]
    [Tooltip("フォーカス距離を調整する対象のVolume。未指定の場合は自身にアタッチされたVolumeを利用します。")]
    [SerializeField] private Volume volume;

    [Tooltip("フォーカスしたいターゲット。通常はプレイヤーのTransformを指定します。")]
    [SerializeField] private Transform focusTarget;

    [Tooltip("フォーカス距離を計測するカメラ。未指定の場合はMain Cameraが使用されます。")]
    [SerializeField] private Camera focusCamera;

    [Header("Focus Settings")]
    [Tooltip("フォーカス距離の追従スピード。値を大きくするほど素早くターゲットに追従します。")]
    [SerializeField, Min(0f)] private float focusLerpSpeed = 10f;

    [Tooltip("計算されたフォーカス距離の下限。")]
    [SerializeField, Min(0.01f)] private float minFocusDistance = 0.1f;

    [Tooltip("計算されたフォーカス距離の上限。")]
    [SerializeField, Min(0.01f)] private float maxFocusDistance = 25f;

    private DepthOfField depthOfField;
    private float currentFocusDistance;

    private void Awake()
    {
        if (volume == null)
        {
            volume = GetComponent<Volume>();
        }

        if (focusCamera == null)
        {
            focusCamera = Camera.main;
        }

        TryCacheDepthOfField();
    }

    private void OnEnable()
    {
        TryCacheDepthOfField();
    }

    private void Update()
    {
        if (depthOfField == null || focusTarget == null || focusCamera == null)
        {
            return;
        }

        var cameraTransform = focusCamera.transform;
        var toTarget = focusTarget.position - cameraTransform.position;

        // カメラ前方方向に投影した距離をフォーカス距離として利用する。
        var forwardDistance = Mathf.Max(Vector3.Dot(toTarget, cameraTransform.forward), 0f);
        var targetDistance = Mathf.Clamp(forwardDistance, minFocusDistance, maxFocusDistance);

        currentFocusDistance = Mathf.Lerp(currentFocusDistance <= 0f ? targetDistance : currentFocusDistance,
            targetDistance, 1f - Mathf.Exp(-focusLerpSpeed * Time.deltaTime));

        depthOfField.active = true;
        depthOfField.focusDistance.value = currentFocusDistance;
    }

    /// <summary>
    /// フォーカスターゲットを動的に差し替えたい場合に利用します。
    /// </summary>
    public void SetFocusTarget(Transform target)
    {
        focusTarget = target;
    }

    /// <summary>
    /// 対象VolumeからDepth of Fieldコンポーネントをキャッシュする。
    /// </summary>
    private void TryCacheDepthOfField()
    {
        if (volume == null || volume.profile == null)
        {
            depthOfField = null;
            return;
        }

        if (!volume.profile.TryGet(out depthOfField))
        {
            // プロファイルにDepth of Fieldが存在しない場合は追加してアクティブにする。
            depthOfField = volume.profile.Add<DepthOfField>();
        }

        if (depthOfField != null)
        {
            depthOfField.active = true;
            currentFocusDistance = depthOfField.focusDistance.value;
        }
    }
}

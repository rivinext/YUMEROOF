using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 家具などのアイテムに「壁掛け用アイテム」を設置するための補助スクリプト。
/// 設置可能な位置には <see cref="AnchorPoint"/> を子オブジェクトとして配置しておきます。
/// </summary>
public class WallAttachmentPlacer : MonoBehaviour
{
    [Header("壁掛けアンカー")]
    [Tooltip("壁用アイテムを取り付けられる AnchorPoint の一覧。未設定なら子階層から自動取得します。")]
    [SerializeField] private AnchorPoint[] wallAnchors = System.Array.Empty<AnchorPoint>();

    [Tooltip("プレハブ内で壁に合わせたいアンカーの名前。空欄なら最初に見つかった AnchorPoint を使用します。")]
    [SerializeField] private string itemAnchorName = "WallAnchor";

    /// <summary>
    /// 現在登録されている壁アンカーの読み取り専用リスト。
    /// </summary>
    public IReadOnlyList<AnchorPoint> WallAnchors => wallAnchors;

    private void Reset()
    {
        CacheAnchors();
    }

    private void Awake()
    {
        CacheAnchors();
    }

    /// <summary>
    /// 未設定の場合、子階層から AnchorPoint を拾っておく。
    /// </summary>
    private void CacheAnchors()
    {
        if (wallAnchors == null || wallAnchors.Length == 0)
        {
            wallAnchors = GetComponentsInChildren<AnchorPoint>(includeInactive: true);
        }
    }

    /// <summary>
    /// 最初に空いているアンカーに壁用プレハブを配置する。
    /// </summary>
    /// <param name="wallItemPrefab">配置したい壁用アイテムのプレハブ。</param>
    /// <param name="placedInstance">生成したインスタンス。</param>
    /// <returns>配置できた場合は true。</returns>
    public bool TryPlace(GameObject wallItemPrefab, out GameObject placedInstance)
    {
        AnchorPoint anchor = FindFirstAvailableAnchor();
        return TryPlaceAtAnchor(wallItemPrefab, anchor, out placedInstance);
    }

    /// <summary>
    /// 指定したアンカーに壁用アイテムを配置する。
    /// </summary>
    /// <param name="wallItemPrefab">配置したい壁用アイテムのプレハブ。</param>
    /// <param name="anchor">ターゲットのアンカー。</param>
    /// <param name="placedInstance">生成したインスタンス。</param>
    /// <param name="itemAnchorOverride">プレハブ内のアンカーを明示的に指定したい場合に渡す。</param>
    /// <returns>配置できた場合は true。</returns>
    public bool TryPlaceAtAnchor(GameObject wallItemPrefab, AnchorPoint anchor, out GameObject placedInstance, Transform itemAnchorOverride = null)
    {
        placedInstance = null;

        if (wallItemPrefab == null || anchor == null)
        {
            return false;
        }

        if (anchor.IsOccupied)
        {
            return false;
        }

        placedInstance = Instantiate(wallItemPrefab);

        Transform itemAnchor = itemAnchorOverride != null
            ? itemAnchorOverride
            : FindItemAnchor(placedInstance);

        AlignToAnchor(placedInstance, itemAnchor, anchor.transform);

        anchor.SetOccupied(true);

        var placedFurniture = placedInstance.GetComponent<PlacedFurniture>();
        if (placedFurniture != null)
        {
            placedFurniture.attachedAnchor = anchor;
        }

        return true;
    }

    /// <summary>
    /// アンカーの占有を解除する（設置物撤去時など）。
    /// </summary>
    public void ReleaseAnchor(AnchorPoint anchor)
    {
        if (anchor == null)
            return;

        anchor.SetOccupied(false);
    }

    private AnchorPoint FindFirstAvailableAnchor()
    {
        foreach (var anchor in wallAnchors)
        {
            if (anchor != null && !anchor.IsOccupied)
            {
                return anchor;
            }
        }

        return null;
    }

    private Transform FindItemAnchor(GameObject instance)
    {
        if (instance == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(itemAnchorName))
        {
            Transform namedAnchor = instance.transform.Find(itemAnchorName);
            if (namedAnchor != null)
            {
                return namedAnchor;
            }
        }

        AnchorPoint anchor = instance.GetComponentInChildren<AnchorPoint>();
        return anchor != null ? anchor.transform : null;
    }

    /// <summary>
    /// プレハブ内のアンカーを指定した壁アンカー位置に合わせ、AnchorManager で方向を揃える。
    /// </summary>
    private void AlignToAnchor(GameObject obj, Transform itemAnchor, Transform targetAnchor)
    {
        if (obj == null || targetAnchor == null)
        {
            return;
        }

        Quaternion targetRotation = targetAnchor.rotation;
        Vector3 wallNormal = -targetAnchor.forward;

        if (itemAnchor != null)
        {
            AnchorManager.AlignToWall(obj, itemAnchor, wallNormal, targetRotation);

            Vector3 offset = itemAnchor.position - obj.transform.position;
            obj.transform.position = targetAnchor.position - offset;
        }
        else
        {
            AnchorManager.AlignToWall(obj, null, wallNormal, targetRotation);
            obj.transform.position = targetAnchor.position;
        }
    }
}

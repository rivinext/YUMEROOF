using UnityEngine;
using System.Collections.Generic;

// 配置された家具コンポーネント
public class PlacedFurniture : MonoBehaviour
{
    public FurnitureData furnitureData;    // 家具データ
    public string itemID => furnitureData?.itemID; // アイテムIDへのアクセス
    public bool isSelected = false;        // 選択中か
    public bool isOnSurface = false;       // 他の家具の上に置かれているか
    public PlacedFurniture parentFurniture; // 親となる家具（上に置かれている場合）
    public List<PlacedFurniture> childFurnitures = new List<PlacedFurniture>(); // 上に置かれた家具
    public AnchorPoint attachedAnchor;      // 設置時に使用したアンカーポイント

    private Renderer[] renderers;
    private Collider[] colliders;
    private Material[] originalMaterials;

    // コーナーマーカー用
    public GameObject[] cornerMarkers;     // 四隅のマーカー
    public GameObject removeButton;        // 削除ボタンUI

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();

        // 元のマテリアルを保存
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterial;
        }
    }

    // 選択状態の切り替え
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // コーナーマーカーの表示/非表示
        if (cornerMarkers != null)
        {
            foreach (var marker in cornerMarkers)
            {
                if (marker != null)
                    marker.SetActive(selected);
            }
        }

        // 削除ボタンの表示/非表示
        if (removeButton != null)
            removeButton.SetActive(selected);
    }

    // 配置可能状態の表示
    public void SetPlacementValid(bool valid)
    {
        if (cornerMarkers != null)
        {
            Color markerColor = valid ? Color.white : Color.red;
            foreach (var marker in cornerMarkers)
            {
                if (marker != null)
                {
                    ApplyMarkerColor(marker.GetComponent<Renderer>(), markerColor);
                }
            }
        }
    }

    private void ApplyMarkerColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        // renderer.material は共有マテリアルではなく、このレンダラー用にインスタンス化されたマテリアルを返す
        // （コーナーマーカーのカラー変更が他オブジェクトに波及しないようにするため）。
        var material = renderer.material;
        material.color = color;
    }

    // 親家具に追加
    public void SetParentFurniture(PlacedFurniture parent)
    {
        if (parentFurniture != null)
        {
            parentFurniture.childFurnitures.Remove(this);
        }

        parentFurniture = parent;
        isOnSurface = (parent != null);

        if (parent != null)
        {
            parent.childFurnitures.Add(this);
            transform.SetParent(parent.transform);
        }
        else
        {
            transform.SetParent(null);
        }
    }

    // アンカー占有状態を解除
    public void ReleaseAnchor()
    {
        if (attachedAnchor != null)
        {
            attachedAnchor.SetOccupied(false);
            attachedAnchor = null;
        }
    }

    // インベントリに収納
    public void StoreToInventory()
    {
        // 子オブジェクトも一緒に収納
        foreach (var child in childFurnitures.ToArray())
        {
            if (child != null)
            {
                if (child.furnitureData != null)
                    InventoryManager.Instance?.AddFurniture(child.furnitureData.itemID, 1);
                child.StoreToInventory();
            }
        }
        ReleaseAnchor();

        SetParentFurniture(null);

        if (furnitureData != null)
        {
            EnvironmentStatsManager.Instance?.RemoveValues(
                furnitureData.cozy,
                furnitureData.nature);
        }

        // 対象のインタラクションを無効化
        if (colliders != null)
        {
            foreach (var col in colliders)
            {
                if (col != null)
                    col.enabled = false;
            }
        }
        SetSelected(false);

        Destroy(gameObject);
    }

    // 他のオブジェクトと重なっているかチェック
    public bool IsOverlapping()
    {
        if (colliders == null || colliders.Length == 0)
            return false;

        int furnitureMask = LayerMask.GetMask("Furniture");
        int blockerLayer = LayerMask.NameToLayer("PlacementBlocker");
        int blockerMask = blockerLayer >= 0 ? (1 << blockerLayer) : 0;

        foreach (var collider in colliders)
        {
            if (collider == null) continue;

            if (TryGetOrientedBox(collider, out Vector3 boxCenter, out Vector3 boxHalfExtents, out Quaternion boxRotation))
            {
                if (CheckBoxOverlap(boxCenter, boxHalfExtents, boxRotation, furnitureMask, blockerMask))
                    return true;
                continue;
            }

            if (TryGetSphereParameters(collider, out Vector3 sphereCenter, out float sphereRadius))
            {
                if (CheckSphereOverlap(sphereCenter, sphereRadius, furnitureMask, blockerMask))
                    return true;
                continue;
            }

            if (TryGetCapsuleParameters(collider, out Vector3 capsulePoint0, out Vector3 capsulePoint1, out float capsuleRadius))
            {
                if (CheckCapsuleOverlap(capsulePoint0, capsulePoint1, capsuleRadius, furnitureMask, blockerMask))
                    return true;
                continue;
            }

            Bounds fallbackBounds = collider.bounds;
            if (CheckBoxOverlap(fallbackBounds.center, fallbackBounds.extents, Quaternion.identity, furnitureMask, blockerMask))
                return true;
        }
        return false;
    }

    private bool CheckBoxOverlap(Vector3 center, Vector3 halfExtents, Quaternion rotation, int furnitureMask, int blockerMask)
    {
        if (blockerMask != 0)
        {
            Collider[] blockers = Physics.OverlapBox(
                center,
                halfExtents,
                rotation,
                blockerMask,
                QueryTriggerInteraction.Collide
            );

            if (blockers.Length > 0)
            {
                return true;
            }
        }

        if (furnitureMask == 0)
            return false;

        Collider[] overlaps = Physics.OverlapBox(
            center,
            halfExtents,
            rotation,
            furnitureMask,
            QueryTriggerInteraction.Ignore
        );

        return HasValidFurnitureOverlap(overlaps);
    }

    private bool CheckSphereOverlap(Vector3 center, float radius, int furnitureMask, int blockerMask)
    {
        if (blockerMask != 0)
        {
            Collider[] blockers = Physics.OverlapSphere(
                center,
                radius,
                blockerMask,
                QueryTriggerInteraction.Collide
            );

            if (blockers.Length > 0)
            {
                return true;
            }
        }

        if (furnitureMask == 0)
            return false;

        Collider[] overlaps = Physics.OverlapSphere(
            center,
            radius,
            furnitureMask,
            QueryTriggerInteraction.Ignore
        );

        return HasValidFurnitureOverlap(overlaps);
    }

    private bool CheckCapsuleOverlap(Vector3 point0, Vector3 point1, float radius, int furnitureMask, int blockerMask)
    {
        if (blockerMask != 0)
        {
            Collider[] blockers = Physics.OverlapCapsule(
                point0,
                point1,
                radius,
                blockerMask,
                QueryTriggerInteraction.Collide
            );

            if (blockers.Length > 0)
            {
                return true;
            }
        }

        if (furnitureMask == 0)
            return false;

        Collider[] overlaps = Physics.OverlapCapsule(
            point0,
            point1,
            radius,
            furnitureMask,
            QueryTriggerInteraction.Ignore
        );

        return HasValidFurnitureOverlap(overlaps);
    }

    private bool HasValidFurnitureOverlap(Collider[] overlaps)
    {
        foreach (var overlap in overlaps)
        {
            if (IsIgnorableOverlap(overlap))
                continue;

            return true;
        }

        return false;
    }

    private bool TryGetOrientedBox(Collider collider, out Vector3 center, out Vector3 halfExtents, out Quaternion rotation)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                center = boxCollider.transform.TransformPoint(boxCollider.center);
                halfExtents = Vector3.Scale(boxCollider.size * 0.5f, GetAbsLossyScale(boxCollider.transform));
                rotation = boxCollider.transform.rotation;
                return true;
            case MeshCollider meshCollider when meshCollider.sharedMesh != null:
                Bounds meshBounds = meshCollider.sharedMesh.bounds;
                center = meshCollider.transform.TransformPoint(meshBounds.center);
                halfExtents = Vector3.Scale(meshBounds.extents, GetAbsLossyScale(meshCollider.transform));
                rotation = meshCollider.transform.rotation;
                return true;
            default:
                center = Vector3.zero;
                halfExtents = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
        }
    }

    private bool TryGetSphereParameters(Collider collider, out Vector3 center, out float radius)
    {
        if (collider is SphereCollider sphereCollider)
        {
            center = sphereCollider.transform.TransformPoint(sphereCollider.center);
            Vector3 lossyScale = GetAbsLossyScale(sphereCollider.transform);
            float maxScale = Mathf.Max(lossyScale.x, Mathf.Max(lossyScale.y, lossyScale.z));
            radius = sphereCollider.radius * maxScale;
            return true;
        }

        center = Vector3.zero;
        radius = 0f;
        return false;
    }

    private bool TryGetCapsuleParameters(Collider collider, out Vector3 point0, out Vector3 point1, out float radius)
    {
        if (collider is CapsuleCollider capsuleCollider)
        {
            Vector3 lossyScale = GetAbsLossyScale(capsuleCollider.transform);
            Vector3 center = capsuleCollider.transform.TransformPoint(capsuleCollider.center);

            Vector3 axis;
            float radiusScale;
            float heightScale;

            switch (capsuleCollider.direction)
            {
                case 0:
                    axis = Vector3.right;
                    radiusScale = Mathf.Max(lossyScale.y, lossyScale.z);
                    heightScale = lossyScale.x;
                    break;
                case 2:
                    axis = Vector3.forward;
                    radiusScale = Mathf.Max(lossyScale.x, lossyScale.y);
                    heightScale = lossyScale.z;
                    break;
                default:
                    axis = Vector3.up;
                    radiusScale = Mathf.Max(lossyScale.x, lossyScale.z);
                    heightScale = lossyScale.y;
                    break;
            }

            radius = capsuleCollider.radius * radiusScale;
            float scaledHeight = capsuleCollider.height * heightScale;
            float worldHalf = Mathf.Max(0f, (scaledHeight * 0.5f) - radius);
            Vector3 worldAxis = capsuleCollider.transform.TransformDirection(axis).normalized;

            point0 = center + worldAxis * worldHalf;
            point1 = center - worldAxis * worldHalf;
            return true;
        }

        point0 = Vector3.zero;
        point1 = Vector3.zero;
        radius = 0f;
        return false;
    }

    private static Vector3 GetAbsLossyScale(Transform target)
    {
        Vector3 lossy = target.lossyScale;
        return new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
    }

    private bool IsIgnorableOverlap(Collider overlap)
    {
        if (overlap == null)
            return true;

        if (overlap.GetComponent<AnchorPoint>() != null)
            return true;

        Transform overlapTransform = overlap.transform;
        if (overlapTransform == transform)
            return true;

        if (overlapTransform == transform.parent)
            return true;

        if (overlapTransform.IsChildOf(transform))
            return true;

        return false;
    }

    // 配置面の高さを取得（上に物を置く用）
    public float GetSurfaceHeight()
    {
        if (!furnitureData.canStackOn) return transform.position.y;

        // Y軸の最大値を返す
        float maxY = transform.position.y;
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                maxY = Mathf.Max(maxY, renderer.bounds.max.y);
        }
        return maxY;
    }

    // 底面のオフセットを取得
    public Vector3 GetBottomOffset()
    {
        float minY = transform.position.y;
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                minY = Mathf.Min(minY, renderer.bounds.min.y);
        }
        return Vector3.up * (transform.position.y - minY);
    }
}

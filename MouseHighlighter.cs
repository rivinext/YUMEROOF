using System.Collections.Generic;
using UnityEngine;

public class MouseHighlighter : MonoBehaviour
{
    [SerializeField] private float radiusPixels = 24f;
    [SerializeField] private float maxDistance = Mathf.Infinity;
    [SerializeField] private LayerMask dropLayers = ~0;

    private HighlightTarget currentTarget;
    private readonly HashSet<DropMaterial> processedDrops = new HashSet<DropMaterial>();

    void Update()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            ClearCurrent();
            processedDrops.Clear();
            return;
        }

        // マウスの位置からRayを飛ばす
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan);

        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxDistance, LayerMaskHelper.ExcludeInvisibleWall(dropLayers), QueryTriggerInteraction.Collide);

        if (hitSomething)
        {
            HandleHighlight(hit.collider.GetComponentInParent<HighlightTarget>());
            ProcessDrops(mainCamera, ray);
        }
        else
        {
            ClearCurrent();
        }

        // 同一フレーム内での重複処理を防いだ後にクリア
        processedDrops.Clear();
    }

    void ClearCurrent()
    {
        if (currentTarget != null)
        {
            currentTarget.Unhighlight();
            currentTarget = null;
        }
    }

    private void HandleHighlight(HighlightTarget target)
    {
        if (target != null)
        {
            // 新しいターゲットに乗った
            if (currentTarget != target)
            {
                ClearCurrent();
                currentTarget = target;
                currentTarget.Highlight();
            }
        }
        else
        {
            // ターゲットがないのでクリア
            ClearCurrent();
        }
    }

    private void ProcessDrops(Camera mainCamera, Ray ray)
    {
        if (radiusPixels <= 0f)
        {
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, LayerMaskHelper.ExcludeInvisibleWall(dropLayers), QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 cursorPosition = Input.mousePosition;

        for (int h = 0; h < hits.Length; h++)
        {
            float depth = hits[h].distance;
            if (depth <= 0f)
            {
                continue;
            }

            Vector3 worldCenter = mainCamera.ScreenToWorldPoint(new Vector3(cursorPosition.x, cursorPosition.y, depth));
            Vector3 worldEdge = mainCamera.ScreenToWorldPoint(new Vector3(cursorPosition.x + radiusPixels, cursorPosition.y, depth));
            float worldRadius = Vector3.Distance(worldCenter, worldEdge);

            if (worldRadius <= 0f)
            {
                continue;
            }

            Collider[] colliders = Physics.OverlapSphere(worldCenter, worldRadius, LayerMaskHelper.ExcludeInvisibleWall(dropLayers), QueryTriggerInteraction.Collide);
            for (int i = 0; i < colliders.Length; i++)
            {
                var drop = colliders[i].GetComponentInParent<DropMaterial>();
                if (drop == null)
                {
                    continue;
                }

                if (!processedDrops.Add(drop))
                {
                    continue;
                }

                drop.Interact();
            }
        }
    }
}

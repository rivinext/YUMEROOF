using UnityEngine;

public class PlayerRayInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionDistance = 2f;
    public LayerMask interactionLayers = ~0;
    public Vector3 originOffset = Vector3.up;
    public float horizontalFanAngle = 45f;
    public int horizontalRayCount = 5;
    public float verticalFanAngle = 45f;
    public int verticalRayCount = 3;
    [Tooltip("When enabled, trigger colliders will also be considered when casting for interactables.")]
    public bool includeTriggerColliders = true;
    [SerializeField, HideInInspector] private bool triggerInit;

    public enum CastMode { Ray, Sphere, Box }
    public CastMode castMode = CastMode.Ray;
    public float sphereRadius = 0.5f;
    public Vector3 boxHalfExtents = Vector3.one * 0.5f;

    private IInteractable currentTarget;
    private RayOutlineHighlighter highlighter;
    [SerializeField] private SpeechBubbleController speechBubbleController;
    private bool skipHideOnce;
    private PlayerController playerController;

    void Awake()
    {
        EnsureTriggerInitialized();
        highlighter = GetComponent<RayOutlineHighlighter>();
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
        if (speechBubbleController == null)
            speechBubbleController = FindFirstObjectByType<SpeechBubbleController>(FindObjectsInactive.Include);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureTriggerInitialized();
    }
#endif

    private void EnsureTriggerInitialized()
    {
        if (!triggerInit)
        {
            includeTriggerColliders = true;
            triggerInit = true;
        }
    }

    void Update()
    {
        if (playerController != null && playerController.IsSitting)
        {
            if (currentTarget != null)
            {
                SetHighlight(currentTarget, false);
                currentTarget = null;
            }
            return;
        }

        IInteractable newTarget = FindBestTarget();
        if (newTarget != currentTarget)
        {
            bool newIsGhost = false;
            var mbNew = newTarget as MonoBehaviour;
            if (mbNew != null && mbNew.GetComponent<BuildingGhostInteractable>() != null)
                newIsGhost = true;

            if (currentTarget != null)
            {
                skipHideOnce = newIsGhost;
                SetHighlight(currentTarget, false);
            }

            currentTarget = newTarget;
            if (currentTarget != null)
            {
                if (currentTarget is DropMaterial dropTarget)
                {
                    SetHighlight(dropTarget, false);
                    dropTarget.Interact();
                    currentTarget = null;
                }
                else
                {
                    SetHighlight(currentTarget, true);
                }
            }
        }

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            currentTarget.Interact();
        }
    }

    private IInteractable FindBestTarget()
    {
        IInteractable best = null;
        float bestDist = float.MaxValue;

        Vector3 origin = transform.position + originOffset;
        Vector3 forward = transform.forward;

        float hStep = horizontalRayCount > 1 ? horizontalFanAngle * 2f / (horizontalRayCount - 1) : 0f;
        float vStep = verticalRayCount > 1 ? verticalFanAngle * 2f / (verticalRayCount - 1) : 0f;
        for (int hi = 0; hi < horizontalRayCount; hi++)
        {
            float hAngle = horizontalRayCount > 1 ? -horizontalFanAngle + hStep * hi : 0f;
            Vector3 horizDir = Quaternion.AngleAxis(hAngle, transform.up) * forward;
            for (int vi = 0; vi < verticalRayCount; vi++)
            {
                float vAngle = verticalRayCount > 1 ? -verticalFanAngle + vStep * vi : 0f;
                Vector3 dir = Quaternion.AngleAxis(vAngle, transform.right) * horizDir;
                if (Cast(origin, dir, out RaycastHit hit))
                {
                    IInteractable trig = hit.collider.GetComponentInParent<IInteractable>();
                    if (trig != null && hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        best = trig;
                    }
                }
            }
        }

        return best;
    }

    private void SetHighlight(IInteractable target, bool enabled)
    {
        var mb = target as MonoBehaviour;

        if (highlighter != null)
        {
            if (enabled && mb != null)
                highlighter.Highlight(mb.gameObject);
            else
                highlighter.Clear();
        }

        if (enabled && mb != null)
        {
            // BuildingGhostInteractableの場合：ヒント文字列を使って通常版を表示
            var ghost = mb.GetComponent<BuildingGhostInteractable>();
            if (ghost != null && speechBubbleController != null)
            {
                speechBubbleController.Show(mb.transform, ghost.GetCachedHintText());
            }
            else if (target != null && speechBubbleController != null)
            {
                speechBubbleController.Hide();
            }

        }
        else if (target != null && speechBubbleController != null)
        {
            if (!skipHideOnce)
                speechBubbleController.Hide();
            skipHideOnce = false;
        }

        if (mb == null) return;

        // 以下は既存のインタラクションUIのON/OFF（元のまま）
        var bed = mb.GetComponent<BedTrigger>();
        if (bed != null)
        {
            if (bed.interactionPrompt != null)
                bed.interactionPrompt.SetActive(enabled);
            bed.isPlayerNearby = enabled;
        }

        var sit = mb.GetComponent<SitTrigger>();
        if (sit != null && sit.interactionPrompt != null)
            sit.interactionPrompt.SetActive(enabled);

        var shop = mb.GetComponent<ShopTrigger>();
        if (shop != null && shop.pressEHint != null)
            shop.pressEHint.SetActive(enabled);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + originOffset;
        Vector3 forward = transform.forward;
        float hStep = horizontalRayCount > 1 ? horizontalFanAngle * 2f / (horizontalRayCount - 1) : 0f;
        float vStep = verticalRayCount > 1 ? verticalFanAngle * 2f / (verticalRayCount - 1) : 0f;

        Gizmos.color = Color.yellow;
        for (int hi = 0; hi < horizontalRayCount; hi++)
        {
            float hAngle = horizontalRayCount > 1 ? -horizontalFanAngle + hStep * hi : 0f;
            Vector3 horizDir = Quaternion.AngleAxis(hAngle, transform.up) * forward;
            for (int vi = 0; vi < verticalRayCount; vi++)
            {
                float vAngle = verticalRayCount > 1 ? -verticalFanAngle + vStep * vi : 0f;
                Vector3 dir = Quaternion.AngleAxis(vAngle, transform.right) * horizDir;
                switch (castMode)
                {
                    case CastMode.Ray:
                        Gizmos.DrawRay(origin, dir * interactionDistance);
                        break;
                    case CastMode.Sphere:
                        Gizmos.DrawRay(origin, dir * interactionDistance);
                        Gizmos.DrawWireSphere(origin + dir * interactionDistance, sphereRadius);
                        break;
                    case CastMode.Box:
                        Gizmos.DrawRay(origin, dir * interactionDistance);
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(origin + dir * interactionDistance, transform.rotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);
                        Gizmos.matrix = oldMatrix;
                        break;
                }
            }
        }
    }

    private bool Cast(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        QueryTriggerInteraction triggerInteraction = includeTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        switch (castMode)
        {
            case CastMode.Sphere:
                return Physics.SphereCast(origin, sphereRadius, direction, out hit, interactionDistance, interactionLayers, triggerInteraction);
            case CastMode.Box:
                return Physics.BoxCast(origin, boxHalfExtents, direction, out hit, transform.rotation, interactionDistance, interactionLayers, triggerInteraction);
            default:
                return Physics.Raycast(origin, direction, out hit, interactionDistance, interactionLayers, triggerInteraction);
        }
    }
}

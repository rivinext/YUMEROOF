using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PlayerPlayAreaReturnVolume : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private Collider playArea;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoFillColor = new Color(1f, 0.25f, 0.25f, 0.15f);
    [SerializeField] private Color gizmoWireColor = new Color(1f, 0.25f, 0.25f, 0.75f);

    [Header("Return Settings")]
    [SerializeField] private string overrideSpawnPointName;
    [SerializeField] private float idleCrossFade = 0.1f;
    [SerializeField, Tooltip("Minimum seconds between forced return attempts.")]
    private float returnCooldown = 1.5f;

    [Header("Events")]
    public UnityEvent OnReturned;

    private bool isReturning;
    private Coroutine cooldownRoutine;

    private void Reset()
    {
        EnsurePlayAreaReference();
    }

    private void Awake()
    {
        EnsurePlayAreaReference();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsurePlayAreaReference();
    }
#endif

    private void EnsurePlayAreaReference()
    {
        if (playArea == null)
        {
            playArea = GetComponent<Collider>();
        }

        if (playArea != null)
        {
            playArea.isTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        TryReturnPlayerToSpawn();
    }

    private void TryReturnPlayerToSpawn()
    {
        if (isReturning)
        {
            return;
        }

        var manager = SceneTransitionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("SceneTransitionManager is not available. Unable to force player return.", this);
            return;
        }

        isReturning = true;
        bool success = manager.ForceReturnPlayerToSpawn(overrideSpawnPointName, idleCrossFade);
        if (!success)
        {
            isReturning = false;
            Debug.LogWarning("ForceReturnPlayerToSpawn failed.", this);
            return;
        }

        OnReturned?.Invoke();

        if (returnCooldown > 0f)
        {
            if (cooldownRoutine != null)
            {
                StopCoroutine(cooldownRoutine);
            }

            cooldownRoutine = StartCoroutine(ReturnCooldownRoutine());
        }
        else
        {
            isReturning = false;
        }
    }

    private IEnumerator ReturnCooldownRoutine()
    {
        yield return new WaitForSeconds(returnCooldown);
        isReturning = false;
        cooldownRoutine = null;
    }

    private void OnDisable()
    {
        if (cooldownRoutine != null)
        {
            StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        isReturning = false;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Collider colliderToDraw = playArea != null ? playArea : GetComponent<Collider>();
        if (colliderToDraw == null)
        {
            return;
        }

        Gizmos.matrix = Matrix4x4.identity;

        if (colliderToDraw is BoxCollider box)
        {
            DrawBoxColliderGizmos(box);
        }
        else if (colliderToDraw is MeshCollider meshCollider && meshCollider.sharedMesh != null)
        {
            Gizmos.matrix = meshCollider.transform.localToWorldMatrix;
            Gizmos.color = gizmoFillColor;
            Gizmos.DrawMesh(meshCollider.sharedMesh);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireMesh(meshCollider.sharedMesh);
        }
        else
        {
            Bounds bounds = colliderToDraw.bounds;
            Gizmos.color = gizmoFillColor;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawBoxColliderGizmos(BoxCollider box)
    {
        Vector3 scaledSize = Vector3.Scale(box.size, box.transform.lossyScale);
        Matrix4x4 matrix = Matrix4x4.TRS(box.transform.TransformPoint(box.center), box.transform.rotation, scaledSize);
        Gizmos.matrix = matrix;
        Gizmos.color = gizmoFillColor;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = gizmoWireColor;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}

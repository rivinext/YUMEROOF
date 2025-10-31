using System.Collections;
using Player;
using UnityEngine;

[DisallowMultipleComponent]
public class WardrobeSilhouetteSync : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Wardrobe UI controller that dispatches equip events when items are generated.")]
    private WardrobeUIController wardrobeUIController;

    [SerializeField]
    [Tooltip("Player occlusion silhouette component that needs to refresh its target renderers after wardrobe changes.")]
    private PlayerOcclusionSilhouette playerSilhouette;

    private Coroutine refreshRoutine;
    private readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
    private bool warnedMissingWardrobeController;
    private bool warnedMissingSilhouette;

    private void OnEnable()
    {
        bool hasController = EnsureWardrobeController();
        bool hasSilhouette = EnsurePlayerSilhouette();

        if (hasController)
        {
            wardrobeUIController.OnItemEquipped.AddListener(OnWardrobeItemEquipped);
        }

        if (hasController && hasSilhouette)
        {
            StartRefreshRoutine();
        }
    }

    private void OnDisable()
    {
        if (wardrobeUIController != null)
        {
            wardrobeUIController.OnItemEquipped.RemoveListener(OnWardrobeItemEquipped);
        }

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private void Start()
    {
        if (EnsureWardrobeController() && EnsurePlayerSilhouette())
        {
            StartRefreshRoutine();
        }
    }

    private void OnWardrobeItemEquipped(WardrobeTabType category, GameObject instance, WardrobeItemView itemView)
    {
        if (EnsurePlayerSilhouette())
        {
            StartRefreshRoutine();
        }
    }

    private void StartRefreshRoutine()
    {
        if (playerSilhouette == null)
        {
            return;
        }

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }

        refreshRoutine = StartCoroutine(RefreshSilhouetteNextFrame());
    }

    private IEnumerator RefreshSilhouetteNextFrame()
    {
        yield return waitForEndOfFrame;
        playerSilhouette.RefreshTargetRenderers();
        refreshRoutine = null;
    }

    private bool EnsureWardrobeController()
    {
        if (wardrobeUIController != null)
        {
            return true;
        }

        wardrobeUIController = GetComponent<WardrobeUIController>();
        if (wardrobeUIController == null)
        {
            if (!warnedMissingWardrobeController)
            {
                Debug.LogWarning($"{nameof(WardrobeSilhouetteSync)} on {name} is missing a {nameof(WardrobeUIController)} reference.", this);
                warnedMissingWardrobeController = true;
            }

            return false;
        }

        warnedMissingWardrobeController = false;
        return true;
    }

    private bool EnsurePlayerSilhouette()
    {
        if (playerSilhouette != null)
        {
            return true;
        }

        if (!warnedMissingSilhouette)
        {
            Debug.LogWarning($"{nameof(WardrobeSilhouetteSync)} on {name} is missing a {nameof(PlayerOcclusionSilhouette)} reference.", this);
            warnedMissingSilhouette = true;
        }

        return false;
    }
}

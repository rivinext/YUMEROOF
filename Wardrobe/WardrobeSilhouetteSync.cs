using UnityEngine;

[DisallowMultipleComponent]
public class WardrobeSilhouetteSync : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Previously used to refresh player occlusion silhouettes after wardrobe changes. Kept for backward compatibility.")]
    private WardrobeUIController wardrobeUIController;

    private bool warnedMissingWardrobeController;
    private bool warnedObsolete;

    private void Awake()
    {
        WarnObsolete();
    }

    private void OnEnable()
    {
        WarnObsolete();

        // Disable the component once the warning has been displayed to avoid unnecessary updates.
        enabled = false;
    }

    private void WarnObsolete()
    {
        if (!warnedMissingWardrobeController && wardrobeUIController == null)
        {
            wardrobeUIController = GetComponent<WardrobeUIController>();

            if (wardrobeUIController == null)
            {
                Debug.LogWarning($"{nameof(WardrobeSilhouetteSync)} on {name} is missing a {nameof(WardrobeUIController)} reference.", this);
                warnedMissingWardrobeController = true;
            }
        }

        if (!warnedObsolete)
        {
            Debug.LogWarning($"{nameof(WardrobeSilhouetteSync)} is obsolete because player occlusion silhouettes have been removed.", this);
            warnedObsolete = true;
        }
    }
}

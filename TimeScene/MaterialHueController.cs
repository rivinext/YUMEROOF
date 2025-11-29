using UnityEngine;

public class MaterialHueController : MonoBehaviour
{
    [SerializeField] private HueSyncCoordinator hueCoordinator;
    [SerializeField] private MaterialPresetService presetService;
    [SerializeField] private MaterialPresetUIController presetUIController;
    [SerializeField] private int initialPresetIndex;

    private void Awake()
    {
        if (hueCoordinator == null)
        {
            hueCoordinator = GetComponent<HueSyncCoordinator>();
        }

        if (presetService == null)
        {
            presetService = GetComponent<MaterialPresetService>();
        }

        if (presetUIController == null)
        {
            presetUIController = GetComponent<MaterialPresetUIController>();
        }
    }

    private void Start()
    {
        hueCoordinator?.InitializeSelectors();
        presetUIController?.Initialize(presetService, hueCoordinator, initialPresetIndex);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        hueCoordinator?.ApplyColor();
    }
}

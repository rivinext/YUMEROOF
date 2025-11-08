using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MilestoneAreaUnlocker : MonoBehaviour
{
    [System.Serializable]
    public class AreaStage
    {
        [Tooltip("Milestone identifier used to determine when this stage unlocks.")]
        public string milestoneId;

        [Tooltip("Objects that should be deactivated when this stage unlocks.")]
        public GameObject[] deactivateOnUnlock;

        [Tooltip("Objects that should be activated when this stage unlocks.")]
        public GameObject[] activateOnUnlock;

        [Tooltip("If true, keep the previous area deactivated even while this stage is locked.")]
        public bool deactivateOldAreaWhenLocked;

        [Tooltip("If true, return furniture from the previous area to the inventory before deactivating it.")]
        public bool returnFurnitureOnDeactivate;

        [HideInInspector]
        public bool isUnlocked;
    }

    [SerializeField]
    private List<AreaStage> areaStages = new List<AreaStage>();

    private Coroutine waitCoroutine;

    void OnEnable()
    {
        RefreshAreas();

        if (waitCoroutine == null)
        {
            waitCoroutine = StartCoroutine(WaitForMilestoneManager());
        }
    }

    void OnDisable()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (MilestoneManager.Instance != null)
        {
            MilestoneManager.Instance.OnMilestoneProgress -= HandleMilestoneProgress;
        }
    }

    void OnDestroy()
    {
        if (MilestoneManager.Instance != null)
        {
            MilestoneManager.Instance.OnMilestoneProgress -= HandleMilestoneProgress;
        }
    }

    private IEnumerator WaitForMilestoneManager()
    {
        while (MilestoneManager.Instance == null)
        {
            yield return null;
        }

        MilestoneManager.Instance.OnMilestoneProgress += HandleMilestoneProgress;
        RefreshAreas();
        waitCoroutine = null;
    }

    private void HandleMilestoneProgress(MilestoneManager.Milestone milestone, int cozy, int nature, int itemCount)
    {
        RefreshAreas();
    }

    public void RefreshAreas()
    {
        if (areaStages == null)
        {
            return;
        }

        int completed = MilestoneManager.Instance?.CurrentMilestoneIndex ?? 0;

        foreach (var stage in areaStages)
        {
            if (stage == null)
            {
                continue;
            }

            int stageIndex = GetMilestoneIndex(stage.milestoneId);
            bool shouldBeUnlocked = completed > stageIndex;

            if (stage.isUnlocked == shouldBeUnlocked)
            {
                continue;
            }

            stage.isUnlocked = shouldBeUnlocked;

            if (shouldBeUnlocked)
            {
                if (stage.returnFurnitureOnDeactivate)
                {
                    ReturnFurnitureFromDeactivateTargets(stage);
                }

                SetActive(stage.activateOnUnlock, true);
                SetActive(stage.deactivateOnUnlock, false);
            }
            else
            {
                SetActive(stage.activateOnUnlock, false);
                bool shouldActivateOldArea = !stage.deactivateOldAreaWhenLocked;
                SetActive(stage.deactivateOnUnlock, shouldActivateOldArea);
            }
        }
    }

    private void ReturnFurnitureFromDeactivateTargets(AreaStage stage)
    {
        if (stage == null || stage.deactivateOnUnlock == null)
        {
            return;
        }

        foreach (var deactivateRoot in stage.deactivateOnUnlock)
        {
            if (deactivateRoot == null)
            {
                continue;
            }

            var furnitures = deactivateRoot.GetComponentsInChildren<PlacedFurniture>(true);
            if (furnitures == null || furnitures.Length == 0)
            {
                continue;
            }

            var furnitureArray = furnitures.ToArray();
            foreach (var furniture in furnitureArray)
            {
                if (furniture == null)
                {
                    continue;
                }

                var data = furniture.furnitureData;
                if (data != null && !string.IsNullOrEmpty(data.itemID))
                {
                    InventoryManager.Instance?.AddFurniture(data.itemID, 1);
                }

                FurnitureSaveManager.Instance?.RemoveFurniture(furniture);
                furniture.StoreToInventory();
            }
        }
    }

    private int GetMilestoneIndex(string milestoneId)
    {
        if (string.IsNullOrEmpty(milestoneId))
        {
            return 0;
        }

        int underscoreIndex = milestoneId.LastIndexOf('_');
        if (underscoreIndex >= 0 && underscoreIndex < milestoneId.Length - 1)
        {
            string numberPart = milestoneId.Substring(underscoreIndex + 1);
            if (int.TryParse(numberPart, out int result))
            {
                return Mathf.Max(0, result - 1);
            }
        }

        if (int.TryParse(milestoneId, out int numeric))
        {
            return Mathf.Max(0, numeric - 1);
        }

        return 0;
    }

    private void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null && objects[i].activeSelf != active)
            {
                objects[i].SetActive(active);
            }
        }
    }
}

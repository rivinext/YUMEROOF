using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles equipping and unequipping of runtime wardrobe items on the player.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Player/Equipment Slot")]
public class EquipmentSlot : MonoBehaviour
{
    [Tooltip("装着したアイテムを配置するアンカー Transform。空の場合は自身の Transform が使用されます。")]
    [SerializeField]
    private Transform anchor;

    [Tooltip("このスロットが扱うワードローブカテゴリ。UI 側と一致させてください。")]
    [SerializeField]
    private WardrobeCategory category = WardrobeCategory.Tops;

    [Tooltip("現在生成済みの装備オブジェクト。実行時に自動更新されます。")]
    [SerializeField]
    private GameObject currentInstance;

    private EquipmentItem equippedItem;

    /// <summary>
    /// Gets the instantiated equipment object currently attached to this slot.
    /// </summary>
    public GameObject CurrentInstance => currentInstance;

    /// <summary>
    /// Gets the wardrobe category handled by this slot.
    /// </summary>
    public WardrobeCategory Category => category;

    /// <summary>
    /// Gets the wardrobe item that is currently equipped in this slot.
    /// </summary>
    public EquipmentItem EquippedItem => equippedItem;

    /// <summary>
    /// Equip the supplied item by instantiating its prefab under the anchor.
    /// </summary>
    /// <param name="item">Wardrobe item to equip.</param>
    public void Equip(EquipmentItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("Tried to equip a null item. The slot will be cleared instead.", this);
            Unequip();
            return;
        }

        if (item.EquipmentPrefab == null)
        {
            Debug.LogWarning($"Equipment item '{item.name}' does not have an equipment prefab assigned.", item);
            Unequip();
            return;
        }

        if (item.Category != category)
        {
            Debug.LogWarning($"Item '{item.name}' does not match slot category {category}.", this);
        }

        Unequip();

        Transform targetAnchor = anchor == null ? transform : anchor;
        GameObject instance = Instantiate(item.EquipmentPrefab, targetAnchor);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        currentInstance = instance;
        equippedItem = item;

        ApplyMaterialOverrides(instance, item.MaterialOverrides);
        ApplySkinnedMeshBindings(instance, targetAnchor);
        NotifyReceiversEquipped(instance, item);
    }

    /// <summary>
    /// Destroys the currently equipped object, if any, and notifies receivers.
    /// </summary>
    public void Unequip()
    {
        if (currentInstance == null)
        {
            return;
        }

        NotifyReceiversUnequipped(currentInstance);
        Destroy(currentInstance);
        currentInstance = null;
        equippedItem = null;
    }

    private static void ApplyMaterialOverrides(GameObject instance, IReadOnlyList<Material> overrides)
    {
        if (instance == null || overrides == null || overrides.Count == 0)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                continue;
            }

            Material[] newMaterials = new Material[sharedMaterials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                int index = Mathf.Min(i, overrides.Count - 1);
                newMaterials[i] = overrides[index];
            }

            renderer.sharedMaterials = newMaterials;
        }
    }

    private void ApplySkinnedMeshBindings(GameObject instance, Transform targetAnchor)
    {
        if (instance == null || targetAnchor == null)
        {
            return;
        }

        var anchorRenderer = targetAnchor.GetComponentInParent<SkinnedMeshRenderer>();
        if (anchorRenderer == null)
        {
            return;
        }

        foreach (var attachment in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (attachment == null)
            {
                continue;
            }

            attachment.rootBone = anchorRenderer.rootBone;
            attachment.bones = anchorRenderer.bones;
        }
    }

    private void NotifyReceiversEquipped(GameObject instance, EquipmentItem item)
    {
        if (instance == null)
        {
            return;
        }

        foreach (var receiver in instance.GetComponentsInChildren<IEquipmentSlotReceiver>(true))
        {
            receiver.OnEquipped(this, item);
        }
    }

    private void NotifyReceiversUnequipped(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        foreach (var receiver in instance.GetComponentsInChildren<IEquipmentSlotReceiver>(true))
        {
            receiver.OnUnequipped(this);
        }
    }
}

/// <summary>
/// Optional interface that equipment prefabs can implement to react to equip and unequip events.
/// </summary>
public interface IEquipmentSlotReceiver
{
    /// <summary>
    /// Called when the associated item has been equipped onto a slot.
    /// </summary>
    /// <param name="slot">Slot that triggered the equip.</param>
    /// <param name="item">Equipped wardrobe item.</param>
    void OnEquipped(EquipmentSlot slot, EquipmentItem item);

    /// <summary>
    /// Called when the item is unequipped from the slot.
    /// </summary>
    /// <param name="slot">Slot triggering the unequip.</param>
    void OnUnequipped(EquipmentSlot slot);
}

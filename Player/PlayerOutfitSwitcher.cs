using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerOutfitSwitcher : MonoBehaviour
    {
        [System.Serializable]
        private struct OutfitEntry
        {
            [Tooltip("割り当てる装備カテゴリを指定します。")]
            public WardrobeCategory category;

            [Tooltip("該当カテゴリで装備する EquipmentItem アセット。")]
            public EquipmentItem equipment;
        }

        [System.Serializable]
        private struct SlotBinding
        {
            [Tooltip("このスイッチャーで制御するワードローブカテゴリ。")]
            public WardrobeCategory category;

            [Tooltip("プレイヤー階層内の EquipmentSlot 参照。")]
            public EquipmentSlot slot;
        }

        [Header("Default Outfit")]
        [FormerlySerializedAs("outfits")]
        [SerializeField]
        private OutfitEntry[] defaultOutfits = System.Array.Empty<OutfitEntry>();

        [Header("Slot Bindings")]
        [SerializeField]
        private SlotBinding[] slotBindings = System.Array.Empty<SlotBinding>();

        [SerializeField]
        private PlayerOcclusionSilhouette occlusionSilhouette;

        private readonly Dictionary<WardrobeCategory, EquipmentSlot> slotLookup = new();
        private readonly Dictionary<WardrobeCategory, EquipmentItem> equippedLookup = new();
        private Coroutine pendingSilhouetteRefresh;

        private void OnDisable()
        {
            if (pendingSilhouetteRefresh != null)
            {
                StopCoroutine(pendingSilhouetteRefresh);
                pendingSilhouetteRefresh = null;
            }
        }

        private void Awake()
        {
            BuildSlotLookup();
            ApplySerializedOutfits(Application.isPlaying);
        }

        private void OnValidate()
        {
            BuildSlotLookup();
            ApplySerializedOutfits(Application.isPlaying);
        }

        /// <summary>
        /// 装備カテゴリを直接指定してアウトフィットを切り替えます。
        /// </summary>
        /// <param name="category">対象カテゴリ。</param>
        /// <param name="item">装備するアイテム。null の場合は該当カテゴリを解除します。</param>
        public void SetOutfit(WardrobeCategory category, EquipmentItem item)
        {
            if (slotLookup.Count == 0)
            {
                BuildSlotLookup();
            }

            if (item == null)
            {
                equippedLookup.Remove(category);
            }
            else
            {
                equippedLookup[category] = item;
            }

            if (!slotLookup.TryGetValue(category, out EquipmentSlot slot) || slot == null)
            {
                if (item != null)
                {
                    Debug.LogWarning($"No EquipmentSlot bound for category {category}.", this);
                }

                if (slot == null)
                {
                    return;
                }
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (item == null)
            {
                slot.Unequip();
            }
            else
            {
                slot.Equip(item);
            }

            RefreshTargetRenderers();
        }

        /// <summary>
        /// EquipmentItem 自体からカテゴリを推論して装備します。
        /// </summary>
        /// <param name="item">装備するアイテム。</param>
        public void SetOutfit(EquipmentItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("Attempted to set outfit with a null EquipmentItem.", this);
                return;
            }

            SetOutfit(item.Category, item);
        }

        private void BuildSlotLookup()
        {
            EnsureSlotBindingsInitialized();
            slotLookup.Clear();

            if (slotBindings == null || slotBindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < slotBindings.Length; i++)
            {
                SlotBinding binding = slotBindings[i];

                if (binding.slot == null)
                {
                    Debug.LogWarning($"Slot binding for category {binding.category} is missing a reference to an EquipmentSlot.", this);
                    continue;
                }

                if (slotLookup.TryGetValue(binding.category, out EquipmentSlot existing) && existing != null && existing != binding.slot)
                {
                    Debug.LogWarning($"Duplicate slot binding detected for category {binding.category}. Using the most recent reference.", this);
                }

                slotLookup[binding.category] = binding.slot;
            }
        }

        private void EnsureSlotBindingsInitialized()
        {
            if (slotBindings != null && slotBindings.Length > 0)
            {
                return;
            }

            EquipmentSlot[] slots = GetComponentsInChildren<EquipmentSlot>(true);
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            slotBindings = new SlotBinding[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                slotBindings[i] = new SlotBinding
                {
                    category = slots[i].Category,
                    slot = slots[i]
                };
            }
        }

        private void ApplySerializedOutfits(bool equipNow)
        {
            equippedLookup.Clear();

            if (defaultOutfits != null)
            {
                for (int i = 0; i < defaultOutfits.Length; i++)
                {
                    OutfitEntry entry = defaultOutfits[i];

                    if (entry.equipment == null)
                    {
                        continue;
                    }

                    equippedLookup[entry.category] = entry.equipment;

                    if (!slotLookup.ContainsKey(entry.category) && slotBindings != null && slotBindings.Length > 0)
                    {
                        Debug.LogWarning($"No EquipmentSlot is configured for category {entry.category}, but an equipment item is assigned.", this);
                    }
                }
            }

            if (!equipNow)
            {
                return;
            }

            if (slotBindings == null || slotBindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < slotBindings.Length; i++)
            {
                SlotBinding binding = slotBindings[i];
                EquipmentSlot slot = binding.slot;

                if (slot == null)
                {
                    continue;
                }

                EquipmentItem item = null;
                equippedLookup.TryGetValue(binding.category, out item);

                if (item == null)
                {
                    slot.Unequip();
                }
                else
                {
                    slot.Equip(item);
                }
            }

            RefreshTargetRenderers();
        }

        private void RefreshTargetRenderers()
        {
            if (occlusionSilhouette == null)
            {
                occlusionSilhouette = GetComponentInChildren<PlayerOcclusionSilhouette>(true);
            }

            if (occlusionSilhouette == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                occlusionSilhouette.RefreshTargetRenderers();
                return;
            }

            if (!isActiveAndEnabled)
            {
                occlusionSilhouette.RefreshTargetRenderers();
                return;
            }

            if (pendingSilhouetteRefresh != null)
            {
                StopCoroutine(pendingSilhouetteRefresh);
            }

            pendingSilhouetteRefresh = StartCoroutine(RefreshTargetRenderersCoroutine());
        }

        private IEnumerator RefreshTargetRenderersCoroutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (occlusionSilhouette == null)
            {
                occlusionSilhouette = GetComponentInChildren<PlayerOcclusionSilhouette>(true);
            }

            if (occlusionSilhouette != null)
            {
                occlusionSilhouette.RefreshTargetRenderers();
            }

            pendingSilhouetteRefresh = null;
        }
    }
}

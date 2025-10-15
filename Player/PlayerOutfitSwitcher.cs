using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerOutfitSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public struct OutfitSlot
        {
            [Tooltip("任意の衣装名を指定します。")]
            public string outfitName;

            [Tooltip("衣装のルートとなる GameObject を設定します。")]
            public GameObject outfitRoot;

            [Tooltip("衣装用に使用する SkinnedMeshRenderer。必要に応じて設定します。")]
            public SkinnedMeshRenderer overrideRenderer;

            [Tooltip("衣装で使用するマテリアル群。必要に応じて設定します。")]
            public Material[] overrideMaterials;
        }

        [SerializeField]
        private OutfitSlot[] outfits = System.Array.Empty<OutfitSlot>();

        [SerializeField]
        private int activeIndex;

        private void Awake()
        {
            ApplyOutfit(activeIndex);
        }

        private void OnValidate()
        {
            ApplyOutfit(activeIndex);
        }

        public void SetOutfit(int index)
        {
            if (outfits == null || outfits.Length == 0)
            {
                Debug.LogWarning("Outfit list is empty.", this);
                return;
            }

            if (index < 0 || index >= outfits.Length)
            {
                Debug.LogWarning($"Outfit index {index} is out of range.", this);
                return;
            }

            if (activeIndex == index)
            {
                return;
            }

            activeIndex = index;
            ApplyOutfit(activeIndex);
        }

        private void ApplyOutfit(int index)
        {
            if (outfits == null || outfits.Length == 0)
            {
                return;
            }

            for (int i = 0; i < outfits.Length; i++)
            {
                bool isActive = i == index;
                var slot = outfits[i];

                if (slot.outfitRoot != null)
                {
                    slot.outfitRoot.SetActive(isActive);
                }

                if (slot.overrideRenderer != null)
                {
                    slot.overrideRenderer.enabled = isActive;

                    if (isActive && slot.overrideMaterials != null && slot.overrideMaterials.Length > 0)
                    {
                        slot.overrideRenderer.sharedMaterials = slot.overrideMaterials;
                    }
                }
            }
        }
    }
}

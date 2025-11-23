using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゴースト用のインタラクト検知トリガー。
/// プレイヤーがトリガー内に入った際に BuildingGhostInteractable へ通知し、
/// 退出時にはフォーカスを解除します。距離チェックを挟むことで、
/// コライダー外縁からの過剰な検知を防ぎます。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GhostInteractionTrigger : MonoBehaviour
{
    [Tooltip("フォーカス対象となる BuildingGhostInteractable。未設定の場合は親階層から自動取得します。")]
    [SerializeField] private BuildingGhostInteractable ghostInteractable;

    [Tooltip("判定の中心。未設定の場合、このトリガーの transform を使用します。")]
    [SerializeField] private Transform distanceCenter;

    [Tooltip("0 より大きい場合、この距離以内でのみフォーカスを許可します。0 なら距離チェックを行いません。")]
    [SerializeField] private float maxFocusDistance = 0f;

    [Tooltip("プレイヤーを識別するためのタグ。空の場合はタグチェックをスキップします。")]
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<PlayerRayInteractor> activeInteractors = new HashSet<PlayerRayInteractor>();

    private void Awake()
    {
        if (ghostInteractable == null)
        {
            ghostInteractable = GetComponentInParent<BuildingGhostInteractable>();
        }

        if (distanceCenter == null)
        {
            distanceCenter = transform;
        }

        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        ClearInteractors();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        var interactor = other.GetComponentInParent<PlayerRayInteractor>();
        if (interactor == null || activeInteractors.Contains(interactor))
        {
            return;
        }

        if (!IsWithinAllowedDistance(interactor.transform))
        {
            return;
        }

        activeInteractors.Add(interactor);
        ghostInteractable?.OnFocus(interactor);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        var interactor = other.GetComponentInParent<PlayerRayInteractor>();
        if (interactor == null || !activeInteractors.Contains(interactor))
        {
            return;
        }

        activeInteractors.Remove(interactor);
        ghostInteractable?.OnBlur(interactor);
    }

    private void ClearInteractors()
    {
        if (ghostInteractable == null || activeInteractors.Count == 0)
        {
            activeInteractors.Clear();
            return;
        }

        foreach (var interactor in activeInteractors)
        {
            if (interactor != null)
            {
                ghostInteractable.OnBlur(interactor);
            }
        }

        activeInteractors.Clear();
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }

    private bool IsWithinAllowedDistance(Transform interactorTransform)
    {
        if (maxFocusDistance <= 0f || distanceCenter == null || interactorTransform == null)
        {
            return true;
        }

        float sqrDistance = (distanceCenter.position - interactorTransform.position).sqrMagnitude;
        return sqrDistance <= maxFocusDistance * maxFocusDistance;
    }

    public void SetInteractable(BuildingGhostInteractable interactable)
    {
        ghostInteractable = interactable;
    }

    public void SetMaxFocusDistance(float distance)
    {
        maxFocusDistance = distance;
    }
}

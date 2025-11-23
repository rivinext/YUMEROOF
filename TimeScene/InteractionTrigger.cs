using UnityEngine;
using UnityEngine.UI;

public abstract class InteractionTrigger : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    public string interactionName = "Interact";
    public float interactionDistance = 2f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI Elements")]
    public GameObject interactionPanel;   // インタラクション用UIパネル
    protected GameObject player;
    protected bool isPlayerNearby = false;
    protected bool isPanelOpen = false;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        // UIの初期設定
        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    protected virtual void Update()
    {
        if (player == null) return;

        // 距離チェック
        float distance = Vector3.Distance(transform.position, player.transform.position);
        isPlayerNearby = distance <= interactionDistance;

        // インタラクション処理
        if (isPlayerNearby && Input.GetKeyDown(interactionKey) && !isPanelOpen)
        {
            Interact();
        }
        else if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public virtual void Interact()
    {
        OpenPanel();
    }

    protected virtual void OpenPanel()
    {
        isPanelOpen = true;

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);

            // プレイヤー操作を無効化
            PlayerController.SetGlobalInputEnabled(false);

            // パネル内のボタンを設定
            SetupPanelButtons();
        }
    }

    protected virtual void ClosePanel()
    {
        isPanelOpen = false;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        // プレイヤー操作を有効化
        PlayerController.SetGlobalInputEnabled(true);
    }

    protected abstract void SetupPanelButtons();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}

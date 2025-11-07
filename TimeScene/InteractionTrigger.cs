using UnityEngine;
using UnityEngine.UI;

public abstract class InteractionTrigger : MonoBehaviour, IInteractable, IInteractionPromptDataProvider
{
    [Header("Interaction Settings")]
    public string interactionName = "Interact";
    public float interactionDistance = 2f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI Elements")]
    public GameObject interactionPanel;   // インタラクション用UIパネル
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float promptOffset = 1f;
    [SerializeField] private string promptLocalizationKey = string.Empty;

    protected GameObject player;
    protected bool isPlayerNearby = false;
    protected bool isPanelOpen = false;
    protected SharedInteractionPromptController promptController;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        promptController = SharedInteractionPromptController.Instance;

        if (promptAnchor == null)
            promptAnchor = transform;

        // UIの初期設定
        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    protected virtual void Update()
    {
        if (player == null) return;

        // 距離チェック
        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distance <= interactionDistance;

        // プロンプト表示切り替え
        if (isPlayerNearby != wasNearby && !isPanelOpen)
        {
            if (isPlayerNearby)
            {
                ShowPrompt();
            }
            else
            {
                promptController?.HidePrompt(this);
            }
        }

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

        promptController?.HidePrompt(this);

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

        if (isPlayerNearby)
            ShowPrompt();

        // プレイヤー操作を有効化
        PlayerController.SetGlobalInputEnabled(true);
    }

    protected abstract void SetupPanelButtons();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }

    protected virtual void OnDisable()
    {
        promptController?.HidePrompt(this);
    }

    protected void ShowPrompt()
    {
        if (promptController == null)
            return;

        if (TryGetInteractionPromptData(out var promptData) && promptData.IsValid)
        {
            promptController.ShowPrompt(this, promptData);
        }
    }

    public bool TryGetInteractionPromptData(out InteractionPromptData data)
    {
        var anchor = promptAnchor != null ? promptAnchor : transform;
        data = new InteractionPromptData(anchor, promptOffset, promptLocalizationKey);
        return true;
    }
}

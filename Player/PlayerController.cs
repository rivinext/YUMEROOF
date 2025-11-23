using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float walkSpeed = 5.0f;       // 歩き速度
    public float runSpeed = 9.0f;        // 走り速度
    public float rotationSpeed = 10f;
    public float moveDamping = 0.15f;    // 動きの滑らかさ

    [Header("Particles (one-shot)")]
    public ParticleSystem walkParticles;   // 歩行時に一発発生させる用
    public ParticleSystem runParticles;    // 走行時に一発発生させる用
    [Tooltip("スティック微入力を無視するしきい値")]
    public float inputDeadZone = 0.1f;
    [Tooltip("歩きの1歩分とみなす距離(m)")]
    public float walkStepDistance = 0.45f;
    [Tooltip("走りの1歩分とみなす距離(m)")]
    public float runStepDistance = 0.55f;
    [Tooltip("1歩でEmitする粒子数")]
    public int particlesPerStep = 1;

    [Header("Sit State Controller")]
    [SerializeField] private PlayerSitStateController sitStateController;
    public bool IsSitting => sitStateController != null && sitStateController.IsSitting;

    // --- 入力有効/無効 ---
    public static bool GlobalInputEnabled { get; private set; } = true;
    private bool inputEnabled = true;
    public static void SetGlobalInputEnabled(bool enabled) => GlobalInputEnabled = enabled;
    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

    private Rigidbody rb;
    private Animator animator;
    [SerializeField] private Transform cameraTargetOverride;
    private OrthographicCameraController cameraController;

    [Header("Blink Controller")]
    [SerializeField] private PlayerBlinkController blinkController;

    [Header("Emote Controller")]
    [SerializeField] private PlayerEmoteController emoteController;

    [Header("Sleep Controller")]
    [SerializeField] private PlayerIdleSleepController sleepController;

    private Vector3 currentMoveVelocity;
    private Vector3 moveDampVelocity;
    private bool hadInputLastFrame = false;

    // 距離積算で足音（足跡）を出す
    private Vector3 prevPosition;
    private float distanceAccumulator = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (sitStateController == null)
        {
            sitStateController = GetComponent<PlayerSitStateController>();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (sitStateController != null)
        {
            sitStateController.StandUpRequested += HandleStandUpRequested;
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        if (blinkController == null)
        {
            blinkController = GetComponent<PlayerBlinkController>();
        }

        if (emoteController == null)
        {
            emoteController = GetComponent<PlayerEmoteController>();
        }

        if (sleepController == null)
        {
            sleepController = GetComponent<PlayerIdleSleepController>();
        }

        if (sitStateController != null)
        {
            sitStateController.Configure(blinkController, sleepController, emoteController);
        }

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        prevPosition = rb.position;

        // 連続発生型は使わないので、EmissionはOFF推奨（Emitで都度発生）
        if (walkParticles) { var e = walkParticles.emission; e.enabled = false; }
        if (runParticles)  { var e = runParticles.emission;  e.enabled = false; }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (sitStateController != null)
        {
            sitStateController.StandUpRequested -= HandleStandUpRequested;
        }
    }

    void OnDestroy()
    {
        // Ensure the global input flag is reset when the player is destroyed.
        SetGlobalInputEnabled(true);
        if (sitStateController != null)
        {
            sitStateController.StandUpRequested -= HandleStandUpRequested;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<OrthographicCameraController>();
            if (cameraController != null)
            {
                Transform target = cameraTargetOverride;

                // すでに別のターゲットが設定されている場合は尊重する
                if (target == null && cameraController.cameraTarget != null)
                {
                    bool isPlayerTarget = cameraController.cameraTarget == transform;

                    if (!isPlayerTarget)
                    {
                        Transform existing = cameraController.cameraTarget;

                        if (existing != null)
                        {
                            // プレイヤー階層下のターゲットかどうかを確認
                            if (!existing.IsChildOf(transform))
                            {
                                return;
                            }
                        }
                    }
                }

                if (target == null)
                {
                    target = transform.Find("CameraTarget");
                }

                cameraController.cameraTarget = target != null ? target : transform;
            }
            else
            {
                Debug.LogWarning("OrthographicCameraController not found on main camera. Using default camera rotation.");
            }
        }
    }

    void Update()
    {
        sitStateController?.Tick();

        if (!GlobalInputEnabled || !inputEnabled)
            return;

        // Interaction for sitting is now handled via PlayerProximityInteractor.
        // The E key will only trigger Sit() when a SitTrigger is focused.
    }

    private void HandleStandUpRequested()
    {
        StandUp();
    }

    void FixedUpdate()
    {
        // 入力が無効なら停止＆積算リセット
        bool canProcessInput = GlobalInputEnabled && inputEnabled;
        bool canControlBlink = blinkController != null && (emoteController == null || !emoteController.IsBlinkLocked);

        if (!canProcessInput)
        {
            bool isSeatedIdle = sitStateController != null && sitStateController.IsSeatIdle;
            Transform seatedAnchor = sitStateController != null ? sitStateController.CurrentSeatAnchor : null;

            if (isSeatedIdle && seatedAnchor != null)
            {
                rb.MovePosition(new Vector3(seatedAnchor.position.x, rb.position.y, seatedAnchor.position.z));
                rb.MoveRotation(seatedAnchor.rotation);
            }
            animator?.SetFloat("moveSpeed", 0f);
            distanceAccumulator = 0f;
            prevPosition = rb.position;
            hadInputLastFrame = false;
            return;
        }

        if (canControlBlink)
        {
            blinkController.SetBlinkingEnabled(true);
        }

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(moveHorizontal, 0, moveVertical);
        float inputMagnitude = inputDirection.magnitude;
        bool hasInput = inputMagnitude > inputDeadZone;

        if (hasInput)
        {
            if (!hadInputLastFrame)
            {
                if (canControlBlink)
                {
                    blinkController.NotifyActive();
                }
                sleepController?.NotifyActive(IsSitting);
            }
        }
        else
        {
            if (canControlBlink)
            {
                blinkController.NotifyInactive(Time.fixedDeltaTime);
            }
            if (!IsSitting)
            {
                sleepController?.NotifyInactive(Time.fixedDeltaTime);
            }
        }

        hadInputLastFrame = hasInput;

        // カメラ基準で入力を回す
        float cameraRotationY = 45f;
        if (cameraController != null)
            cameraRotationY = cameraController.CurrentCameraRotationY;

        Quaternion cameraRotation = Quaternion.Euler(0, cameraRotationY, 0);
        Vector3 moveDirection = cameraRotation * inputDirection;

        // Shiftで速度切替
        bool isRunKey = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = isRunKey ? runSpeed : walkSpeed;

        // ターゲット速度
        Vector3 targetVelocity = moveDirection.normalized * currentSpeed;

        // 補間して移動
        currentMoveVelocity = Vector3.SmoothDamp(
            currentMoveVelocity,
            targetVelocity,
            ref moveDampVelocity,
            moveDamping
        );

        Vector3 newPos = rb.position + currentMoveVelocity * Time.fixedDeltaTime;

        if (animator != null && animator.applyRootMotion)
        {
            newPos = rb.position; // root motion handles movement
        }
        else
        {
            rb.MovePosition(new Vector3(newPos.x, rb.position.y, newPos.z));
        }

        // 回転
        if (hasInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // アニメータ用
        animator?.SetFloat("moveSpeed", currentMoveVelocity.magnitude);

        // ===== ここから「距離ベースの一発発生」 =====
        // 入力がない（スティック離した）瞬間は積算をリセットすると挙動が安定
        if (!hasInput)
        {
            distanceAccumulator = 0f;
            prevPosition = newPos;
            return;
        }

        // 実際に進んだ距離を積算
        float moved = Vector3.Distance(prevPosition, newPos);
        distanceAccumulator += moved;
        prevPosition = newPos;

        // いまのモード（歩き/走り）に応じた「一歩距離」を閾値にEmit
        float stepThreshold = isRunKey ? runStepDistance : walkStepDistance;

        while (distanceAccumulator >= stepThreshold)
        {
            EmitStep(isRunKey);
            distanceAccumulator -= stepThreshold;
        }
    }

    public void Sit(Transform anchor, Collider seatCol)
    {
        sitStateController?.Sit(anchor, seatCol);
    }

    public void StartSeatMove()
    {
        sitStateController?.StartSeatMove();
    }

    public void StartSeatMove(float normalizedStartTime)
    {
        sitStateController?.StartSeatMove(normalizedStartTime);
    }

    public void StandUp()
    {
        sitStateController?.StandUp();
    }

    public void StartStandUpMove()
    {
        sitStateController?.StartStandUpMove();
    }

    private void EmitStep(bool isRun)
    {
        ParticleSystem ps = isRun ? runParticles : walkParticles;
        if (ps == null) return;

        // 足元位置から発生させたい場合は、psのTransformを足元子オブジェクトに
        ps.Emit(particlesPerStep);
    }
}

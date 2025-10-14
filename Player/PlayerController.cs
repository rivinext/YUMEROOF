using System.Collections;
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

    [Header("Sit")]
    [SerializeField] private string sitTriggerName = "SitDown";
    [SerializeField] private string standTriggerName = "StandUp";
    [SerializeField] private string sitBoolName = "IsSitting";
    private Transform seatAnchor;
    public bool IsSitting { get; private set; } = false;
    private bool isMovingToSeat = false;
    [SerializeField] private AnimationCurve seatMoveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve standUpForwardOffsetCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);

    // --- 入力有効/無効 ---
    public static bool GlobalInputEnabled { get; private set; } = true;
    private bool inputEnabled = true;
    public static void SetGlobalInputEnabled(bool enabled) => GlobalInputEnabled = enabled;
    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

    private Rigidbody rb;
    private Collider playerCollider;
    private Collider seatCollider;
    private Animator animator;
    [SerializeField] private Transform cameraTargetOverride;
    private OrthographicCameraController cameraController;

    private Vector3 currentMoveVelocity;
    private Vector3 moveDampVelocity;

    // 距離積算で足音（足跡）を出す
    private Vector3 prevPosition;
    private float distanceAccumulator = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        prevPosition = rb.position;

        // 連続発生型は使わないので、EmissionはOFF推奨（Emitで都度発生）
        if (walkParticles) { var e = walkParticles.emission; e.enabled = false; }
        if (runParticles)  { var e = runParticles.emission;  e.enabled = false; }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Ensure the global input flag is reset when the player is destroyed.
        SetGlobalInputEnabled(true);
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
        if (IsSitting)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                StandUp();
            }
            return;
        }

        if (!GlobalInputEnabled || !inputEnabled)
            return;

        // Interaction for sitting is now handled via PlayerRayInteractor.
        // The E key will only trigger Sit() when a SitTrigger is focused.
    }

    void FixedUpdate()
    {
        // 入力が無効なら停止＆積算リセット
        if (!GlobalInputEnabled || !inputEnabled)
        {
            if (IsSitting && !isMovingToSeat && seatAnchor != null)
            {
                rb.MovePosition(new Vector3(seatAnchor.position.x, rb.position.y, seatAnchor.position.z));
                rb.MoveRotation(seatAnchor.rotation);
            }
            animator?.SetFloat("moveSpeed", 0f);
            distanceAccumulator = 0f;
            prevPosition = rb.position;
            return;
        }

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(moveHorizontal, 0, moveVertical);

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
        if (inputDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // アニメータ用
        animator?.SetFloat("moveSpeed", currentMoveVelocity.magnitude);

        // ===== ここから「距離ベースの一発発生」 =====
        bool hasInput = inputDirection.magnitude > inputDeadZone;

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
        if (anchor != null)
        {
            seatAnchor = anchor;
        }
        seatCollider = seatCol;
        if (playerCollider != null && seatCollider != null)
            Physics.IgnoreCollision(playerCollider, seatCollider, true);
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        SetGlobalInputEnabled(false);
        if (animator != null)
            animator.applyRootMotion = true;
        animator?.SetTrigger(sitTriggerName);
        animator?.SetBool(sitBoolName, true);
        StartCoroutine(MoveToSeat(anchor));
    }

    public void StartSeatMove()
    {
        if (seatAnchor != null && !isMovingToSeat)
        {
            StartCoroutine(MoveToSeat(seatAnchor));
        }
    }

    private IEnumerator MoveToSeat(Transform anchor, float normalizedStartTime = -1f)
    {
        if (anchor == null)
            yield break;

        isMovingToSeat = true;

        if (normalizedStartTime >= 0f && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < normalizedStartTime)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }
        }
        // Delay has been removed; encode any desired wait time in the initial flat
        // section of the seatMoveCurve.

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 targetPos = anchor.position;
        Quaternion targetRot = anchor.rotation;

        float duration = seatMoveCurve.keys[^1].time;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = seatMoveCurve.Evaluate(elapsed);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (animator != null)
            {
                animator.MatchTarget(targetPos, targetRot, AvatarTarget.Root,
                    new MatchTargetWeightMask(Vector3.one, 1f), 0f, 1f, true);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalT = seatMoveCurve.Evaluate(duration);
        transform.position = Vector3.Lerp(startPos, targetPos, finalT);
        transform.rotation = Quaternion.Slerp(startRot, targetRot, finalT);
        IsSitting = true;
        isMovingToSeat = false;
        if (animator != null)
            animator.applyRootMotion = false;
    }

    public void StandUp()
    {
        if (animator != null)
            animator.applyRootMotion = true;
        StartCoroutine(StandUpRoutine());
    }

    private IEnumerator StandUpRoutine()
    {
        animator?.SetTrigger(standTriggerName);
        animator?.SetBool(sitBoolName, false);

        StartStandUpMove();
        yield break;
    }

    public void StartStandUpMove()
    {
        StartCoroutine(MoveFromSeat());
    }

    private IEnumerator MoveFromSeat()
    {
        if (seatAnchor == null)
        {
            rb.isKinematic = false;
            SetGlobalInputEnabled(true);
            IsSitting = false;
            if (animator != null)
                animator.applyRootMotion = false;
            yield break;
        }

        Vector3 startPos = seatAnchor.position;
        Quaternion targetRot = seatAnchor.rotation;

        float duration = standUpForwardOffsetCurve.keys[^1].time;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offset = standUpForwardOffsetCurve.Evaluate(elapsed);
            transform.position = startPos + seatAnchor.forward * offset;
            transform.rotation = targetRot;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos + seatAnchor.forward * standUpForwardOffsetCurve.Evaluate(duration);
        transform.rotation = targetRot;

        if (playerCollider != null && seatCollider != null)
            Physics.IgnoreCollision(playerCollider, seatCollider, false);

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SetGlobalInputEnabled(true);
        IsSitting = false;
        seatAnchor = null;
        seatCollider = null;
        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void EmitStep(bool isRun)
    {
        ParticleSystem ps = isRun ? runParticles : walkParticles;
        if (ps == null) return;

        // 足元位置から発生させたい場合は、psのTransformを足元子オブジェクトに
        ps.Emit(particlesPerStep);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Idle, Move, Attack, Dead
}

public class PlayerStateMachine : MonoBehaviour
{
    #region 组件引用
    [Header("核心组件")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public Animator anim;
    public WeaponManager weaponManager;

    [Header("攻击判定")]
    [Tooltip("攻击判定区域（手动在引擎内调整Collider2D形状）")]
    public Collider2D attackCheckCollider; // 拖拽攻击判定碰撞体（需勾选IsTrigger）

    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float globalAttackCooldown = 0.5f; // 全局攻击冷却（武器无冷却时用）
    #endregion

    #region 状态相关变量
    public PlayerState currentState;
    private float attackTimer;
    private Vector2 moveDirection;
    private bool isDead;
    #endregion

    #region 生命周期
    void Awake()
    {
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        anim ??= GetComponent<Animator>();
        weaponManager ??= GetComponent<WeaponManager>();

        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // 初始化攻击判定碰撞体（默认隐藏）
        if (attackCheckCollider != null)
        {
            attackCheckCollider.enabled = false;
            attackCheckCollider.isTrigger = true; // 必须设为Trigger
        }
    }

    void Start()
    {
        ChangeState(PlayerState.Idle);
        attackTimer = 0;
    }

    void Update()
    {
        if (isDead) return;

        attackTimer = Mathf.Max(0, attackTimer - Time.deltaTime);
        UpdateCurrentState();
        FlipSprite();
    }

    void FixedUpdate()
    {
        if (isDead || currentState == PlayerState.Attack)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        rb.velocity = Vector2.Lerp(rb.velocity, moveDirection.normalized * moveSpeed, 0.1f);
    }
    #endregion

    #region 状态管理
    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                if (moveDirection.magnitude > 0.1f) ChangeState(PlayerState.Move);
                else if (attackTimer <= 0 && Input.GetMouseButtonDown(0)) ChangeState(PlayerState.Attack);
                break;
            case PlayerState.Move:
                if (moveDirection.magnitude < 0.1f) ChangeState(PlayerState.Idle);
                else if (attackTimer <= 0 && Input.GetMouseButtonDown(0)) ChangeState(PlayerState.Attack);
                break;
            case PlayerState.Attack:
                if (attackTimer <= 0)
                {
                    ChangeState(moveDirection.magnitude > 0.1f ? PlayerState.Move : PlayerState.Idle);
                }
                break;
        }
    }

    public void ChangeState(PlayerState newState)
    {
        ExitCurrentState();
        currentState = newState;
        EnterNewState(newState);
        anim?.SetInteger("State", (int)newState);
    }

    void ExitCurrentState() { }

    void EnterNewState(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Attack:
                // 获取当前武器冷却（优先武器自身，其次全局）
                float weaponCooldown = globalAttackCooldown;
                if (weaponManager?.GetCurrentWeaponData() != null)
                {
                    weaponCooldown = weaponManager.GetCurrentWeaponData().attackCooldown;
                }
                attackTimer = weaponCooldown;
                OnAttack(); // 执行攻击逻辑
                break;
            case PlayerState.Dead:
                isDead = true;
                rb.velocity = Vector2.zero;
                rb.simulated = false;
                weaponManager?.HideAllWeapons();
                Debug.Log("玩家死亡");
                break;
        }
    }
    #endregion

    #region 攻击核心逻辑（通用判定）
    void OnAttack()
    {
        if (isDead || weaponManager == null || attackCheckCollider == null) return;

        // 1. 获取当前武器数据
        WeaponData currentWeapon = weaponManager.GetCurrentWeaponData();
        if (currentWeapon == null) return;

        // 2. 触发武器攻击（特效/动画）
        weaponManager.TriggerWeaponAttack();

        // 3. 玩家攻击反馈
        sr.color = Color.yellow;
        Invoke(nameof(ResetPlayerColor), 0.2f);

        // 4. 通用攻击判定（依赖引擎内调整的Collider2D）
        // 临时启用碰撞体检测 → 检测后关闭（避免持续判定）
        attackCheckCollider.enabled = true;
        // 检测碰撞体内的敌人
        Collider2D[] hitEnemies = new Collider2D[10]; // 预设数组减少GC
        ContactFilter2D filter = new ContactFilter2D().NoFilter();
        int hitCount = Physics2D.OverlapCollider(attackCheckCollider, filter, hitEnemies);

        for (int i = 0; i < hitCount; i++)
        {
            var enemy = hitEnemies[i];
            if (enemy != null && enemy.CompareTag("Enemy"))
            {
                // 攻击敌人逻辑（可扩展伤害、击退等）
                Debug.Log($"[{currentWeapon.weaponType}] 击中敌人：{enemy.name}");
                SpriteRenderer enemySr = enemy.GetComponent<SpriteRenderer>();
                if (enemySr != null)
                {
                    enemySr.color = Color.red;
                    StartCoroutine(ResetEnemyColor(enemySr));
                }
            }
        }
        // 立即关闭判定碰撞体
        attackCheckCollider.enabled = false;
    }

    IEnumerator ResetEnemyColor(SpriteRenderer enemySr)
    {
        yield return new WaitForSeconds(0.2f);
        if (enemySr != null) enemySr.color = Color.green;
    }

    void ResetPlayerColor() => sr.color = Color.white;
    #endregion

    #region 输入与辅助逻辑
    void OnMove(InputValue value)
    {
        if (isDead) return;
        moveDirection = value.Get<Vector2>();
    }

    // PlayerStateMachine.cs 的 FlipSprite 方法修改后
    void FlipSprite()
    {
        if (isDead) return;
        if (Mathf.Abs(moveDirection.x) > 0.1f)
        {
            // 玩家精灵翻转
            sr.flipX = moveDirection.x < 0;

            // 同步攻击判定体朝向（原有逻辑保留）
            if (attackCheckCollider != null)
            {
                attackCheckCollider.transform.localScale = new Vector3(
                    Mathf.Abs(attackCheckCollider.transform.localScale.x) * (moveDirection.x < 0 ? -1 : 1),
                    attackCheckCollider.transform.localScale.y,
                    1
                );
            }

            // ========== 新增：同步武器翻转 ==========
            if (weaponManager != null)
            {
                weaponManager.SyncWeaponFlip(moveDirection.x < 0);
            }
        }
    }

    public void Die()
    {
        if (!isDead) ChangeState(PlayerState.Dead);
    }

    /// <summary>
    /// 重置攻击计时器（供武器切换调用）
    /// </summary>
    public void ResetAttackTimer()
    {
        attackTimer = 0;
        Debug.Log("PlayerStateMachine：攻击计时器已重置");
    }
    #endregion
}
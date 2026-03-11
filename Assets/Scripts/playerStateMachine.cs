using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 玩家状态枚举
public enum PlayerState
{
    Idle,    // 闲置
    Move,    // 移动
    Attack,  // 攻击
    Dead     // 死亡
}

public class PlayerStateMachine : MonoBehaviour
{
    #region 组件引用
    [Header("核心组件")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public Animator anim;
    public WeaponManager weaponManager;

    [Header("攻击检测")]
    [Tooltip("攻击判定碰撞体（需放在人物根节点，用于范围检测）")]
    public Collider2D attackCheckCollider;

    [Header("移动参数")]
    public float moveSpeed = 5f;          // 移动速度
    public float globalAttackCooldown = 0.5f; // 全局攻击冷却
    #endregion

    #region 状态机变量
    public PlayerState currentState;      // 当前状态
    private float attackTimer;            // 攻击冷却计时器
    private Vector2 moveDirection;        // 移动方向
    private bool isDead;                  // 是否死亡
    private bool isAttacking = false;     // 攻击锁定标记（防止重复攻击）
    private PlayerInput playerInput;      // 输入系统组件
    private InputAction moveAction;       // 移动动作
    private InputAction attackAction;     // 攻击动作
    #endregion

    #region 生命周期
    void Awake()
    {
        // 自动获取组件（空合并运算符优化）
        rb ??= GetComponent<Rigidbody2D>();
        sr ??= GetComponent<SpriteRenderer>();
        anim ??= GetComponent<Animator>();
        weaponManager ??= GetComponent<WeaponManager>();
        playerInput ??= GetComponent<PlayerInput>();

        // 初始化刚体参数
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        // 初始化攻击碰撞体
        if (attackCheckCollider != null)
        {
            attackCheckCollider.enabled = false;
            attackCheckCollider.isTrigger = true;
        }

        // 绑定输入动作
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            attackAction = playerInput.actions["Attack"];
        }
    }

    void Start()
    {
        // 初始状态设为闲置
        ChangeState(PlayerState.Idle);
        attackTimer = 0;
    }

    void Update()
    {
        // 死亡状态直接返回
        if (isDead) return;

        // 更新攻击冷却计时器
        attackTimer = Mathf.Max(0, attackTimer - Time.deltaTime);

        // 更新移动方向
        UpdateMoveDirection();

        // 更新当前状态
        UpdateCurrentState();

        // 翻转精灵
        FlipSprite();
    }

    void FixedUpdate()
    {
        // 死亡/攻击状态停止移动
        if (isDead || currentState == PlayerState.Attack)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        // 平滑移动（使用FixedDeltaTime保证帧率稳定）
        if (rb != null)
        {
            rb.velocity = Vector2.Lerp(
                rb.velocity,
                moveDirection.normalized * moveSpeed,
                Time.fixedDeltaTime * 10f // 插值系数，优化移动手感
            );
        }
    }
    #endregion

    #region 状态逻辑
    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                // 移动输入大于阈值，切换到移动状态
                if (moveDirection.magnitude > 0.1f)
                {
                    ChangeState(PlayerState.Move);
                }
                // 攻击冷却完成且有攻击输入，切换到攻击状态
                else if (attackTimer <= 0 && IsAttackInputPressed() && !isAttacking)
                {
                    isAttacking = true;
                    ChangeState(PlayerState.Attack);
                }
                break;

            case PlayerState.Move:
                // 移动输入小于阈值，切换到闲置状态
                if (moveDirection.magnitude < 0.1f)
                {
                    ChangeState(PlayerState.Idle);
                }
                // 攻击冷却完成且有攻击输入，切换到攻击状态
                else if (attackTimer <= 0 && IsAttackInputPressed() && !isAttacking)
                {
                    isAttacking = true;
                    ChangeState(PlayerState.Attack);
                }
                break;

            case PlayerState.Attack:
                // 攻击冷却结束，根据移动输入切换状态
                if (attackTimer <= 0)
                {
                    ChangeState(moveDirection.magnitude > 0.1f ? PlayerState.Move : PlayerState.Idle);
                }
                break;

            case PlayerState.Dead:
                break;
        }
    }

    /// <summary>
    /// 切换玩家状态
    /// </summary>
    /// <param name="newState">新状态</param>
    public void ChangeState(PlayerState newState)
    {
        ExitCurrentState();
        currentState = newState;
        EnterNewState(newState);

        // 更新动画状态
        if (anim != null)
        {
            anim.SetInteger("State", (int)newState);
        }
    }

    /// <summary>
    /// 退出当前状态时的逻辑
    /// </summary>
    void ExitCurrentState()
    {
        // 攻击状态退出时，仅重置标记（碰撞体由OnAttack的Invoke统一关闭）
        if (currentState == PlayerState.Attack)
        {
            isAttacking = false; // 提前解锁，避免状态切换延迟
        }
    }

    /// <summary>
    /// 进入新状态时的初始化逻辑
    /// </summary>
    /// <param name="newState">新状态</param>
    void EnterNewState(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Attack:
                // 获取最终攻击冷却（武器配置优先）
                float finalCooldown = globalAttackCooldown;
                if (weaponManager?.GetCurrentWeaponData() != null)
                {
                    finalCooldown = weaponManager.GetCurrentWeaponData().attackCooldown;
                }

                // 设置攻击冷却计时器
                attackTimer = finalCooldown;

                // 执行攻击逻辑
                OnAttack();

                // 延迟重置攻击标记（匹配冷却时间）
                Invoke(nameof(ResetAttackFlag), finalCooldown);
                break;

            case PlayerState.Dead:
                isDead = true;

                // 停止刚体运动
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.simulated = false;
                }

                // 隐藏所有武器
                weaponManager?.HideAllWeapons();

                Debug.Log("玩家已死亡");
                break;
        }
    }

    /// <summary>
    /// 重置攻击锁定标记（防止重复攻击）
    /// </summary>
    private void ResetAttackFlag()
    {
        isAttacking = false;

        // 重置武器动画Trigger（增加当前武器校验，避免操作错误对象）
        WeaponData currentWeaponData = weaponManager?.GetCurrentWeaponData();
        if (currentWeaponData == null || weaponManager.currentWeaponObj == null) return;

        Animator weaponAnim = weaponManager.currentWeaponObj.GetComponent<Animator>();
        if (weaponAnim != null && !string.IsNullOrEmpty(currentWeaponData.attackAnimTrigger))
        {
            weaponAnim.ResetTrigger(currentWeaponData.attackAnimTrigger);
        }
    }
    #endregion

    #region 攻击逻辑
    void OnAttack()
    {
        // 死亡/无武器管理器直接返回
        if (isDead || weaponManager == null) return;

        // 获取当前武器数据
        WeaponData currentWeapon = weaponManager.GetCurrentWeaponData();
        if (currentWeapon == null)
        {
            Debug.LogError("攻击失败：未获取到武器数据！");
            return;
        }

        

        // 播放武器攻击动画
        weaponManager.PlayWeaponAttackAnimation();

        // 启用攻击碰撞体（由武器自身的AttackCheck管理，移除重复的碰撞体控制）
        weaponManager.EnableAttackCollider(true);
        // 延迟关闭碰撞体（匹配武器攻击时长）
        Invoke(nameof(DisableWeaponAttackCollider), currentWeapon.attackDuration);
    }

    /// <summary>
    /// 关闭武器攻击碰撞体（统一由武器管理器管理）
    /// </summary>
    private void DisableWeaponAttackCollider()
    {
        weaponManager?.EnableAttackCollider(false);
    }
    #endregion

    #region 移动相关逻辑
    /// <summary>
    /// 更新移动方向（兼容InputSystem/旧输入系统）
    /// </summary>
    void UpdateMoveDirection()
    {
        if (moveAction != null)
        {
            moveDirection = moveAction.ReadValue<Vector2>();
        }
        else
        {
            moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }

    /// <summary>
    /// 检测攻击输入（兼容InputSystem/旧输入系统）
    /// </summary>
    /// <returns>是否按下攻击键</returns>
    bool IsAttackInputPressed()
    {
        if (attackAction != null)
        {
            return attackAction.WasPressedThisFrame();
        }
        else
        {
            return Input.GetMouseButtonDown(0);
        }
    }

    /// <summary>
    /// 翻转精灵（仅处理人物，武器由WeaponManager同步）
    /// </summary>
    void FlipSprite()
    {
        if (isDead || sr == null) return;

        // 仅水平移动时翻转
        if (Mathf.Abs(moveDirection.x) > 0.1f)
        {
            bool isFlipped = moveDirection.x < 0;
            sr.flipX = isFlipped;

            // 同步武器翻转（交给武器管理器处理）
            weaponManager?.SyncWeaponFlip(isFlipped);
        }
    }
    #endregion

    #region 外部调用方法
    /// <summary>
    /// 玩家死亡
    /// </summary>
    public void Die()
    {
        if (!isDead) ChangeState(PlayerState.Dead);
    }

    /// <summary>
    /// 重置攻击计时器
    /// </summary>
    public void ResetAttackTimer()
    {
        attackTimer = 0;
    }

    /// <summary>
    /// 输入系统回调的Move方法
    /// </summary>
    /// <param name="value">输入值</param>
    public void OnMove(InputValue value)
    {
        if (isDead) return;
        moveDirection = value.Get<Vector2>();
    }
    #endregion
}
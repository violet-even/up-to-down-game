using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // 这行是关键，IEnumerator 定义在这里

// 玩家状态枚举（核心：所有状态都在这里定义）
public enum PlayerState
{
    Idle,    // 地面闲置
    Move,    // 地面移动
    Attack,  // 攻击
    Dead     // 死亡
}

public class PlayerStateMachine : MonoBehaviour
{
    #region 组件引用（拖拽赋值）
    [Header("核心组件")]
    public Rigidbody2D rb;               // 玩家刚体
    public SpriteRenderer sr;            // 精灵渲染器（用于翻转）
    public Animator anim;                // 动画控制器（可选，后续加动画）
    #endregion

    #region 状态配置
    [Header("状态参数")]
    public PlayerState currentState;     // 当前状态
    public float moveSpeed = 5f;         // 移动速度
    public float attackCoolDown = 0.5f;  // 攻击冷却（防止连点）
    private float attackTimer;           // 攻击冷却计时器
    private Vector2 moveDirection;       // 移动方向
    private bool isDead = false;         // 是否死亡
    #endregion

    #region 生命周期
    void Awake()
    {
        // 自动获取组件（避免漏拖）
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();
        

        // 刚体基础设置
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // 初始状态：闲置
        ChangeState(PlayerState.Idle);
        attackTimer = 0;
    }

    void Update()
    {
        // 死亡状态：直接返回，禁用所有逻辑
        if (isDead) return;

        // 攻击冷却计时
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // 状态更新（核心：每个帧都处理当前状态的逻辑）
        UpdateCurrentState();

        // 翻转逻辑（根据移动/攻击方向）
        FlipSprite();
    }

    void FixedUpdate()
    {
        // 死亡/攻击状态：不处理移动物理
        if (isDead || currentState == PlayerState.Attack) return;

        // 物理移动（缓动更丝滑）
        rb.velocity = Vector2.Lerp(rb.velocity, moveDirection.normalized * moveSpeed, 0.1f);
    }
    #endregion

    #region 状态核心逻辑
    // 更新当前状态（分状态处理）
    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                HandleIdleState();
                break;
            case PlayerState.Move:
                HandleMoveState();
                break;
            case PlayerState.Attack:
                HandleAttackState();
                break;
            case PlayerState.Dead:
                HandleDeadState();
                break;
        }
    }

    // 切换状态（统一管理状态进入/退出）
    void ChangeState(PlayerState newState)
    {
        // 退出当前状态
        ExitCurrentState();

        // 更新状态
        currentState = newState;

        // 进入新状态
        EnterNewState(newState);

        // 更新动画（可选，后续加动画时用）
        if (anim != null) anim.SetInteger("State", (int)newState);
    }

    // 退出当前状态的逻辑
    void ExitCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Attack:
                // 攻击结束：恢复移动
                break;
        }
    }

    // 进入新状态的逻辑
    void EnterNewState(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Idle:
                break;
            case PlayerState.Move:
                break;
            case PlayerState.Attack:
                // 攻击开始：重置冷却、禁用移动
                attackTimer = attackCoolDown;
                break;
            case PlayerState.Dead:
                // 死亡：禁用刚体、隐藏武器、播放死亡动画
                isDead = true;
                rb.velocity = Vector2.zero;
                rb.simulated = false;
               
                break;
        }
    }
    #endregion

    #region 各状态具体逻辑
    // 闲置状态逻辑
    void HandleIdleState()
    {
        // 有移动输入→切移动状态
        if (moveDirection.magnitude > 0.1f)
        {
            ChangeState(PlayerState.Move);
        }
        // 攻击冷却结束+有攻击输入→切攻击状态
        else if (attackTimer <= 0 && Input.GetMouseButtonDown(0))
        {
            OnAttack();
            ChangeState(PlayerState.Attack);
        }
    }

    // 移动状态逻辑
    void HandleMoveState()
    {
        // 无移动输入→切闲置状态
        if (moveDirection.magnitude < 0.1f)
        {
            ChangeState(PlayerState.Idle);
        }
        // 攻击冷却结束+有攻击输入→切攻击状态
        else if (attackTimer <= 0 && Input.GetMouseButtonDown(0))
        {
            OnAttack();
            ChangeState(PlayerState.Attack);
        }
    }

    // 攻击状态逻辑
    void HandleAttackState()
    {
        // 攻击冷却结束→切回闲置/移动状态
        if (attackTimer <= 0)
        {
            if (moveDirection.magnitude > 0.1f)
                ChangeState(PlayerState.Move);
            else
                ChangeState(PlayerState.Idle);
        }
    }

    // 死亡状态逻辑
    void HandleDeadState()
    {
        // 死亡后无逻辑，锁定状态
    }
    #endregion

    #region 输入与辅助方法
    // Input System的移动回调（和之前的OnMove一致）
    void OnMove(InputValue value)
    {
        if (isDead) return;
        moveDirection = value.Get<Vector2>();
    }

    // 攻击逻辑（含命中检测）
    void OnAttack()
    {
        if (isDead) return;

        // 攻击反馈：玩家精灵变色
        sr.color = Color.yellow;
        Invoke("ResetColor", 0.2f);

        // 攻击命中检测
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                Debug.Log("命中怪物！");
                SpriteRenderer enemySr = enemy.GetComponent<SpriteRenderer>();
                if (enemySr != null)
                {
                    enemySr.color = Color.red;
                    // 用协程延迟恢复颜色
                    StartCoroutine(ResetEnemyColorCoroutine(enemySr));
                }
            }
        }
    }
    // 协程：延迟恢复怪物颜色
    IEnumerator ResetEnemyColorCoroutine(SpriteRenderer enemySr)
    {
        yield return new WaitForSeconds(0.2f); // 等待0.2秒
        if (enemySr != null) // 防止怪物在等待期间被销毁
        {
            enemySr.color = Color.green;
        }
    }

    // 新增：独立的怪物颜色恢复方法（接收SpriteRenderer参数）
    void ResetEnemyColor(SpriteRenderer enemySr)
    {
        if (enemySr != null) // 防止怪物被销毁导致空引用
        {
            enemySr.color = Color.green;
        }
    }

    // 翻转精灵（核心：根据移动/攻击方向）
    void FlipSprite()
    {
        if (isDead) return;

        // 有移动方向→按移动方向翻转
        if (moveDirection.magnitude > 0.1f)
        {
            sr.flipX = moveDirection.x < 0; // x<0（左移）→翻转X轴
        }
        // 无移动方向→保持上次翻转状态（比如攻击时朝向不变）
    }

    // 恢复精灵颜色
    void ResetColor()
    {
        sr.color = Color.white; // 恢复默认颜色（可改成你的角色原色）
    }

    // 外部调用：触发死亡（比如血量为0时调用）
    public void Die()
    {
        if (isDead) return;
        ChangeState(PlayerState.Dead);
        Debug.Log("玩家死亡！");
    }
    #endregion
}
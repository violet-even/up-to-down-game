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
    [Tooltip("攻击判定区域（需挂载到刀预制体下，调整到刀刃位置）")]
    public Collider2D attackCheckCollider;

    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float globalAttackCooldown = 0.5f;
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

        if (attackCheckCollider != null)
        {
            attackCheckCollider.enabled = false;
            attackCheckCollider.isTrigger = true;
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
                float weaponCooldown = globalAttackCooldown;
                if (weaponManager?.GetCurrentWeaponData() != null)
                {
                    weaponCooldown = weaponManager.GetCurrentWeaponData().attackCooldown;
                }
                attackTimer = weaponCooldown;
                OnAttack();
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

    #region 攻击核心逻辑
    void OnAttack()
    {
        // 【关键修复】移除了attackCheckCollider的空判断，因为它已经移到刀的预制体里了
        if (isDead || weaponManager == null) return;

        // 获取当前武器数据
        WeaponData currentWeapon = weaponManager.GetCurrentWeaponData();
        if (currentWeapon == null)
        {
            Debug.LogError("OnAttack：获取不到武器数据！");
            return;
        }

        // 播放武器攻击动画
        Debug.Log("OnAttack：准备播放攻击动画");
        weaponManager.PlayWeaponAttackAnimation();

        // 攻击冷却
        float finalCooldown = globalAttackCooldown;
        if (currentWeapon != null) finalCooldown = currentWeapon.attackCooldown;
        attackTimer = finalCooldown;
    }
    #endregion

    #region 输入与辅助逻辑
    void OnMove(InputValue value)
    {
        if (isDead) return;
        moveDirection = value.Get<Vector2>();
    }

    void FlipSprite()
    {
        if (isDead) return;
        if (Mathf.Abs(moveDirection.x) > 0.1f)
        {
            bool isFlipped = moveDirection.x < 0;
            sr.flipX = isFlipped;

            // 同步武器翻转和层级
            if (weaponManager != null)
            {
                weaponManager.SyncWeaponFlip(isFlipped);
            }

            // 核心：同步 AttackCheck 朝向（跟随刀刃）
            if (attackCheckCollider != null)
            {
                // 不再缩放，而是直接翻转 AttackCheck 的 X 轴
                attackCheckCollider.transform.localScale = new Vector3(
                    Mathf.Abs(attackCheckCollider.transform.localScale.x),
                    attackCheckCollider.transform.localScale.y,
                    1
                );
                attackCheckCollider.transform.rotation = Quaternion.Euler(0, isFlipped ? 180 : 0, 0);
            }
        }
    }

    public void Die()
    {
        if (!isDead) ChangeState(PlayerState.Dead);
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0;
    }
    #endregion
}
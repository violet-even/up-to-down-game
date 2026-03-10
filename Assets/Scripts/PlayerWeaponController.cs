using System;
using UnityEngine;

/// <summary>
/// 玩家武器控制层，处理切换逻辑、解耦输入/数据/状态机
/// </summary>
public class PlayerWeaponController : MonoBehaviour
{
    // 依赖组件
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private WeaponManager weaponManager;

    // 切换配置
    [SerializeField] private float switchDuration = 0.3f;
    private bool _isSwitching;
    private float _switchTimer;

    private void Awake()
    {
        // 自动获取依赖组件
        stateMachine ??= GetComponent<PlayerStateMachine>();
        weaponManager ??= GetComponent<WeaponManager>();
    }

    private void Start()
    {
        // 订阅输入事件
        var inputHandler = FindFirstObjectByType<WeaponInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnWeaponSwitchRequest += HandleSwitchRequest;
            inputHandler.OnScrollSwitchRequest += HandleScrollSwitch;
            Debug.Log("PlayerWeaponController：成功订阅输入事件");
        }
        else
        {
            Debug.LogWarning("PlayerWeaponController：未找到WeaponInputHandler组件！");
        }

        // 订阅库存变更事件
        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnWeaponSwitched += HandleWeaponSwitched;
            Debug.Log("PlayerWeaponController：成功订阅WeaponInventory事件");
        }
        else
        {
            Debug.LogWarning("PlayerWeaponController：未找到WeaponInventory单例！");
        }
    }

    private void Update()
    {
        // 切换计时逻辑
        if (_isSwitching)
        {
            _switchTimer -= Time.deltaTime;
            if (_switchTimer <= 0)
            {
                _isSwitching = false;
                weaponManager.ShowCurrentWeapon(); // 切换完成显示武器
                Debug.Log("PlayerWeaponController：武器切换完成");
            }
        }
    }

    /// <summary>
    /// 处理指定槽位的切换请求
    /// </summary>
    private void HandleSwitchRequest(WeaponInventory.WeaponSlot targetSlot)
    {
        if (_isSwitching || WeaponInventory.Instance == null)
        {
            Debug.LogWarning("PlayerWeaponController：切换请求被拒绝（切换中/库存为空）");
            return;
        }

        // 打断当前攻击并重置状态
        if (stateMachine.currentState == PlayerState.Attack)
        {
            stateMachine.ChangeState(PlayerState.Idle);
            stateMachine.ResetAttackTimer(); // 重置攻击计时器
            Debug.Log("PlayerWeaponController：攻击状态已打断");
        }

        // 开始切换流程
        StartSwitching();
        WeaponInventory.Instance.SwitchActiveSlot(targetSlot);
    }

    /// <summary>
    /// 处理滚轮切换（下一个槽位）
    /// </summary>
    private void HandleScrollSwitch()
    {
        if (_isSwitching || WeaponInventory.Instance == null)
        {
            Debug.LogWarning("PlayerWeaponController：滚轮切换请求被拒绝（切换中/库存为空）");
            return;
        }

        // 打断当前攻击并重置状态
        if (stateMachine.currentState == PlayerState.Attack)
        {
            stateMachine.ChangeState(PlayerState.Idle);
            stateMachine.ResetAttackTimer();
            Debug.Log("PlayerWeaponController：攻击状态已打断（滚轮切换）");
        }

        // 开始切换流程
        StartSwitching();
        WeaponInventory.Instance.SwitchToNextSlot();
    }

    /// <summary>
    /// 处理武器切换完成（库存事件回调）
    /// </summary>
    private void HandleWeaponSwitched(WeaponData newWeapon, WeaponInventory.WeaponSlot activeSlot)
    {
        if (newWeapon == null)
        {
            Debug.LogWarning("PlayerWeaponController：切换的武器数据为空！");
            _isSwitching = false; // 重置切换状态，避免卡死
            return;
        }

        // 切换时先隐藏武器（避免穿模）
        weaponManager.HideCurrentWeapon();
        // 调用武器管理器装备新武器
        weaponManager.EquipWeapon(newWeapon.weaponType);
        Debug.Log($"PlayerWeaponController：装备新武器：{newWeapon.weaponType}");
    }

    /// <summary>
    /// 开始切换计时
    /// </summary>
    private void StartSwitching()
    {
        _isSwitching = true;
        _switchTimer = switchDuration;
        Debug.Log("PlayerWeaponController：开始武器切换，时长：" + switchDuration + "秒");
    }

    private void OnDestroy()
    {
        // 取消事件订阅（防止内存泄漏）
        var inputHandler = FindFirstObjectByType<WeaponInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnWeaponSwitchRequest -= HandleSwitchRequest;
            inputHandler.OnScrollSwitchRequest -= HandleScrollSwitch;
        }

        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnWeaponSwitched -= HandleWeaponSwitched;
        }
    }
}
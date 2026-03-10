using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器切换输入处理（纯输入层），处理滚轮/数字键输入，带死区和防抖
/// </summary>
public class WeaponInputHandler : MonoBehaviour
{
    // 输入动作映射（需在Input System中配置）
    [SerializeField] private InputActionReference scrollWheel;
    [SerializeField] private InputActionReference switchPrimary;
    [SerializeField] private InputActionReference switchSecondary;

    // 滚轮死区（过滤微小输入）
    [SerializeField, Range(0.01f, 0.5f)] private float scrollDeadZone = 0.1f;
    // 滚轮累积阈值（防抖动，需累积一定值才触发）
    [SerializeField, Range(0.1f, 1f)] private float scrollAccumulateThreshold = 0.3f;
    // 输入冷却（防快速重复触发）
    [SerializeField] private float inputCooldown = 0.1f;

    // 切换请求事件（参数：目标槽位）
    public event Action<WeaponInventory.WeaponSlot> OnWeaponSwitchRequest;
    // 滚轮切换请求（无参数，切换下一个槽位）
    public event Action OnScrollSwitchRequest;

    private float _scrollAccumulator;
    private float _lastInputTime;

    private void OnEnable()
    {
        // 订阅输入事件
        scrollWheel.action.performed += OnScrollPerformed;
        switchPrimary.action.performed += _ => OnSwitchPrimary();
        switchSecondary.action.performed += _ => OnSwitchSecondary();
    }

    private void OnDisable()
    {
        // 取消订阅（防止内存泄漏）
        scrollWheel.action.performed -= OnScrollPerformed;
        switchPrimary.action.performed -= _ => OnSwitchPrimary();
        switchSecondary.action.performed -= _ => OnSwitchSecondary();
    }

    /// <summary>
    /// 处理滚轮输入（带死区+累积阈值+方向防抖）
    /// </summary>
    private void OnScrollPerformed(InputAction.CallbackContext ctx)
    {
        // 输入冷却过滤
        if (Time.time - _lastInputTime < inputCooldown) return;

        var scrollValue = ctx.ReadValue<Vector2>().y;
        // 死区过滤：忽略微小输入
        if (Mathf.Abs(scrollValue) < scrollDeadZone) return;

        // 优化累加逻辑：同方向累加，反向重置
        if (Mathf.Sign(scrollValue) == Mathf.Sign(_scrollAccumulator) || _scrollAccumulator == 0)
        {
            _scrollAccumulator += scrollValue;
        }
        else
        {
            _scrollAccumulator = scrollValue;
        }

        // 达到阈值触发切换
        if (Mathf.Abs(_scrollAccumulator) >= scrollAccumulateThreshold)
        {
            OnScrollSwitchRequest?.Invoke();
            _scrollAccumulator = 0;
            _lastInputTime = Time.time;
        }
    }

    /// <summary>
    /// 切换主手武器（数字键1）
    /// </summary>
    private void OnSwitchPrimary()
    {
        if (Time.time - _lastInputTime < inputCooldown) return;
        OnWeaponSwitchRequest?.Invoke(WeaponInventory.WeaponSlot.Primary);
        _lastInputTime = Time.time;
    }

    /// <summary>
    /// 切换副手武器（数字键2）
    /// </summary>
    private void OnSwitchSecondary()
    {
        if (Time.time - _lastInputTime < inputCooldown) return;
        OnWeaponSwitchRequest?.Invoke(WeaponInventory.WeaponSlot.Secondary);
        _lastInputTime = Time.time;
    }
}
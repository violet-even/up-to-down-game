using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// 武器管理器 - 负责武器实例化、切换、动画触发
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("武器挂载点")]
    [Tooltip("所有武器预制体都会实例化到该挂载点下")]
    public Transform weaponPoint;
    [Header("武器配置")]
    [Tooltip("所有可用的武器数据配置（需和WeaponInventory一致）")]
    public WeaponData[] allWeapons;
    [Header("当前状态")]
    [Tooltip("当前装备的武器类型")]
    public WeaponType currentWeaponType;
    // 当前实例化的武器对象
    private GameObject currentWeaponObj;
    // 武器类型与数据的映射表（优化查找性能）
    private Dictionary<WeaponType, WeaponData> weaponDataMap;

    private void Awake()
    {
        InitWeaponDataMap();
    }

    private void Start()
    {
        // 强制初始化小刀，保证测试可用
        EquipWeapon(WeaponType.Knife);
        Debug.Log("WeaponManager：已初始化小刀武器");
    }

    /// <summary>
    /// 初始化武器数据映射表
    /// </summary>
    private void InitWeaponDataMap()
    {
        weaponDataMap = new Dictionary<WeaponType, WeaponData>();
        if (allWeapons == null || allWeapons.Length == 0)
        {
            Debug.LogError("WeaponManager：请在Inspector面板配置allWeapons武器数据！");
            return;
        }
        foreach (var weapon in allWeapons)
        {
            if (weapon == null) continue;
            if (!weaponDataMap.ContainsKey(weapon.weaponType))
            {
                weaponDataMap.Add(weapon.weaponType, weapon);
            }
        }
    }

    /// <summary>
    /// 装备指定类型的武器
    /// </summary>
    public void EquipWeapon(WeaponType type)
    {
        if (currentWeaponType == type && currentWeaponObj != null) return;

        DestroyCurrentWeapon();

        if (!weaponDataMap.TryGetValue(type, out WeaponData targetWeapon))
        {
            Debug.LogError($"WeaponManager：找不到{type}的武器配置！");
            currentWeaponType = WeaponType.None;
            return;
        }

        SpawnWeapon(targetWeapon);
        currentWeaponType = type;
        Debug.Log($"WeaponManager：成功装备{type}");
    }

    /// <summary>
    /// 销毁当前武器
    /// </summary>
    private void DestroyCurrentWeapon()
    {
        if (currentWeaponObj != null)
        {
            Destroy(currentWeaponObj);
            currentWeaponObj = null;
        }
    }

    /// <summary>
    /// 实例化武器
    /// </summary>
    private void SpawnWeapon(WeaponData weaponData)
    {
        if (weaponData.weaponPrefab == null)
        {
            Debug.LogError($"WeaponManager：{weaponData.weaponType}的预制体为空！");
            return;
        }
        currentWeaponObj = Instantiate(weaponData.weaponPrefab, weaponPoint);
        currentWeaponObj.transform.localPosition = Vector3.zero;
        currentWeaponObj.transform.localRotation = Quaternion.identity;
        currentWeaponObj.transform.localScale = Vector3.one; // 缩放你可以自己改
    }

    /// <summary>
    /// 【核心修复】播放武器攻击动画，保证能触发
    /// </summary>
    public void PlayWeaponAttackAnimation()
    {
        if (currentWeaponObj == null)
        {
            Debug.LogError("WeaponManager：当前没有武器实例，无法播放动画！");
            return;
        }

        // 获取武器上的Animator
        Animator weaponAnim = currentWeaponObj.GetComponent<Animator>();
        if (weaponAnim == null)
        {
            Debug.LogError($"WeaponManager：{currentWeaponType}的预制体上没有Animator组件！");
            return;
        }

        // 获取武器数据
        if (!weaponDataMap.TryGetValue(currentWeaponType, out WeaponData data)) return;

        // 触发动画，先重置再触发，避免动画不播放
        if (!string.IsNullOrEmpty(data.attackAnimTrigger))
        {
            weaponAnim.ResetTrigger(data.attackAnimTrigger);
            weaponAnim.SetTrigger(data.attackAnimTrigger);
            Debug.Log($"WeaponManager：已触发{data.attackAnimTrigger}攻击动画");
        }
        else
        {
            Debug.LogError("WeaponManager：武器数据里没配置attackAnimTrigger触发器名称！");
        }
    }

    /// <summary>
    /// 【动画事件用】开启/关闭攻击判定
    /// </summary>
    public void EnableAttackCollider(bool isEnable)
    {
        if (currentWeaponObj == null) return;

        Transform attackCheck = currentWeaponObj.transform.Find("AttackCheck");
        if (attackCheck != null)
        {
            Collider2D col = attackCheck.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = isEnable;
                Debug.Log($"攻击判定：{(isEnable ? "开启" : "关闭")}");
            }
        }
    }

    /// <summary>
    /// 获取当前武器数据
    /// </summary>
    public WeaponData GetCurrentWeaponData()
    {
        weaponDataMap.TryGetValue(currentWeaponType, out var data);
        return data;
    }

    // 以下是兼容原有逻辑的方法
    public void SyncWeaponFlip(bool isFlipped)
    {
        if (currentWeaponObj == null) return;
        SpriteRenderer sr = currentWeaponObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = isFlipped;
            sr.sortingOrder = isFlipped ? -1 : 1;
        }
    }
    public void HideCurrentWeapon() => currentWeaponObj?.SetActive(false);
    public void ShowCurrentWeapon() => currentWeaponObj?.SetActive(true);
    public void HideAllWeapons() => HideCurrentWeapon();
}
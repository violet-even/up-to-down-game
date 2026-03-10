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
        // 初始化武器数据映射表
        InitWeaponDataMap();
    }

    private void Start()
    {
        // 初始化武器数据映射表
        InitWeaponDataMap();
        // 强制实例化 Knife（忽略库存，测试用）
        EquipWeapon(WeaponType.Knife);
        Debug.Log("尝试实例化 Knife，检查 All Weapons 配置！");
    }

    /// <summary>
    /// 初始化武器数据映射表（避免重复遍历数组）
    /// </summary>
    private void InitWeaponDataMap()
    {
        weaponDataMap = new Dictionary<WeaponType, WeaponData>();

        if (allWeapons == null || allWeapons.Length == 0)
        {
            Debug.LogWarning("WeaponManager：未配置任何武器数据！");
            return;
        }

        foreach (var weapon in allWeapons)
        {
            if (weapon == null)
            {
                Debug.LogWarning("WeaponManager：检测到空的武器数据配置项！");
                continue;
            }

            if (!weaponDataMap.ContainsKey(weapon.weaponType))
            {
                weaponDataMap.Add(weapon.weaponType, weapon);
            }
            else
            {
                Debug.LogWarning($"WeaponManager：重复的武器类型配置：{weapon.weaponType}，已忽略重复项");
            }
        }
    }


    /// <summary>
    /// 装备指定类型的武器
    /// </summary>
    /// <param name="type">要装备的武器类型</param>
    public void EquipWeapon(WeaponType type)
    {
        // 避免重复装备同一武器
        if (currentWeaponType == type && currentWeaponObj != null)
        {
            return;
        }

        // 销毁当前武器对象
        DestroyCurrentWeapon();

        // 获取目标武器数据
        if (!weaponDataMap.TryGetValue(type, out WeaponData targetWeapon))
        {
            Debug.LogWarning($"WeaponManager：未找到武器配置：{type}，请检查allWeapons数组");
            currentWeaponType = WeaponType.None;
            return;
        }

        // 实例化并挂载武器
        SpawnWeapon(targetWeapon);

        // 更新当前武器类型
        currentWeaponType = type;
        Debug.Log($"WeaponManager：成功装备武器：{type}");
    }

    /// <summary>
    /// 销毁当前装备的武器对象
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
    /// 生成并挂载武器预制体
    /// </summary>
    /// <param name="weaponData">武器数据</param>
    private void SpawnWeapon(WeaponData weaponData)
    {
        if (weaponData.weaponPrefab == null)
        {
            Debug.LogWarning($"WeaponManager：武器{weaponData.weaponType}的预制体未配置！");
            return;
        }

        // 实例化武器预制体
        currentWeaponObj = Instantiate(weaponData.weaponPrefab, weaponPoint);
        // 重置本地位置和旋转（避免偏移）
        currentWeaponObj.transform.localPosition = Vector3.zero;
        currentWeaponObj.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 触发当前武器的攻击动画
    /// </summary>
    public void TriggerWeaponAttack()
    {
        if (currentWeaponObj == null) return;

        // 获取当前武器数据
        if (!weaponDataMap.TryGetValue(currentWeaponType, out WeaponData currentWeaponData))
        {
            return;
        }

        // 触发攻击动画
        if (!string.IsNullOrEmpty(currentWeaponData.attackAnimTrigger))
        {
            Animator weaponAnim = currentWeaponObj.GetComponent<Animator>();
            if (weaponAnim != null)
            {
                weaponAnim.SetTrigger(currentWeaponData.attackAnimTrigger);
            }
            else
            {
                Debug.LogWarning($"WeaponManager：武器{currentWeaponType}缺少Animator组件！");
            }
        }
    }

    /// <summary>
    /// 获取当前装备的武器数据（给PlayerStateMachine调用）
    /// </summary>
    /// <returns>当前武器数据（无则返回null）</returns>
    public WeaponData GetCurrentWeaponData()
    {
        weaponDataMap.TryGetValue(currentWeaponType, out WeaponData currentWeaponData);
        return currentWeaponData;
    }

    /// <summary>
    /// 隐藏当前装备的武器
    /// </summary>
    public void HideCurrentWeapon()
    {
        if (currentWeaponObj != null)
        {
            currentWeaponObj.SetActive(false);
        }
    }

    /// <summary>
    /// 显示当前装备的武器
    /// </summary>
    public void ShowCurrentWeapon()
    {
        if (currentWeaponObj != null)
        {
            currentWeaponObj.SetActive(true);
        }
    }

    /// <summary>
    /// 切换武器显示/隐藏状态
    /// </summary>
    /// <param name="isVisible">是否显示</param>
    public void SetWeaponVisibility(bool isVisible)
    {
        if (currentWeaponObj != null)
        {
            currentWeaponObj.SetActive(isVisible);
        }
    }

    // WeaponManager.cs 中新增以下代码（放在类内任意位置）

    /// <summary>
    /// 同步武器翻转 + 层级排序
    /// </summary>
    /// <param name="isFlipped">是否朝左（true=左，false=右）</param>
    public void SyncWeaponFlip(bool isFlipped)
    {
        if (currentWeaponObj == null) return;

        SpriteRenderer weaponSr = currentWeaponObj.GetComponent<SpriteRenderer>();
        if (weaponSr != null)
        {
            weaponSr.flipX = isFlipped;

            // ========== 新增：根据朝向调整层级 ==========
            if (isFlipped)
            {
                // 朝左：刀在玩家身后（Order in Layer 比玩家小）
                weaponSr.sortingOrder = -1;
            }
            else
            {
                // 朝右：刀在玩家身前（Order in Layer 比玩家大）
                weaponSr.sortingOrder = 1;
            }
        }
    }
    /// <summary>
    /// 验证武器配置的完整性（编辑器右键调用）
    /// </summary>
    [ContextMenu("验证武器配置")]
    public void ValidateWeaponConfig()
    {
        InitWeaponDataMap();
        Debug.Log($"WeaponManager：武器配置验证完成，共加载{weaponDataMap.Count}种有效武器");

        foreach (var weaponType in System.Enum.GetValues(typeof(WeaponType)))
        {
            if (weaponType.ToString() == "None") continue;

            if (!weaponDataMap.ContainsKey((WeaponType)weaponType))
            {
                Debug.LogWarning($"WeaponManager：缺少武器类型配置：{weaponType}");
            }
        }
    }

    // 兼容原有HideAllWeapons方法（避免PlayerStateMachine报错）
    public void HideAllWeapons()
    {
        HideCurrentWeapon();
    }
}
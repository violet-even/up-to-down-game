using UnityEngine;

/// <summary>
/// 武器类型枚举（全局唯一）
/// </summary>
public enum WeaponType
{
    None,    // 无武器
    Knife,   // 小刀
    Sword,   // 剑
    Stick,   // 棍子
    Gun      // 枪
}

/// <summary>
/// 武器数据结构（全局唯一，所有模块共用）
/// </summary>
[System.Serializable]
public class WeaponData
{
    [Header("基础配置")]
    public WeaponType weaponType;       // 武器类型
    public GameObject weaponPrefab;     // 武器预制体
    public string attackAnimTrigger;    // 攻击动画触发器名称

    [Header("战斗属性")]
    public int attackDamage = 10;       // 攻击力
    public float attackRange;           // 攻击范围（备用）
    public float attackAngle;           // 攻击角度（备用）
    public float attackCooldown = 0.5f; // 攻击冷却时间
    public bool isCircleAttack;         // 是否圆形攻击（备用）
}
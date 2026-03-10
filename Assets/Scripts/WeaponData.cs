using UnityEngine;

/// <summary>
/// 武器类型枚举
/// </summary>
public enum WeaponType
{
    None,
    Knife
}

/// <summary>
/// 武器数据（可在Project面板右键创建）
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "GameData/WeaponData", order = 1)]
public class WeaponData : ScriptableObject
{
    [Header("基础配置")]
    public WeaponType weaponType;
    public GameObject weaponPrefab;
    [Tooltip("必须和Animator里的Trigger名字完全一致")]
    public string attackAnimTrigger = "Attack";

    [Header("战斗属性")]
    public int attackDamage = 1;
    public float attackCooldown = 0.5f;
}
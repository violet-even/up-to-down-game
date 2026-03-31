using UnityEngine;

/// <summary>
/// 武器管理器：控制武器的显示/隐藏、切换（适配刀为第一把武器）
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("核心配置 - 拖拽赋值")]
    [Tooltip("武器挂载点（Player下的WeaponPoint对象）")]
    public Transform weaponPoint; // 武器挂载点（刀粘在玩家手上的锚点）

    [Tooltip("刀的武器对象（WeaponPoint下的Weapon_Knife实例）")]
    public GameObject weaponKnife; // 第一把武器：刀

    [Header("默认配置")]
    [Tooltip("初始默认武器（当前为刀）")]
    public string currentWeapon = "Knife";

    /// <summary>
    /// 初始化：隐藏所有武器，显示默认武器
    /// </summary>
    void Start()
    {
        // 初始隐藏所有武器，避免多武器重叠
        HideAllWeapons();
        // 显示默认武器（刀）
        ShowWeapon(currentWeapon);
    }

    /// <summary>
    /// 每帧检测：测试按键切换武器（可后续扩展）
    /// </summary>
    void Update()
    {
        // 按数字键1：切换到刀（测试用，所见即所得）
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon("Knife");
        }

        // 后续加新武器（如棍、剑）可在此加按键：
        // if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon("Stick");
    }

    /// <summary>
    /// 切换武器核心方法（外部可调用）
    /// </summary>
    /// <param name="weaponName">要切换的武器名称（如"Knife"）</param>
    public void SwitchWeapon(string weaponName)
    {
        // 更新当前武器名称
        currentWeapon = weaponName;
        // 先隐藏所有武器，再显示目标武器
        HideAllWeapons();
        ShowWeapon(weaponName);
        // 控制台提示（调试用，可删除）
        Debug.Log("当前武器：" + currentWeapon);
    }

    /// <summary>
    /// 隐藏所有武器（Public：让玩家状态机死亡时调用）
    /// </summary>
    public void HideAllWeapons()
    {
        // 隐藏刀（后续加新武器，在此加一行：weaponStick.SetActive(false);）
        if (weaponKnife != null)
        {
            weaponKnife.SetActive(false);
        }
    }

    /// <summary>
    /// 显示指定武器
    /// </summary>
    /// <param name="weaponName">武器名称</param>
    void ShowWeapon(string weaponName)
    {
        switch (weaponName)
        {
            case "Knife": // 刀
                if (weaponKnife != null)
                {
                    weaponKnife.SetActive(true);
                }
                break;

                // 后续加新武器（如棍）：
                // case "Stick":
                //     if (weaponStick != null) weaponStick.SetActive(true);
                //     break;
        }
    }
}
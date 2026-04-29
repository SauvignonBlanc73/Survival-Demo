using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// 1. 枚举名单 (7种增益)
public enum UpgradeType
{
    MoveSpeed,          // 活性
    FireRate,           // 准心
    AoERadius,          // 观测
    ChargeBonus,        // 聚能
    Stun,               // 失真
    HpNerf,             // 解构
    EasterEgg_EternalCharge // 回路
}

[System.Serializable]
public class UpgradeOption
{
    public string title;
    public string description;
    public UpgradeType type;
    public float weight;
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("UI 引用")]
    public GameObject upgradePanel;
    public GameObject contentArea;
    public UpgradeButton[] optionButtons; // 这里在面板里设为 2 个

    public Button confirmButton;
    public TextMeshProUGUI countdownText;

    [Header("升级池")]
    public List<UpgradeOption> upgradePool = new List<UpgradeOption>();
    public UpgradeOption eternalChargeEgg;

    [Header("局内能力全局变量")]
    public static float chargeBonusChance = 0f;
    public static float stunChance = 0f;
    public static int hpNerfStacks = 0;

    // 1. 在 UpgradeManager 顶部的全局变量区，加一行：
    public static bool hasEasterEgg = false;

    private UpgradeOption currentSelectedOption;

    void Awake() { Instance = this; }

    public void OpenUpgradeMenu()
    {
        Time.timeScale = 0f;
        upgradePanel.SetActive(true);
        contentArea.SetActive(true);

        countdownText.gameObject.SetActive(false);
        confirmButton.interactable = false; // 初始禁用确定按钮
        confirmButton.gameObject.SetActive(true);
        currentSelectedOption = null;

        List<UpgradeOption> selectedOptions = GenerateTwoOptions();
        for (int i = 0; i < 2; i++)
        {
            optionButtons[i].Setup(selectedOptions[i]);
            optionButtons[i].Highlight(false);
        }
    }

    List<UpgradeOption> GenerateTwoOptions()
    {
        List<UpgradeOption> results = new List<UpgradeOption>();
        int safetyNet = 0;
        while (results.Count < 2 && safetyNet < 100)
        {
            UpgradeOption choice = RollOneOption();
            if (!results.Contains(choice))
            {
                results.Add(choice);
            }
            safetyNet++;
        }
        return results;
    }

    UpgradeOption RollOneOption()
    {
        float roll = Random.Range(0f, 100f);

        // 🔥 新增 && !hasEasterEgg：如果已经拿过彩蛋，这个 2% 就失效了
        if (roll < 2f && eternalChargeEgg != null && !hasEasterEgg) return eternalChargeEgg;

        return upgradePool[Random.Range(0, upgradePool.Count)];
    }

    // 玩家点击了某张卡片
    public void SelectOption(UpgradeButton button, UpgradeOption data)
    {
        // 1. 执行原有的增益逻辑（增加射速、血量等）
        ApplyUpgrade(data);

        // 2. 立即关闭升级面板，防止挡住倒计时和视野
        upgradePanel.SetActive(false);

        // 3. 🔥 关键复用：直接调用 UIManager 已经写好的倒计时恢复逻辑
        // 它会自动处理：显示倒计时 UI -> 3,2,1 -> GO -> Time.timeScale = 1
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResumeGame();
        }
    }

    // 玩家点击了【确定】按钮
    public void OnConfirmClicked()
    {
        if (currentSelectedOption == null) return;

        confirmButton.interactable = false;
        StartCoroutine(CountdownRoutine());
    }

    // 3秒倒计时协程
    IEnumerator CountdownRoutine()
    {
        contentArea.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.5f);

        ApplyUpgrade(currentSelectedOption);
    }

    // 属性真实生效
    public void ApplyUpgrade(UpgradeOption data)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        PlayerController pc = playerObj.GetComponent<PlayerController>();
        AutoWeapon weapon = playerObj.GetComponent<AutoWeapon>();
        PlayerSkills skills = playerObj.GetComponent<PlayerSkills>();

        switch (data.type)
        {
            case UpgradeType.MoveSpeed:
                pc.moveSpeed += 1.0f;
                break;
            case UpgradeType.FireRate:
                weapon.manualFireRate += 1.5f;
                break;
            case UpgradeType.AoERadius:
                skills.aoeRadius += 0.8f;
                break;
            case UpgradeType.ChargeBonus:
                chargeBonusChance += 0.1f;
                break;
            case UpgradeType.Stun:
                stunChance += 0.15f;
                break;
            case UpgradeType.HpNerf:
                // 🔥 修改：去掉 < 2 的限制，让玩家可以一直叠“解构”
                hpNerfStacks++;
                // Debug.Log($"【解构】生效，当前全场敌人最大血量削减: {hpNerfStacks}");
                break;
            case UpgradeType.EasterEgg_EternalCharge:
                skills.canRechargeDuringSkill = true;
                hasEasterEgg = true; // 🔥 新增：拿过之后打上标记
                break;
        }

        upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
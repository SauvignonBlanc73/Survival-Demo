using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    [Header("UI 引用 (已更新为 TMP)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    //public Text titleText;
    //public Text descText;

    public Image buttonBackground;

    public Color normalColor = Color.white;
    public Color easterEggColor = Color.yellow;

    private UpgradeOption myData;
    private Vector3 originalScale; // 记录原始大小

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Setup(UpgradeOption data)
    {
        if (data == null) return;
        myData = data;

        // 设置文字内容
        if (titleText != null) titleText.text = data.title;
        if (descText != null) descText.text = data.description;

        // 根据类型设置背景色（可选逻辑保持不变）
        // 加一层 if 保护，只有当 buttonBackground 不为空时，才修改颜色
        if (buttonBackground != null)
        {
            if (data.type == UpgradeType.EasterEgg_EternalCharge)
            {
                buttonBackground.color = easterEggColor;
            }
            else
            {
                buttonBackground.color = normalColor;
            }
        }
    }

    // 玩家点击了卡片
    public void OnClick()
    {
        UpgradeManager.Instance.SelectOption(this, myData);
    }

    // 给 Manager 调用的缩放高亮方法
    public void Highlight(bool isSelected)
    {
        if (isSelected)
        {
            transform.localScale = originalScale * 1.1f; // 选中时放大 10%
        }
        else
        {
            transform.localScale = originalScale;        // 取消选中时恢复
        }
    }
}
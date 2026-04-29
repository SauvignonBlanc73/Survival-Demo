using UnityEngine;
using System.Collections; // 引入协程所需的命名空间

public class PlayerSkills : MonoBehaviour
{
    public int currentCharge = 0;
    public int maxCharge = 10;
    public float aoeRadius = 3f;
    public int aoeDamage = 3; // 基础伤害
    public float enhanceDuration = 15f; // 大招强化持续时间

    [Header("成长设置")]
    public float damageGrowthPerLevel = 0.15f; // 🔥 每升一级增加 15% 的伤害 (比怪物的 20% 小)

    // 🔥 新增：大招期间能否充能的彩蛋开关，默认是关闭的 (false)
    public bool canRechargeDuringSkill = false;

    public GameObject explosionVFXPrefab;
    public GameObject aimIndicatorPrefab;

    private GameObject currentAimIndicator;
    private bool isAiming = false;
    private AutoWeapon weaponController; // 关联武器控制器

    public GameObject playerGlow; // 在 Inspector 里把那个发光子物体拖进来

    void Awake()
    {
        // 获取挂在同一个物体上的 AutoWeapon 脚本
        weaponController = GetComponent<AutoWeapon>();
    }

    void OnEnable() { Bullet.OnHitEnemy += AddCharge; }
    void OnDisable() { Bullet.OnHitEnemy -= AddCharge; }

    void AddCharge()
    {
        // 1. 依然保留之前的“回路/彩蛋”逻辑门卫
        if (weaponController != null && weaponController.isEnhanced)
        {
            if (!canRechargeDuringSkill) return;
        }

        // --- 【聚能】核心逻辑开始 ---
        int amountToAdd = 1; // 默认加 1 点

        // 访问 UpgradeManager 里的静态概率变量
        // Random.value 会返回一个 0.0 到 1.0 之间的随机数
        if (Random.value < UpgradeManager.chargeBonusChance)
        {
            amountToAdd = 2; // 触发聚能，这次命中加 2 点！
            // Debug.Log("<color=yellow>【聚能触发】额外充能！</color>");
        }
        // --- 【聚能】核心逻辑结束 ---

        // 执行加点，注意不要超过最大值
        currentCharge = Mathf.Min(currentCharge + amountToAdd, maxCharge);

        // 更新 UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCharge(currentCharge, maxCharge);
        }
    }

    void Update()
    {
        // 🔥 将所有的 0 改为了 1，代表鼠标右键

        // 1. 按下右键，且充能就绪 -> 开始瞄准
        if (currentCharge >= maxCharge && Input.GetMouseButtonDown(1))
        {
            StartAiming();
        }

        // 2. 按住右键，且处于瞄准状态 -> 指示器跟随
        if (isAiming && Input.GetMouseButton(1))
        {
            UpdateAiming();
        }

        // 3. 松开右键，且处于瞄准状态 -> 释放大招
        if (isAiming && Input.GetMouseButtonUp(1))
        {
            ExecuteAoE();
        }

        // 4. 按下空格键取消施法（因为右键被占用了）
        if (isAiming && Input.GetKeyDown(KeyCode.Space))
        {
            CancelAiming();
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;
        return pos;
    }

    void StartAiming()
    {
        isAiming = true;
        Vector3 mousePos = GetMouseWorldPos();

        if (aimIndicatorPrefab != null)
        {
            currentAimIndicator = Instantiate(aimIndicatorPrefab, mousePos, Quaternion.identity);
            currentAimIndicator.transform.localScale = Vector3.one * (aoeRadius * 2f);
        }
    }

    void UpdateAiming()
    {
        if (currentAimIndicator != null)
        {
            currentAimIndicator.transform.position = GetMouseWorldPos();
        }
    }

    void CancelAiming()
    {
        isAiming = false;
        if (currentAimIndicator != null) Destroy(currentAimIndicator);
    }

    void ExecuteAoE()
    {
        Vector3 targetPos = currentAimIndicator.transform.position;
        float radiusSqr = aoeRadius * aoeRadius;

        // 🔥 1. 获取当前等级并计算动态伤害
        int level = 1;
        if (UIManager.Instance != null) level = UIManager.Instance.currentLevel;

        // 公式：基础伤害 * (1 + 等级加成)
        float growthFactor = 1f + (level - 1) * damageGrowthPerLevel;
        int finalDamage = Mathf.RoundToInt(aoeDamage * growthFactor);

        // 2. 爆炸视觉效果
        if (explosionVFXPrefab != null) Instantiate(explosionVFXPrefab, targetPos, Quaternion.identity);

        // 3. 范围伤害判定
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetPos, aoeRadius);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                float distSqr = (enemy.transform.position - targetPos).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    // 🔥 使用计算后的 finalDamage
                    enemy.GetComponent<Enemy>().TakeDamage(finalDamage);
                }
            }
        }

        isAiming = false;
        // Vector3 targetPos = GetMouseWorldPos();

        if (currentAimIndicator != null) Destroy(currentAimIndicator);

        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, targetPos, Quaternion.identity);
            vfx.GetComponent<ExplosionEffect>().targetScale = aoeRadius * 2f;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        // float radiusSqr = aoeRadius * aoeRadius;
        foreach (GameObject enemy in enemies)
        {
            if (enemy.activeInHierarchy)
            {
                float distanceSqr = (enemy.transform.position - targetPos).sqrMagnitude;
                if (distanceSqr <= radiusSqr)
                {
                    enemy.GetComponent<Enemy>().TakeDamage(aoeDamage);
                }
            }
        }

        currentCharge = 0;
        if (UIManager.Instance != null) UIManager.Instance.UpdateCharge(0, maxCharge);

        // 🔥 新增：触发 15 秒的强化状态协程！
        // 🔥 修改：用字符串的方式启动和停止协程
        if (weaponController != null)
        {
            StopCoroutine("EnhancedStateRoutine");
            StartCoroutine("EnhancedStateRoutine");
        }
    }

    // 用协程做倒计时是最优雅的
    IEnumerator EnhancedStateRoutine()
    {
        // 1. 开启强化状态
        weaponController.isEnhanced = true;
        if (playerGlow != null) playerGlow.SetActive(true); // 显示光效

        // 2. 通知 UI 进入计时模式
        if (UIManager.Instance != null) UIManager.Instance.StartTimerMode();

        float elapsed = 0f;
        while (elapsed < enhanceDuration)
        {
            elapsed += Time.deltaTime;

            // 计算剩余时间的百分比 (从 1 降到 0)
            float timePercent = 1f - (elapsed / enhanceDuration);

            // 每一帧更新 UI 充能条
            if (UIManager.Instance != null) UIManager.Instance.UpdateChargeManual(timePercent);

            yield return null; // 等待下一帧
        }

        // 3. 强化结束，清理状态
        weaponController.isEnhanced = false;
        if (playerGlow != null) playerGlow.SetActive(false); // 隐藏光效

        if (UIManager.Instance != null) UIManager.Instance.StopTimerMode();

        Debug.Log("强化结束，恢复普通状态。");
    }
}
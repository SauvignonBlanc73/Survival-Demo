using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public int maxHp = 3;
    public bool isElite; // 勾选区分普通和精英

    [Header("成长设置")]
    public float hpGrowthPerLevel = 0.2f; // 每升一级，敌人的血量基础值增加 20%

    private int currentHp;
    private Transform playerTransform; // 缓存玩家位置，避免每帧寻找
    private SpriteRenderer sr;
    private Color originalColor;

    private bool isStunned = false; // 🔥 新增：是否处于眩晕状态

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        // 1. 初始化时找一次玩家（注意：要求主角必须带 Player 标签）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void OnEnable()
    {
        // 1. 获取当前等级（从 UIManager 获取）
        int level = 1;
        if (UIManager.Instance != null) level = UIManager.Instance.currentLevel;

        // 2. 计算动态最大血量
        // 公式：$$基础血量 \times (1 + (当前等级 - 1) \times 成长系数)$$
        float growthFactor = 1f + (level - 1) * hpGrowthPerLevel;
        int dynamicMaxHp = Mathf.RoundToInt(maxHp * growthFactor);

        // 3. 应用原有的削弱逻辑 (hpNerfStacks)
        // 注意：Mathf.Max(1, ...) 确保敌人血量至少为 1，不会直接出生就暴毙
        int finalMaxHp = Mathf.Max(1, dynamicMaxHp - UpgradeManager.hpNerfStacks);

        currentHp = finalMaxHp;

        // 4. 重置状态
        if (sr != null) sr.color = originalColor;
        isStunned = false;
    }

    void Update()
    {
        // 如果处于眩晕状态，直接跳过逻辑，不移动也不旋转
        if (isStunned) return;

        // 2. 追踪逻辑
        if (playerTransform != null)
        {
            // 计算朝向玩家的方向向量
            Vector3 direction = (playerTransform.position - transform.position).normalized;

            // 移动
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            // 旋转：让三角形的“尖头”指向玩家 (2D 经典算法)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // 🔥 新增：被眩晕的方法
    public void Stun(float duration)
    {
        // 🔥 终极防弹衣：如果我已经被打死了（隐藏回对象池），或者血量空了，直接拒绝执行眩晕！
        if (!gameObject.activeInHierarchy || currentHp <= 0) return;

        if (isStunned) return; // 如果已经晕了，就不重复触发了
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        // 视觉反馈：变个颜色（比如蓝色）让玩家知道它动不了了
        Color oldColor = sr.color;
        sr.color = Color.cyan;

        yield return new WaitForSeconds(duration);

        // 恢复正常
        sr.color = oldColor;
        isStunned = false;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        StopAllCoroutines();
        StartCoroutine(FlashRed());
        if (currentHp <= 0) Die();
    }

    IEnumerator FlashRed()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = originalColor;
    }

    void Die()
    {
        // 回收至正确的池子
        EnemyPool.Instance.ReturnEnemy(gameObject, isElite);
        UIManager.Instance.AddScore(10);
    }

    // 在 Enemy.cs 中新增
    void OnTriggerEnter2D(Collider2D other)
    {
        // 如果撞到了玩家
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(10); // 每次撞击扣 10 点血

                // 💡 策划小技巧：怪物撞到玩家后是消失还是继续追？
                // 如果是割草游戏，通常怪物撞到玩家也会自杀（回收），或者弹开。
                // 这里我们选择让怪物撞到后直接“自杀”，产生一种被围攻消耗的感觉。
                Die();
            }
        }
    }
}